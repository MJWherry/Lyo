using System.Diagnostics;
using Lyo.Common.Core;
using Lyo.Common.Core.Extensions;
using Lyo.Exceptions;

namespace Lyo.Common.Metadata.Records;

/// <summary>
/// Catalog row for an IANA media type. File extensions live on <see cref="FileTypeInfo" />; this type is the MIME string, its aliases, and HTTP
/// <c>Content-Type</c> helpers. Several file types may share one row (for example <see cref="Unknown" /> for <c>application/octet-stream</c>).
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class MimeTypeInfo
{
    /// <summary>Default UTF-8 charset token appended by <see cref="ContentType(string)" /> when the caller omits a charset.</summary>
    public const string Utf8Charset = "utf-8";

    /// <summary>application/octet-stream — unknown, raw binary, and generic encrypted payloads share this row.</summary>
    public static readonly MimeTypeInfo Unknown = new("application/octet-stream");

    public static readonly MimeTypeInfo Pdf = new("application/pdf", ["application/x-pdf"]);
    public static readonly MimeTypeInfo Doc = new("application/msword", ["application/x-msword"]);
    public static readonly MimeTypeInfo Docx = new("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
    public static readonly MimeTypeInfo Xls = new("application/vnd.ms-excel", ["application/excel", "application/x-excel", "application/x-msexcel"]);
    public static readonly MimeTypeInfo Xlsx = new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

    public static readonly MimeTypeInfo Csv = new("text/csv", ["text/comma-separated-values", "application/csv"]);
    public static readonly MimeTypeInfo Txt = new("text/plain");
    public static readonly MimeTypeInfo Tex = new("application/x-tex");
    public static readonly MimeTypeInfo Json = new("application/json", ["text/json", "application/x-json"]);
    public static readonly MimeTypeInfo Xml = new("application/xml", ["text/xml"]);
    public static readonly MimeTypeInfo ProblemJson = new("application/problem+json");

    public static readonly MimeTypeInfo Html = new("text/html");
    public static readonly MimeTypeInfo JavaScript = new(
        "application/javascript", ["text/javascript", "application/x-javascript", "application/ecmascript", "text/ecmascript"]);
    public static readonly MimeTypeInfo Graphql = new("application/graphql");
    public static readonly MimeTypeInfo WwwFormUrlEncoded = new("application/x-www-form-urlencoded");

    public static readonly MimeTypeInfo Dump = new("application/x-dump");

    public static readonly MimeTypeInfo Jpeg = new("image/jpeg", ["image/jpg", "image/pjpeg"]);
    public static readonly MimeTypeInfo Png = new("image/png", ["image/x-png"]);
    public static readonly MimeTypeInfo Gif = new("image/gif");
    public static readonly MimeTypeInfo Bmp = new("image/bmp", ["image/x-ms-bmp", "image/x-bmp"]);
    public static readonly MimeTypeInfo Svg = new("image/svg+xml", ["image/svg"]);
    public static readonly MimeTypeInfo Tiff = new("image/tiff", ["image/tif"]);
    public static readonly MimeTypeInfo Webp = new("image/webp");
    public static readonly MimeTypeInfo Ico = new("image/vnd.microsoft.icon", ["image/x-icon"]);
    public static readonly MimeTypeInfo Avif = new("image/avif");

    public static readonly MimeTypeInfo Zip = new("application/zip", ["application/x-zip-compressed"]);
    public static readonly MimeTypeInfo Rar = new("application/x-rar-compressed", ["application/vnd.rar"]);
    public static readonly MimeTypeInfo SevenZip = new("application/x-7z-compressed");
    public static readonly MimeTypeInfo Tar = new("application/x-tar", ["application/gtar"]);
    public static readonly MimeTypeInfo Gz = new("application/gzip", ["application/x-gzip"]);
    public static readonly MimeTypeInfo Bz2 = new("application/x-bzip2", ["application/bzip2", "application/x-bzip"]);
    public static readonly MimeTypeInfo Xz = new("application/x-xz");
    public static readonly MimeTypeInfo Brotli = new("application/x-brotli", ["application/brotli"]);
    public static readonly MimeTypeInfo ZLibStream = new("application/zlib");
    public static readonly MimeTypeInfo DeflateStream = new("application/x-deflate");
    public static readonly MimeTypeInfo SnappyStream = new("application/x-snappy-framed");
    public static readonly MimeTypeInfo ZstdStream = new("application/zstd", ["application/x-zstd"]);
    public static readonly MimeTypeInfo LZ4Stream = new("application/x-lz4");
    public static readonly MimeTypeInfo LZMAStream = new("application/x-lzma");

    public static readonly MimeTypeInfo NuGetPackage = new("application/x-nupkg");
    public static readonly MimeTypeInfo NuGetSymbolsPackage = new("application/x-snupkg");
    public static readonly MimeTypeInfo JavaJar = new("application/java-archive");
    public static readonly MimeTypeInfo JavaWar = new("application/x-java-war");
    public static readonly MimeTypeInfo JavaEar = new("application/x-java-ear");
    public static readonly MimeTypeInfo AndroidAar = new("application/vnd.android.aar");
    public static readonly MimeTypeInfo DebianPackage = new("application/vnd.debian.binary-package", ["application/x-debian-package"]);
    public static readonly MimeTypeInfo RpmPackage = new(
        "application/x-rpm", ["application/redhat-package-manager", "application/x-redhat-package-manager"]);
    public static readonly MimeTypeInfo WindowsInstallerMsi = new("application/x-msi");

    public static readonly MimeTypeInfo Gpg = new("application/pgp-encrypted");
    public static readonly MimeTypeInfo LyoAesGcm = new("application/x-lyo-ciphertext-aes-gcm");
    public static readonly MimeTypeInfo LyoChaCha20Poly1305 = new("application/x-lyo-ciphertext-chacha20-poly1305");
    public static readonly MimeTypeInfo LyoAesCcm = new("application/x-lyo-ciphertext-aes-ccm");
    public static readonly MimeTypeInfo LyoAesSiv = new("application/x-lyo-ciphertext-aes-siv");
    public static readonly MimeTypeInfo LyoXChaCha20Poly1305 = new("application/x-lyo-ciphertext-xchacha20-poly1305");
    public static readonly MimeTypeInfo LyoAesGcmRsa = new("application/x-lyo-ciphertext-aes-gcm-rsa");
    public static readonly MimeTypeInfo LyoRsa = new("application/x-lyo-ciphertext-rsa");
    public static readonly MimeTypeInfo LyoTwoKeyEnvelope = new("application/x-lyo-two-key-envelope");

    public static readonly MimeTypeInfo Wav = new("audio/wav", ["audio/x-wav", "audio/wave"]);
    public static readonly MimeTypeInfo Mp3 = new("audio/mpeg", ["audio/mp3", "audio/x-mpeg", "audio/x-mp3"]);
    public static readonly MimeTypeInfo Ogg = new("audio/ogg", ["application/ogg"]);
    public static readonly MimeTypeInfo Flac = new("audio/flac", ["audio/x-flac"]);
    public static readonly MimeTypeInfo Aac = new("audio/aac", ["audio/x-aac"]);
    public static readonly MimeTypeInfo M4a = new("audio/mp4", ["audio/m4a", "audio/x-m4a"]);
    public static readonly MimeTypeInfo Opus = new("audio/opus");
    public static readonly MimeTypeInfo Pcm = new("audio/pcm");
    public static readonly MimeTypeInfo AudioWebm = new("audio/webm");

    public static readonly MimeTypeInfo Mp4 = new("video/mp4", ["video/mpeg4", "application/mp4"]);
    public static readonly MimeTypeInfo Mov = new("video/quicktime");
    public static readonly MimeTypeInfo Mkv = new("video/x-matroska", ["video/matroska"]);
    public static readonly MimeTypeInfo Avi = new("video/x-msvideo", ["video/avi", "video/msvideo"]);
    public static readonly MimeTypeInfo Mpeg = new("video/mpeg", ["video/mpg"]);
    public static readonly MimeTypeInfo MpegTs = new("video/mp2t");
    public static readonly MimeTypeInfo ThreeGp = new("video/3gpp", ["video/3gp"]);
    public static readonly MimeTypeInfo Hls = new(
        "application/vnd.apple.mpegurl", ["application/x-mpegurl", "audio/mpegurl", "audio/x-mpegurl"]);

    private static readonly Dictionary<string, MimeTypeInfo> ByValue = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<MimeTypeInfo> AllTypes = [];

    /// <summary>Canonical IANA token, for example <c>video/mp4</c>.</summary>
    public string Value { get; }

    /// <summary>Legacy or vendor tokens that <see cref="FromValue" /> still maps to this row.</summary>
    public string[] Aliases { get; }

    /// <summary>Every registered media type. Duplicate field aliases (if any) appear once.</summary>
    public static IReadOnlyList<MimeTypeInfo> All => AllTypes;

    static MimeTypeInfo()
    {
        var fields = typeof(MimeTypeInfo).PublicStaticFields<MimeTypeInfo>();

        foreach (var mime in fields) {
            if (!ByValue.ContainsKey(mime.Value))
                AllTypes.Add(mime);

            ByValue[mime.Value] = mime;
            foreach (var alias in mime.Aliases)
                ByValue[alias] = mime;
        }
    }

    /// <summary>Creates a catalog row. Prefer the static fields; constructed instances are not registered in <see cref="FromValue" />.</summary>
    /// <param name="value">Canonical IANA token.</param>
    /// <param name="aliases">Optional extra tokens that should resolve to this row.</param>
    public MimeTypeInfo(string value, string[]? aliases = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
        Aliases = aliases is { Length: > 0 }
            ? aliases.Select(static a => a.Trim()).Where(static a => a.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            : [];
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>
    /// True when the payload is character data, so a <c>charset</c> parameter is meaningful. Covers <c>text/*</c> plus structured text types under
    /// <c>application/</c> (JSON, XML, JavaScript, GraphQL, and the <c>+json</c> / <c>+xml</c> suffixes from RFC 6839).
    /// </summary>
    public bool IsTextBased
        => Value.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
            || Value.EndsWith("+json", StringComparison.OrdinalIgnoreCase)
            || Value.EndsWith("+xml", StringComparison.OrdinalIgnoreCase)
            || Value is "application/json" or "application/xml" or "application/javascript" or "application/graphql" or "application/x-www-form-urlencoded";

    /// <summary>
    /// <see cref="Value" /> plus a <c>; charset=</c> parameter, for HTTP <c>Content-Type</c> headers on text payloads (for example
    /// <c>text/csv; charset=utf-8</c>). Prefer this over concatenating the parameter at the call site.
    /// </summary>
    /// <param name="charset">Charset name; falls back to <see cref="Utf8Charset" />.</param>
    public string ContentType(string charset = Utf8Charset) => charset.IsNullOrWhitespace() ? Value : $"{Value}; charset={charset.Trim()}";

    /// <summary>
    /// HTTP <c>Content-Type</c> header value: <see cref="ContentType(string)" /> for text payloads, bare <see cref="Value" /> for binary ones. Prefer this
    /// over choosing at the call site — appending <c>charset</c> to a PDF or spreadsheet is meaningless.
    /// </summary>
    public string HttpContentType => IsTextBased ? ContentType() : Value;

    /// <summary>Looks up a catalog row by MIME token, including aliases. Strips RFC 6838 parameters after the first <c>;</c>.</summary>
    /// <param name="mimeType">MIME token, for example <c>application/pdf</c> or <c>text/plain; charset=utf-8</c>.</param>
    /// <returns>The catalog row, or <see cref="Unknown" /> when the token is not registered.</returns>
    public static MimeTypeInfo FromValue(string? mimeType)
    {
        if (mimeType.IsNullOrWhitespace())
            return Unknown;

        var trimmed = mimeType.Trim();
        var semicolon = trimmed.IndexOf(';');
        if (semicolon >= 0)
            trimmed = trimmed[..semicolon].Trim();

        return trimmed.Length > 0 && ByValue.TryGetValue(trimmed, out var mime) ? mime : Unknown;
    }
}
