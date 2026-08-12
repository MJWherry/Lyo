using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using Lyo.Exceptions;
using Lyo.TextEncoding.Internal;

namespace Lyo.TextEncoding;

/// <summary>Static charset resolve / encode / decode / convert / detect helpers.</summary>
public static class CharsetEncoding
{
    private const int StreamBufferSize = 8192;
    private const int DetectSampleSize = 4096;

    private static readonly Regex DeclarationRegex = new(
        @"(?:encoding|charset)\s*=\s*[""']?(?<name>[A-Za-z0-9_\-.:]+)[""']?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
#if NET7_0_OR_GREATER
        | RegexOptions.NonBacktracking
#endif
        , TimeSpan.FromMilliseconds(250));

    /// <summary>Ensure code pages provider is registered (idempotent).</summary>
    public static void EnsureCodePagesRegistered() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    /// <summary>Resolve by web/.NET name or numeric code-page string. Throws if unknown.</summary>
    public static Encoding GetEncoding(string nameOrCodePage, CharsetEncodingOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(nameOrCodePage);
        options ??= CharsetEncodingOptions.Default;
        if (options.RegisterCodePages)
            EnsureCodePagesRegistered();

        try {
            Encoding encoding;
            if (int.TryParse(nameOrCodePage.Trim(), out var codePage))
                encoding = Encoding.GetEncoding(codePage);
            else if (CharsetInfo.FromWebName(nameOrCodePage) is { } info)
                encoding = info.ToEncoding();
            else
                encoding = Encoding.GetEncoding(nameOrCodePage.Trim());

            return ApplyFallbacks(encoding, options);
        }
        catch (ArgumentException ex) {
            throw new EncodingException($"Unknown charset '{nameOrCodePage}'.", ex);
        }
        catch (NotSupportedException ex) {
            throw new EncodingException($"Unsupported charset '{nameOrCodePage}'.", ex);
        }
    }

    /// <summary>Try resolve by name or code-page string.</summary>
    public static bool TryGetEncoding(string nameOrCodePage, out Encoding? encoding, CharsetEncodingOptions? options = null)
    {
        encoding = null;
        if (string.IsNullOrWhiteSpace(nameOrCodePage))
            return false;

        try {
            encoding = GetEncoding(nameOrCodePage, options);
            return true;
        }
        catch (EncodingException) {
            return false;
        }
    }

    /// <summary>Resolve by code page.</summary>
    public static Encoding GetEncoding(int codePage, CharsetEncodingOptions? options = null)
    {
        options ??= CharsetEncodingOptions.Default;
        if (options.RegisterCodePages)
            EnsureCodePagesRegistered();

        try {
            return ApplyFallbacks(Encoding.GetEncoding(codePage), options);
        }
        catch (ArgumentException ex) {
            throw new EncodingException($"Unknown code page {codePage}.", ex);
        }
        catch (NotSupportedException ex) {
            throw new EncodingException($"Unsupported code page {codePage}.", ex);
        }
    }

    /// <summary>Resolve from <see cref="CharsetInfo" /> with optional fallbacks from options.</summary>
    public static Encoding GetEncoding(CharsetInfo charset, CharsetEncodingOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(charset);
        options ??= CharsetEncodingOptions.Default;
        if (options.RegisterCodePages)
            EnsureCodePagesRegistered();

        return ApplyFallbacks(charset.ToEncoding(), options);
    }

    /// <summary>Map BCL encoding to catalog entry (well-known or custom).</summary>
    public static CharsetInfo ToCharsetInfo(Encoding encoding)
    {
        ArgumentHelpers.ThrowIfNull(encoding);
        return CharsetInfo.FromEncoding(encoding);
    }

