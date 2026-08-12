using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using Lyo.Common.Enums;
using Lyo.Exceptions;

namespace Lyo.TextEncoding;

/// <summary>Static binary↔text codecs (Base64, Base64Url, Hex) with span-first hot paths.</summary>
public static class BinaryEncoding
{
    private const int StreamBufferSize = 8192;
    private const int MimeLineLength = 76;

    /// <summary>Maximum encoded character count for <paramref name="byteCount" /> input bytes (without line wraps).</summary>
    public static int GetMaxEncodedCharCount(BinaryEncodingKind kind, int byteCount)
    {
        if (byteCount < 0)
            throw new ArgumentOutOfRangeException(nameof(byteCount));

        return kind switch {
            BinaryEncodingKind.Base64 => (byteCount + 2) / 3 * 4,
            BinaryEncodingKind.Base64Url => (byteCount + 2) / 3 * 4,
            BinaryEncodingKind.Hex => checked(byteCount * 2),
            var _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    /// <summary>Maximum decoded byte count for <paramref name="charCount" /> encoded characters.</summary>
    public static int GetMaxDecodedByteCount(BinaryEncodingKind kind, int charCount)
    {
        if (charCount < 0)
            throw new ArgumentOutOfRangeException(nameof(charCount));

        return kind switch {
            BinaryEncodingKind.Base64 or BinaryEncodingKind.Base64Url => charCount / 4 * 3 + 3,
            BinaryEncodingKind.Hex => charCount / 2,
            var _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    /// <summary>Encode bytes to text. Empty input yields <see cref="string.Empty" />.</summary>
    public static string Encode(BinaryEncodingKind kind, ReadOnlySpan<byte> data, TextLetterCase hexLetterCase = TextLetterCase.Upper, int lineLength = 0)
    {
        if (data.IsEmpty)
            return string.Empty;

        var max = GetMaxEncodedCharCount(kind, data.Length);
        char[]? rented = null;
        var buffer = max <= 512 ? stackalloc char[max] : rented = ArrayPool<char>.Shared.Rent(max);
        try {
            if (!TryEncode(kind, data, buffer, out var written, hexLetterCase))
                throw new EncodingException("Failed to encode binary payload.");

            return ApplyLineWrap(buffer[..written], lineLength, kind);
        }
        finally {
            if (rented is not null)
                ArrayPool<char>.Shared.Return(rented);
        }
    }

    /// <inheritdoc cref="Encode(BinaryEncodingKind, ReadOnlySpan{byte}, TextLetterCase, int)" />
    public static string Encode(BinaryEncodingKind kind, byte[] data, TextLetterCase hexLetterCase = TextLetterCase.Upper, int lineLength = 0)
    {
        ArgumentHelpers.ThrowIfNull(data);
        return Encode(kind, data.AsSpan(), hexLetterCase, lineLength);
    }

    /// <summary>Encode stream to end (materializes). Does not close <paramref name="stream" />.</summary>
    public static string Encode(BinaryEncodingKind kind, Stream stream, TextLetterCase hexLetterCase = TextLetterCase.Upper, int lineLength = 0)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
#if NET5_0_OR_GREATER
        return Encode(kind, ms.TryGetBuffer(out var seg) ? seg.AsSpan() : ms.ToArray().AsSpan(), hexLetterCase, lineLength);
#else
        return Encode(kind, ms.ToArray().AsSpan(), hexLetterCase, lineLength);
#endif
    }

    /// <summary>
    /// Try encode into <paramref name="destination" />. Line wrapping is not applied (use <see cref="Encode(BinaryEncodingKind, ReadOnlySpan{byte}, TextLetterCase, int)" /> for
    /// wrapped output).
    /// </summary>
    public static bool TryEncode(
        BinaryEncodingKind kind,
        ReadOnlySpan<byte> data,
        Span<char> destination,
        out int charsWritten,
        TextLetterCase hexLetterCase = TextLetterCase.Upper,
        int lineLength = 0)
    {
        _ = lineLength; // wrapping requires expanding destination; convenience Encode applies wrap after
        charsWritten = 0;
        if (data.IsEmpty)
            return true;

        return kind switch {
            BinaryEncodingKind.Base64 => TryEncodeBase64(data, destination, out charsWritten, false),
            BinaryEncodingKind.Base64Url => TryEncodeBase64(data, destination, out charsWritten, true),
            BinaryEncodingKind.Hex => TryEncodeHex(data, destination, out charsWritten, hexLetterCase),
            var _ => false
        };
    }

    /// <summary>Decode encoded text to bytes. Whitespace is ignored for Base64/Base64Url.</summary>
    public static byte[] Decode(BinaryEncodingKind kind, ReadOnlySpan<char> encoded)
    {
        if (encoded.IsEmpty)
            return [];

        var max = GetMaxDecodedByteCount(kind, encoded.Length);
        byte[]? rented = null;
        var buffer = max <= 512 ? stackalloc byte[max] : rented = ArrayPool<byte>.Shared.Rent(max);
        try {
            if (!TryDecode(kind, encoded, buffer, out var written))
                throw new FormatException("Invalid encoded payload.");

            return buffer[..written].ToArray();
        }
        finally {
            if (rented is not null)
                ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <inheritdoc cref="Decode(BinaryEncodingKind, ReadOnlySpan{char})" />
    public static byte[] Decode(BinaryEncodingKind kind, string encoded)
    {
        ArgumentHelpers.ThrowIfNull(encoded);
        return Decode(kind, encoded.AsSpan());
    }

    /// <summary>Try decode into <paramref name="destination" />.</summary>
    public static bool TryDecode(BinaryEncodingKind kind, ReadOnlySpan<char> encoded, Span<byte> destination, out int bytesWritten)
    {
        bytesWritten = 0;
        if (encoded.IsEmpty)
            return true;

        return kind switch {
            BinaryEncodingKind.Base64 => TryDecodeBase64(encoded, destination, out bytesWritten, false),
            BinaryEncodingKind.Base64Url => TryDecodeBase64(encoded, destination, out bytesWritten, true),
            BinaryEncodingKind.Hex => TryDecodeHex(encoded, destination, out bytesWritten),
            var _ => false
        };
    }

    /// <summary>Try decode; allocates result array on success.</summary>
    public static bool TryDecode(BinaryEncodingKind kind, ReadOnlySpan<char> encoded, out byte[]? data)
    {
        data = null;
        if (encoded.IsEmpty) {
            data = [];
            return true;
        }

        var max = GetMaxDecodedByteCount(kind, encoded.Length);
        byte[]? rented = null;
        var buffer = max <= 512 ? stackalloc byte[max] : rented = ArrayPool<byte>.Shared.Rent(max);
        try {
            if (!TryDecode(kind, encoded, buffer, out var written))
                return false;

            data = buffer[..written].ToArray();
            return true;
        }
        finally {
            if (rented is not null)
                ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <summary>PEM-armor Base64 (76-char lines) with BEGIN/END labels.</summary>
    public static string EncodePem(string label, ReadOnlySpan<byte> data)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(label);
        var body = Encode(BinaryEncodingKind.Base64, data, lineLength: MimeLineLength);
        return $"-----BEGIN {label}-----\r\n{body}\r\n-----END {label}-----";
    }

    /// <inheritdoc cref="EncodePem(string, ReadOnlySpan{byte})" />
    public static string EncodePem(string label, byte[] data)
    {
        ArgumentHelpers.ThrowIfNull(data);
        return EncodePem(label, data.AsSpan());
    }

    /// <summary>Decode PEM-armored Base64; returns false when armor/payload is invalid.</summary>
    public static bool TryDecodePem(ReadOnlySpan<char> text, out string? label, out byte[]? data)
    {
        label = null;
        data = null;
        if (text.IsEmpty)
            return false;

        var s = text.ToString();
        const string begin = "-----BEGIN ";
        const string endMark = "-----END ";
        var beginIdx = s.IndexOf(begin, StringComparison.Ordinal);
        if (beginIdx < 0)
            return false;

        var labelStart = beginIdx + begin.Length;
        var labelEnd = s.IndexOf("-----", labelStart, StringComparison.Ordinal);
        if (labelEnd < 0)
            return false;

        label = s[labelStart..labelEnd].Trim();
        var bodyStart = labelEnd + 5;
        while (bodyStart < s.Length && (s[bodyStart] == '\r' || s[bodyStart] == '\n'))
            bodyStart++;

        var endIdx = s.IndexOf(endMark, bodyStart, StringComparison.Ordinal);
        if (endIdx < 0)
            return false;

        var endLabelStart = endIdx + endMark.Length;
        var endLabelEnd = s.IndexOf("-----", endLabelStart, StringComparison.Ordinal);
        if (endLabelEnd < 0)
            return false;

        var endLabel = s[endLabelStart..endLabelEnd].Trim();
        if (!string.Equals(label, endLabel, StringComparison.Ordinal))
            return false;

        var body = s[bodyStart..endIdx];
        return TryDecode(BinaryEncodingKind.Base64, body.AsSpan(), out data);
    }

    /// <summary>Decode PEM-armored Base64; throws on invalid armor/payload.</summary>
    public static byte[] DecodePem(ReadOnlySpan<char> text, out string label)
    {
        if (!TryDecodePem(text, out var lbl, out var data) || lbl is null || data is null)
            throw new FormatException("Invalid PEM payload.");

        label = lbl;
        return data;
    }

    /// <inheritdoc cref="DecodePem(ReadOnlySpan{char}, out string)" />
    public static byte[] DecodePem(string text, out string label)
    {
        ArgumentHelpers.ThrowIfNull(text);
        return DecodePem(text.AsSpan(), out label);
    }

    /// <summary>Chunked encode: binary in → text out. Does not close streams.</summary>
    public static async Task EncodeAsync(
        BinaryEncodingKind kind,
        Stream input,
        TextWriter output,
        TextLetterCase hexLetterCase = TextLetterCase.Upper,
        int lineLength = 0,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNull(output);
        if (kind is BinaryEncodingKind.Base64 or BinaryEncodingKind.Base64Url) {
            await EncodeBase64StreamAsync(kind == BinaryEncodingKind.Base64Url, input, output, lineLength, ct).ConfigureAwait(false);
            return;
        }

        if (kind != BinaryEncodingKind.Hex)
            throw new ArgumentOutOfRangeException(nameof(kind), kind, null);

        var byteBuf = ArrayPool<byte>.Shared.Rent(StreamBufferSize);
        var charBuf = ArrayPool<char>.Shared.Rent(StreamBufferSize * 2);
        try {
            int read;
#if NET5_0_OR_GREATER
            while ((read = await input.ReadAsync(byteBuf.AsMemory(0, StreamBufferSize), ct).ConfigureAwait(false)) > 0) {
#else
            while ((read = await input.ReadAsync(byteBuf, 0, StreamBufferSize, ct).ConfigureAwait(false)) > 0) {
#endif
                if (!TryEncodeHex(byteBuf.AsSpan(0, read), charBuf.AsSpan(0, read * 2), out var written, hexLetterCase))
                    throw new EncodingException("Hex encode failed.");
#if NET5_0_OR_GREATER
                await output.WriteAsync(charBuf.AsMemory(0, written), ct).ConfigureAwait(false);
#else
                await output.WriteAsync(charBuf, 0, written).ConfigureAwait(false);
#endif
            }
        }
        finally {
            ArrayPool<byte>.Shared.Return(byteBuf);
            ArrayPool<char>.Shared.Return(charBuf);
        }
    }

    /// <summary>Streaming decode: text in → binary out. Does not close streams. Whitespace ignored for Base64.</summary>
    public static async Task DecodeAsync(BinaryEncodingKind kind, TextReader input, Stream output, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNull(output);
        if (kind is BinaryEncodingKind.Base64 or BinaryEncodingKind.Base64Url) {
            await DecodeBase64StreamAsync(kind == BinaryEncodingKind.Base64Url, input, output, ct).ConfigureAwait(false);
            return;
        }

        if (kind != BinaryEncodingKind.Hex)
            throw new ArgumentOutOfRangeException(nameof(kind), kind, null);

        await DecodeHexStreamAsync(input, output, ct).ConfigureAwait(false);
    }

    /// <summary>Encode file contents to a string.</summary>
    public static async Task<string> EncodeFileAsync(
        BinaryEncodingKind kind,
        string path,
        TextLetterCase hexLetterCase = TextLetterCase.Upper,
        int lineLength = 0,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        ArgumentHelpers.ThrowIfFileNotFound(path);
#if NET5_0_OR_GREATER
        var bytes = await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
#else
        var bytes = File.ReadAllBytes(path);
        ct.ThrowIfCancellationRequested();
#endif
        return Encode(kind, bytes.AsSpan(), hexLetterCase, lineLength);
    }

    /// <summary>Encode input file to output text file.</summary>
    public static async Task EncodeFileAsync(
        BinaryEncodingKind kind,
        string inputPath,
        string outputPath,
        TextLetterCase hexLetterCase = TextLetterCase.Upper,
        int lineLength = 0,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentHelpers.ThrowIfFileNotFound(inputPath);
#if NET5_0_OR_GREATER
        await using var input = File.OpenRead(inputPath);
        await using var output = new StreamWriter(outputPath, false, Encoding.ASCII);
#else
        using var input = File.OpenRead(inputPath);
        using var output = new StreamWriter(outputPath, false, Encoding.ASCII);
#endif
        await EncodeAsync(kind, input, output, hexLetterCase, lineLength, ct).ConfigureAwait(false);
    }

    /// <summary>Decode encoded text file to bytes (streaming).</summary>
    public static async Task<byte[]> DecodeFileAsync(BinaryEncodingKind kind, string path, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        ArgumentHelpers.ThrowIfFileNotFound(path);
#if NET5_0_OR_GREATER
        using var input = new StreamReader(path);
        await using var ms = new MemoryStream();
#else
        using var input = new StreamReader(path);
        using var ms = new MemoryStream();
#endif
        await DecodeAsync(kind, input, ms, ct).ConfigureAwait(false);
        return ms.ToArray();
    }

    /// <summary>Decode encoded text file to binary output file (streaming).</summary>
    public static async Task DecodeFileAsync(BinaryEncodingKind kind, string inputPath, string outputPath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentHelpers.ThrowIfFileNotFound(inputPath);
#if NET5_0_OR_GREATER
        using var input = new StreamReader(inputPath);
        await using var output = File.Create(outputPath);
#else
        using var input = new StreamReader(inputPath);
        using var output = File.Create(outputPath);
#endif
        await DecodeAsync(kind, input, output, ct).ConfigureAwait(false);
    }

    /// <summary>Decode encoded text from stream (materializes). Does not close <paramref name="encodedStream" />.</summary>
    public static byte[] Decode(BinaryEncodingKind kind, Stream encodedStream)
    {
        ArgumentHelpers.ThrowIfNull(encodedStream);
        using var reader = new StreamReader(encodedStream, Encoding.ASCII, false, StreamBufferSize, true);
        return Decode(kind, reader.ReadToEnd().AsSpan());
    }

    private static string ApplyLineWrap(ReadOnlySpan<char> encoded, int lineLength, BinaryEncodingKind kind)
    {
        if (lineLength <= 0 || kind == BinaryEncodingKind.Hex || encoded.Length <= lineLength)
            return encoded.ToString();

        var lines = (encoded.Length + lineLength - 1) / lineLength;
        var capacity = encoded.Length + (lines - 1) * 2;
        var sb = new StringBuilder(capacity);
        for (var i = 0; i < encoded.Length; i += lineLength) {
            if (i > 0)
                sb.Append("\r\n");

            var len = Math.Min(lineLength, encoded.Length - i);
#if NET5_0_OR_GREATER
            sb.Append(encoded.Slice(i, len));
#else
            sb.Append(encoded.Slice(i, len).ToString());
#endif
        }

        return sb.ToString();
    }

    private static async Task EncodeBase64StreamAsync(bool urlSafe, Stream input, TextWriter output, int lineLength, CancellationToken ct)
    {
        var byteBuf = ArrayPool<byte>.Shared.Rent(StreamBufferSize + 2);
        var charBuf = ArrayPool<char>.Shared.Rent((StreamBufferSize + 2) / 3 * 4);
        var carryLen = 0;
        var col = 0;
        try {
            int read;
#if NET5_0_OR_GREATER
            while ((read = await input.ReadAsync(byteBuf.AsMemory(carryLen, StreamBufferSize), ct).ConfigureAwait(false)) > 0) {
#else
            while ((read = await input.ReadAsync(byteBuf, carryLen, StreamBufferSize, ct).ConfigureAwait(false)) > 0) {
#endif
                var total = carryLen + read;
                var consumable = total - total % 3;
                if (consumable > 0) {
                    var kind = urlSafe ? BinaryEncodingKind.Base64Url : BinaryEncodingKind.Base64;
                    if (!TryEncode(kind, byteBuf.AsSpan(0, consumable), charBuf, out var written))
                        throw new EncodingException("Base64 encode failed.");

                    col = await WriteWrappedAsync(output, charBuf, written, lineLength, col, ct).ConfigureAwait(false);
                }

                carryLen = total - consumable;
                if (carryLen > 0)
                    Buffer.BlockCopy(byteBuf, consumable, byteBuf, 0, carryLen);
            }

            if (carryLen > 0) {
                var kind = urlSafe ? BinaryEncodingKind.Base64Url : BinaryEncodingKind.Base64;
                if (!TryEncode(kind, byteBuf.AsSpan(0, carryLen), charBuf, out var written))
                    throw new EncodingException("Base64 encode failed.");

                _ = await WriteWrappedAsync(output, charBuf, written, lineLength, col, ct).ConfigureAwait(false);
            }
        }
        finally {
            ArrayPool<byte>.Shared.Return(byteBuf);
            ArrayPool<char>.Shared.Return(charBuf);
        }
    }

    private static async Task<int> WriteWrappedAsync(TextWriter output, char[] chars, int length, int lineLength, int col, CancellationToken ct)
    {
        if (lineLength <= 0) {
#if NET5_0_OR_GREATER
            await output.WriteAsync(chars.AsMemory(0, length), ct).ConfigureAwait(false);
#else
            await output.WriteAsync(chars, 0, length).ConfigureAwait(false);
#endif
            return col;
        }

        var offset = 0;
        while (offset < length) {
            var room = lineLength - col;
            if (room == 0) {
#if NET5_0_OR_GREATER
                await output.WriteAsync("\r\n".AsMemory(), ct).ConfigureAwait(false);
#else
                await output.WriteAsync("\r\n").ConfigureAwait(false);
#endif
                col = 0;
                room = lineLength;
            }

            var take = Math.Min(room, length - offset);
#if NET5_0_OR_GREATER
            await output.WriteAsync(chars.AsMemory(offset, take), ct).ConfigureAwait(false);
#else
            await output.WriteAsync(chars, offset, take).ConfigureAwait(false);
#endif
            col += take;
            offset += take;
        }

        return col;
    }

    private static async Task DecodeBase64StreamAsync(bool urlSafe, TextReader input, Stream output, CancellationToken ct)
    {
        var charBuf = ArrayPool<char>.Shared.Rent(StreamBufferSize);
        var quartet = ArrayPool<char>.Shared.Rent(4);
        var byteOut = ArrayPool<byte>.Shared.Rent(6);
        var qLen = 0;
        try {
            int read;
            while ((read = await input.ReadAsync(charBuf, 0, charBuf.Length).ConfigureAwait(false)) > 0) {
                ct.ThrowIfCancellationRequested();
                for (var i = 0; i < read; i++) {
                    var c = charBuf[i];
                    if (char.IsWhiteSpace(c))
                        continue;

                    if (urlSafe) {
                        if (c == '-')
                            c = '+';
                        else if (c == '_')
                            c = '/';
                    }

                    quartet[qLen++] = c;
                    if (qLen != 4)
                        continue;

                    if (!TryDecodeBase64(quartet.AsSpan(0, 4), byteOut, out var written, false))
                        throw new FormatException("Invalid Base64 payload.");
#if NET5_0_OR_GREATER
                    await output.WriteAsync(byteOut.AsMemory(0, written), ct).ConfigureAwait(false);
#else
                    await output.WriteAsync(byteOut, 0, written, ct).ConfigureAwait(false);
#endif
                    qLen = 0;
                }
            }

            if (qLen > 0) {
                // pad to 4
                while (qLen < 4)
                    quartet[qLen++] = '=';

                if (!TryDecodeBase64(quartet.AsSpan(0, 4), byteOut, out var written, false))
                    throw new FormatException("Invalid Base64 payload.");
#if NET5_0_OR_GREATER
                await output.WriteAsync(byteOut.AsMemory(0, written), ct).ConfigureAwait(false);
#else
                await output.WriteAsync(byteOut, 0, written, ct).ConfigureAwait(false);
#endif
            }
        }
        finally {
            ArrayPool<char>.Shared.Return(charBuf);
            ArrayPool<char>.Shared.Return(quartet);
            ArrayPool<byte>.Shared.Return(byteOut);
        }
    }

    private static async Task DecodeHexStreamAsync(TextReader input, Stream output, CancellationToken ct)
    {
        var charBuf = ArrayPool<char>.Shared.Rent(StreamBufferSize);
        var byteOut = ArrayPool<byte>.Shared.Rent(StreamBufferSize / 2 + 1);
        var pending = -1;
        try {
            int read;
            while ((read = await input.ReadAsync(charBuf, 0, charBuf.Length).ConfigureAwait(false)) > 0) {
                ct.ThrowIfCancellationRequested();
                var outLen = 0;
                for (var i = 0; i < read; i++) {
                    var c = charBuf[i];
                    if (char.IsWhiteSpace(c))
                        continue;

                    var v = HexValue(c);
                    if (v < 0)
                        throw new FormatException("Invalid hexadecimal character.");

                    if (pending < 0)
                        pending = v;
                    else {
                        byteOut[outLen++] = (byte)((pending << 4) | v);
                        pending = -1;
                    }
                }

                if (outLen > 0) {
#if NET5_0_OR_GREATER
                    await output.WriteAsync(byteOut.AsMemory(0, outLen), ct).ConfigureAwait(false);
#else
                    await output.WriteAsync(byteOut, 0, outLen, ct).ConfigureAwait(false);
#endif
                }
            }

            if (pending >= 0)
                throw new FormatException("Hex length must be even.");
        }
        finally {
            ArrayPool<char>.Shared.Return(charBuf);
            ArrayPool<byte>.Shared.Return(byteOut);
        }
    }

    private static bool TryEncodeBase64(ReadOnlySpan<byte> data, Span<char> destination, out int charsWritten, bool urlSafe)
    {
        charsWritten = 0;
        var needed = (data.Length + 2) / 3 * 4;
        if (destination.Length < needed)
            return false;

#if NET5_0_OR_GREATER
        if (!Convert.TryToBase64Chars(data, destination, out charsWritten))
            return false;
#else
        var s = Convert.ToBase64String(data.ToArray());
        if (destination.Length < s.Length)
            return false;

        s.AsSpan().CopyTo(destination);
        charsWritten = s.Length;
#endif
        if (!urlSafe)
            return true;

        for (var i = 0; i < charsWritten; i++) {
            var c = destination[i];
            if (c == '+')
                destination[i] = '-';
            else if (c == '/')
                destination[i] = '_';
        }

        while (charsWritten > 0 && destination[charsWritten - 1] == '=')
            charsWritten--;

        return true;
    }

    private static bool TryDecodeBase64(ReadOnlySpan<char> encoded, Span<byte> destination, out int bytesWritten, bool urlSafe)
    {
        bytesWritten = 0;
        if (encoded.IsEmpty)
            return true;

        // Strip whitespace into a compact buffer when needed
        char[]? rented = null;
        try {
            var compactLen = 0;
            for (var i = 0; i < encoded.Length; i++) {
                if (!char.IsWhiteSpace(encoded[i]))
                    compactLen++;
            }

            ReadOnlySpan<char> working;
            if (compactLen != encoded.Length || urlSafe) {
                var paddedLen = compactLen;
                if (urlSafe) {
                    var mod = paddedLen % 4;
                    if (mod == 1)
                        return false;

                    if (mod > 0)
                        paddedLen += 4 - mod;
                }
                else if (paddedLen % 4 != 0)
                    return false;

                rented = ArrayPool<char>.Shared.Rent(paddedLen);
                var span = rented.AsSpan(0, paddedLen);
                var o = 0;
                for (var i = 0; i < encoded.Length; i++) {
                    var c = encoded[i];
                    if (char.IsWhiteSpace(c))
                        continue;

                    if (urlSafe) {
                        if (c == '-')
                            c = '+';
                        else if (c == '_')
                            c = '/';
                    }

                    span[o++] = c;
                }

                while (o < paddedLen)
                    span[o++] = '=';

                working = span;
            }
            else
                working = encoded;

#if NET5_0_OR_GREATER
            return Convert.TryFromBase64Chars(working, destination, out bytesWritten);
#else
            try {
                var bytes = Convert.FromBase64String(working.ToString());
                if (destination.Length < bytes.Length)
                    return false;

                bytes.CopyTo(destination);
                bytesWritten = bytes.Length;
                return true;
            }
            catch (FormatException) {
                return false;
            }
#endif
        }
        finally {
            if (rented is not null)
                ArrayPool<char>.Shared.Return(rented);
        }
    }

    private static bool TryEncodeHex(ReadOnlySpan<byte> data, Span<char> destination, out int charsWritten, TextLetterCase letterCase)
    {
        charsWritten = 0;
        var needed = data.Length * 2;
        if (destination.Length < needed)
            return false;

#if NET5_0_OR_GREATER
        if (letterCase == TextLetterCase.Upper) {
            var hex = Convert.ToHexString(data);
            hex.AsSpan().CopyTo(destination);
            charsWritten = hex.Length;
            return true;
        }
#endif
        for (var i = 0; i < data.Length; i++) {
            var b = data[i];
            destination[i * 2] = letterCase == TextLetterCase.Upper ? NibbleToHexUpper(b >> 4) : NibbleToHexLower(b >> 4);
            destination[i * 2 + 1] = letterCase == TextLetterCase.Upper ? NibbleToHexUpper(b & 0xF) : NibbleToHexLower(b & 0xF);
        }

        charsWritten = needed;
        return true;
    }

    private static bool TryDecodeHex(ReadOnlySpan<char> hex, Span<byte> destination, out int bytesWritten)
    {
        bytesWritten = 0;
        // Ignore whitespace
        var pending = -1;
        for (var i = 0; i < hex.Length; i++) {
            var c = hex[i];
            if (char.IsWhiteSpace(c))
                continue;

            var v = HexValue(c);
            if (v < 0)
                return false;

            if (pending < 0)
                pending = v;
            else {
                if (bytesWritten >= destination.Length)
                    return false;

                destination[bytesWritten++] = (byte)((pending << 4) | v);
                pending = -1;
            }
        }

        return pending < 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static char NibbleToHexUpper(int nibble) => (char)(nibble < 10 ? '0' + nibble : 'A' + (nibble - 10));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static char NibbleToHexLower(int nibble) => (char)(nibble < 10 ? '0' + nibble : 'a' + (nibble - 10));

    private static int HexValue(char c)
    {
        if (c is >= '0' and <= '9')
            return c - '0';

        if (c is >= 'a' and <= 'f')
            return c - 'a' + 10;

        return c is >= 'A' and <= 'F' ? c - 'A' + 10 : -1;
    }
}