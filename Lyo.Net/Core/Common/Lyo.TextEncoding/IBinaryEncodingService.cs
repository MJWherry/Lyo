namespace Lyo.TextEncoding;

/// <summary>
/// Injectable façade for binary↔text codecs (Base64, Base64Url, Hex) over buffers, streams, and files. Prefer <see cref="BinaryEncoding" /> for static call sites; inject
/// when defaults or test doubles matter.
/// </summary>
/// <remarks>Stream overloads do not close the stream.</remarks>
public interface IBinaryEncodingService
{
    /// <summary>Encode bytes to text.</summary>
    string Encode(BinaryEncodingKind kind, ReadOnlySpan<byte> data);

    /// <inheritdoc cref="Encode(BinaryEncodingKind, ReadOnlySpan{byte})" />
    string Encode(BinaryEncodingKind kind, byte[] data);

    /// <summary>Encode to end-of-stream (materializes). Does not close <paramref name="stream" />.</summary>
    string Encode(BinaryEncodingKind kind, Stream stream);

    /// <summary>Encode file contents to text.</summary>
    Task<string> EncodeFileAsync(BinaryEncodingKind kind, string path, CancellationToken ct = default);

    /// <summary>Chunked encode: binary in → text out.</summary>
    Task EncodeAsync(BinaryEncodingKind kind, Stream input, TextWriter output, CancellationToken ct = default);

    /// <summary>Encode input file to output text file.</summary>
    Task EncodeFileAsync(BinaryEncodingKind kind, string inputPath, string outputPath, CancellationToken ct = default);

    /// <summary>Decode encoded text to bytes.</summary>
    byte[] Decode(BinaryEncodingKind kind, ReadOnlySpan<char> encoded);

    /// <inheritdoc cref="Decode(BinaryEncodingKind, ReadOnlySpan{char})" />
    byte[] Decode(BinaryEncodingKind kind, string encoded);

    /// <summary>Decode encoded text from stream (materializes). Does not close <paramref name="encodedStream" />.</summary>
    byte[] Decode(BinaryEncodingKind kind, Stream encodedStream);

    /// <summary>Decode encoded text file to bytes (streaming).</summary>
    Task<byte[]> DecodeFileAsync(BinaryEncodingKind kind, string path, CancellationToken ct = default);

    /// <summary>Streaming decode: text in → binary out.</summary>
    Task DecodeAsync(BinaryEncodingKind kind, TextReader input, Stream output, CancellationToken ct = default);

    /// <summary>Decode encoded text file to binary output file (streaming).</summary>
    Task DecodeFileAsync(BinaryEncodingKind kind, string inputPath, string outputPath, CancellationToken ct = default);

    /// <summary>PEM-armor Base64 with BEGIN/END labels.</summary>
    string EncodePem(string label, ReadOnlySpan<byte> data);

    /// <summary>Decode PEM-armored Base64.</summary>
    byte[] DecodePem(ReadOnlySpan<char> text, out string label);

    /// <summary>Try decode PEM-armored Base64.</summary>
    bool TryDecodePem(ReadOnlySpan<char> text, out string? label, out byte[]? data);

    /// <summary>Maximum encoded character count for <paramref name="byteCount" /> input bytes.</summary>
    int GetMaxEncodedCharCount(BinaryEncodingKind kind, int byteCount);

    /// <summary>Maximum decoded byte count for <paramref name="charCount" /> encoded characters.</summary>
    int GetMaxDecodedByteCount(BinaryEncodingKind kind, int charCount);

    /// <summary>Try encode into <paramref name="destination" /> without allocating the result string.</summary>
    bool TryEncode(BinaryEncodingKind kind, ReadOnlySpan<byte> data, Span<char> destination, out int charsWritten);

    /// <summary>Try decode into <paramref name="destination" /> without allocating the result array.</summary>
    bool TryDecode(BinaryEncodingKind kind, ReadOnlySpan<char> encoded, Span<byte> destination, out int bytesWritten);

    /// <summary>Try decode; allocates result array on success.</summary>
    bool TryDecode(BinaryEncodingKind kind, ReadOnlySpan<char> encoded, out byte[]? data);
}