    /// <summary>BOM sniff, then UTF-8 validity heuristic; else options default.</summary>
    public static CharsetDetectionResult DetectEncoding(ReadOnlySpan<byte> data, CharsetEncodingOptions? options = null)
    {
        options ??= CharsetEncodingOptions.Default;
        if (options.RegisterCodePages)
            EnsureCodePagesRegistered();

        if (TryDetectBom(data, out var bomEncoding)) {
            var applied = ApplyFallbacks(bomEncoding, options);
            return new() {
                Encoding = applied,
                Kind = CharsetDetectionKind.Bom,
                Charset = ToCharsetInfo(bomEncoding),
                ConsumedPrefix = []
            };
        }

        if (!data.IsEmpty && IsValidUtf8(data)) {
            var utf8 = ApplyFallbacks(Encoding.UTF8, options);
            return new() {
                Encoding = utf8,
                Kind = CharsetDetectionKind.Utf8Heuristic,
                Charset = CharsetInfo.Utf8,
                ConsumedPrefix = []
            };
        }

        var fallback = GetEncoding(options.DefaultCharset, options);
        return new() {
            Encoding = fallback,
            Kind = CharsetDetectionKind.Default,
            Charset = options.DefaultCharset,
            ConsumedPrefix = []
        };
    }

    /// <inheritdoc cref="DetectEncoding(ReadOnlySpan{byte}, CharsetEncodingOptions?)" />
    public static CharsetDetectionResult DetectEncoding(byte[] data, CharsetEncodingOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(data);
        return DetectEncoding(data.AsSpan(), options);
    }

    /// <summary>
    /// Detect from stream preamble. Seekable streams are rewound after peek (<see cref="CharsetDetectionResult.ConsumedPrefix" /> empty). Non-seekable streams leave the sample
    /// in <see cref="CharsetDetectionResult.ConsumedPrefix" /> — use <see cref="CreateReplayStream" />.
    /// </summary>
    public static CharsetDetectionResult DetectEncoding(Stream stream, CharsetEncodingOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        options ??= CharsetEncodingOptions.Default;
        var buffer = ArrayPool<byte>.Shared.Rent(DetectSampleSize);
        try {
            long? pos = stream.CanSeek ? stream.Position : null;
            var read = stream.Read(buffer, 0, DetectSampleSize);
            var result = DetectEncoding(buffer.AsSpan(0, read), options);
            if (pos is { } p) {
                stream.Position = p;
                return result;
            }

            var prefix = read == 0 ? [] : buffer.AsSpan(0, read).ToArray();
            return new() {
                Encoding = result.Encoding,
                Kind = result.Kind,
                Charset = result.Charset,
                DeclaredName = result.DeclaredName,
                ConsumedPrefix = prefix
            };
        }
        finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>Replay <see cref="CharsetDetectionResult.ConsumedPrefix" /> then the remainder of <paramref name="stream" />.</summary>
    public static Stream CreateReplayStream(Stream stream, CharsetDetectionResult detection, bool leaveOpen = true)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        ArgumentHelpers.ThrowIfNull(detection);
        if (detection.ConsumedPrefix is not { Length: > 0 })
            return stream;

        return new PrefixedStream(detection.ConsumedPrefix, stream, leaveOpen);
    }

    /// <summary>Create a write-through charset converting stream.</summary>
    public static CharsetConvertingStream CreateConvertingStream(Stream inner, Encoding from, Encoding to, bool leaveOpen = true, CharsetEncodingOptions? options = null)
        => new(inner, from, to, leaveOpen, options);

    /// <summary>Create a write-through charset converting stream.</summary>
    public static CharsetConvertingStream CreateConvertingStream(Stream inner, CharsetInfo from, CharsetInfo to, bool leaveOpen = true, CharsetEncodingOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(from);
        ArgumentHelpers.ThrowIfNull(to);
        return CreateConvertingStream(inner, GetEncoding(from, options), GetEncoding(to, options), leaveOpen, options);
    }

