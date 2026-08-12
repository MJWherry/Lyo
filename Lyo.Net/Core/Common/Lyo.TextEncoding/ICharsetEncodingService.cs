using System.Text;

namespace Lyo.TextEncoding;

/// <summary>
/// Injectable façade for character-set encode/decode/convert over buffers, streams, and files. Resolves any encoding registered with <see cref="Encoding" /> (CodePages
/// included when enabled). Prefer <see cref="CharsetEncoding" /> for static call sites; inject when defaults or test doubles matter.
/// </summary>
/// <remarks>
/// Code pages are registered on construction when options allow. Stream overloads do not close the stream unless noted. Charset names accept web names, .NET names, or
/// numeric code-page strings (e.g. "1252").
/// </remarks>
public interface ICharsetEncodingService
{
    /// <summary>Resolve by web/.NET name or code-page number string. Throws if unknown.</summary>
    Encoding GetEncoding(string nameOrCodePage);

    /// <summary>Try resolve by name or code-page string.</summary>
    bool TryGetEncoding(string nameOrCodePage, out Encoding? encoding);

    /// <summary>Resolve by code page.</summary>
    Encoding GetEncoding(int codePage);

    /// <summary>Resolve from <see cref="CharsetInfo" />.</summary>
    Encoding GetEncoding(CharsetInfo charset);

    /// <summary>Map detected/resolved BCL encoding to a well-known <see cref="CharsetInfo" /> when possible; else <see cref="CharsetInfo.Custom" />.</summary>
    CharsetInfo ToCharsetInfo(Encoding encoding);

    /// <summary>BOM sniff, then UTF-8 validity heuristic; else options default.</summary>
    CharsetDetectionResult DetectEncoding(ReadOnlySpan<byte> data);

    /// <inheritdoc cref="DetectEncoding(ReadOnlySpan{byte})" />
    CharsetDetectionResult DetectEncoding(byte[] data);

    /// <summary>Detect from stream preamble. Seekable streams are rewound; non-seekable fill <see cref="CharsetDetectionResult.ConsumedPrefix" />.</summary>
    CharsetDetectionResult DetectEncoding(Stream stream);

    /// <summary>Replay consumed prefix then the remainder of <paramref name="stream" />.</summary>
    Stream CreateReplayStream(Stream stream, CharsetDetectionResult detection, bool leaveOpen = true);

    /// <summary>Create a write-through charset converting stream.</summary>
    CharsetConvertingStream CreateConvertingStream(Stream inner, Encoding from, Encoding to, bool leaveOpen = true);

    /// <summary>Create a write-through charset converting stream.</summary>
    CharsetConvertingStream CreateConvertingStream(Stream inner, CharsetInfo from, CharsetInfo to, bool leaveOpen = true);

    /// <summary>Detect encoding of a file (reads a small sample).</summary>
    Task<CharsetDetectionResult> DetectEncodingFileAsync(string path, CancellationToken ct = default);

    /// <summary>Sniff charset label from already-decoded text (XML/HTML declaration).</summary>
    CharsetDetectionResult DetectEncodingFromText(string text);

    /// <summary>Try sniff charset label from already-decoded text.</summary>
    bool TryDetectEncodingFromText(string text, out CharsetDetectionResult? result);

    /// <summary>Encode text to bytes.</summary>
    byte[] GetBytes(string text, Encoding? encoding = null);

    /// <summary>Encode text using <paramref name="charset" />.</summary>
    byte[] GetBytes(string text, CharsetInfo charset);

    /// <summary>Encode characters to bytes.</summary>
    byte[] GetBytes(ReadOnlySpan<char> text, Encoding? encoding = null);

    /// <summary>Try encode into <paramref name="destination" />.</summary>
    bool TryGetBytes(ReadOnlySpan<char> text, Span<byte> destination, out int bytesWritten, Encoding? encoding = null);

    /// <summary>Decode bytes to string.</summary>
    string GetString(ReadOnlySpan<byte> bytes, Encoding? encoding = null);

