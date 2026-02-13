using System;

namespace YtdOptimizerGUI
{
    public class TextureItem
    {
        public string FileName { get; set; } = "";
        public string TextureName { get; set; } = "";
        public string FileType { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
        public long SizeBytes { get; set; }
        public string Format { get; set; } = "";
        public int MipLevels { get; set; }
        public string FilePath { get; set; } = "";
        public bool IsSelected { get; set; } = true;
        public bool NeedsOptimization { get; set; }

        public string SizeMB => $"{SizeBytes / (1024.0 * 1024.0):F2}";
        public string Dimensions => $"{Width}x{Height}";
        public string DisplayName => string.IsNullOrEmpty(TextureName) ? FileName : $"{FileName} → {TextureName}";
    }

    public class FileItem
    {
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string FileType { get; set; } = "";
        public long SizeBytes { get; set; }
        public int TextureCount { get; set; }
        public List<TextureItem> Textures { get; set; } = new();
        public bool IsExpanded { get; set; }
        public bool IsSelected { get; set; } = true;

        public string SizeMB => $"{SizeBytes / (1024.0 * 1024.0):F2}";
    }
}