    /// <summary>Detect encoding of a file (reads a small sample).</summary>
    public static async Task<CharsetDetectionResult> DetectEncodingFileAsync(string path, CharsetEncodingOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        ArgumentHelpers.ThrowIfFileNotFound(path);
#if NET5_0_OR_GREATER
        await using var fs = File.OpenRead(path);
#else
        using var fs = File.OpenRead(path);
#endif
        var buffer = ArrayPool<byte>.Shared.Rent(DetectSampleSize);
        try {
#if NET5_0_OR_GREATER
            var read = await fs.ReadAsync(buffer.AsMemory(0, DetectSampleSize), ct).ConfigureAwait(false);
#else
            var read = await fs.ReadAsync(buffer, 0, DetectSampleSize, ct).ConfigureAwait(false);
#endif
            return DetectEncoding(buffer.AsSpan(0, read), options);
        }
        finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>Sniff charset label from already-decoded text (XML/HTML declaration).</summary>
    public static CharsetDetectionResult DetectEncodingFromText(string text, CharsetEncodingOptions? options = null)
    {
        if (!TryDetectEncodingFromText(text, out var result, options) || result is null)
            throw new EncodingException("No charset declaration found in text.");

        return result;
    }

    /// <summary>Try sniff charset label from already-decoded text.</summary>
    public static bool TryDetectEncodingFromText(string text, out CharsetDetectionResult? result, CharsetEncodingOptions? options = null)
    {
        result = null;
        if (string.IsNullOrEmpty(text))
            return false;

        options ??= CharsetEncodingOptions.Default;
        var sample = text.Length > DetectSampleSize ? text[..DetectSampleSize] : text;
        var match = DeclarationRegex.Match(sample);
        if (!match.Success)
            return false;

        var name = match.Groups["name"].Value;
        if (!TryGetEncoding(name, out var encoding, options) || encoding is null)
            return false;

        result = new() {
            Encoding = encoding,
            Kind = CharsetDetectionKind.TextDeclaration,
            Charset = CharsetInfo.FromWebName(name) ?? CharsetInfo.Custom(name, name),
            DeclaredName = name,
            ConsumedPrefix = []
        };

        return true;
    }

    /// <summary>Encode text to bytes.</summary>
    public static byte[] GetBytes(string text, Encoding? encoding = null, CharsetEncodingOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(text);
        return GetBytes(text.AsSpan(), encoding, options);
    }

    /// <summary>Encode text to bytes.</summary>
    public static byte[] GetBytes(string text, CharsetInfo charset, CharsetEncodingOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(text);
        ArgumentHelpers.ThrowIfNull(charset);
        return GetBytes(text.AsSpan(), GetEncoding(charset, options), options);
    }

    /// <summary>Encode characters to bytes.</summary>
    public static byte[] GetBytes(ReadOnlySpan<char> text, Encoding? encoding = null, CharsetEncodingOptions? options = null)
    {
        options ??= CharsetEncodingOptions.Default;
        encoding ??= GetEncoding(options.DefaultCharset, options);
#if NET5_0_OR_GREATER
        var max = encoding.GetMaxByteCount(text.Length);
        byte[]? rented = null;
        Span<byte> buffer = max <= 512 ? stackalloc byte[max] : (rented = ArrayPool<byte>.Shared.Rent(max));
        try {
            var written = encoding.GetBytes(text, buffer);
            return buffer[..written].ToArray();
        }
        finally {
            if (rented is not null)
                ArrayPool<byte>.Shared.Return(rented);
        }
#else
        return encoding.GetBytes(text.ToString());
#endif
    }

    /// <summary>Try encode into <paramref name="destination" />.</summary>
    public static bool TryGetBytes(ReadOnlySpan<char> text, Span<byte> destination, out int bytesWritten, Encoding? encoding = null, CharsetEncodingOptions? options = null)
    {
        bytesWritten = 0;
        options ??= CharsetEncodingOptions.Default;
        encoding ??= GetEncoding(options.DefaultCharset, options);
#if NET5_0_OR_GREATER
        if (encoding.GetMaxByteCount(text.Length) > destination.Length && encoding.GetByteCount(text) > destination.Length)
            return false;
        try {
            bytesWritten = encoding.GetBytes(text, destination);
            return true;
        }
        catch (ArgumentException) {
            return false;
        }
#else
        var bytes = encoding.GetBytes(text.ToString());
        if (destination.Length < bytes.Length)
            return false;

        bytes.CopyTo(destination);
        bytesWritten = bytes.Length;
        return true;
#endif
    }

    /// <summary>Decode bytes to string.</summary>
    public static string GetString(ReadOnlySpan<byte> bytes, Encoding? encoding = null, CharsetEncodingOptions? options = null)
    {
        options ??= CharsetEncodingOptions.Default;
        encoding ??= GetEncoding(options.DefaultCharset, options);
#if NET5_0_OR_GREATER
        return encoding.GetString(bytes);
#else
        return encoding.GetString(bytes.ToArray());
#endif
    }

    /// <inheritdoc cref="GetString(ReadOnlySpan{byte}, Encoding?, CharsetEncodingOptions?)" />
    public static string GetString(byte[] bytes, Encoding? encoding = null, CharsetEncodingOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(bytes);
        return GetString(bytes.AsSpan(), encoding, options);
    }

    /// <summary>Decode bytes using <paramref name="charset" />.</summary>
    public static string GetString(ReadOnlySpan<byte> bytes, CharsetInfo charset, CharsetEncodingOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(charset);
        return GetString(bytes, GetEncoding(charset, options), options);
    }

    /// <summary>Try decode into <paramref name="destination" />.</summary>
    public static bool TryGetString(ReadOnlySpan<byte> bytes, Span<char> destination, out int charsWritten, Encoding? encoding = null, CharsetEncodingOptions? options = null)
    {
        charsWritten = 0;
        options ??= CharsetEncodingOptions.Default;
        encoding ??= GetEncoding(options.DefaultCharset, options);
#if NET5_0_OR_GREATER
        if (encoding.GetMaxCharCount(bytes.Length) > destination.Length && encoding.GetCharCount(bytes) > destination.Length)
            return false;
        try {
            charsWritten = encoding.GetChars(bytes, destination);
            return true;
        }
        catch (ArgumentException) {
            return false;
        }
#else
        var s = encoding.GetString(bytes.ToArray());
        if (destination.Length < s.Length)
            return false;

        s.AsSpan().CopyTo(destination);
        charsWritten = s.Length;
        return true;
#endif
    }

    /// <summary>Read all text from a file.</summary>
    public static async Task<string> ReadAllTextAsync(
        string path,
        Encoding? encoding = null,
        bool detectEncodingFromBom = true,
        CharsetEncodingOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        ArgumentHelpers.ThrowIfFileNotFound(path);
        options ??= CharsetEncodingOptions.Default;
        encoding ??= GetEncoding(options.DefaultCharset, options);
#if NET5_0_OR_GREATER
        await using var fs = File.OpenRead(path);
#else
        using var fs = File.OpenRead(path);
#endif
        return await ReadToEndAsync(fs, encoding, detectEncodingFromBom, options, ct).ConfigureAwait(false);
    }

    /// <summary>Read all text from a file using <paramref name="charset" />.</summary>
    public static Task<string> ReadAllTextAsync(
        string path,
        CharsetInfo charset,
        bool detectEncodingFromBom = true,
        CharsetEncodingOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(charset);
        return ReadAllTextAsync(path, GetEncoding(charset, options), detectEncodingFromBom, options, ct);
    }

    /// <summary>Write all text to a file.</summary>
    public static async Task WriteAllTextAsync(
        string path,
        string text,
        Encoding? encoding = null,
        bool? emitBom = null,
        CharsetEncodingOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        ArgumentHelpers.ThrowIfNull(text);
        options ??= CharsetEncodingOptions.Default;
        encoding ??= GetEncoding(options.DefaultCharset, options);
        var writeBom = emitBom ?? options.EmitBom;
        var body = GetBytes(text.AsSpan(), encoding, options);
        var preamble = writeBom ? encoding.GetPreamble() : [];
        byte[] bytes;
        if (preamble.Length == 0)
            bytes = body;
        else {
            bytes = new byte[preamble.Length + body.Length];
            preamble.CopyTo(bytes, 0);
            body.CopyTo(bytes, preamble.Length);
        }

#if NET5_0_OR_GREATER
        await File.WriteAllBytesAsync(path, bytes, ct).ConfigureAwait(false);
#else
        File.WriteAllBytes(path, bytes);
        ct.ThrowIfCancellationRequested();
        await Task.CompletedTask.ConfigureAwait(false);
#endif
    }

    /// <summary>Write all text to a file using <paramref name="charset" />.</summary>
    public static Task WriteAllTextAsync(
        string path,
        string text,
        CharsetInfo charset,
        bool? emitBom = null,
        CharsetEncodingOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(charset);
        return WriteAllTextAsync(path, text, GetEncoding(charset, options), emitBom, options, ct);
    }

    /// <summary>Read stream to end as text. Does not close <paramref name="stream" />.</summary>
    public static async Task<string> ReadToEndAsync(
        Stream stream,
        Encoding? encoding = null,
        bool detectEncodingFromBom = true,
        CharsetEncodingOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        options ??= CharsetEncodingOptions.Default;
        encoding ??= GetEncoding(options.DefaultCharset, options);
        using var reader = new StreamReader(stream, encoding, detectEncodingFromBom, StreamBufferSize, true);
#if NET5_0_OR_GREATER
        return await reader.ReadToEndAsync(ct).ConfigureAwait(false);
#else
        var text = await reader.ReadToEndAsync().ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();
        return text;
#endif
    }

    /// <summary>Write text to stream. Does not close <paramref name="stream" /> when <paramref name="leaveOpen" /> is true.</summary>
    public static async Task WriteAsync(
        Stream stream,
        string text,
        Encoding? encoding = null,
        bool leaveOpen = true,
        bool? emitBom = null,
        CharsetEncodingOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        ArgumentHelpers.ThrowIfNull(text);
        options ??= CharsetEncodingOptions.Default;
        encoding ??= GetEncoding(options.DefaultCharset, options);
        var writeBom = emitBom ?? options.EmitBom;
        if (writeBom) {
            var preamble = encoding.GetPreamble();
            if (preamble.Length > 0) {
#if NET5_0_OR_GREATER
                await stream.WriteAsync(preamble.AsMemory(), ct).ConfigureAwait(false);
#else
                await stream.WriteAsync(preamble, 0, preamble.Length, ct).ConfigureAwait(false);
#endif
            }
        }

        // Write body as bytes so StreamWriter does not emit a second preamble.
        var body = GetBytes(text.AsSpan(), encoding, options);
#if NET5_0_OR_GREATER
        await stream.WriteAsync(body.AsMemory(), ct).ConfigureAwait(false);
#else
        await stream.WriteAsync(body, 0, body.Length, ct).ConfigureAwait(false);
#endif
        if (!leaveOpen)
            await stream.FlushAsync().ConfigureAwait(false);

        _ = leaveOpen;
    }

    /// <summary>Read all bytes from a file.</summary>
    public static async Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        ArgumentHelpers.ThrowIfFileNotFound(path);
#if NET5_0_OR_GREATER
        return await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
#else
        var bytes = File.ReadAllBytes(path);
        ct.ThrowIfCancellationRequested();
        return bytes;
#endif
    }

