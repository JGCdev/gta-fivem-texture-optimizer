using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Xml;
using CodeWalker.GameFiles;

namespace YtdOptimizer
{
    class Program
    {
        static string texconvPath = "";
        static int targetSize = 512;
        static readonly string[] SupportedExtensions = { ".ytd", ".ydd", ".ydr", ".yft" };

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Texture Optimizer - Optimizes GTA V / FiveM texture files");
                Console.WriteLine("Supported formats: YTD, YDD, YDR, YFT");
                Console.WriteLine();
                Console.WriteLine("Usage: YtdOptimizer <input_folder> <output_folder> [max_size] [texconv_path]");
                Console.WriteLine("Example: YtdOptimizer ./input ./output 512 ./texconv.exe");
                return;
            }

            string inputFolder = args[0];
            string outputFolder = args[1];
            targetSize = args.Length > 2 ? int.Parse(args[2]) : 512;
            texconvPath = args.Length > 3 ? args[3] : "texconv.exe";

            if (!Directory.Exists(inputFolder))
            {
                Console.WriteLine($"Error: Input folder '{inputFolder}' not found");
                return;
            }

            // Convert to absolute path
            texconvPath = Path.GetFullPath(texconvPath);
            
            if (!File.Exists(texconvPath))
            {
                var possiblePaths = new[] { 
                    "texconv.exe", 
                    "tools/texconv.exe", 
                    "../texconv.exe",
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "texconv.exe")
                };
                
