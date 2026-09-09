using System.Text;

namespace Lyo.TextEncoding;

/// <summary>
/// Injectable facade for character-set encode/decode/convert over buffers, streams, and files. Resolves any encoding registered with <see cref="Encoding" /> (CodePages
/// included when enabled). Prefer <see cref="CharsetEncoding" /> for static call sites; inject when defaults or test doubles matter.
/// </summary>
/// <remarks>
/// Code pages are registered on construction when options allow. Stream overloads do not close the stream unless noted. Charset names accept web names, .NET names, or
/// numeric code-page strings (for example "1252").
/// </remarks>
public interface ICharsetEncodingService
{
    /// <summary>Resolves by web/.NET name or code-page number string. Throws if unknown.</summary>
    Encoding GetEncoding(string nameOrCodePage);

    /// <summary>Tries to resolve by name or code-page string.</summary>
    bool TryGetEncoding(string nameOrCodePage, out Encoding? encoding);

    /// <summary>Resolves by code page.</summary>
    Encoding GetEncoding(int codePage);

    /// <summary>Resolves from <see cref="CharsetInfo" />.</summary>
    Encoding GetEncoding(CharsetInfo charset);

    /// <summary>Maps a detected or resolved BCL encoding to a well-known <see cref="CharsetInfo" /> when possible; else <see cref="CharsetInfo.Custom" />.</summary>
    CharsetInfo ToCharsetInfo(Encoding encoding);

    /// <summary>BOM sniff, then a UTF-8 validity heuristic; otherwise the options default.</summary>
    CharsetDetectionResult DetectEncoding(ReadOnlySpan<byte> data);

    /// <inheritdoc cref="DetectEncoding(ReadOnlySpan{byte})" />
    CharsetDetectionResult DetectEncoding(byte[] data);

    /// <summary>Detects from the stream preamble. Seekable streams are rewound; non-seekable fill <see cref="CharsetDetectionResult.ConsumedPrefix" />.</summary>
    CharsetDetectionResult DetectEncoding(Stream stream);

    /// <summary>Replays the consumed prefix then the rest of <paramref name="stream" />.</summary>
    Stream CreateReplayStream(Stream stream, CharsetDetectionResult detection, bool leaveOpen = true);

    /// <summary>Mints a write-through charset converting stream.</summary>
    CharsetConvertingStream CreateConvertingStream(Stream inner, Encoding from, Encoding to, bool leaveOpen = true);

    /// <summary>Mints a write-through charset converting stream.</summary>
    CharsetConvertingStream CreateConvertingStream(Stream inner, CharsetInfo from, CharsetInfo to, bool leaveOpen = true);

    /// <summary>Detects the encoding of a file (reads a small sample).</summary>
    Task<CharsetDetectionResult> DetectEncodingFileAsync(string path, CancellationToken ct = default);

    /// <summary>Sniffs a charset label from already-decoded text (XML/HTML declaration).</summary>
    CharsetDetectionResult DetectEncodingFromText(string text);

    /// <summary>Tries to sniff a charset label from already-decoded text.</summary>
    bool TryDetectEncodingFromText(string text, out CharsetDetectionResult? result);

    /// <summary>Encodes text to bytes.</summary>
    byte[] GetBytes(string text, Encoding? encoding = null);

    /// <summary>Encodes text using <paramref name="charset" />.</summary>
    byte[] GetBytes(string text, CharsetInfo charset);

    /// <summary>Encodes characters to bytes.</summary>
    byte[] GetBytes(ReadOnlySpan<char> text, Encoding? encoding = null);

    /// <summary>Tries to encode into <paramref name="destination" />.</summary>
    bool TryGetBytes(ReadOnlySpan<char> text, Span<byte> destination, out int bytesWritten, Encoding? encoding = null);

    /// <summary>Decodes bytes to a string.</summary>
    string GetString(ReadOnlySpan<byte> bytes, Encoding? encoding = null);

