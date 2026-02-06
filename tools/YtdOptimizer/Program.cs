using System;
using System.IO;
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

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("YTD Optimizer - Optimizes GTA V texture files");
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
                // Try relative paths
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

            var ytdFiles = Directory.GetFiles(inputFolder, "*.ytd");
            Console.WriteLine($"\n========================================");
            Console.WriteLine($"YTD Optimizer (CodeWalker)");
            Console.WriteLine($"========================================");
            Console.WriteLine($"Input: {inputFolder}");
            Console.WriteLine($"Output: {outputFolder}");
            Console.WriteLine($"Target size: {targetSize}x{targetSize}");
            Console.WriteLine($"Files: {ytdFiles.Length}");
            Console.WriteLine($"========================================\n");

            int processed = 0, skipped = 0, errors = 0;
            long originalBytes = 0, optimizedBytes = 0;

            foreach (var ytdPath in ytdFiles)
            {
                try
                {
                    var result = ProcessYtd(ytdPath, outputFolder);
                    originalBytes += result.originalSize;
                    optimizedBytes += result.optimizedSize;

                    if (result.status == "optimized")
                    {
                        processed++;
                        double reduction = result.originalSize > 0 
                            ? (1.0 - (double)result.optimizedSize / result.originalSize) * 100 
                            : 0;
                        Console.WriteLine($"[OK] {Path.GetFileName(ytdPath)} - {result.texturesChanged} textures, {reduction:F1}% smaller");
                    }
                    else if (result.status == "skipped")
                    {
                        skipped++;
                        Console.WriteLine($"[SKIP] {Path.GetFileName(ytdPath)} - {result.reason}");
                    }
                    else
                    {
                        errors++;
                        Console.WriteLine($"[ERR] {Path.GetFileName(ytdPath)} - {result.reason}");
                    }
                }
                catch (Exception ex)
                {
                    errors++;
                    Console.WriteLine($"[ERR] {Path.GetFileName(ytdPath)} - {ex.Message}");
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

            // Load YTD
            var ytd = new YtdFile();
            try
            {
                ytd.Load(originalData);
            }
            catch (Exception ex)
            {
                // Copy original if can't load
                File.Copy(inputPath, Path.Combine(outputFolder, Path.GetFileName(inputPath)), true);
                return ("error", $"Load failed: {ex.Message}", originalSize, originalSize, 0);
            }

            if (ytd.TextureDict?.Textures?.data_items == null || ytd.TextureDict.Textures.data_items.Length == 0)
            {
                File.Copy(inputPath, Path.Combine(outputFolder, Path.GetFileName(inputPath)), true);
                return ("skipped", "No textures", originalSize, originalSize, 0);
            }

            var textures = ytd.TextureDict.Textures.data_items;
            
            // Check if any texture needs resizing
            bool needsResize = false;
            foreach (var tex in textures)
            {
                if (tex != null && (tex.Width > targetSize || tex.Height > targetSize))
                {
                    needsResize = true;
                    break;
                }
            }

            if (!needsResize)
            {
                File.Copy(inputPath, Path.Combine(outputFolder, Path.GetFileName(inputPath)), true);
                return ("skipped", "Already optimized", originalSize, originalSize, 0);
            }

            // Create temp folder for DDS files
            string baseName = Path.GetFileNameWithoutExtension(inputPath);
            string tempFolder = Path.Combine(Path.GetTempPath(), $"ytd_opt_{baseName}_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempFolder);

            try
            {
                // Export to XML + DDS
                string xml = YtdXml.GetXml(ytd, tempFolder);
                string xmlPath = Path.Combine(tempFolder, $"{baseName}.xml");
                File.WriteAllText(xmlPath, xml);

                // Resize DDS files
                int changed = ResizeDdsFiles(tempFolder);

                // Import back from XML + DDS
                string modifiedXml = File.ReadAllText(xmlPath);
                var newYtd = XmlYtd.GetYtd(modifiedXml, tempFolder);
                newYtd.Name = ytd.Name;

                // Save
                byte[] newData = newYtd.Save();
                string outputPath = Path.Combine(outputFolder, Path.GetFileName(inputPath));
                File.WriteAllBytes(outputPath, newData);

                return ("optimized", "", originalSize, newData.Length, changed);
            }
            finally
            {
                // Cleanup temp
                try { Directory.Delete(tempFolder, true); } catch { }
            }
        }

        static int ResizeDdsFiles(string folder)
        {
            int changed = 0;
            var ddsFiles = Directory.GetFiles(folder, "*.dds");

            foreach (var ddsPath in ddsFiles)
            {
                try
                {
                    // Read DDS header to get dimensions
                    byte[] header = new byte[128];
                    using (var fs = File.OpenRead(ddsPath))
                    {
                        fs.Read(header, 0, 128);
                    }
                    // File is now closed

                    int height = BitConverter.ToInt32(header, 12);
                    int width = BitConverter.ToInt32(header, 16);

                    if (width > targetSize || height > targetSize)
                    {
                        // Calculate new size
                        double ratio = Math.Min((double)targetSize / width, (double)targetSize / height);
                        int newW = Math.Max(4, (int)(width * ratio));
                        int newH = Math.Max(4, (int)(height * ratio));
                        
                        // Round to power of 2
                        newW = (int)Math.Pow(2, Math.Ceiling(Math.Log(newW) / Math.Log(2)));
                        newH = (int)Math.Pow(2, Math.Ceiling(Math.Log(newH) / Math.Log(2)));

                        // Determine format
                        uint fourcc = BitConverter.ToUInt32(header, 84);
                        string format = fourcc == 0x31545844 ? "BC1_UNORM" : "BC3_UNORM";

                        int mips = Math.Min(10, (int)(Math.Log(Math.Min(newW, newH)) / Math.Log(2)) + 1);

                        Console.WriteLine($"    Resizing: {Path.GetFileName(ddsPath)} {width}x{height} -> {newW}x{newH}");

                        // Run texconv
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