                foreach (var p in possiblePaths)
                {
                    if (File.Exists(p))
                    {
                        texconvPath = Path.GetFullPath(p);
                        break;
                    }
                }
            }

            if (!File.Exists(texconvPath))
            {
                Console.WriteLine($"Error: texconv.exe not found at '{texconvPath}'");
                return;
            }

            Console.WriteLine($"texconv path: {texconvPath}");

            Directory.CreateDirectory(outputFolder);

            // Find all supported files
            var files = new List<string>();
            foreach (var ext in SupportedExtensions)
            {
                files.AddRange(Directory.GetFiles(inputFolder, $"*{ext}"));
            }

            Console.WriteLine($"\n========================================");
            Console.WriteLine($"Texture Optimizer (CodeWalker)");
            Console.WriteLine($"========================================");
            Console.WriteLine($"Input: {inputFolder}");
            Console.WriteLine($"Output: {outputFolder}");
            Console.WriteLine($"Target size: {targetSize}x{targetSize}");
            Console.WriteLine($"Files: {files.Count} ({string.Join(", ", SupportedExtensions)})");
            Console.WriteLine($"========================================\n");

            int processed = 0, skipped = 0, errors = 0;
            long originalBytes = 0, optimizedBytes = 0;

            foreach (var filePath in files)
            {
                try
                {
                    var ext = Path.GetExtension(filePath).ToLowerInvariant();
                    (string status, string reason, long originalSize, long optimizedSize, int texturesChanged) result = ext switch
                    {
                        ".ytd" => ProcessYtd(filePath, outputFolder),
                        ".ydd" => ProcessYdd(filePath, outputFolder),
                        ".ydr" => ProcessYdr(filePath, outputFolder),
                        ".yft" => ProcessYft(filePath, outputFolder),
                        _ => ("skipped", "Unknown format", 0L, 0L, 0)
                    };

                    originalBytes += result.originalSize;
                    optimizedBytes += result.optimizedSize;

                    if (result.status == "optimized")
                    {
                        processed++;
                        double reduction = result.originalSize > 0 
                            ? (1.0 - (double)result.optimizedSize / result.originalSize) * 100 
                            : 0;
                        Console.WriteLine($"[OK] {Path.GetFileName(filePath)} - {result.texturesChanged} textures, {reduction:F1}% smaller");
                    }
                    else if (result.status == "skipped")
                    {
                        skipped++;
                        Console.WriteLine($"[SKIP] {Path.GetFileName(filePath)} - {result.reason}");
                    }
                    else
                    {
                        errors++;
                        Console.WriteLine($"[ERR] {Path.GetFileName(filePath)} - {result.reason}");
                    }
                }
                catch (Exception ex)
                {
                    errors++;
                    Console.WriteLine($"[ERR] {Path.GetFileName(filePath)} - {ex.Message}");
                }
            }

            Console.WriteLine($"\n========================================");
            Console.WriteLine($"Summary");
            Console.WriteLine($"========================================");
            Console.WriteLine($"Processed: {processed}");
            Console.WriteLine($"Skipped: {skipped}");
            Console.WriteLine($"Errors: {errors}");
            
            double origMB = originalBytes / (1024.0 * 1024.0);
            double optMB = optimizedBytes / (1024.0 * 1024.0);
            double totalReduction = originalBytes > 0 
                ? (1.0 - (double)optimizedBytes / originalBytes) * 100 
                : 0;
            Console.WriteLine($"\nTotal: {origMB:F2} MB -> {optMB:F2} MB ({totalReduction:F1}% reduction)");
        }

        static (string status, string reason, long originalSize, long optimizedSize, int texturesChanged) ProcessYtd(string inputPath, string outputFolder)
        {
            byte[] originalData = File.ReadAllBytes(inputPath);
            long originalSize = originalData.Length;

            var ytd = new YtdFile();
            try { ytd.Load(originalData); }
            catch (Exception ex)
            {
                File.Copy(inputPath, Path.Combine(outputFolder, Path.GetFileName(inputPath)), true);
                return ("error", $"Load failed: {ex.Message}", originalSize, originalSize, 0);
            }

            if (ytd.TextureDict?.Textures?.data_items == null || ytd.TextureDict.Textures.data_items.Length == 0)
            {
                File.Copy(inputPath, Path.Combine(outputFolder, Path.GetFileName(inputPath)), true);
                return ("skipped", "No textures", originalSize, originalSize, 0);
            }

            bool needsResize = ytd.TextureDict.Textures.data_items.Any(t => t != null && (t.Width > targetSize || t.Height > targetSize));
            if (!needsResize)
            {
                File.Copy(inputPath, Path.Combine(outputFolder, Path.GetFileName(inputPath)), true);
                return ("skipped", "Already optimized", originalSize, originalSize, 0);
            }

            string baseName = Path.GetFileNameWithoutExtension(inputPath);
            string tempFolder = Path.Combine(Path.GetTempPath(), $"opt_{baseName}_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempFolder);

            try
            {
                string xml = YtdXml.GetXml(ytd, tempFolder);
                string xmlPath = Path.Combine(tempFolder, $"{baseName}.xml");
                File.WriteAllText(xmlPath, xml);

                int changed = ResizeDdsFiles(tempFolder);

                string modifiedXml = File.ReadAllText(xmlPath);
                var newYtd = XmlYtd.GetYtd(modifiedXml, tempFolder);
                newYtd.Name = ytd.Name;

                byte[] newData = newYtd.Save();
                File.WriteAllBytes(Path.Combine(outputFolder, Path.GetFileName(inputPath)), newData);

                return ("optimized", "", originalSize, newData.Length, changed);
            }
            finally { try { Directory.Delete(tempFolder, true); } catch { } }
        }

        static (string status, string reason, long originalSize, long optimizedSize, int texturesChanged) ProcessYdd(string inputPath, string outputFolder)
        {
            byte[] originalData = File.ReadAllBytes(inputPath);
            long originalSize = originalData.Length;

            var ydd = new YddFile();
            try { ydd.Load(originalData); }
            catch (Exception ex)
            {
                File.Copy(inputPath, Path.Combine(outputFolder, Path.GetFileName(inputPath)), true);
                return ("error", $"Load failed: {ex.Message}", originalSize, originalSize, 0);
            }

            string baseName = Path.GetFileNameWithoutExtension(inputPath);
            string tempFolder = Path.Combine(Path.GetTempPath(), $"opt_{baseName}_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempFolder);

            try
            {
                string xml = YddXml.GetXml(ydd, tempFolder);
                string xmlPath = Path.Combine(tempFolder, $"{baseName}.xml");
                File.WriteAllText(xmlPath, xml);

                var ddsFiles = Directory.GetFiles(tempFolder, "*.dds");
                if (ddsFiles.Length == 0)
                {
                    File.Copy(inputPath, Path.Combine(outputFolder, Path.GetFileName(inputPath)), true);
                    return ("skipped", "No embedded textures", originalSize, originalSize, 0);
                }

                bool needsResize = ddsFiles.Any(f => NeedsResize(f));
                if (!needsResize)
                {
                    File.Copy(inputPath, Path.Combine(outputFolder, Path.GetFileName(inputPath)), true);
                    return ("skipped", "Already optimized", originalSize, originalSize, 0);
                }

                int changed = ResizeDdsFiles(tempFolder);

                string modifiedXml = File.ReadAllText(xmlPath);
                var newYdd = XmlYdd.GetYdd(modifiedXml, tempFolder);
                newYdd.Name = ydd.Name;

                byte[] newData = newYdd.Save();
                File.WriteAllBytes(Path.Combine(outputFolder, Path.GetFileName(inputPath)), newData);

                return ("optimized", "", originalSize, newData.Length, changed);
            }
            finally { try { Directory.Delete(tempFolder, true); } catch { } }
        }

        static (string status, string reason, long originalSize, long optimizedSize, int texturesChanged) ProcessYdr(string inputPath, string outputFolder)
        {
            byte[] originalData = File.ReadAllBytes(inputPath);
            long originalSize = originalData.Length;

            var ydr = new YdrFile();
            try { ydr.Load(originalData); }
            catch (Exception ex)
            {
                File.Copy(inputPath, Path.Combine(outputFolder, Path.GetFileName(inputPath)), true);
                return ("error", $"Load failed: {ex.Message}", originalSize, originalSize, 0);
            }

            string baseName = Path.GetFileNameWithoutExtension(inputPath);
            string tempFolder = Path.Combine(Path.GetTempPath(), $"opt_{baseName}_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempFolder);

            try
            {
                string xml = YdrXml.GetXml(ydr, tempFolder);
                string xmlPath = Path.Combine(tempFolder, $"{baseName}.xml");
                File.WriteAllText(xmlPath, xml);

                var ddsFiles = Directory.GetFiles(tempFolder, "*.dds");
                if (ddsFiles.Length == 0)
                {
                    File.Copy(inputPath, Path.Combine(outputFolder, Path.GetFileName(inputPath)), true);
                    return ("skipped", "No embedded textures", originalSize, originalSize, 0);
                }

                bool needsResize = ddsFiles.Any(f => NeedsResize(f));
                if (!needsResize)
                {
                    File.Copy(inputPath, Path.Combine(outputFolder, Path.GetFileName(inputPath)), true);
                    return ("skipped", "Already optimized", originalSize, originalSize, 0);
                }

                int changed = ResizeDdsFiles(tempFolder);

                string modifiedXml = File.ReadAllText(xmlPath);
                var newYdr = XmlYdr.GetYdr(modifiedXml, tempFolder);
                newYdr.Name = ydr.Name;

                byte[] newData = newYdr.Save();
                File.WriteAllBytes(Path.Combine(outputFolder, Path.GetFileName(inputPath)), newData);

                return ("optimized", "", originalSize, newData.Length, changed);
            }
            finally { try { Directory.Delete(tempFolder, true); } catch { } }
        }

        static (string status, string reason, long originalSize, long optimizedSize, int texturesChanged) ProcessYft(string inputPath, string outputFolder)
        {
            byte[] originalData = File.ReadAllBytes(inputPath);
            long originalSize = originalData.Length;

            var yft = new YftFile();
            try { yft.Load(originalData); }
            catch (Exception ex)
            {
                File.Copy(inputPath, Path.Combine(outputFolder, Path.GetFileName(inputPath)), true);
                return ("error", $"Load failed: {ex.Message}", originalSize, originalSize, 0);
            }

            string baseName = Path.GetFileNameWithoutExtension(inputPath);
            string tempFolder = Path.Combine(Path.GetTempPath(), $"opt_{baseName}_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempFolder);

            try
            {
                string xml = YftXml.GetXml(yft, tempFolder);
                string xmlPath = Path.Combine(tempFolder, $"{baseName}.xml");
                File.WriteAllText(xmlPath, xml);

                var ddsFiles = Directory.GetFiles(tempFolder, "*.dds");
                if (ddsFiles.Length == 0)
                {
                    File.Copy(inputPath, Path.Combine(outputFolder, Path.GetFileName(inputPath)), true);
                    return ("skipped", "No embedded textures", originalSize, originalSize, 0);
                }

                bool needsResize = ddsFiles.Any(f => NeedsResize(f));
                if (!needsResize)
                {
                    File.Copy(inputPath, Path.Combine(outputFolder, Path.GetFileName(inputPath)), true);
                    return ("skipped", "Already optimized", originalSize, originalSize, 0);
                }

                int changed = ResizeDdsFiles(tempFolder);

                string modifiedXml = File.ReadAllText(xmlPath);
                var newYft = XmlYft.GetYft(modifiedXml, tempFolder);
                newYft.Name = yft.Name;

                byte[] newData = newYft.Save();
                File.WriteAllBytes(Path.Combine(outputFolder, Path.GetFileName(inputPath)), newData);

                return ("optimized", "", originalSize, newData.Length, changed);
            }
            finally { try { Directory.Delete(tempFolder, true); } catch { } }
        }

        static bool NeedsResize(string ddsPath)
        {
            try
            {
                byte[] header = new byte[128];
                using (var fs = File.OpenRead(ddsPath))
                {
                    fs.Read(header, 0, 128);
                }
                int height = BitConverter.ToInt32(header, 12);
                int width = BitConverter.ToInt32(header, 16);
                return width > targetSize || height > targetSize;
            }
            catch { return false; }
        }

        static int ResizeDdsFiles(string folder)
        {
            int changed = 0;
            var ddsFiles = Directory.GetFiles(folder, "*.dds");

            foreach (var ddsPath in ddsFiles)
            {
                try
                {
                    byte[] header = new byte[128];
                    using (var fs = File.OpenRead(ddsPath))
                    {
                        fs.Read(header, 0, 128);
                    }

                    int height = BitConverter.ToInt32(header, 12);
                    int width = BitConverter.ToInt32(header, 16);

                    if (width > targetSize || height > targetSize)
                    {
                        double ratio = Math.Min((double)targetSize / width, (double)targetSize / height);
                        int newW = Math.Max(4, (int)(width * ratio));
                        int newH = Math.Max(4, (int)(height * ratio));
                        
                        newW = (int)Math.Pow(2, Math.Ceiling(Math.Log(newW) / Math.Log(2)));
                        newH = (int)Math.Pow(2, Math.Ceiling(Math.Log(newH) / Math.Log(2)));

                        uint fourcc = BitConverter.ToUInt32(header, 84);
                        string format = fourcc == 0x31545844 ? "BC1_UNORM" : "BC3_UNORM";

                        int mips = Math.Min(10, (int)(Math.Log(Math.Min(newW, newH)) / Math.Log(2)) + 1);

                        Console.WriteLine($"    Resizing: {Path.GetFileName(ddsPath)} {width}x{height} -> {newW}x{newH}");

                        var psi = new ProcessStartInfo
                        {
                            FileName = texconvPath,
                            Arguments = $"-w {newW} -h {newH} -m {mips} -f {format} -o \"{folder}\" -y \"{ddsPath}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };

                        using var proc = Process.Start(psi);
                        proc?.WaitForExit(60000);

                        if (proc?.ExitCode == 0)
                        {
                            changed++;
                        }
                        else
                        {
                            Console.WriteLine($"      texconv failed: exit code {proc?.ExitCode}");
                        }
                    }
                }
                catch (Exception ex) 
                { 
                    Console.WriteLine($"      Error: {ex.Message}");
                }
            }

            return changed;
        }
    }
}
