namespace Lyo.TextEncoding;

/// <inheritdoc cref="IBinaryEncodingService" />
/// <seealso cref="Shared" />
public sealed class BinaryEncodingService(BinaryEncodingOptions? options = null) : IBinaryEncodingService
{
    private readonly BinaryEncodingOptions _options = options ?? BinaryEncodingOptions.Default;

    /// <summary>Singleton with <see cref="BinaryEncodingOptions.Default" />.</summary>
    public static BinaryEncodingService Shared { get; } = new(BinaryEncodingOptions.Default);

    /// <inheritdoc />
    public string Encode(BinaryEncodingKind kind, ReadOnlySpan<byte> data)
        => BinaryEncoding.Encode(kind, data, _options.DefaultHexLetterCase, _options.LineLength);

    /// <inheritdoc />
    public string Encode(BinaryEncodingKind kind, byte[] data)
        => BinaryEncoding.Encode(kind, data, _options.DefaultHexLetterCase, _options.LineLength);

    /// <inheritdoc />
    public string Encode(BinaryEncodingKind kind, Stream stream)
        => BinaryEncoding.Encode(kind, stream, _options.DefaultHexLetterCase, _options.LineLength);

    /// <inheritdoc />
    public Task<string> EncodeFileAsync(BinaryEncodingKind kind, string path, CancellationToken ct = default)
        => BinaryEncoding.EncodeFileAsync(kind, path, _options.DefaultHexLetterCase, _options.LineLength, ct);

    /// <inheritdoc />
    public Task EncodeAsync(BinaryEncodingKind kind, Stream input, TextWriter output, CancellationToken ct = default)
        => BinaryEncoding.EncodeAsync(kind, input, output, _options.DefaultHexLetterCase, _options.LineLength, ct);

    /// <inheritdoc />
    public Task EncodeFileAsync(BinaryEncodingKind kind, string inputPath, string outputPath, CancellationToken ct = default)
        => BinaryEncoding.EncodeFileAsync(kind, inputPath, outputPath, _options.DefaultHexLetterCase, _options.LineLength, ct);

    /// <inheritdoc />
    public byte[] Decode(BinaryEncodingKind kind, ReadOnlySpan<char> encoded)
        => BinaryEncoding.Decode(kind, encoded);

    /// <inheritdoc />
    public byte[] Decode(BinaryEncodingKind kind, string encoded)
        => BinaryEncoding.Decode(kind, encoded);

    /// <inheritdoc />
    public byte[] Decode(BinaryEncodingKind kind, Stream encodedStream)
        => BinaryEncoding.Decode(kind, encodedStream);

    /// <inheritdoc />
    public Task<byte[]> DecodeFileAsync(BinaryEncodingKind kind, string path, CancellationToken ct = default)
        => BinaryEncoding.DecodeFileAsync(kind, path, ct);

    /// <inheritdoc />
    public Task DecodeAsync(BinaryEncodingKind kind, TextReader input, Stream output, CancellationToken ct = default)
        => BinaryEncoding.DecodeAsync(kind, input, output, ct);

    /// <inheritdoc />
    public Task DecodeFileAsync(BinaryEncodingKind kind, string inputPath, string outputPath, CancellationToken ct = default)
        => BinaryEncoding.DecodeFileAsync(kind, inputPath, outputPath, ct);

    /// <inheritdoc />
    public string EncodePem(string label, ReadOnlySpan<byte> data)
        => BinaryEncoding.EncodePem(label, data);

    /// <inheritdoc />
    public byte[] DecodePem(ReadOnlySpan<char> text, out string label)
        => BinaryEncoding.DecodePem(text, out label);

    /// <inheritdoc />
    public bool TryDecodePem(ReadOnlySpan<char> text, out string? label, out byte[]? data)
        => BinaryEncoding.TryDecodePem(text, out label, out data);

    /// <inheritdoc />
    public int GetMaxEncodedCharCount(BinaryEncodingKind kind, int byteCount)
        => BinaryEncoding.GetMaxEncodedCharCount(kind, byteCount);

    /// <inheritdoc />
    public int GetMaxDecodedByteCount(BinaryEncodingKind kind, int charCount)
        => BinaryEncoding.GetMaxDecodedByteCount(kind, charCount);

    /// <inheritdoc />
    public bool TryEncode(BinaryEncodingKind kind, ReadOnlySpan<byte> data, Span<char> destination, out int charsWritten)
        => BinaryEncoding.TryEncode(kind, data, destination, out charsWritten, _options.DefaultHexLetterCase);

    /// <inheritdoc />
    public bool TryDecode(BinaryEncodingKind kind, ReadOnlySpan<char> encoded, Span<byte> destination, out int bytesWritten)
        => BinaryEncoding.TryDecode(kind, encoded, destination, out bytesWritten);

    /// <inheritdoc />
    public bool TryDecode(BinaryEncodingKind kind, ReadOnlySpan<char> encoded, out byte[]? data)
        => BinaryEncoding.TryDecode(kind, encoded, out data);
}