    /// <summary>Decode with <paramref name="from" /> then encode with <paramref name="to" />.</summary>
    public static byte[] Convert(ReadOnlySpan<byte> bytes, Encoding from, Encoding to, CharsetEncodingOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(from);
        ArgumentHelpers.ThrowIfNull(to);
        options ??= CharsetEncodingOptions.Default;
        from = ApplyFallbacks(from, options);
        to = ApplyFallbacks(to, options);
        var text = GetString(bytes, from, options);
        return GetBytes(text.AsSpan(), to, options);
    }

    /// <inheritdoc cref="Convert(ReadOnlySpan{byte}, Encoding, Encoding, CharsetEncodingOptions?)" />
    public static byte[] Convert(byte[] bytes, Encoding from, Encoding to, CharsetEncodingOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(bytes);
        return Convert(bytes.AsSpan(), from, to, options);
    }

    /// <summary>Convert using charset catalog entries.</summary>
    public static byte[] Convert(ReadOnlySpan<byte> bytes, CharsetInfo from, CharsetInfo to, CharsetEncodingOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(from);
        ArgumentHelpers.ThrowIfNull(to);
        return Convert(bytes, GetEncoding(from, options), GetEncoding(to, options), options);
    }

    /// <summary>Convert using name/code-page strings.</summary>
    public static byte[] Convert(ReadOnlySpan<byte> bytes, string fromNameOrCodePage, string toNameOrCodePage, CharsetEncodingOptions? options = null)
        => Convert(bytes, GetEncoding(fromNameOrCodePage, options), GetEncoding(toNameOrCodePage, options), options);

