using System.Reflection;
using Lyo.Common.Enums;
// ReSharper disable StringLiteralTypo

namespace Lyo.Common.Records;

/// <summary>Represents file type information with MIME types, extensions, and category.</summary>
public sealed class FileTypeInfo
{
    /// <summary>Appended to a single-key ciphertext extension for two-key envelope files (e.g. <c>.ag</c> → <c>.ag2k</c>).</summary>
    public const string TwoKeyEnvelopeSuffix = "2k";

    public string Name { get; }
    public string MimeType { get; }
    /// <summary>Canonical extension for defaults and naming (lowercase, leading dot).</summary>
    public string DefaultExtension { get; }
    /// <summary>Alternate spellings still recognized by <see cref="FromExtension"/>.</summary>
    public string[] Aliases { get; }
    /// <summary>Additional MIME strings that resolve to this type via <see cref="FromMimeType"/> (e.g. legacy names).</summary>
    public string[] MimeTypeAliases { get; }
    /// <summary><see cref="DefaultExtension"/> followed by <see cref="Aliases"/>.</summary>
    public string[] Extensions { get; }
    public FileTypeCategory Category { get; }
    public string Description { get; }

    public FileTypeInfo(
        string name,
        string mimeType,
        string defaultExtension,
        string[]? aliases,
        FileTypeCategory category,
        string description,
        string[]? mimeTypeAliases = null)
    {
        Name = name;
        MimeType = mimeType;
        DefaultExtension = NormalizeExtension(defaultExtension);
        Aliases = aliases is { Length: > 0 } ? aliases.Select(NormalizeExtension).ToArray() : [];
        MimeTypeAliases = mimeTypeAliases is { Length: > 0 }
            ? mimeTypeAliases.Select(static a => a.Trim()).Where(static a => a.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            : [];
        Extensions = Aliases.Length == 0
            ? [DefaultExtension]
            : Enumerable.Repeat(DefaultExtension, 1).Concat(Aliases).ToArray();
        Category = category;
        Description = description;
    }

    private static string NormalizeExtension(string extension)
    {
        var t = extension.Trim();
        if (t.Length == 0)
            return ".";
        if (!t.StartsWith(".", StringComparison.Ordinal))
            t = "." + t;
        return t.ToLowerInvariant();
    }

    // Unknown
    public static readonly FileTypeInfo Unknown = new("Unknown", "application/octet-stream", ".unknown", null, FileTypeCategory.Unknown, "Unknown or unsupported file type");

    // 📄 Document Formats
    public static readonly FileTypeInfo Pdf = new("PDF", "application/pdf", ".pdf", null, FileTypeCategory.Documents, "Adobe PDF document", ["application/x-pdf"]);

    public static readonly FileTypeInfo Doc = new(
        "DOC",
        "application/msword",
        ".doc",
        null,
        FileTypeCategory.Documents,
        "Microsoft Word document",
        ["application/x-msword"]);

    public static readonly FileTypeInfo Docx = new(
        "DOCX", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ".docx", null, FileTypeCategory.Documents, "Microsoft Word Open XML document");

    public static readonly FileTypeInfo Xls = new(
        "XLS",
        "application/vnd.ms-excel",
        ".xls",
        null,
        FileTypeCategory.Documents,
        "Microsoft Excel spreadsheet",
        ["application/excel", "application/x-excel", "application/x-msexcel"]);

    public static readonly FileTypeInfo Xlsx = new(
        "XLSX", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx", null, FileTypeCategory.Documents, "Microsoft Excel Open XML spreadsheet");

    // 🧾 Text & Data
    public static readonly FileTypeInfo Csv = new(
        "CSV",
        "text/csv",
        ".csv",
        null,
        FileTypeCategory.DataFiles,
        "Comma-separated values file",
        ["text/comma-separated-values", "application/csv"]);

    public static readonly FileTypeInfo Txt = new("TXT", "text/plain", ".txt", null, FileTypeCategory.DataFiles, "Plain text file");

    public static readonly FileTypeInfo Tex = new("TEX", "application/x-tex", ".tex", null, FileTypeCategory.DataFiles, "LaTeX document file");

    public static readonly FileTypeInfo Json = new(
        "JSON",
        "application/json",
        ".json",
        null,
        FileTypeCategory.DataFiles,
        "JSON data file",
        ["text/json", "application/x-json"]);

    public static readonly FileTypeInfo Xml = new(
        "XML",
        "application/xml",
        ".xml",
        null,
        FileTypeCategory.DataFiles,
        "XML data file",
        ["text/xml"]);

    // 🌐 Web Formats
    public static readonly FileTypeInfo Html = new("HTML", "text/html", ".html", [".htm"], FileTypeCategory.DataFiles, "HTML web page");

    // 🧱 Binary & Dump Files
    public static readonly FileTypeInfo Bin = new("BIN", "application/octet-stream", ".bin", null, FileTypeCategory.DataFiles, "Raw binary file");

    public static readonly FileTypeInfo Dump = new("DUMP", "application/x-dump", ".dump", null, FileTypeCategory.DataFiles, "Generic data dump file");

    // 🖼️ Image Formats
    public static readonly FileTypeInfo Jpeg = new(
        "JPEG",
        "image/jpeg",
        ".jpg",
        [".jpeg"],
        FileTypeCategory.Images,
        "JPEG image file",
        ["image/jpg", "image/pjpeg"]);

    public static readonly FileTypeInfo Png = new(
        "PNG",
        "image/png",
        ".png",
        null,
        FileTypeCategory.Images,
        "Portable Network Graphics image",
        ["image/x-png"]);

    public static readonly FileTypeInfo Gif = new("GIF", "image/gif", ".gif", null, FileTypeCategory.Images, "Graphics Interchange Format image");

    public static readonly FileTypeInfo Bmp = new(
        "BMP",
        "image/bmp",
        ".bmp",
        null,
        FileTypeCategory.Images,
        "Bitmap image file",
        ["image/x-ms-bmp", "image/x-bmp"]);

    public static readonly FileTypeInfo Svg = new(
        "SVG",
        "image/svg+xml",
        ".svg",
        null,
        FileTypeCategory.Images,
        "Scalable Vector Graphics image",
        ["image/svg"]);

    public static readonly FileTypeInfo Tiff = new(
        "TIFF",
        "image/tiff",
        ".tif",
        [".tiff"],
        FileTypeCategory.Images,
        "Tagged Image File Format",
        ["image/tif"]);

    public static readonly FileTypeInfo Webp = new("WEBP", "image/webp", ".webp", null, FileTypeCategory.Images, "WebP image file");

    public static readonly FileTypeInfo Ico = new(
        "ICO",
        "image/vnd.microsoft.icon",
        ".ico",
        null,
        FileTypeCategory.Images,
        "Windows icon file",
        ["image/x-icon"]);

    // 📦 Compressed Formats
    public static readonly FileTypeInfo Zip = new(
        "ZIP",
        "application/zip",
        ".zip",
        null,
        FileTypeCategory.Compressed,
        "ZIP archive file",
        ["application/x-zip-compressed"]);

    public static readonly FileTypeInfo Rar = new(
        "RAR",
        "application/x-rar-compressed",
        ".rar",
        null,
        FileTypeCategory.Compressed,
        "RAR archive file",
        ["application/vnd.rar"]);

    public static readonly FileTypeInfo SevenZip = new("7Z", "application/x-7z-compressed", ".7z", null, FileTypeCategory.Compressed, "7-Zip archive file");

    public static readonly FileTypeInfo Tar = new(
        "TAR",
        "application/x-tar",
        ".tar",
        null,
        FileTypeCategory.Compressed,
        "TAR archive file",
        ["application/gtar"]);

    public static readonly FileTypeInfo Gz = new(
        "GZ",
        "application/gzip",
        ".gz",
        [".gzip"],
        FileTypeCategory.Compressed,
        "GZIP compressed file",
        ["application/x-gzip"]);

    public static readonly FileTypeInfo Bz2 = new(
        "BZ2",
        "application/x-bzip2",
        ".bz2",
        [".bzip2"],
        FileTypeCategory.Compressed,
        "BZIP2 compressed file",
        ["application/bzip2", "application/x-bzip"]);

    public static readonly FileTypeInfo Xz = new("XZ", "application/x-xz", ".xz", null, FileTypeCategory.Compressed, "XZ compressed file");

    public static readonly FileTypeInfo Brotli = new(
        "Brotli",
        "application/x-brotli",
        ".br",
        null,
        FileTypeCategory.Compressed,
        "Brotli compressed stream",
        ["application/brotli"]);

    public static readonly FileTypeInfo ZLibStream = new("ZLib", "application/zlib", ".zlib", null, FileTypeCategory.Compressed, "ZLIB compressed stream");

    public static readonly FileTypeInfo DeflateStream = new("Deflate", "application/x-deflate", ".deflate", null, FileTypeCategory.Compressed, "DEFLATE compressed stream");

    public static readonly FileTypeInfo SnappyStream = new("Snappy", "application/x-snappy-framed", ".snappy", null, FileTypeCategory.Compressed, "Snappy compressed stream");

    public static readonly FileTypeInfo ZstdStream = new(
        "Zstandard",
        "application/zstd",
        ".zst",
        null,
        FileTypeCategory.Compressed,
        "Zstandard compressed stream",
        ["application/x-zstd"]);

    public static readonly FileTypeInfo LZ4Stream = new("LZ4", "application/x-lz4", ".lz4", null, FileTypeCategory.Compressed, "LZ4 compressed stream");

    public static readonly FileTypeInfo LZMAStream = new("LZMA", "application/x-lzma", ".lzma", null, FileTypeCategory.Compressed, "LZMA compressed stream");

    // 🔒 Encrypted Formats
    public static readonly FileTypeInfo Enc = new("ENC", "application/octet-stream", ".enc", [".crypt", ".aes"], FileTypeCategory.Encrypted, "Encrypted file");

    public static readonly FileTypeInfo Gpg = new("GPG", "application/pgp-encrypted", ".gpg", [".pgp"], FileTypeCategory.Encrypted, "GPG encrypted file");

    public static readonly FileTypeInfo LyoAesGcm = new(
        "Lyo AES-GCM", "application/x-lyo-ciphertext-aes-gcm", ".ag", null, FileTypeCategory.Encrypted, "Lyo AES-GCM symmetric ciphertext");

    public static readonly FileTypeInfo LyoChaCha20Poly1305 = new(
        "Lyo ChaCha20-Poly1305", "application/x-lyo-ciphertext-chacha20-poly1305", ".chacha", null, FileTypeCategory.Encrypted, "Lyo ChaCha20-Poly1305 symmetric ciphertext");

    public static readonly FileTypeInfo LyoAesCcm = new(
        "Lyo AES-CCM", "application/x-lyo-ciphertext-aes-ccm", ".ccm", null, FileTypeCategory.Encrypted, "Lyo AES-CCM symmetric ciphertext");

    public static readonly FileTypeInfo LyoAesSiv = new(
        "Lyo AES-SIV", "application/x-lyo-ciphertext-aes-siv", ".siv", null, FileTypeCategory.Encrypted, "Lyo AES-SIV synthetic IV ciphertext");

    public static readonly FileTypeInfo LyoXChaCha20Poly1305 = new(
        "Lyo XChaCha20-Poly1305", "application/x-lyo-ciphertext-xchacha20-poly1305", ".xchacha", null, FileTypeCategory.Encrypted, "Lyo XChaCha20-Poly1305 symmetric ciphertext");

    public static readonly FileTypeInfo LyoAesGcmRsa = new(
        "Lyo AES-GCM+RSA", "application/x-lyo-ciphertext-aes-gcm-rsa", ".agr", null, FileTypeCategory.Encrypted, "Lyo AES-GCM with RSA key wrap");

    public static readonly FileTypeInfo LyoRsa = new(
        "Lyo RSA", "application/x-lyo-ciphertext-rsa", ".rsa", null, FileTypeCategory.Encrypted, "Lyo RSA ciphertext");

    public static readonly FileTypeInfo LyoTwoKeyEnvelope = new(
        "Lyo two-key envelope",
        "application/x-lyo-two-key-envelope",
        ".ag2k",
        [".chacha2k", ".ccm2k", ".siv2k", ".xchacha2k", ".agr2k", ".rsa2k"],
        FileTypeCategory.Encrypted,
        "Lyo envelope: data encrypted with a DEK and key-wrapped by a KEK");

    // 🎵 Audio Formats
    public static readonly FileTypeInfo Wav = new(
        "WAV",
        "audio/wav",
        ".wav",
        null,
        FileTypeCategory.Audio,
        "WAV audio file",
        ["audio/x-wav", "audio/wave"]);

    public static readonly FileTypeInfo Mp3 = new(
        "MP3",
        "audio/mpeg",
        ".mp3",
        null,
        FileTypeCategory.Audio,
        "MP3 audio file",
        ["audio/mp3", "audio/x-mpeg", "audio/x-mp3"]);

    public static readonly FileTypeInfo Ogg = new(
        "OGG",
        "audio/ogg",
        ".ogg",
        [".oga"],
        FileTypeCategory.Audio,
        "OGG audio file",
        ["application/ogg"]);

    public static readonly FileTypeInfo Flac = new(
        "FLAC",
        "audio/flac",
        ".flac",
        null,
        FileTypeCategory.Audio,
        "FLAC audio file",
        ["audio/x-flac"]);

    public static readonly FileTypeInfo Aac = new(
        "AAC",
        "audio/aac",
        ".aac",
        null,
        FileTypeCategory.Audio,
        "AAC audio file",
        ["audio/x-aac"]);

    public static readonly FileTypeInfo M4a = new(
        "M4A",
        "audio/mp4",
        ".m4a",
        null,
        FileTypeCategory.Audio,
        "M4A audio file",
        ["audio/m4a", "audio/x-m4a"]);

    public static readonly FileTypeInfo Opus = new("OPUS", "audio/opus", ".opus", null, FileTypeCategory.Audio, "OPUS audio file");

    public static readonly FileTypeInfo Pcm = new("PCM", "audio/pcm", ".pcm", null, FileTypeCategory.Audio, "PCM audio file");

    public static readonly FileTypeInfo Webm = new("WEBM", "audio/webm", ".webm", null, FileTypeCategory.Audio, "WebM audio file");

    /// <summary>Distinct default extensions for stream algorithms in <c>Lyo.Compression.Constants.Data.AlgorithmExtensions</c> (GZip through XZ).</summary>
    public static readonly IReadOnlyList<string> StreamCompressionAlgorithmDefaultExtensions =
    [
        Gz.DefaultExtension,
        Brotli.DefaultExtension,
        ZLibStream.DefaultExtension,
        DeflateStream.DefaultExtension,
        SnappyStream.DefaultExtension,
        ZstdStream.DefaultExtension,
        LZ4Stream.DefaultExtension,
        LZMAStream.DefaultExtension,
        Bz2.DefaultExtension,
        Xz.DefaultExtension
    ];

    /// <summary>Same as <see cref="StreamCompressionAlgorithmDefaultExtensions"/> but ordered longest-first for stripping nested suffixes.</summary>
    public static readonly IReadOnlyList<string> StreamCompressionExtensionsLongestFirst =
        StreamCompressionAlgorithmDefaultExtensions.OrderByDescending(s => s.Length).ThenBy(s => s, StringComparer.Ordinal).ToArray();

    /// <summary>
    ///     Encryption-related suffixes stripped from a filename before inferring compression (longest first; includes two-key and
    ///     asymmetric variants).
    /// </summary>
    public static readonly IReadOnlyList<string> EncryptionFilenameStripSuffixesLongestFirst =
    [
        LyoXChaCha20Poly1305.DefaultExtension + TwoKeyEnvelopeSuffix,
        LyoChaCha20Poly1305.DefaultExtension + TwoKeyEnvelopeSuffix,
        LyoXChaCha20Poly1305.DefaultExtension,
        LyoChaCha20Poly1305.DefaultExtension,
        LyoAesGcmRsa.DefaultExtension + TwoKeyEnvelopeSuffix,
        LyoAesCcm.DefaultExtension + TwoKeyEnvelopeSuffix,
        LyoAesSiv.DefaultExtension + TwoKeyEnvelopeSuffix,
        LyoRsa.DefaultExtension + TwoKeyEnvelopeSuffix,
        LyoAesGcm.DefaultExtension + TwoKeyEnvelopeSuffix,
        LyoAesCcm.DefaultExtension,
        LyoAesSiv.DefaultExtension,
        LyoAesGcmRsa.DefaultExtension,
        LyoRsa.DefaultExtension,
        LyoAesGcm.DefaultExtension
    ];

    /// <summary>Stream-compression and Lyo ciphertext suffixes tried when resolving a stored file without explicit metadata.</summary>
    public static readonly IReadOnlyList<string> CommonStorageResolutionSuffixes =
        StreamCompressionAlgorithmDefaultExtensions
            .Concat(
            [
                LyoAesGcm.DefaultExtension,
                LyoChaCha20Poly1305.DefaultExtension,
                LyoAesCcm.DefaultExtension,
                LyoAesSiv.DefaultExtension,
                LyoXChaCha20Poly1305.DefaultExtension,
                LyoAesGcmRsa.DefaultExtension,
                LyoRsa.DefaultExtension
            ])
            .Concat(LyoTwoKeyEnvelope.Extensions)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    // Static registry with fast lookups
    private static readonly Dictionary<string, FileTypeInfo> _byMimeType = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, FileTypeInfo> _byExtension = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<FileTypeInfo> _allTypes = new();

    /// <summary>Gets all registered file types.</summary>
    public static IReadOnlyList<FileTypeInfo> All => _allTypes;

    static FileTypeInfo()
    {
        // Register all types using reflection to find static fields
        var type = typeof(FileTypeInfo);
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(FileTypeInfo))
            .Select(f => (FileTypeInfo)f.GetValue(null)!)
            .ToList();

        foreach (var fileType in fields) {
            _allTypes.Add(fileType);
            _byMimeType[fileType.MimeType] = fileType;
            foreach (var mimeAlias in fileType.MimeTypeAliases)
                _byMimeType[mimeAlias] = fileType;
            foreach (var ext in fileType.Extensions)
                _byExtension[ext.ToLowerInvariant()] = fileType;
        }
    }

    /// <summary>Finds a file type by its MIME type.</summary>
    /// <param name="mimeType">The MIME type (e.g., "application/pdf").</param>
    /// <returns>The file type, or Unknown if not found.</returns>
    public static FileTypeInfo FromMimeType(string? mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
            return Unknown;

        var trimmed = mimeType!.Trim();
        return _byMimeType.TryGetValue(trimmed, out var type) ? type : Unknown;
    }

    /// <summary>Finds a file type by its extension.</summary>
    /// <param name="extension">The file extension (with or without leading dot, e.g., ".pdf", "pdf").</param>
    /// <returns>The file type, or Unknown if not found.</returns>
    public static FileTypeInfo FromExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return Unknown;

        var normalized = extension!.Trim();
        if (!normalized.StartsWith(".", StringComparison.Ordinal))
            normalized = "." + normalized;

        normalized = normalized.ToLowerInvariant();
        return _byExtension.TryGetValue(normalized, out var type) ? type : Unknown;
    }

    /// <summary>Gets file types by category.</summary>
    /// <param name="category">The file type category.</param>
    /// <returns>An enumerable of file types in the specified category.</returns>
    public static IEnumerable<FileTypeInfo> ByCategory(FileTypeCategory category) => _allTypes.Where(t => t.Category == category);

    /// <summary>Gets the file type from a file path.</summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>The file type, or Unknown if not recognized.</returns>
    public static FileTypeInfo FromFilePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Unknown;

        var extension = Path.GetExtension(filePath);
        if (string.IsNullOrWhiteSpace(extension))
            extension = filePath!.TrimStart('.');

        return FromExtension(extension);
    }

    /// <summary>Gets the file type from a FileInfo.</summary>
    /// <param name="fileInfo">The file info.</param>
    /// <returns>The file type, or Unknown if not recognized.</returns>
    public static FileTypeInfo FromFileInfo(FileInfo? fileInfo) => fileInfo == null ? Unknown : FromFilePath(fileInfo.FullName);
}
