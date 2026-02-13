using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CodeWalker.GameFiles;

namespace YtdOptimizerGUI
{
    public partial class MainForm : Form
    {
        // Colors for dark theme
        private readonly Color BackgroundColor = Color.FromArgb(30, 30, 30);
        private readonly Color PanelColor = Color.FromArgb(45, 45, 48);
        private readonly Color ButtonColor = Color.FromArgb(62, 62, 66);
        private readonly Color ButtonHoverColor = Color.FromArgb(82, 82, 86);
        private readonly Color AccentColor = Color.FromArgb(0, 122, 204);
        private readonly Color TextColor = Color.FromArgb(241, 241, 241);
        private readonly Color SecondaryTextColor = Color.FromArgb(153, 153, 153);
        private readonly Color GridLineColor = Color.FromArgb(62, 62, 66);
        private readonly Color SuccessColor = Color.FromArgb(78, 201, 176);
        private readonly Color WarningColor = Color.FromArgb(255, 198, 109);
        private readonly Color ErrorColor = Color.FromArgb(244, 135, 113);

        // Controls
        private Panel topPanel = null!;
        private Button btnSelectFolder = null!;
        private Button btnScan = null!;
        private Button btnSelectOutput = null!;
        private Label lblInputPath = null!;
        private Label lblOutputPath = null!;
        private ComboBox cmbTargetSize = null!;
        private Label lblTargetSize = null!;
        private CheckBox chkSelectAll = null!;
        private DataGridView gridFiles = null!;
        private Panel bottomPanel = null!;
        private Button btnOptimize = null!;
        private ProgressBar progressBar = null!;
        private Label lblStatus = null!;
        private Label lblStats = null!;
        private RichTextBox txtLog = null!;
        private SplitContainer splitContainer = null!;

        // Data
        private List<FileItem> files = new();
        private string inputFolder = "";
        private string outputFolder = "";
        private int targetSize = 512;
        private string texconvPath = "";
        private bool isProcessing = false;

        private static readonly string[] SupportedExtensions = { ".ytd", ".ydd", ".ydr", ".yft" };

        public MainForm()
        {
            InitializeComponent();
            SetupForm();
            FindTexconv();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1100, 700);
            this.MinimumSize = new Size(900, 600);
            this.Name = "MainForm";
            this.Text = "FiveM Texture Optimizer";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ResumeLayout(false);
        }

        private void SetupForm()
        {
            this.BackColor = BackgroundColor;
            this.ForeColor = TextColor;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            // Header Panel with Logo
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(25, 25, 28)
            };

            // Logo/Title
            var logoLabel = new Label
            {
                Text = "🎨",
                Font = new Font("Segoe UI Emoji", 20F),
                Location = new Point(15, 5),
                Size = new Size(45, 40),
                ForeColor = AccentColor
            };

            var titleLabel = new Label
            {
                Text = "FiveM Texture Optimizer",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                Location = new Point(55, 10),
                Size = new Size(280, 30),
                ForeColor = TextColor
            };

            var versionLabel = new Label
            {
                Text = "v1.0",
                Font = new Font("Segoe UI", 9F),
                Location = new Point(335, 18),
                Size = new Size(40, 20),
                ForeColor = SecondaryTextColor
            };

            headerPanel.Controls.AddRange(new Control[] { logoLabel, titleLabel, versionLabel });