    /// <summary>Streaming convert with stateful encoder/decoder (sync). Does not close streams.</summary>
    public static void Convert(Stream input, Stream output, Encoding from, Encoding to, CharsetEncodingOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNull(output);
        ArgumentHelpers.ThrowIfNull(from);
        ArgumentHelpers.ThrowIfNull(to);
        options ??= CharsetEncodingOptions.Default;
        from = ApplyFallbacks(from, options);
        to = ApplyFallbacks(to, options);
        var decoder = from.GetDecoder();
        var encoder = to.GetEncoder();
        var byteIn = ArrayPool<byte>.Shared.Rent(StreamBufferSize);
        var charBuf = ArrayPool<char>.Shared.Rent(from.GetMaxCharCount(StreamBufferSize));
        var byteOut = ArrayPool<byte>.Shared.Rent(to.GetMaxByteCount(charBuf.Length));
        try {
            int read;
            while ((read = input.Read(byteIn, 0, StreamBufferSize)) > 0) {
                var chars = decoder.GetChars(byteIn, 0, read, charBuf, 0, false);
                var outLen = encoder.GetBytes(charBuf, 0, chars, byteOut, 0, false);
                output.Write(byteOut, 0, outLen);
            }

            var flushChars = decoder.GetChars(byteIn, 0, 0, charBuf, 0, true);
            var flushOut = encoder.GetBytes(charBuf, 0, flushChars, byteOut, 0, true);
            if (flushOut > 0)
                output.Write(byteOut, 0, flushOut);
        }
        finally {
            ArrayPool<byte>.Shared.Return(byteIn);
            ArrayPool<char>.Shared.Return(charBuf);
            ArrayPool<byte>.Shared.Return(byteOut);
        }
    }