    /// <inheritdoc cref="GetString(ReadOnlySpan{byte}, Encoding?)" />
    string GetString(byte[] bytes, Encoding? encoding = null);

    /// <summary>Decode bytes using <paramref name="charset" />.</summary>
    string GetString(ReadOnlySpan<byte> bytes, CharsetInfo charset);

    /// <summary>Try decode into <paramref name="destination" />.</summary>
    bool TryGetString(ReadOnlySpan<byte> bytes, Span<char> destination, out int charsWritten, Encoding? encoding = null);

    /// <summary>Read all text from a file.</summary>
    Task<string> ReadAllTextAsync(string path, Encoding? encoding = null, bool detectEncodingFromBom = true, CancellationToken ct = default);

    /// <summary>Read all text from a file using <paramref name="charset" />.</summary>
    Task<string> ReadAllTextAsync(string path, CharsetInfo charset, bool detectEncodingFromBom = true, CancellationToken ct = default);

    /// <summary>Write all text to a file.</summary>
    Task WriteAllTextAsync(string path, string text, Encoding? encoding = null, bool? emitBom = null, CancellationToken ct = default);

    /// <summary>Write all text to a file using <paramref name="charset" />.</summary>
    Task WriteAllTextAsync(string path, string text, CharsetInfo charset, bool? emitBom = null, CancellationToken ct = default);

    /// <summary>Read stream to end as text. Does not close <paramref name="stream" />.</summary>
    Task<string> ReadToEndAsync(Stream stream, Encoding? encoding = null, bool detectEncodingFromBom = true, CancellationToken ct = default);

    /// <summary>Write text to stream.</summary>
    Task WriteAsync(Stream stream, string text, Encoding? encoding = null, bool leaveOpen = true, bool? emitBom = null, CancellationToken ct = default);

    /// <summary>Read all bytes from a file.</summary>
    Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct = default);

    /// <summary>Decode with <paramref name="from" /> then encode with <paramref name="to" />.</summary>
    byte[] Convert(ReadOnlySpan<byte> bytes, Encoding from, Encoding to);

    /// <summary>Convert using charset catalog entries.</summary>
    byte[] Convert(ReadOnlySpan<byte> bytes, CharsetInfo from, CharsetInfo to);

    /// <inheritdoc cref="Convert(ReadOnlySpan{byte}, Encoding, Encoding)" />
    byte[] Convert(byte[] bytes, Encoding from, Encoding to);

    /// <summary>Streaming convert (sync). Does not close streams.</summary>
    void Convert(Stream input, Stream output, Encoding from, Encoding to);

    /// <summary>Streaming convert using charset catalog entries (sync).</summary>
    void Convert(Stream input, Stream output, CharsetInfo from, CharsetInfo to);

    /// <summary>Streaming convert. Does not close streams.</summary>
    Task ConvertAsync(Stream input, Stream output, Encoding from, Encoding to, CancellationToken ct = default);

    /// <summary>Streaming convert using charset catalog entries.</summary>
    Task ConvertAsync(Stream input, Stream output, CharsetInfo from, CharsetInfo to, CancellationToken ct = default);

    /// <summary>Convert file using BCL encodings.</summary>
    Task ConvertFileAsync(string inputPath, string outputPath, Encoding from, Encoding to, CancellationToken ct = default);

    /// <summary>Convert file using charset catalog entries.</summary>
    Task ConvertFileAsync(string inputPath, string outputPath, CharsetInfo from, CharsetInfo to, CancellationToken ct = default);

    /// <summary>Convert using name/code-page strings.</summary>
    byte[] Convert(ReadOnlySpan<byte> bytes, string fromNameOrCodePage, string toNameOrCodePage);

    /// <summary>Convert file using name/code-page strings.</summary>
    Task ConvertFileAsync(string inputPath, string outputPath, string fromNameOrCodePage, string toNameOrCodePage, CancellationToken ct = default);
}