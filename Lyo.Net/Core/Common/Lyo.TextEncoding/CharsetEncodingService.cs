using System.Text;

namespace Lyo.TextEncoding;

/// <inheritdoc cref="ICharsetEncodingService" />
/// <seealso cref="Shared" />
public sealed class CharsetEncodingService : ICharsetEncodingService
{
    private readonly CharsetEncodingOptions _options;

    /// <summary>Singleton with <see cref="CharsetEncodingOptions.Default" />.</summary>
    public static CharsetEncodingService Shared { get; } = new(CharsetEncodingOptions.Default);

    /// <summary>Create a service with optional options (registers CodePages when enabled).</summary>
    public CharsetEncodingService(CharsetEncodingOptions? options = null)
    {
        _options = options ?? CharsetEncodingOptions.Default;
        if (_options.RegisterCodePages)
            CharsetEncoding.EnsureCodePagesRegistered();
    }

    /// <inheritdoc />
    public Encoding GetEncoding(string nameOrCodePage) => CharsetEncoding.GetEncoding(nameOrCodePage, _options);

    /// <inheritdoc />
    public bool TryGetEncoding(string nameOrCodePage, out Encoding? encoding) => CharsetEncoding.TryGetEncoding(nameOrCodePage, out encoding, _options);

    /// <inheritdoc />
    public Encoding GetEncoding(int codePage) => CharsetEncoding.GetEncoding(codePage, _options);

    /// <inheritdoc />
    public Encoding GetEncoding(CharsetInfo charset) => CharsetEncoding.GetEncoding(charset, _options);

    /// <inheritdoc />
    public CharsetInfo ToCharsetInfo(Encoding encoding) => CharsetEncoding.ToCharsetInfo(encoding);

    /// <inheritdoc />
    public CharsetDetectionResult DetectEncoding(ReadOnlySpan<byte> data) => CharsetEncoding.DetectEncoding(data, _options);

    /// <inheritdoc />
    public CharsetDetectionResult DetectEncoding(byte[] data) => CharsetEncoding.DetectEncoding(data, _options);

    /// <inheritdoc />
    public CharsetDetectionResult DetectEncoding(Stream stream) => CharsetEncoding.DetectEncoding(stream, _options);

    /// <inheritdoc />
    public Stream CreateReplayStream(Stream stream, CharsetDetectionResult detection, bool leaveOpen = true) => CharsetEncoding.CreateReplayStream(stream, detection, leaveOpen);

    /// <inheritdoc />
    public CharsetConvertingStream CreateConvertingStream(Stream inner, Encoding from, Encoding to, bool leaveOpen = true)
        => CharsetEncoding.CreateConvertingStream(inner, from, to, leaveOpen, _options);

    /// <inheritdoc />
    public CharsetConvertingStream CreateConvertingStream(Stream inner, CharsetInfo from, CharsetInfo to, bool leaveOpen = true)
        => CharsetEncoding.CreateConvertingStream(inner, from, to, leaveOpen, _options);

    /// <inheritdoc />
    public Task<CharsetDetectionResult> DetectEncodingFileAsync(string path, CancellationToken ct = default) => CharsetEncoding.DetectEncodingFileAsync(path, _options, ct);

    /// <inheritdoc />
    public CharsetDetectionResult DetectEncodingFromText(string text) => CharsetEncoding.DetectEncodingFromText(text, _options);

    /// <inheritdoc />
    public bool TryDetectEncodingFromText(string text, out CharsetDetectionResult? result) => CharsetEncoding.TryDetectEncodingFromText(text, out result, _options);

    /// <inheritdoc />
    public byte[] GetBytes(string text, Encoding? encoding = null) => CharsetEncoding.GetBytes(text, encoding, _options);

    /// <inheritdoc />
    public byte[] GetBytes(string text, CharsetInfo charset) => CharsetEncoding.GetBytes(text, charset, _options);

    /// <inheritdoc />
    public byte[] GetBytes(ReadOnlySpan<char> text, Encoding? encoding = null) => CharsetEncoding.GetBytes(text, encoding, _options);

    /// <inheritdoc />
    public bool TryGetBytes(ReadOnlySpan<char> text, Span<byte> destination, out int bytesWritten, Encoding? encoding = null)
        => CharsetEncoding.TryGetBytes(text, destination, out bytesWritten, encoding, _options);

    /// <inheritdoc />
    public string GetString(ReadOnlySpan<byte> bytes, Encoding? encoding = null) => CharsetEncoding.GetString(bytes, encoding, _options);

    /// <inheritdoc />
    public string GetString(byte[] bytes, Encoding? encoding = null) => CharsetEncoding.GetString(bytes, encoding, _options);

