# FiveM Texture Optimizer

🎨 GUI tool to optimize GTA V / FiveM textures. Reduce file sizes by up to 90%.

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4) ![Windows](https://img.shields.io/badge/Windows-10%2F11-0078D6) ![License](https://img.shields.io/badge/License-MIT-green)

## Features

- ✅ Modern GUI with dark theme
- ✅ Individual file selection for optimization
- ✅ Texture preview with dimensions and format
- ✅ Configurable maximum size (128-2048px)
- ✅ Real-time process log
- ✅ Size reduction statistics

## Supported Formats

| Format | Description | Support |
|--------|-------------|---------|
| **YTD** | Texture Dictionary | ✅ Full |
| **YDD** | Drawable Dictionary | ✅ Embedded textures |
| **YDR** | Drawable | ✅ Embedded textures |
| **YFT** | Fragment | ✅ Embedded textures |

> **Note:** YFT files usually store textures in a separate `.ytd` file with the same name. If a YFT shows "No embedded textures", optimize the associated YTD instead.

## Requirements

- Windows 10/11 (64-bit)
- The executable includes everything needed (.NET 8 runtime bundled)

## Installation

### Option 1: Use the executable (Recommended)

1. Download the `dist/` folder
2. Run `FiveM Texture Optimizer.exe`
3. Done!

### Option 2: Build from source

See the [Development](#development) section.

## Usage

### Graphical Interface

1. **Select Input Folder** - Folder containing YTD/YDD/YDR/YFT files
2. **Select Output Folder** - Where optimized files will be saved
3. **Scan** - Analyzes and lists all files
4. **Select** - Check/uncheck individual files
5. **Configure size** - Choose maximum size (128-2048px)
6. **Optimize** - Processes the selected files

### Command Line (Optional)

```powershell
python optimize.py <input_folder> <output_folder> [max_size]
```

```powershell
# Example: textures max 512px
python optimize.py input output 512
```

## Recommended Sizes

| Size | Recommended use |
|------|-----------------|
| 128px | Minimum, low quality |
| 256px | Servers with many players |
| **512px** | **Quality/performance balance** |
| 1024px | High quality |
| 2048px | Maximum quality |

## Typical Results

| File | Original | Optimized | Reduction |
|------|----------|-----------|-----------|
| catamaran.ytd | 3.9 MB | 0.4 MB | 90% |
| interior_club.ytd | 5.2 MB | 0.8 MB | 85% |
| vehicle_hd.ytd | 2.1 MB | 0.5 MB | 76% |

## Limitations

- ❌ **FXA files** (compiled FiveM assets with `FXAP` magic) not supported - use the original uncompiled files instead
- ❌ **Corrupt files** - copied unmodified to the destination

## Development

### Build requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11

### Build the application

```powershell
# Navigate to the GUI project
cd tools/YtdOptimizerGUI

# Build and publish to dist/
dotnet publish -c Release -o ../../dist
```

### Project Structure

```
├── dist/                    # Distributable executables
│   ├── FiveM Texture Optimizer.exe  # GUI application
│   └── texconv.exe          # Texture converter
├── tools/
│   ├── texconv.exe          # Microsoft DirectXTex
│   ├── YtdOptimizerGUI/     # GUI source code (C# WinForms)
│   ├── YtdOptimizer/        # CLI tool (C#)
│   └── codewalker/          # CodeWalker.Core library
├── optimize.py              # Python wrapper script (optional)
└── README.md
```

### GUI Project Files

| File | Description |
|------|-------------|
| `YtdOptimizerGUI.csproj` | Project configuration |
| `Program.cs` | Entry point |
| `MainForm.cs` | Main form and logic |
| `Models.cs` | Data classes |

## Credits

- [CodeWalker](https://github.com/dexyfex/CodeWalker) - Library for GTA V formats
- [DirectXTex](https://github.com/microsoft/DirectXTex) - texconv.exe for DDS processing

## License

MIT