            // Top Panel (Controls)
            topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = PanelColor,
                Padding = new Padding(15)
            };

            // Row 1: Buttons
            btnSelectFolder = CreateButton("📂 Carpeta Entrada", 15, 15, 160);
            btnSelectFolder.Click += BtnSelectFolder_Click;

            btnSelectOutput = CreateButton("📁 Carpeta Salida", 185, 15, 160);
            btnSelectOutput.Click += BtnSelectOutput_Click;

            btnScan = CreateButton("🔍 Escanear", 355, 15, 120);
            btnScan.Click += BtnScan_Click;
            btnScan.Enabled = false;

            // Row 1 Right side: Target size and select all
            lblTargetSize = new Label
            {
                Text = "Tamaño máx:",
                Location = new Point(520, 18),
                Size = new Size(85, 20),
                ForeColor = TextColor
            };

            cmbTargetSize = new ComboBox
            {
                Location = new Point(610, 15),
                Size = new Size(70, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = ButtonColor,
                ForeColor = TextColor,
                FlatStyle = FlatStyle.Flat
            };
            cmbTargetSize.Items.AddRange(new object[] { "128", "256", "512", "1024", "2048" });
            cmbTargetSize.SelectedItem = "512";
            cmbTargetSize.SelectedIndexChanged += CmbTargetSize_SelectedIndexChanged;

            chkSelectAll = new CheckBox
            {
                Text = "Seleccionar todo",
                Location = new Point(700, 17),
                Size = new Size(130, 22),
                ForeColor = TextColor,
                Checked = true
            };
            chkSelectAll.CheckedChanged += ChkSelectAll_CheckedChanged;

            // Row 2: Path displays
            var lblInput = new Label
            {
                Text = "Entrada:",
                Location = new Point(15, 55),
                Size = new Size(60, 20),
                ForeColor = SecondaryTextColor
            };

            lblInputPath = new Label
            {
                Text = "No seleccionada",
                Location = new Point(80, 55),
                Size = new Size(400, 20),
                ForeColor = SecondaryTextColor
            };

            var lblOutput = new Label
            {
                Text = "Salida:",
                Location = new Point(500, 55),
                Size = new Size(50, 20),
                ForeColor = SecondaryTextColor
            };

            lblOutputPath = new Label
            {
                Text = "No seleccionada",
                Location = new Point(555, 55),
                Size = new Size(400, 20),
                ForeColor = SecondaryTextColor
            };

            topPanel.Controls.AddRange(new Control[] {
                btnSelectFolder, btnSelectOutput, btnScan,
                lblTargetSize, cmbTargetSize, chkSelectAll,
                lblInput, lblInputPath, lblOutput, lblOutputPath
            });

            // Split container for grid and log
            splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 400,
                BackColor = BackgroundColor,
                Panel1MinSize = 200,
                Panel2MinSize = 100
            };

            // DataGridView for files
            gridFiles = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = BackgroundColor,
                GridColor = GridLineColor,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = false
            };

            gridFiles.DefaultCellStyle.BackColor = BackgroundColor;
            gridFiles.DefaultCellStyle.ForeColor = TextColor;
            gridFiles.DefaultCellStyle.SelectionBackColor = AccentColor;
            gridFiles.DefaultCellStyle.SelectionForeColor = TextColor;
            gridFiles.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(35, 35, 35);
            gridFiles.ColumnHeadersDefaultCellStyle.BackColor = PanelColor;
            gridFiles.ColumnHeadersDefaultCellStyle.ForeColor = TextColor;
            gridFiles.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            gridFiles.ColumnHeadersHeight = 35;
            gridFiles.RowTemplate.Height = 28;

            SetupGridColumns();

            // Log panel
            var logPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PanelColor,
                Padding = new Padding(10)
            };

            var lblLog = CreateLabel("Log de operaciones:", 0, 0);
            lblLog.Dock = DockStyle.Top;

            txtLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = BackgroundColor,
                ForeColor = TextColor,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Font = new Font("Consolas", 9F)
            };

            logPanel.Controls.Add(txtLog);
            logPanel.Controls.Add(lblLog);

            splitContainer.Panel1.Controls.Add(gridFiles);
            splitContainer.Panel2.Controls.Add(logPanel);

            // Bottom Panel
            bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = PanelColor,
                Padding = new Padding(15)
            };

            progressBar = new ProgressBar
            {
                Location = new Point(15, 15),
                Size = new Size(700, 25),
                Style = ProgressBarStyle.Continuous
            };

            lblStatus = CreateLabel("Listo", 15, 45);
            lblStats = CreateLabel("", 300, 45, 400);

            btnOptimize = CreateButton("⚡ Optimizar Seleccionados", 730, 15, 200);
            btnOptimize.Height = 45;
            btnOptimize.BackColor = AccentColor;
            btnOptimize.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnOptimize.Click += BtnOptimize_Click;
            btnOptimize.Enabled = false;

            bottomPanel.Controls.AddRange(new Control[] {
                progressBar, lblStatus, lblStats, btnOptimize
            });

            // Add all to form (order matters for docking!)
            // For Dock.Top panels, add in reverse visual order (bottom to top)
            this.Controls.Add(splitContainer);  // Fill - goes in remaining space
            this.Controls.Add(bottomPanel);     // Bottom
            this.Controls.Add(topPanel);        // Top - below header
            this.Controls.Add(headerPanel);     // Top - at very top

            // Adjust positions on resize
            this.Resize += MainForm_Resize;
        }

        private void SetupGridColumns()
        {
            gridFiles.Columns.Clear();

            var chkCol = new DataGridViewCheckBoxColumn
            {
                Name = "Selected",
                HeaderText = "✓",
                Width = 40,
                FillWeight = 10
            };

            var fileCol = new DataGridViewTextBoxColumn
            {
                Name = "File",
                HeaderText = "Archivo",
                FillWeight = 40
            };

            var textureCol = new DataGridViewTextBoxColumn
            {
                Name = "Texture",
                HeaderText = "Textura",
                FillWeight = 30
            };

            var typeCol = new DataGridViewTextBoxColumn
            {
                Name = "Type",
                HeaderText = "Tipo",
                Width = 60,
                FillWeight = 10
            };

            var dimCol = new DataGridViewTextBoxColumn
            {
                Name = "Dimensions",
                HeaderText = "Dimensiones",
                Width = 100,
                FillWeight = 15
            };

            var formatCol = new DataGridViewTextBoxColumn
            {
                Name = "Format",
                HeaderText = "Formato",
                Width = 80,
                FillWeight = 12
            };

            var sizeCol = new DataGridViewTextBoxColumn
            {
                Name = "Size",
                HeaderText = "Tamaño (MB)",
                Width = 90,
                FillWeight = 12
            };

            var statusCol = new DataGridViewTextBoxColumn
            {
                Name = "Status",
                HeaderText = "Estado",
                Width = 100,
                FillWeight = 15
            };

            gridFiles.Columns.AddRange(new DataGridViewColumn[] {
                chkCol, fileCol, textureCol, typeCol, dimCol, formatCol, sizeCol, statusCol
            });

            gridFiles.CellValueChanged += GridFiles_CellValueChanged;
            gridFiles.CellContentClick += GridFiles_CellContentClick;
        }

        private void MainForm_Resize(object? sender, EventArgs e)
        {
            progressBar.Width = this.ClientSize.Width - btnOptimize.Width - 60;
            btnOptimize.Left = progressBar.Right + 15;
        }

        private Button CreateButton(string text, int x, int y, int width)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = ButtonColor,
                ForeColor = TextColor,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = GridLineColor;
            btn.FlatAppearance.MouseOverBackColor = ButtonHoverColor;
            return btn;
        }

        private Label CreateLabel(string text, int x, int y, int width = 120)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 20),
                ForeColor = TextColor,
                AutoSize = false
            };
        }

        private void FindTexconv()
        {
            var possiblePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "texconv.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "texconv.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "texconv.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "texconv.exe"),
                "texconv.exe"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    texconvPath = Path.GetFullPath(path);
                    Log($"texconv encontrado: {texconvPath}", SuccessColor);
                    return;
                }
            }

            Log("ADVERTENCIA: texconv.exe no encontrado. Asegúrese de colocarlo junto al ejecutable.", WarningColor);
        }

        private void BtnSelectFolder_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Seleccione la carpeta con archivos YTD/YDD/YDR/YFT"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                inputFolder = dialog.SelectedPath;
                lblInputPath.Text = inputFolder;
                lblInputPath.ForeColor = TextColor;
                btnScan.Enabled = true;

                if (string.IsNullOrEmpty(outputFolder))
                {
                    outputFolder = Path.Combine(inputFolder, "optimized");
                    lblOutputPath.Text = outputFolder;
                    lblOutputPath.ForeColor = TextColor;
                }

                Log($"Carpeta seleccionada: {inputFolder}");
            }
        }

        private void BtnSelectOutput_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Seleccione la carpeta de salida"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                outputFolder = dialog.SelectedPath;
                lblOutputPath.Text = outputFolder;
                lblOutputPath.ForeColor = TextColor;
                Log($"Carpeta de salida: {outputFolder}");
            }
        }

        private void CmbTargetSize_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (int.TryParse(cmbTargetSize.SelectedItem?.ToString(), out int size))
            {
                targetSize = size;
                Log($"Tamaño objetivo: {targetSize}x{targetSize}");
                UpdateNeedsOptimization();
            }
        }

        private void ChkSelectAll_CheckedChanged(object? sender, EventArgs e)
        {
            foreach (DataGridViewRow row in gridFiles.Rows)
            {
                row.Cells["Selected"].Value = chkSelectAll.Checked;
            }
            UpdateStats();
        }

        private void GridFiles_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0 && e.RowIndex >= 0)
            {
                gridFiles.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void GridFiles_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0 && e.RowIndex >= 0)
            {
                UpdateStats();
            }
        }

        private async void BtnScan_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(inputFolder) || !Directory.Exists(inputFolder))
            {
                MessageBox.Show("Por favor seleccione una carpeta válida.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnScan.Enabled = false;
            btnOptimize.Enabled = false;
            gridFiles.Rows.Clear();
            files.Clear();

            lblStatus.Text = "Escaneando archivos...";
            Log("Iniciando escaneo de archivos...");

            await Task.Run(() => ScanFiles());

            PopulateGrid();
            UpdateStats();

            btnScan.Enabled = true;
            btnOptimize.Enabled = files.Count > 0;
            lblStatus.Text = $"Escaneo completado: {files.Count} archivos encontrados";
            Log($"Escaneo completado: {files.Count} archivos con {files.Sum(f => f.Textures.Count)} texturas", SuccessColor);
        }

        private void ScanFiles()
        {
            var allFiles = new List<string>();
            foreach (var ext in SupportedExtensions)
            {
                allFiles.AddRange(Directory.GetFiles(inputFolder, $"*{ext}", SearchOption.AllDirectories));
            }

            foreach (var filePath in allFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    var ext = Path.GetExtension(filePath).ToLowerInvariant();
                    var fileItem = new FileItem
                    {
                        FileName = Path.GetFileName(filePath),
                        FilePath = filePath,
                        FileType = ext.ToUpper().TrimStart('.'),
                        SizeBytes = fileInfo.Length
                    };

                    var textures = GetTexturesFromFile(filePath, ext);
                    fileItem.Textures = textures;
                    fileItem.TextureCount = textures.Count;

                    files.Add(fileItem);
                }
                catch (Exception ex)
                {
                    this.Invoke(() => Log($"Error leyendo {Path.GetFileName(filePath)}: {ex.Message}", ErrorColor));
                }
            }
        }

        private List<TextureItem> GetTexturesFromFile(string filePath, string ext)
        {
            var textures = new List<TextureItem>();
            byte[] data = File.ReadAllBytes(filePath);

            try
            {
                switch (ext)
                {
                    case ".ytd":
                        var ytd = new YtdFile();
                        ytd.Load(data);
                        if (ytd.TextureDict?.Textures?.data_items != null)
                        {
                            foreach (var tex in ytd.TextureDict.Textures.data_items)
                            {
                                if (tex != null)
                                {
                                    textures.Add(new TextureItem
                                    {
                                        FileName = Path.GetFileName(filePath),
                                        TextureName = tex.Name ?? "unknown",
                                        FileType = "YTD",
                                        Width = tex.Width,
                                        Height = tex.Height,
                                        Format = tex.Format.ToString(),
                                        MipLevels = tex.Levels,
                                        FilePath = filePath,
                                        NeedsOptimization = tex.Width > targetSize || tex.Height > targetSize
                                    });
                                }
                            }
                        }
                        break;

                    case ".ydd":
                        var ydd = new YddFile();
                        ydd.Load(data);
                        // YDD files have embedded textures in drawables
                        textures.Add(new TextureItem
                        {
                            FileName = Path.GetFileName(filePath),
                            TextureName = "(embebidas)",
                            FileType = "YDD",
                            FilePath = filePath,
                            NeedsOptimization = true
                        });
                        break;

                    case ".ydr":
                        var ydr = new YdrFile();
                        ydr.Load(data);
                        textures.Add(new TextureItem
                        {
                            FileName = Path.GetFileName(filePath),
                            TextureName = "(embebidas)",
                            FileType = "YDR",
                            FilePath = filePath,
                            NeedsOptimization = true
                        });
                        break;

                    case ".yft":
                        var yft = new YftFile();
                        yft.Load(data);
                        textures.Add(new TextureItem
                        {
                            FileName = Path.GetFileName(filePath),
                            TextureName = "(embebidas)",
                            FileType = "YFT",
                            FilePath = filePath,
                            NeedsOptimization = true
                        });
                        break;
                }
            }
            catch (Exception ex)
            {
                this.Invoke(() => Log($"Error procesando {Path.GetFileName(filePath)}: {ex.Message}", WarningColor));
            }

            return textures;
        }

        private void PopulateGrid()
        {
            gridFiles.Rows.Clear();

            foreach (var file in files)
            {
                foreach (var tex in file.Textures)
                {
                    var rowIndex = gridFiles.Rows.Add(
                        true,
                        tex.FileName,
                        tex.TextureName,
                        tex.FileType,
                        tex.Width > 0 ? tex.Dimensions : "-",
                        tex.Format ?? "-",
                        file.SizeMB,
                        tex.NeedsOptimization ? "Pendiente" : "Optimizado"
                    );

                    var row = gridFiles.Rows[rowIndex];
                    row.Tag = tex;

                    if (!tex.NeedsOptimization)
                    {
                        row.Cells["Status"].Style.ForeColor = SuccessColor;
                    }
                    else
                    {
                        row.Cells["Status"].Style.ForeColor = WarningColor;
                    }
                }
            }
        }

        private void UpdateNeedsOptimization()
        {
            foreach (DataGridViewRow row in gridFiles.Rows)
            {
                if (row.Tag is TextureItem tex)
                {
                    tex.NeedsOptimization = tex.Width > targetSize || tex.Height > targetSize;
                    row.Cells["Status"].Value = tex.NeedsOptimization ? "Pendiente" : "Optimizado";
                    row.Cells["Status"].Style.ForeColor = tex.NeedsOptimization ? WarningColor : SuccessColor;
                }
            }
        }

        private void UpdateStats()
        {
            int selected = 0;
            int total = gridFiles.Rows.Count;
            long totalSize = 0;

            foreach (DataGridViewRow row in gridFiles.Rows)
            {
                if (row.Cells["Selected"].Value is true)
                {
                    selected++;
                    if (row.Tag is TextureItem tex)
                    {
                        var file = files.FirstOrDefault(f => f.FilePath == tex.FilePath);
                        if (file != null)
                        {
                            totalSize += file.SizeBytes;
                        }
                    }
                }
            }

            lblStats.Text = $"Seleccionados: {selected}/{total} | Tamaño: {totalSize / (1024.0 * 1024.0):F2} MB";
        }

        private async void BtnOptimize_Click(object? sender, EventArgs e)
        {
            if (isProcessing) return;

            if (string.IsNullOrEmpty(texconvPath) || !File.Exists(texconvPath))
            {
                MessageBox.Show("texconv.exe no encontrado. Colóquelo junto al ejecutable.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var selectedFiles = GetSelectedFiles();
            if (selectedFiles.Count == 0)
            {
                MessageBox.Show("No hay archivos seleccionados para optimizar.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            isProcessing = true;
            btnOptimize.Enabled = false;
            btnScan.Enabled = false;
            btnSelectFolder.Enabled = false;

            Directory.CreateDirectory(outputFolder);

            progressBar.Minimum = 0;
            progressBar.Maximum = selectedFiles.Count;
            progressBar.Value = 0;

            Log($"\n========================================");
            Log($"Iniciando optimización de {selectedFiles.Count} archivos");
            Log($"Salida: {outputFolder}");
            Log($"Tamaño objetivo: {targetSize}x{targetSize}");
            Log($"========================================\n");

            int processed = 0, skipped = 0, errors = 0;
            long originalBytes = 0, optimizedBytes = 0;

            foreach (var filePath in selectedFiles)
            {
                var fileName = Path.GetFileName(filePath);
                lblStatus.Text = $"Procesando: {fileName}";

                try
                {
                    var result = await Task.Run(() => ProcessFile(filePath));

                    originalBytes += result.originalSize;
                    optimizedBytes += result.optimizedSize;

                    if (result.status == "optimized")
                    {
                        processed++;
                        double reduction = result.originalSize > 0
                            ? (1.0 - (double)result.optimizedSize / result.originalSize) * 100
                            : 0;
                        Log($"[OK] {fileName} - {result.texturesChanged} texturas, {reduction:F1}% reducción", SuccessColor);
                        UpdateRowStatus(filePath, "Completado", SuccessColor);
                    }
                    else if (result.status == "skipped")
                    {
                        skipped++;
                        Log($"[SKIP] {fileName} - {result.reason}", WarningColor);
                        UpdateRowStatus(filePath, "Omitido", WarningColor);
                    }
                    else
                    {
                        errors++;
                        Log($"[ERR] {fileName} - {result.reason}", ErrorColor);
                        UpdateRowStatus(filePath, "Error", ErrorColor);
                    }
                }
                catch (Exception ex)
                {
                    errors++;
                    Log($"[ERR] {fileName} - {ex.Message}", ErrorColor);
                    UpdateRowStatus(filePath, "Error", ErrorColor);
                }

                progressBar.Value++;
            }

            // Summary
            double origMB = originalBytes / (1024.0 * 1024.0);
            double optMB = optimizedBytes / (1024.0 * 1024.0);
            double totalReduction = originalBytes > 0
                ? (1.0 - (double)optimizedBytes / originalBytes) * 100
                : 0;

            Log($"\n========================================");
            Log($"RESUMEN");
            Log($"========================================");
            Log($"Procesados: {processed}");
            Log($"Omitidos: {skipped}");
            Log($"Errores: {errors}");
            Log($"Total: {origMB:F2} MB → {optMB:F2} MB ({totalReduction:F1}% reducción)", SuccessColor);
            Log($"========================================\n");

            lblStatus.Text = $"Completado: {processed} procesados, {skipped} omitidos, {errors} errores";
            lblStats.Text = $"Reducción total: {origMB:F2} MB → {optMB:F2} MB ({totalReduction:F1}%)";

            isProcessing = false;
            btnOptimize.Enabled = true;
            btnScan.Enabled = true;
            btnSelectFolder.Enabled = true;

            MessageBox.Show(
                $"Optimización completada!\n\n" +
                $"Procesados: {processed}\n" +
                $"Omitidos: {skipped}\n" +
                $"Errores: {errors}\n\n" +
                $"Reducción: {origMB:F2} MB → {optMB:F2} MB ({totalReduction:F1}%)",
                "Completado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private List<string> GetSelectedFiles()
        {
            var selected = new HashSet<string>();

            foreach (DataGridViewRow row in gridFiles.Rows)
            {
                if (row.Cells["Selected"].Value is true && row.Tag is TextureItem tex)
                {
                    selected.Add(tex.FilePath);
                }
            }

            return selected.ToList();
        }

        private void UpdateRowStatus(string filePath, string status, Color color)
        {
            this.Invoke(() =>
            {
                foreach (DataGridViewRow row in gridFiles.Rows)
                {
                    if (row.Tag is TextureItem tex && tex.FilePath == filePath)
                    {
                        row.Cells["Status"].Value = status;
                        row.Cells["Status"].Style.ForeColor = color;
                    }
                }
            });
        }

        private (string status, string reason, long originalSize, long optimizedSize, int texturesChanged) ProcessFile(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();

            return ext switch
            {
                ".ytd" => ProcessYtd(filePath),
                ".ydd" => ProcessYdd(filePath),
                ".ydr" => ProcessYdr(filePath),
                ".yft" => ProcessYft(filePath),
                _ => ("skipped", "Formato desconocido", 0L, 0L, 0)
            };
        }

        private (string status, string reason, long originalSize, long optimizedSize, int texturesChanged) ProcessYtd(string inputPath)
        {
            byte[] originalData = File.ReadAllBytes(inputPath);
            long originalSize = originalData.Length;

            var ytd = new YtdFile();
            try { ytd.Load(originalData); }
            catch (Exception ex)
            {
                File.Copy(inputPath, Path.Combine(outputFolder, Path.GetFileName(inputPath)), true);
                return ("error", $"Error al cargar: {ex.Message}", originalSize, originalSize, 0);
            }

            if (ytd.TextureDict?.Textures?.data_items == null || ytd.TextureDict.Textures.data_items.Length == 0)
            {
                File.Copy(inputPath, Path.Combine(outputFolder, Path.GetFileName(inputPath)), true);
                return ("skipped", "Sin texturas", originalSize, originalSize, 0);
            }

            bool needsResize = ytd.TextureDict.Textures.data_items.Any(t => t != null && (t.Width > targetSize || t.Height > targetSize));
            if (!needsResize)
            {
                File.Copy(inputPath, Path.Combine(outputFolder, Path.GetFileName(inputPath)), true);
                return ("skipped", "Ya optimizado", originalSize, originalSize, 0);
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

        private (string status, string reason, long originalSize, long optimizedSize, int texturesChanged) ProcessYdd(string inputPath)
        {
            byte[] originalData = File.ReadAllBytes(inputPath);
            long originalSize = originalData.Length;

            var ydd = new YddFile();
            try { ydd.Load(originalData); }
            catch (Exception ex)
            {
                File.Copy(inputPath, Path.Combine(outputFolder, Path.GetFileName(inputPath)), true);
                return ("error", $"Error al cargar: {ex.Message}", originalSize, originalSize, 0);
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
                    return ("skipped", "Sin texturas embebidas", originalSize, originalSize, 0);
                }

                bool needsResize = ddsFiles.Any(f => NeedsResize(f));
                if (!needsResize)
                {
                    File.Copy(inputPath, Path.Combine(outputFolder, Path.GetFileName(inputPath)), true);
                    return ("skipped", "Ya optimizado", originalSize, originalSize, 0);
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

        private (string status, string reason, long originalSize, long optimizedSize, int texturesChanged) ProcessYdr(string inputPath)
        {
            byte[] originalData = File.ReadAllBytes(inputPath);
            long originalSize = originalData.Length;

            var ydr = new YdrFile();
            try { ydr.Load(originalData); }
            catch (Exception ex)
            {
                File.Copy(inputPath, Path.Combine(outputFolder, Path.GetFileName(inputPath)), true);
                return ("error", $"Error al cargar: {ex.Message}", originalSize, originalSize, 0);
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
                    return ("skipped", "Sin texturas embebidas", originalSize, originalSize, 0);
                }

                bool needsResize = ddsFiles.Any(f => NeedsResize(f));
                if (!needsResize)
                {
                    File.Copy(inputPath, Path.Combine(outputFolder, Path.GetFileName(inputPath)), true);
                    return ("skipped", "Ya optimizado", originalSize, originalSize, 0);
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

        private (string status, string reason, long originalSize, long optimizedSize, int texturesChanged) ProcessYft(string inputPath)
        {
            byte[] originalData = File.ReadAllBytes(inputPath);
            long originalSize = originalData.Length;

            var yft = new YftFile();
            try { yft.Load(originalData); }
            catch (Exception ex)
            {
                File.Copy(inputPath, Path.Combine(outputFolder, Path.GetFileName(inputPath)), true);
                return ("error", $"Error al cargar: {ex.Message}", originalSize, originalSize, 0);
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
                    return ("skipped", "Sin texturas embebidas", originalSize, originalSize, 0);
                }

                bool needsResize = ddsFiles.Any(f => NeedsResize(f));
                if (!needsResize)
                {
                    File.Copy(inputPath, Path.Combine(outputFolder, Path.GetFileName(inputPath)), true);
                    return ("skipped", "Ya optimizado", originalSize, originalSize, 0);
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

        private bool NeedsResize(string ddsPath)
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

        private int ResizeDdsFiles(string folder)
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

                        this.Invoke(() => Log($"    Redimensionando: {Path.GetFileName(ddsPath)} {width}x{height} → {newW}x{newH}"));

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
                            this.Invoke(() => Log($"      texconv falló: código {proc?.ExitCode}", ErrorColor));
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.Invoke(() => Log($"      Error: {ex.Message}", ErrorColor));
                }
            }

            return changed;
        }

        private void Log(string message, Color? color = null)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(() => Log(message, color));
                return;
            }

            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionLength = 0;
            txtLog.SelectionColor = color ?? TextColor;
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            txtLog.ScrollToCaret();
        }
    }
}