    /// <inheritdoc />
    public string GetString(ReadOnlySpan<byte> bytes, CharsetInfo charset) => CharsetEncoding.GetString(bytes, charset, _options);

    /// <inheritdoc />
    public bool TryGetString(ReadOnlySpan<byte> bytes, Span<char> destination, out int charsWritten, Encoding? encoding = null)
        => CharsetEncoding.TryGetString(bytes, destination, out charsWritten, encoding, _options);

    /// <inheritdoc />
    public Task<string> ReadAllTextAsync(string path, Encoding? encoding = null, bool detectEncodingFromBom = true, CancellationToken ct = default)
        => CharsetEncoding.ReadAllTextAsync(path, encoding, detectEncodingFromBom, _options, ct);

    /// <inheritdoc />
    public Task<string> ReadAllTextAsync(string path, CharsetInfo charset, bool detectEncodingFromBom = true, CancellationToken ct = default)
        => CharsetEncoding.ReadAllTextAsync(path, charset, detectEncodingFromBom, _options, ct);

    /// <inheritdoc />
    public Task WriteAllTextAsync(string path, string text, Encoding? encoding = null, bool? emitBom = null, CancellationToken ct = default)
        => CharsetEncoding.WriteAllTextAsync(path, text, encoding, emitBom, _options, ct);

    /// <inheritdoc />
    public Task WriteAllTextAsync(string path, string text, CharsetInfo charset, bool? emitBom = null, CancellationToken ct = default)
        => CharsetEncoding.WriteAllTextAsync(path, text, charset, emitBom, _options, ct);

    /// <inheritdoc />
    public Task<string> ReadToEndAsync(Stream stream, Encoding? encoding = null, bool detectEncodingFromBom = true, CancellationToken ct = default)
        => CharsetEncoding.ReadToEndAsync(stream, encoding, detectEncodingFromBom, _options, ct);

    /// <inheritdoc />
    public Task WriteAsync(Stream stream, string text, Encoding? encoding = null, bool leaveOpen = true, bool? emitBom = null, CancellationToken ct = default)
        => CharsetEncoding.WriteAsync(stream, text, encoding, leaveOpen, emitBom, _options, ct);

    /// <inheritdoc />
    public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct = default) => CharsetEncoding.ReadAllBytesAsync(path, ct);

    /// <inheritdoc />
    public byte[] Convert(ReadOnlySpan<byte> bytes, Encoding from, Encoding to) => CharsetEncoding.Convert(bytes, from, to, _options);

    /// <inheritdoc />
    public byte[] Convert(ReadOnlySpan<byte> bytes, CharsetInfo from, CharsetInfo to) => CharsetEncoding.Convert(bytes, from, to, _options);

    /// <inheritdoc />
    public byte[] Convert(byte[] bytes, Encoding from, Encoding to) => CharsetEncoding.Convert(bytes, from, to, _options);

    /// <inheritdoc />
    public void Convert(Stream input, Stream output, Encoding from, Encoding to) => CharsetEncoding.Convert(input, output, from, to, _options);

    /// <inheritdoc />
    public void Convert(Stream input, Stream output, CharsetInfo from, CharsetInfo to) => CharsetEncoding.Convert(input, output, from, to, _options);

    /// <inheritdoc />
    public Task ConvertAsync(Stream input, Stream output, Encoding from, Encoding to, CancellationToken ct = default)
        => CharsetEncoding.ConvertAsync(input, output, from, to, _options, ct);

    /// <inheritdoc />
    public Task ConvertAsync(Stream input, Stream output, CharsetInfo from, CharsetInfo to, CancellationToken ct = default)
        => CharsetEncoding.ConvertAsync(input, output, from, to, _options, ct);

    /// <inheritdoc />
    public Task ConvertFileAsync(string inputPath, string outputPath, Encoding from, Encoding to, CancellationToken ct = default)
        => CharsetEncoding.ConvertFileAsync(inputPath, outputPath, from, to, _options, ct);

    /// <inheritdoc />
    public Task ConvertFileAsync(string inputPath, string outputPath, CharsetInfo from, CharsetInfo to, CancellationToken ct = default)
        => CharsetEncoding.ConvertFileAsync(inputPath, outputPath, from, to, _options, ct);

    /// <inheritdoc />
    public byte[] Convert(ReadOnlySpan<byte> bytes, string fromNameOrCodePage, string toNameOrCodePage)
        => CharsetEncoding.Convert(bytes, fromNameOrCodePage, toNameOrCodePage, _options);

    /// <inheritdoc />
    public Task ConvertFileAsync(string inputPath, string outputPath, string fromNameOrCodePage, string toNameOrCodePage, CancellationToken ct = default)
        => CharsetEncoding.ConvertFileAsync(inputPath, outputPath, fromNameOrCodePage, toNameOrCodePage, _options, ct);
}