# GTA V / FiveM Texture Optimizer

Automatically reduces texture sizes in GTA V asset files to decrease memory usage. Achieves up to 90% file size reduction.

## Supported Formats

| Format | Description | Contains |
|--------|-------------|----------|
| **YTD** | Texture Dictionary | Textures only |
| **YDD** | Drawable Dictionary | Models + embedded textures |
| **YDR** | Drawable | Single model + embedded textures |
| **YFT** | Fragment | Vehicles/destructible objects + textures |

> **Note:** YFT files typically store textures in a separate `.ytd` file with the same name. If a YFT shows "No embedded textures", optimize the associated YTD instead.

## Requirements

- Windows 10/11
- .NET 7.0+ Runtime ([Download](https://dotnet.microsoft.com/download))
- Python 3.8+ (optional, for the wrapper script)

## Installation

1. Clone or download this repository
2. Ensure .NET 7.0+ is installed
3. Ready to use!

## Usage

```powershell
python optimize.py <input_folder> <output_folder> [max_texture_size]
```

### Examples

```powershell
# Recommended: 512px max texture size (good balance)
python optimize.py input output 512

# Smaller files, lower quality
python optimize.py input output 256

# Higher quality, larger files
python optimize.py input output 1024
```

### Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| input_folder | Folder containing original .ytd/.ydd/.ydr/.yft files | required |
| output_folder | Folder where optimized files will be saved | required |
| max_texture_size | Maximum texture dimension in pixels | 512 |

## How It Works

1. Loads GTA V asset files using CodeWalker.Core library
2. Exports embedded textures to DDS format
3. Resizes textures larger than the target size using texconv
4. Rebuilds the asset file with optimized textures
5. Saves to output folder

## Results

| File | Original | Optimized | Reduction |
|------|----------|-----------|-----------|
| catamaran.ytd | 3.9 MB | 0.4 MB | 90% |
| skidoo800R.ytd | 0.5 MB | 0.3 MB | 52% |

## Limitations

- **FXA files** (FiveM compiled assets with `FXAP` magic) are not supported - use original uncompiled files
- **YFT without embedded textures** - optimize the associated YTD file instead
- Textures are resized proportionally, maintaining aspect ratio

## Project Structure

```
├── optimize.py          # Main script (Python wrapper)
├── optimize/            # Input folder (place files here)
├── output/              # Output folder (optimized files)
├── tools/
│   ├── texconv.exe      # Microsoft DirectXTex texture converter
│   ├── oo2core_8_win64.dll  # Oodle decompression (optional)
│   ├── YtdOptimizer/    # C# tool using CodeWalker.Core
│   └── codewalker/      # CodeWalker library for GTA V formats
└── README.md
```

## Credits

- [CodeWalker](https://github.com/dexyfex/CodeWalker) - GTA V file format library
- [DirectXTex](https://github.com/microsoft/DirectXTex) - texconv.exe for DDS processing

## License

MIT
