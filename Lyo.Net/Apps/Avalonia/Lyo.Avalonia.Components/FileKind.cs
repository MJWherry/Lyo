using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Lyo.Desktop.Components;

/// <summary>Icon drawn beside a file or folder name.</summary>
public enum FileGlyph
{
    /// <summary>A directory.</summary>
    Folder,

    /// <summary>A PDF.</summary>
    Pdf,

    /// <summary>A spreadsheet or CSV.</summary>
    Table,

    /// <summary>An image.</summary>
    Image,

    /// <summary>A document or plain text file.</summary>
    Document,

    /// <summary>Source or markup.</summary>
    Code,

    /// <summary>Audio.</summary>
    Audio,

    /// <summary>Video.</summary>
    Video,

    /// <summary>An archive.</summary>
    Archive,

    /// <summary>Anything else.</summary>
    File
}

/// <summary>File-type, compression, and encryption chips, plus the icon for a name.</summary>
public static class FileKind
{
    private static readonly (FileGlyph Glyph, string[] Extensions)[] Glyphs = [
        (FileGlyph.Pdf, [".pdf"]),
        (FileGlyph.Table, [".csv", ".xls", ".xlsx"]),
        (FileGlyph.Image, [".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp"]),
        (FileGlyph.Document, [".doc", ".docx", ".txt", ".rtf"]),
        (FileGlyph.Code, [".cs", ".ts", ".tsx", ".js", ".jsx", ".json", ".xml", ".yaml", ".yml", ".html", ".htm", ".css", ".py", ".java", ".go", ".rs"]),
        (FileGlyph.Audio, [".mp3", ".wav"]),
        (FileGlyph.Video, [".mp4"]),
        (FileGlyph.Archive, [".zip", ".gz", ".tgz", ".7z"])
    ];

    private static readonly (string Name, string[] Mimes, string[] Extensions)[] Types = [
        ("PDF", ["application/pdf"], [".pdf"]),
        ("DOC", ["application/msword"], [".doc"]),
        ("DOCX", ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"], [".docx"]),
        ("XLS", ["application/vnd.ms-excel"], [".xls"]),
        ("XLSX", ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"], [".xlsx"]),
        ("CSV", ["text/csv"], [".csv"]),
        ("TXT", ["text/plain"], [".txt"]),
        ("JSON", ["application/json"], [".json"]),
        ("XML", ["application/xml", "text/xml"], [".xml"]),
        ("YAML", ["application/yaml", "text/yaml", "text/x-yaml"], [".yaml", ".yml"]),
        ("HTML", ["text/html"], [".html", ".htm"]),
        ("JavaScript", ["text/javascript", "application/javascript"], [".js", ".mjs"]),
        ("PNG", ["image/png"], [".png"]),
        ("JPEG", ["image/jpeg"], [".jpg", ".jpeg"]),
        ("GIF", ["image/gif"], [".gif"]),
        ("SVG", ["image/svg+xml"], [".svg"]),
        ("WEBP", ["image/webp"], [".webp"]),
        ("ZIP", ["application/zip"], [".zip"]),
        ("GZ", ["application/gzip"], [".gz"]),
        ("MP3", ["audio/mpeg"], [".mp3"]),
        ("MP4", ["video/mp4"], [".mp4"]),
        ("WAV", ["audio/wav"], [".wav"])
    ];

    /// <summary>Icon for a tree or grid row. Directories are folders. Files follow the extension.</summary>
    public static FileGlyph Glyph(string? name, bool isDirectory)
    {
        if (isDirectory)
            return FileGlyph.Folder;
        var ext = Extension(name);
        if (ext == null)
            return FileGlyph.File;
        foreach (var (glyph, extensions) in Glyphs) {
            if (extensions.Contains(ext))
                return glyph;
        }

        return FileGlyph.File;
    }

    /// <summary>File-type chip from the media type, then the file name. Directories are <c>Folder</c>.</summary>
    public static ChipSpec TypeChip(string? contentType, string? fileName, bool isDirectory)
    {
        if (isDirectory)
            return new("Folder", ChipHues.Amber);
        var name = TypeName(contentType, fileName);
        if (name == null)
            return new(string.IsNullOrWhiteSpace(contentType) ? "File" : MediaType(contentType), -1);
        return new(name, HueFor(name));
    }

    /// <summary>Compression chip. Directories have none. A file that is not compressed is <c>None</c>.</summary>
    public static ChipSpec? CompressionChip(bool isDirectory, bool compressed, string? algorithm)
    {
        if (isDirectory)
            return null;
        if (!compressed)
            return new("None", -1);
        var label = string.IsNullOrWhiteSpace(algorithm) ? "Yes" : algorithm.Trim();
        return new(label, CompressionHue(label));
    }

    /// <summary>Encryption chip. Directories have none. A file that is not encrypted is <c>None</c>.</summary>
    public static ChipSpec? EncryptionChip(bool isDirectory, bool encrypted, string? algorithm)
    {
        if (isDirectory)
            return null;
        if (!encrypted)
            return new("None", -1);
        var label = string.IsNullOrWhiteSpace(algorithm) ? "Yes" : algorithm.Trim();
        return new(label, EncryptionHue(label));
    }

    private static int HueFor(string name)
    {
        var sum = 0;
        foreach (var ch in name)
            sum += ch;
        return ChipHues.List[(sum * 5) % ChipHues.List.Length];
    }

    private static int CompressionHue(string algorithm)
        => Normalize(algorithm) switch {
            "BROTLI" => ChipHues.Lime,
            "GZIP" => ChipHues.Amber,
            "DEFLATE" or "ZLIB" => ChipHues.Cyan,
            "LZ4" => ChipHues.Coral,
            _ => -1
        };

    private static int EncryptionHue(string algorithm)
        => Normalize(algorithm) switch {
            "AESGCM" or "AESGCMRSA" or "AESCCM" or "AESSIV" => ChipHues.Blue,
            "CHACHA20POLY1305" or "XCHACHA20POLY1305" => ChipHues.Teal,
            "RSA" => ChipHues.Violet,
            _ => -1
        };

    private static string Normalize(string value)
    {
        Span<char> buffer = stackalloc char[value.Length];
        var count = 0;
        foreach (var ch in value) {
            if (ch is '-' or '_')
                continue;
            buffer[count++] = char.ToUpperInvariant(ch);
        }

        return new string(buffer[..count]);
    }

    private static string? TypeName(string? contentType, string? fileName)
    {
        var media = MediaType(contentType).ToLowerInvariant();
        if (media.Length > 0) {
            foreach (var type in Types) {
                if (type.Mimes.Any(item => item == media))
                    return type.Name;
            }
        }

        var ext = Extension(fileName);
        if (ext == null)
            return null;
        foreach (var type in Types) {
            if (type.Extensions.Contains(ext))
                return type.Name;
        }

        return null;
    }

    private static string MediaType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return "";
        var semi = contentType.IndexOf(';');
        return (semi < 0 ? contentType : contentType[..semi]).Trim();
    }

    private static string? Extension(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;
        var baseName = fileName.Replace('\\', '/');
        var slash = baseName.LastIndexOf('/');
        if (slash >= 0)
            baseName = baseName[(slash + 1)..];
        var dot = baseName.LastIndexOf('.');
        if (dot <= 0)
            return null;
        return baseName[dot..].ToLowerInvariant();
    }
}

/// <summary>Draws <see cref="FileGlyph" /> as a small path.</summary>
public class FileGlyphView : UserControl
{
    /// <summary>Which icon to draw.</summary>
    public static readonly Avalonia.StyledProperty<FileGlyph> GlyphProperty =
        Avalonia.AvaloniaProperty.Register<FileGlyphView, FileGlyph>(nameof(Glyph));