    /// <inheritdoc cref="GetString(ReadOnlySpan{byte}, Encoding?)" />
    string GetString(byte[] bytes, Encoding? encoding = null);

    /// <summary>Decodes bytes using <paramref name="charset" />.</summary>
    string GetString(ReadOnlySpan<byte> bytes, CharsetInfo charset);

    /// <summary>Tries to decode into <paramref name="destination" />.</summary>
    bool TryGetString(ReadOnlySpan<byte> bytes, Span<char> destination, out int charsWritten, Encoding? encoding = null);

    /// <summary>Reads all text from a file.</summary>
    Task<string> ReadAllTextAsync(string path, Encoding? encoding = null, bool detectEncodingFromBom = true, CancellationToken ct = default);

    /// <summary>Reads all text from a file using <paramref name="charset" />.</summary>
    Task<string> ReadAllTextAsync(string path, CharsetInfo charset, bool detectEncodingFromBom = true, CancellationToken ct = default);

    /// <summary>Writes all text to a file.</summary>
    Task WriteAllTextAsync(string path, string text, Encoding? encoding = null, bool? emitBom = null, CancellationToken ct = default);

    /// <summary>Writes all text to a file using <paramref name="charset" />.</summary>
    Task WriteAllTextAsync(string path, string text, CharsetInfo charset, bool? emitBom = null, CancellationToken ct = default);

    /// <summary>Reads a stream to the end as text. Does not close <paramref name="stream" />.</summary>
    Task<string> ReadToEndAsync(Stream stream, Encoding? encoding = null, bool detectEncodingFromBom = true, CancellationToken ct = default);

    /// <summary>Writes text to a stream.</summary>
    Task WriteAsync(Stream stream, string text, Encoding? encoding = null, bool leaveOpen = true, bool? emitBom = null, CancellationToken ct = default);

    /// <summary>Reads all bytes from a file.</summary>
    Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct = default);

    /// <summary>Decodes with <paramref name="from" /> then encodes with <paramref name="to" />.</summary>
    byte[] Convert(ReadOnlySpan<byte> bytes, Encoding from, Encoding to);

    /// <summary>Converts using charset catalog entries.</summary>
    byte[] Convert(ReadOnlySpan<byte> bytes, CharsetInfo from, CharsetInfo to);

    /// <inheritdoc cref="Convert(ReadOnlySpan{byte}, Encoding, Encoding)" />
    byte[] Convert(byte[] bytes, Encoding from, Encoding to);

    /// <summary>Converts a stream (sync). Does not close streams.</summary>
    void Convert(Stream input, Stream output, Encoding from, Encoding to);

    /// <summary>Converts a stream using charset catalog entries (sync).</summary>
    void Convert(Stream input, Stream output, CharsetInfo from, CharsetInfo to);

    /// <summary>Converts a stream. Does not close streams.</summary>
    Task ConvertAsync(Stream input, Stream output, Encoding from, Encoding to, CancellationToken ct = default);

    /// <summary>Converts a stream using charset catalog entries.</summary>
    Task ConvertAsync(Stream input, Stream output, CharsetInfo from, CharsetInfo to, CancellationToken ct = default);

    /// <summary>Converts a file using BCL encodings.</summary>
    Task ConvertFileAsync(string inputPath, string outputPath, Encoding from, Encoding to, CancellationToken ct = default);

    /// <summary>Converts a file using charset catalog entries.</summary>
    Task ConvertFileAsync(string inputPath, string outputPath, CharsetInfo from, CharsetInfo to, CancellationToken ct = default);

    /// <summary>Converts using name or code-page strings.</summary>
    byte[] Convert(ReadOnlySpan<byte> bytes, string fromNameOrCodePage, string toNameOrCodePage);

    /// <summary>Converts a file using name or code-page strings.</summary>
    Task ConvertFileAsync(string inputPath, string outputPath, string fromNameOrCodePage, string toNameOrCodePage, CancellationToken ct = default);
}