    /// <summary>Streaming convert using charset catalog entries (sync).</summary>
    public static void Convert(Stream input, Stream output, CharsetInfo from, CharsetInfo to, CharsetEncodingOptions? options = null)
        => Convert(input, output, GetEncoding(from, options), GetEncoding(to, options), options);

    /// <summary>Streaming convert with stateful encoder/decoder. Does not close streams.</summary>
    public static async Task ConvertAsync(Stream input, Stream output, Encoding from, Encoding to, CharsetEncodingOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNull(output);
        ArgumentHelpers.ThrowIfNull(from);
        ArgumentHelpers.ThrowIfNull(to);
        options ??= CharsetEncodingOptions.Default;
        from = ApplyFallbacks(from, options);
        to = ApplyFallbacks(to, options);
        var decoder = from.GetDecoder();
        var encoder = to.GetEncoder();
        var byteIn = ArrayPool<byte>.Shared.Rent(StreamBufferSize);
        var charBuf = ArrayPool<char>.Shared.Rent(from.GetMaxCharCount(StreamBufferSize));
        var byteOut = ArrayPool<byte>.Shared.Rent(to.GetMaxByteCount(charBuf.Length));
        try {
            int read;
#if NET5_0_OR_GREATER
            while ((read = await input.ReadAsync(byteIn.AsMemory(0, StreamBufferSize), ct).ConfigureAwait(false)) > 0) {
#else
            while ((read = await input.ReadAsync(byteIn, 0, StreamBufferSize, ct).ConfigureAwait(false)) > 0) {
#endif
                var chars = decoder.GetChars(byteIn, 0, read, charBuf, 0, false);
                var outLen = encoder.GetBytes(charBuf, 0, chars, byteOut, 0, false);
#if NET5_0_OR_GREATER
                await output.WriteAsync(byteOut.AsMemory(0, outLen), ct).ConfigureAwait(false);
#else
                await output.WriteAsync(byteOut, 0, outLen, ct).ConfigureAwait(false);
#endif
            }

            var flushChars = decoder.GetChars(byteIn, 0, 0, charBuf, 0, true);
            var flushOut = encoder.GetBytes(charBuf, 0, flushChars, byteOut, 0, true);
            if (flushOut > 0) {
#if NET5_0_OR_GREATER
                await output.WriteAsync(byteOut.AsMemory(0, flushOut), ct).ConfigureAwait(false);
#else
                await output.WriteAsync(byteOut, 0, flushOut, ct).ConfigureAwait(false);
#endif
            }
        }
        finally {
            ArrayPool<byte>.Shared.Return(byteIn);
            ArrayPool<char>.Shared.Return(charBuf);
            ArrayPool<byte>.Shared.Return(byteOut);
        }
    }

    /// <summary>Streaming convert using charset catalog entries.</summary>
    public static Task ConvertAsync(Stream input, Stream output, CharsetInfo from, CharsetInfo to, CharsetEncodingOptions? options = null, CancellationToken ct = default)
        => ConvertAsync(input, output, GetEncoding(from, options), GetEncoding(to, options), options, ct);

    /// <summary>Convert file using BCL encodings.</summary>
    public static async Task ConvertFileAsync(
        string inputPath,
        string outputPath,
        Encoding from,
        Encoding to,
        CharsetEncodingOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentHelpers.ThrowIfFileNotFound(inputPath);
#if NET5_0_OR_GREATER
        await using var input = File.OpenRead(inputPath);
        await using var output = File.Create(outputPath);
#else
        using var input = File.OpenRead(inputPath);
        using var output = File.Create(outputPath);
#endif
        await ConvertAsync(input, output, from, to, options, ct).ConfigureAwait(false);
    }

    /// <summary>Convert file using charset catalog entries.</summary>
    public static Task ConvertFileAsync(
        string inputPath,
        string outputPath,
        CharsetInfo from,
        CharsetInfo to,
        CharsetEncodingOptions? options = null,
        CancellationToken ct = default)
        => ConvertFileAsync(inputPath, outputPath, GetEncoding(from, options), GetEncoding(to, options), options, ct);

    /// <summary>Convert file using name/code-page strings.</summary>
    public static Task ConvertFileAsync(
        string inputPath,
        string outputPath,
        string fromNameOrCodePage,
        string toNameOrCodePage,
        CharsetEncodingOptions? options = null,
        CancellationToken ct = default)
        => ConvertFileAsync(inputPath, outputPath, GetEncoding(fromNameOrCodePage, options), GetEncoding(toNameOrCodePage, options), options, ct);

    /// <summary>Clone <paramref name="encoding" /> when options specify fallbacks (does not mutate BCL singletons).</summary>
    internal static Encoding ApplyFallbacks(Encoding encoding, CharsetEncodingOptions options)
    {
        if (options.EncoderFallback is null && options.DecoderFallback is null)
            return encoding;

        var clone = (Encoding)encoding.Clone();
        if (options.EncoderFallback is { } ef)
            clone.EncoderFallback = ef;

        if (options.DecoderFallback is { } df)
            clone.DecoderFallback = df;

        return clone;
    }

    private static bool TryDetectBom(ReadOnlySpan<byte> data, out Encoding encoding)
    {
        encoding = Encoding.UTF8;
        if (data.Length >= 4) {
            if (data[0] == 0x00 && data[1] == 0x00 && data[2] == 0xFE && data[3] == 0xFF) {
                encoding = Encoding.UTF32; // BE via GetEncoding
                encoding = Encoding.GetEncoding(12001);
                return true;
            }

            if (data[0] == 0xFF && data[1] == 0xFE && data[2] == 0x00 && data[3] == 0x00) {
                encoding = Encoding.UTF32;
                return true;
            }
        }

        if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF) {
            encoding = Encoding.UTF8;
            return true;
        }

        if (data.Length >= 2) {
            if (data[0] == 0xFF && data[1] == 0xFE) {
                encoding = Encoding.Unicode;
                return true;
            }

            if (data[0] == 0xFE && data[1] == 0xFF) {
                encoding = Encoding.BigEndianUnicode;
                return true;
            }
        }

        return false;
    }

    private static bool IsValidUtf8(ReadOnlySpan<byte> data)
    {
        var encoding = (Encoding)Encoding.UTF8.Clone();
        encoding.DecoderFallback = DecoderFallback.ExceptionFallback;
        try {
#if NET5_0_OR_GREATER
            encoding.GetCharCount(data);
#else
            encoding.GetCharCount(data.ToArray());
#endif
            return true;
        }
        catch (DecoderFallbackException) {
            return false;
        }
    }
}