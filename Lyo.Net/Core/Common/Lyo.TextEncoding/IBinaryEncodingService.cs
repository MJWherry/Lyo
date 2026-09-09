namespace Lyo.TextEncoding;

/// <summary>
/// Injectable facade for binary-to-text codecs (Base64, Base64Url, Hex) over buffers, streams, and files. Prefer <see cref="BinaryEncoding" /> for static call sites; inject
/// when defaults or test doubles matter.
/// </summary>
/// <remarks>Stream overloads do not close the stream.</remarks>
public interface IBinaryEncodingService
{
    /// <summary>Encodes bytes to text.</summary>
    string Encode(BinaryEncodingKind kind, ReadOnlySpan<byte> data);

    /// <inheritdoc cref="Encode(BinaryEncodingKind, ReadOnlySpan{byte})" />
    string Encode(BinaryEncodingKind kind, byte[] data);

    /// <summary>Encodes to end-of-stream (materializes). Does not close <paramref name="stream" />.</summary>
    string Encode(BinaryEncodingKind kind, Stream stream);

    /// <summary>Encodes file contents to text.</summary>
    Task<string> EncodeFileAsync(BinaryEncodingKind kind, string path, CancellationToken ct = default);

    /// <summary>Chunked encode: binary in to text out.</summary>
    Task EncodeAsync(BinaryEncodingKind kind, Stream input, TextWriter output, CancellationToken ct = default);

    /// <summary>Encodes an input file to an output text file.</summary>
    Task EncodeFileAsync(BinaryEncodingKind kind, string inputPath, string outputPath, CancellationToken ct = default);

    /// <summary>Decodes encoded text to bytes.</summary>
    byte[] Decode(BinaryEncodingKind kind, ReadOnlySpan<char> encoded);

    /// <inheritdoc cref="Decode(BinaryEncodingKind, ReadOnlySpan{char})" />
    byte[] Decode(BinaryEncodingKind kind, string encoded);

    /// <summary>Decodes encoded text from a stream (materializes). Does not close <paramref name="encodedStream" />.</summary>
    byte[] Decode(BinaryEncodingKind kind, Stream encodedStream);

    /// <summary>Decodes an encoded text file to bytes (streaming).</summary>
    Task<byte[]> DecodeFileAsync(BinaryEncodingKind kind, string path, CancellationToken ct = default);

    /// <summary>Streaming decode: text in to binary out.</summary>
    Task DecodeAsync(BinaryEncodingKind kind, TextReader input, Stream output, CancellationToken ct = default);

    /// <summary>Decodes an encoded text file to a binary output file (streaming).</summary>
    Task DecodeFileAsync(BinaryEncodingKind kind, string inputPath, string outputPath, CancellationToken ct = default);

    /// <summary>PEM-armors Base64 with BEGIN/END labels.</summary>
    string EncodePem(string label, ReadOnlySpan<byte> data);

    /// <summary>Decodes PEM-armored Base64.</summary>
    byte[] DecodePem(ReadOnlySpan<char> text, out string label);

    /// <summary>Tries to decode PEM-armored Base64.</summary>
    bool TryDecodePem(ReadOnlySpan<char> text, out string? label, out byte[]? data);

    /// <summary>Largest encoded character count for <paramref name="byteCount" /> input bytes.</summary>
    int GetMaxEncodedCharCount(BinaryEncodingKind kind, int byteCount);

    /// <summary>Largest decoded byte count for <paramref name="charCount" /> encoded characters.</summary>
    int GetMaxDecodedByteCount(BinaryEncodingKind kind, int charCount);

    /// <summary>Tries to encode into <paramref name="destination" /> without allocating the result string.</summary>
    bool TryEncode(BinaryEncodingKind kind, ReadOnlySpan<byte> data, Span<char> destination, out int charsWritten);

    /// <summary>Tries to decode into <paramref name="destination" /> without allocating the result array.</summary>
    bool TryDecode(BinaryEncodingKind kind, ReadOnlySpan<char> encoded, Span<byte> destination, out int bytesWritten);

    /// <summary>Tries to decode; allocates the result array on success.</summary>
    bool TryDecode(BinaryEncodingKind kind, ReadOnlySpan<char> encoded, out byte[]? data);
}