    public FileGlyphView()
    {
        Width = 16;
        Height = 16;
        MinWidth = 16;
        MinHeight = 16;
        MaxWidth = 16;
        MaxHeight = 16;
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Center;
        ClipToBounds = true;
        Redraw();
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size available)
    {
        base.MeasureOverride(new Size(16, 16));
        return new Size(16, 16);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        base.ArrangeOverride(new Size(16, 16));
        return new Size(16, 16);
    }

    /// <summary>Which icon to draw.</summary>
    public FileGlyph Glyph
    {
        get => GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(Avalonia.AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == GlyphProperty)
            Redraw();
    }

    private void Redraw()
    {
        Content = new Viewbox {
            Width = 16,
            Height = 16,
            Stretch = Stretch.Uniform,
            Child = new Avalonia.Controls.Shapes.Path {
                Stretch = Stretch.Uniform,
                Fill = new SolidColorBrush(Fill(Glyph)),
                Data = Geometry.Parse(Data(Glyph))
            }
        };
    }

    private static Color Fill(FileGlyph glyph)
        => glyph switch {
            FileGlyph.Folder => Color.Parse("#E8B84A"),
            FileGlyph.Pdf => Color.Parse("#E74C3C"),
            FileGlyph.Image => Color.Parse("#58D68D"),
            FileGlyph.Code => Color.Parse("#5DADE2"),
            FileGlyph.Archive => Color.Parse("#F5B041"),
            FileGlyph.Audio or FileGlyph.Video => Color.Parse("#AF7AC5"),
            FileGlyph.Table => Color.Parse("#48C9B0"),
            _ => Color.Parse("#AED6F1")
        };

    private static string Data(FileGlyph glyph)
        => glyph switch {
            FileGlyph.Folder => "M1,4 H7 L9,6 H15 V14 H1 Z",
            FileGlyph.Image => "M2,2 H14 V14 H2 Z M4,10 L7,7 L9,9 L12,6 L14,8 V12 H2 V10 Z",
            FileGlyph.Archive => "M4,1 H12 V15 H4 Z M6,3 H10 V5 H6 Z M6,7 H10 V9 H6 Z",
            _ => "M4,1 H10 L14,5 V15 H4 Z M10,1 V5 H14"
        };
}
