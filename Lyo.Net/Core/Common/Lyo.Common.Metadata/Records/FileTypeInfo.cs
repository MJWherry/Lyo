using System.Diagnostics;
using Lyo.Common.Core;
using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Extensions;
using Lyo.Exceptions;

// ReSharper disable StringLiteralTypo

namespace Lyo.Common.Metadata.Records;

/// <summary>Catalog row for a file type: MIME, extensions, category, and display name.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class FileTypeInfo
{
    /// <summary>Suffix appended to a single-key ciphertext extension for two-key envelopes (for example <c>.ag</c> becomes <c>.ag2k</c>).</summary>
    public const string TwoKeyEnvelopeSuffix = "2k";

    /// <summary>Default UTF-8 charset token; same value as <see cref="MimeTypeInfo.Utf8Charset" />.</summary>
    public const string Utf8Charset = MimeTypeInfo.Utf8Charset;

    // Unrecognized type
    public static readonly FileTypeInfo Unknown = new("Unknown", MimeTypeInfo.Unknown, ".unknown", null, FileTypeCategory.Unknown, "Unknown or unsupported file type");

    // Document formats
    public static readonly FileTypeInfo Pdf = new("PDF", MimeTypeInfo.Pdf, ".pdf", null, FileTypeCategory.Documents, "Adobe PDF document");

    public static readonly FileTypeInfo Doc = new("DOC", MimeTypeInfo.Doc, ".doc", null, FileTypeCategory.Documents, "Microsoft Word document");

    public static readonly FileTypeInfo Docx = new(
        "DOCX", MimeTypeInfo.Docx, ".docx", null, FileTypeCategory.Documents, "Microsoft Word Open XML document");

    public static readonly FileTypeInfo Xls = new("XLS", MimeTypeInfo.Xls, ".xls", null, FileTypeCategory.Documents, "Microsoft Excel spreadsheet");

    public static readonly FileTypeInfo Xlsx = new(
        "XLSX", MimeTypeInfo.Xlsx, ".xlsx", null, FileTypeCategory.Documents, "Microsoft Excel Open XML spreadsheet");

    // Text and structured data
    public static readonly FileTypeInfo Csv = new("CSV", MimeTypeInfo.Csv, ".csv", null, FileTypeCategory.DataFiles, "Comma-separated values file");

    public static readonly FileTypeInfo Txt = new("TXT", MimeTypeInfo.Txt, ".txt", null, FileTypeCategory.DataFiles, "Plain text file");

    public static readonly FileTypeInfo Tex = new("TEX", MimeTypeInfo.Tex, ".tex", null, FileTypeCategory.DataFiles, "LaTeX document file");

    public static readonly FileTypeInfo Json = new("JSON", MimeTypeInfo.Json, ".json", null, FileTypeCategory.DataFiles, "JSON data file");

    public static readonly FileTypeInfo Xml = new("XML", MimeTypeInfo.Xml, ".xml", null, FileTypeCategory.DataFiles, "XML data file");

    /// <summary>RFC 9457 problem-details body; the content type ASP.NET Core emits for <c>ProblemDetails</c> responses.</summary>
    public static readonly FileTypeInfo ProblemJson = new(
        "Problem JSON", MimeTypeInfo.ProblemJson, ".problem.json", null, FileTypeCategory.DataFiles, "RFC 9457 problem details response body");

    // Web formats
    public static readonly FileTypeInfo Html = new("HTML", MimeTypeInfo.Html, ".html", [".htm"], FileTypeCategory.DataFiles, "HTML web page");

    public static readonly FileTypeInfo JavaScript = new(
        "JavaScript", MimeTypeInfo.JavaScript, ".js", [".mjs"], FileTypeCategory.DataFiles, "JavaScript source file");

    public static readonly FileTypeInfo Graphql = new("GraphQL", MimeTypeInfo.Graphql, ".graphql", [".gql"], FileTypeCategory.DataFiles, "GraphQL schema, query, or request body");

    public static readonly FileTypeInfo WwwFormUrlEncoded = new(
        "WWW form URL-encoded", MimeTypeInfo.WwwFormUrlEncoded, ".form", null, FileTypeCategory.DataFiles,
        "application/x-www-form-urlencoded HTTP body (common extension .form)");

    // Binary and dump files
    public static readonly FileTypeInfo Bin = new("BIN", MimeTypeInfo.Unknown, ".bin", null, FileTypeCategory.DataFiles, "Raw binary file");

    public static readonly FileTypeInfo Dump = new("DUMP", MimeTypeInfo.Dump, ".dump", null, FileTypeCategory.DataFiles, "Generic data dump file");

    // Image formats
    public static readonly FileTypeInfo Jpeg = new("JPEG", MimeTypeInfo.Jpeg, ".jpg", [".jpeg"], FileTypeCategory.Images, "JPEG image file");

    public static readonly FileTypeInfo Png = new("PNG", MimeTypeInfo.Png, ".png", null, FileTypeCategory.Images, "Portable Network Graphics image");

    public static readonly FileTypeInfo Gif = new("GIF", MimeTypeInfo.Gif, ".gif", null, FileTypeCategory.Images, "Graphics Interchange Format image");

    public static readonly FileTypeInfo Bmp = new("BMP", MimeTypeInfo.Bmp, ".bmp", null, FileTypeCategory.Images, "Bitmap image file");

    public static readonly FileTypeInfo Svg = new("SVG", MimeTypeInfo.Svg, ".svg", null, FileTypeCategory.Images, "Scalable Vector Graphics image");

    public static readonly FileTypeInfo Tiff = new("TIFF", MimeTypeInfo.Tiff, ".tif", [".tiff"], FileTypeCategory.Images, "Tagged Image File Format");

    public static readonly FileTypeInfo Webp = new("WEBP", MimeTypeInfo.Webp, ".webp", null, FileTypeCategory.Images, "WebP image file");

    public static readonly FileTypeInfo Ico = new("ICO", MimeTypeInfo.Ico, ".ico", null, FileTypeCategory.Images, "Windows icon file");

    public static readonly FileTypeInfo Avif = new("AVIF", MimeTypeInfo.Avif, ".avif", null, FileTypeCategory.Images, "AV1 Image File Format");

    // Compressed formats
    public static readonly FileTypeInfo Zip = new("ZIP", MimeTypeInfo.Zip, ".zip", null, FileTypeCategory.Compressed, "ZIP archive file");

    public static readonly FileTypeInfo Rar = new("RAR", MimeTypeInfo.Rar, ".rar", null, FileTypeCategory.Compressed, "RAR archive file");

    public static readonly FileTypeInfo SevenZip = new("7Z", MimeTypeInfo.SevenZip, ".7z", null, FileTypeCategory.Compressed, "7-Zip archive file");

    public static readonly FileTypeInfo Tar = new("TAR", MimeTypeInfo.Tar, ".tar", null, FileTypeCategory.Compressed, "TAR archive file");

    public static readonly FileTypeInfo Gz = new("GZ", MimeTypeInfo.Gz, ".gz", [".gzip"], FileTypeCategory.Compressed, "GZIP compressed file");

    public static readonly FileTypeInfo Bz2 = new("BZ2", MimeTypeInfo.Bz2, ".bz2", [".bzip2"], FileTypeCategory.Compressed, "BZIP2 compressed file");

    public static readonly FileTypeInfo Xz = new("XZ", MimeTypeInfo.Xz, ".xz", null, FileTypeCategory.Compressed, "XZ compressed file");

    public static readonly FileTypeInfo Brotli = new("Brotli", MimeTypeInfo.Brotli, ".br", null, FileTypeCategory.Compressed, "Brotli compressed stream");

    public static readonly FileTypeInfo ZLibStream = new("ZLib", MimeTypeInfo.ZLibStream, ".zlib", null, FileTypeCategory.Compressed, "ZLIB compressed stream");

    public static readonly FileTypeInfo DeflateStream = new("Deflate", MimeTypeInfo.DeflateStream, ".deflate", null, FileTypeCategory.Compressed, "DEFLATE compressed stream");

    public static readonly FileTypeInfo SnappyStream = new("Snappy", MimeTypeInfo.SnappyStream, ".snappy", null, FileTypeCategory.Compressed, "Snappy compressed stream");

    public static readonly FileTypeInfo ZstdStream = new("Zstandard", MimeTypeInfo.ZstdStream, ".zst", null, FileTypeCategory.Compressed, "Zstandard compressed stream");

    public static readonly FileTypeInfo LZ4Stream = new("LZ4", MimeTypeInfo.LZ4Stream, ".lz4", null, FileTypeCategory.Compressed, "LZ4 compressed stream");

    public static readonly FileTypeInfo LZMAStream = new("LZMA", MimeTypeInfo.LZMAStream, ".lzma", null, FileTypeCategory.Compressed, "LZMA compressed stream");

    // Package and distribution archives
    public static readonly FileTypeInfo NuGetPackage = new("NuGet package", MimeTypeInfo.NuGetPackage, ".nupkg", null, FileTypeCategory.PackageManager, "NuGet package archive");

    public static readonly FileTypeInfo NuGetSymbolsPackage = new(
        "NuGet symbols package", MimeTypeInfo.NuGetSymbolsPackage, ".snupkg", null, FileTypeCategory.PackageManager, "NuGet symbol package archive");

    public static readonly FileTypeInfo JavaJar = new("JAR", MimeTypeInfo.JavaJar, ".jar", null, FileTypeCategory.PackageManager, "Java archive (JAR)");

    public static readonly FileTypeInfo JavaWar = new("WAR", MimeTypeInfo.JavaWar, ".war", null, FileTypeCategory.PackageManager, "Java web application archive (WAR)");

    public static readonly FileTypeInfo JavaEar = new("EAR", MimeTypeInfo.JavaEar, ".ear", null, FileTypeCategory.PackageManager, "Java enterprise archive (EAR)");

    public static readonly FileTypeInfo AndroidAar = new("AAR", MimeTypeInfo.AndroidAar, ".aar", null, FileTypeCategory.PackageManager, "Android library archive (AAR)");

    public static readonly FileTypeInfo DebianPackage = new(
        "Debian package", MimeTypeInfo.DebianPackage, ".deb", null, FileTypeCategory.PackageManager, "Debian binary package");

    public static readonly FileTypeInfo RpmPackage = new("RPM package", MimeTypeInfo.RpmPackage, ".rpm", null, FileTypeCategory.PackageManager, "RPM package");

    public static readonly FileTypeInfo WindowsInstallerMsi = new("MSI", MimeTypeInfo.WindowsInstallerMsi, ".msi", null, FileTypeCategory.PackageManager, "Windows Installer package (MSI)");

    // Encrypted formats
    public static readonly FileTypeInfo Enc = new("ENC", MimeTypeInfo.Unknown, ".enc", [".crypt", ".aes"], FileTypeCategory.Encrypted, "Encrypted file");

    public static readonly FileTypeInfo Gpg = new("GPG", MimeTypeInfo.Gpg, ".gpg", [".pgp"], FileTypeCategory.Encrypted, "GPG encrypted file");

    public static readonly FileTypeInfo LyoAesGcm = new(
        "Lyo AES-GCM", MimeTypeInfo.LyoAesGcm, ".ag", null, FileTypeCategory.Encrypted, "Lyo AES-GCM symmetric ciphertext");

    public static readonly FileTypeInfo LyoChaCha20Poly1305 = new(
        "Lyo ChaCha20-Poly1305", MimeTypeInfo.LyoChaCha20Poly1305, ".chacha", null, FileTypeCategory.Encrypted, "Lyo ChaCha20-Poly1305 symmetric ciphertext");

    public static readonly FileTypeInfo LyoAesCcm = new(
        "Lyo AES-CCM", MimeTypeInfo.LyoAesCcm, ".ccm", null, FileTypeCategory.Encrypted, "Lyo AES-CCM symmetric ciphertext");

    public static readonly FileTypeInfo LyoAesSiv = new(
        "Lyo AES-SIV", MimeTypeInfo.LyoAesSiv, ".siv", null, FileTypeCategory.Encrypted, "Lyo AES-SIV synthetic IV ciphertext");

    public static readonly FileTypeInfo LyoXChaCha20Poly1305 = new(
        "Lyo XChaCha20-Poly1305", MimeTypeInfo.LyoXChaCha20Poly1305, ".xchacha", null, FileTypeCategory.Encrypted, "Lyo XChaCha20-Poly1305 symmetric ciphertext");

    public static readonly FileTypeInfo LyoAesGcmRsa = new(
        "Lyo AES-GCM+RSA", MimeTypeInfo.LyoAesGcmRsa, ".agr", null, FileTypeCategory.Encrypted, "Lyo AES-GCM with RSA key wrap");

    public static readonly FileTypeInfo LyoRsa = new("Lyo RSA", MimeTypeInfo.LyoRsa, ".rsa", null, FileTypeCategory.Encrypted, "Lyo RSA ciphertext");

    public static readonly FileTypeInfo LyoTwoKeyEnvelope = new(
        "Lyo two-key envelope", MimeTypeInfo.LyoTwoKeyEnvelope, ".ag2k", [".chacha2k", ".ccm2k", ".siv2k", ".xchacha2k", ".agr2k", ".rsa2k"], FileTypeCategory.Encrypted,
        "Lyo envelope: data encrypted with a DEK and key-wrapped by a KEK");

    // Audio formats
    public static readonly FileTypeInfo Wav = new("WAV", MimeTypeInfo.Wav, ".wav", null, FileTypeCategory.Audio, "WAV audio file");

    public static readonly FileTypeInfo Mp3 = new("MP3", MimeTypeInfo.Mp3, ".mp3", null, FileTypeCategory.Audio, "MP3 audio file");

    public static readonly FileTypeInfo Ogg = new("OGG", MimeTypeInfo.Ogg, ".ogg", [".oga"], FileTypeCategory.Audio, "OGG audio file");

    public static readonly FileTypeInfo Flac = new("FLAC", MimeTypeInfo.Flac, ".flac", null, FileTypeCategory.Audio, "FLAC audio file");

    public static readonly FileTypeInfo Aac = new("AAC", MimeTypeInfo.Aac, ".aac", null, FileTypeCategory.Audio, "AAC audio file");

    public static readonly FileTypeInfo M4a = new("M4A", MimeTypeInfo.M4a, ".m4a", null, FileTypeCategory.Audio, "M4A audio file");

    public static readonly FileTypeInfo Opus = new("OPUS", MimeTypeInfo.Opus, ".opus", null, FileTypeCategory.Audio, "OPUS audio file");

    public static readonly FileTypeInfo Pcm = new("PCM", MimeTypeInfo.Pcm, ".pcm", null, FileTypeCategory.Audio, "PCM audio file");

    public static readonly FileTypeInfo Webm = new("WEBM", MimeTypeInfo.AudioWebm, ".webm", null, FileTypeCategory.Audio, "WebM audio file");

    // Video formats
    /// <summary>MPEG-4 video (<c>video/mp4</c>, <c>.mp4</c> / <c>.m4v</c>).</summary>
    public static readonly FileTypeInfo Mp4 = new("MP4", MimeTypeInfo.Mp4, ".mp4", [".m4v"], FileTypeCategory.Video, "MPEG-4 video file");

    /// <summary>QuickTime movie (<c>video/quicktime</c>, <c>.mov</c> / <c>.qt</c>).</summary>
    public static readonly FileTypeInfo Mov = new("MOV", MimeTypeInfo.Mov, ".mov", [".qt"], FileTypeCategory.Video, "QuickTime movie file");

    /// <summary>Matroska video (<c>video/x-matroska</c>, <c>.mkv</c> / <c>.mk3d</c>).</summary>
    public static readonly FileTypeInfo Mkv = new("MKV", MimeTypeInfo.Mkv, ".mkv", [".mk3d"], FileTypeCategory.Video, "Matroska video file");

    /// <summary>Audio Video Interleave (<c>video/x-msvideo</c>, <c>.avi</c>).</summary>
    public static readonly FileTypeInfo Avi = new("AVI", MimeTypeInfo.Avi, ".avi", null, FileTypeCategory.Video, "Audio Video Interleave file");

    /// <summary>MPEG video (<c>video/mpeg</c>, <c>.mpeg</c> / <c>.mpg</c> / <c>.mpe</c> / <c>.m1v</c>).</summary>
    public static readonly FileTypeInfo Mpeg = new("MPEG", MimeTypeInfo.Mpeg, ".mpeg", [".mpg", ".mpe", ".m1v"], FileTypeCategory.Video, "MPEG video file");

    /// <summary>
    /// MPEG transport stream (<c>video/mp2t</c>, <c>.ts</c> / <c>.m2ts</c> / <c>.mts</c>). <c>.ts</c> is also TypeScript in
    /// <see cref="ProgrammingLanguageInfo" />; this catalog treats it as video for content-type and blob lookup.
    /// </summary>
    public static readonly FileTypeInfo MpegTs = new(
        "MPEG-TS", MimeTypeInfo.MpegTs, ".ts", [".m2ts", ".mts"], FileTypeCategory.Video, "MPEG transport stream");

    /// <summary>3GPP mobile video (<c>video/3gpp</c>, <c>.3gp</c> / <c>.3gpp</c>).</summary>
    public static readonly FileTypeInfo ThreeGp = new("3GP", MimeTypeInfo.ThreeGp, ".3gp", [".3gpp"], FileTypeCategory.Video, "3GPP mobile video file");

    /// <summary>HTTP Live Streaming playlist (<c>application/vnd.apple.mpegurl</c>, <c>.m3u8</c> / <c>.m3u</c>).</summary>
    public static readonly FileTypeInfo Hls = new("HLS", MimeTypeInfo.Hls, ".m3u8", [".m3u"], FileTypeCategory.Video, "HTTP Live Streaming playlist");

    /// <summary>Unique default extensions for stream algorithms from <c>Lyo.Compression.Models.CompressionAlgorithm.Extension</c> (GZip through XZ).</summary>
    public static readonly IReadOnlyList<string> StreamCompressionAlgorithmDefaultExtensions = [
        Gz.DefaultExtension, Brotli.DefaultExtension, ZLibStream.DefaultExtension, DeflateStream.DefaultExtension, SnappyStream.DefaultExtension, ZstdStream.DefaultExtension,
        LZ4Stream.DefaultExtension, LZMAStream.DefaultExtension, Bz2.DefaultExtension, Xz.DefaultExtension
    ];

    /// <summary>Same tokens as <see cref="StreamCompressionAlgorithmDefaultExtensions" />, sorted longest-first so nested suffixes can be stripped safely.</summary>
    public static readonly IReadOnlyList<string> StreamCompressionExtensionsLongestFirst =
        StreamCompressionAlgorithmDefaultExtensions.OrderByDescending(s => s.Length).ThenBy(s => s, StringComparer.Ordinal).ToArray();

    /// <summary>Encryption suffixes removed from a filename before compression is inferred (longest first; includes two-key and asymmetric variants).</summary>
    public static readonly IReadOnlyList<string> EncryptionFilenameStripSuffixesLongestFirst = [
        LyoXChaCha20Poly1305.DefaultExtension + TwoKeyEnvelopeSuffix, LyoChaCha20Poly1305.DefaultExtension + TwoKeyEnvelopeSuffix, LyoXChaCha20Poly1305.DefaultExtension,
        LyoChaCha20Poly1305.DefaultExtension, LyoAesGcmRsa.DefaultExtension + TwoKeyEnvelopeSuffix, LyoAesCcm.DefaultExtension + TwoKeyEnvelopeSuffix,
        LyoAesSiv.DefaultExtension + TwoKeyEnvelopeSuffix, LyoRsa.DefaultExtension + TwoKeyEnvelopeSuffix, LyoAesGcm.DefaultExtension + TwoKeyEnvelopeSuffix,
        LyoAesCcm.DefaultExtension, LyoAesSiv.DefaultExtension, LyoAesGcmRsa.DefaultExtension, LyoRsa.DefaultExtension, LyoAesGcm.DefaultExtension
    ];

    /// <summary>Stream-compression and Lyo ciphertext suffixes tried when a stored file has no explicit type metadata.</summary>
    public static readonly IReadOnlyList<string> CommonStorageResolutionSuffixes = StreamCompressionAlgorithmDefaultExtensions
        .Concat(
        [
            LyoAesGcm.DefaultExtension, LyoChaCha20Poly1305.DefaultExtension, LyoAesCcm.DefaultExtension, LyoAesSiv.DefaultExtension, LyoXChaCha20Poly1305.DefaultExtension,
            LyoAesGcmRsa.DefaultExtension, LyoRsa.DefaultExtension
        ])
        .Concat(LyoTwoKeyEnvelope.Extensions)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private static readonly Dictionary<string, FileTypeInfo> ByMimeValue = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, FileTypeInfo> ByExtension = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<FileTypeInfo> AllTypes = [];

    /// <summary>Display name, for example <c>PDF</c>.</summary>
    public string Name { get; }

    /// <summary>IANA media type for this file type.</summary>
    public MimeTypeInfo Mime { get; }

    /// <summary>Canonical MIME string; same as <see cref="MimeTypeInfo.Value" />.</summary>
    public string MimeType => Mime.Value;

    /// <summary>Canonical extension used for defaults and naming (lowercase, leading dot).</summary>
    public string DefaultExtension { get; }

    /// <summary>Alternate spellings that <see cref="FromExtension" /> still accepts.</summary>
    public string[] Aliases { get; }

    /// <summary>Extra MIME strings that <see cref="FromMimeType" /> maps to this type (legacy names, for example).</summary>
    public string[] MimeTypeAliases => Mime.Aliases;

    /// <summary><see cref="DefaultExtension" /> plus <see cref="Aliases" />.</summary>
    public string[] Extensions { get; }

    public FileTypeCategory Category { get; }

    public string Description { get; }

    /// <summary>Every registered file type in the catalog.</summary>
    public static IReadOnlyList<FileTypeInfo> All => AllTypes;

    static FileTypeInfo()
    {
        var fields = typeof(FileTypeInfo).PublicStaticFields<FileTypeInfo>();

        foreach (var fileType in fields) {
            AllTypes.Add(fileType);
            if (!ByMimeValue.ContainsKey(fileType.MimeType))
                ByMimeValue[fileType.MimeType] = fileType;

            foreach (var ext in fileType.Extensions)
                ByExtension[ext.ToLowerInvariant()] = fileType;
        }
    }

    /// <summary>Creates a catalog row. Prefer the static fields; this constructor exists so tests can add a one-off type.</summary>
    /// <param name="name">Display name.</param>
    /// <param name="mime">IANA media type. Several file types may share one row (for example <see cref="MimeTypeInfo.Unknown" />).</param>
    /// <param name="defaultExtension">Canonical extension, with or without a leading dot.</param>
    /// <param name="aliases">Alternate extensions that <see cref="FromExtension" /> still accepts.</param>
    /// <param name="category">File-type bucket.</param>
    /// <param name="description">Human-readable description.</param>
    public FileTypeInfo(string name, MimeTypeInfo mime, string defaultExtension, string[]? aliases, FileTypeCategory category, string description)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        ArgumentHelpers.ThrowIfNull(mime);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(defaultExtension);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(description);
        Name = name;
        Mime = mime;
        DefaultExtension = NormalizeExtension(defaultExtension);
        Aliases = aliases is { Length: > 0 } ? aliases.Select(NormalizeExtension).ToArray() : [];
        Extensions = Aliases.Length == 0 ? [DefaultExtension] : Enumerable.Repeat(DefaultExtension, 1).Concat(Aliases).ToArray();
        Category = category;
        Description = description;
    }

    public override string ToString() => $"{Name} ({MimeType})";

    /// <summary>
    /// <see cref="MimeType" /> plus a <c>; charset=</c> parameter, for HTTP <c>Content-Type</c> headers on text payloads (for example
    /// <c>text/csv; charset=utf-8</c>). Prefer this over concatenating the parameter at the call site.
    /// </summary>
    /// <param name="charset">Charset name; falls back to <see cref="Utf8Charset" />.</param>
    public string ContentType(string charset = Utf8Charset) => Mime.ContentType(charset);

    /// <summary>
    /// True when the payload is character data, so a <c>charset</c> parameter is meaningful. Delegates to <see cref="MimeTypeInfo.IsTextBased" />.
    /// </summary>
    public bool IsTextBased => Mime.IsTextBased;

    /// <summary>
    /// HTTP <c>Content-Type</c> header value: <see cref="ContentType(string)" /> for text payloads, bare <see cref="MimeType" /> for binary ones. Prefer this
    /// over choosing at the call site — appending <c>charset</c> to a PDF or spreadsheet is meaningless.
    /// </summary>
    public string HttpContentType => Mime.HttpContentType;

    private static string NormalizeExtension(string extension)
    {
        var t = extension.Trim();
        if (t.Length == 0)
            return ".";

        if (!t.StartsWith(".", StringComparison.Ordinal))
            t = "." + t;

        return t.ToLowerInvariant();
    }

    /// <summary>Looks up a catalog row by MIME string, including aliases and <c>Content-Type</c> parameters after the first <c>;</c>.</summary>
    /// <param name="mimeType">MIME token, for example <c>application/pdf</c> or <c>text/plain; charset=utf-8</c>.</param>
    /// <returns>The catalog row, or <see cref="Unknown" /> when the MIME is not registered.</returns>
    public static FileTypeInfo FromMimeType(string? mimeType) => FromMime(MimeTypeInfo.FromValue(mimeType));

    /// <summary>Looks up a catalog row by <see cref="MimeTypeInfo" />.</summary>
    /// <param name="mime">The media-type row.</param>
    /// <returns>The first file type registered with that MIME, or <see cref="Unknown" /> when none match.</returns>
    public static FileTypeInfo FromMime(MimeTypeInfo? mime)
        => mime == null || !ByMimeValue.TryGetValue(mime.Value, out var type) ? Unknown : type;

    /// <summary>Looks up a catalog row by extension.</summary>
    /// <param name="extension">Extension with or without a leading dot, for example <c>.pdf</c> or <c>pdf</c>.</param>
    /// <returns>The catalog row, or <see cref="Unknown" /> when the extension is not registered.</returns>
    public static FileTypeInfo FromExtension(string? extension)
    {
        if (extension.IsNullOrWhitespace())
            return Unknown;

        var normalized = extension.Trim();
        if (!normalized.StartsWith(".", StringComparison.Ordinal))
            normalized = "." + normalized;

        normalized = normalized.ToLowerInvariant();
        return ByExtension.TryGetValue(normalized, out var type) ? type : Unknown;
    }

    /// <summary>Lists catalog rows that belong to <paramref name="category" />.</summary>
    /// <param name="category">The file-type bucket.</param>
    /// <returns>Matching catalog rows.</returns>
    public static IEnumerable<FileTypeInfo> ByCategory(FileTypeCategory category) => AllTypes.Where(t => t.Category == category);

    /// <summary>Resolves a catalog row from a file path.</summary>
    /// <param name="filePath">The path.</param>
    /// <returns>The catalog row, or <see cref="Unknown" /> when unrecognized.</returns>
    public static FileTypeInfo FromFilePath(string? filePath)
    {
        if (filePath.IsNullOrWhitespace())
            return Unknown;

        var extension = Path.GetExtension(filePath);
        if (extension.IsNullOrWhitespace())
            extension = filePath.TrimStart('.');

        return FromExtension(extension);
    }

    /// <summary>Resolves a catalog row from a <see cref="FileInfo" />.</summary>
    /// <param name="fileInfo">The file metadata.</param>
    /// <returns>The catalog row, or <see cref="Unknown" /> when unrecognized.</returns>
    public static FileTypeInfo FromFileInfo(FileInfo? fileInfo) => fileInfo == null ? Unknown : FromFilePath(fileInfo.FullName);
}
