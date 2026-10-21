using System.Runtime.CompilerServices;
using System.Text;
using Lyo.Common.Core.Enums;
using Lyo.Exceptions;

namespace Lyo.Common.Core.Conversion;

/// <summary>
/// Text and hex conversions for spans. Text methods take an <see cref="Encoding" /> and use UTF-8 when it is omitted. Upper hex matches
/// <see cref="Convert.ToHexString(ReadOnlySpan{byte})" />. Hex has no separators and no whitespace.
/// </summary>
public static class ByteConvert
{
    /// <summary>Encodes <paramref name="bytes" /> as hex. Empty input returns <see cref="string.Empty" />.</summary>
    /// <param name="bytes">Bytes to encode.</param>
    /// <param name="letterCase">Letter case for A–F. Upper is the default.</param>
    public static string ToHex(ReadOnlySpan<byte> bytes, TextLetterCase letterCase = TextLetterCase.Upper)
    {
        if (bytes.IsEmpty)
            return string.Empty;

#if NET9_0_OR_GREATER
        return letterCase == TextLetterCase.Upper ? Convert.ToHexString(bytes) : Convert.ToHexStringLower(bytes);
#else
        var chars = new char[bytes.Length * 2];
        TryToHex(bytes, chars, out var _);
        return new(chars);
#endif
    }

    /// <inheritdoc cref="ToHex(ReadOnlySpan{byte}, TextLetterCase)" />
    public static string ToHex(byte[] bytes, TextLetterCase letterCase = TextLetterCase.Upper)
    {
        ArgumentHelpers.ThrowIfNull(bytes);
        return ToHex(bytes.AsSpan(), letterCase);
    }

    /// <summary>Writes hex into <paramref name="destination" />. Returns false when the span is shorter than two characters per byte.</summary>
    /// <param name="bytes">Bytes to encode.</param>
    /// <param name="destination">Buffer that receives the hex characters.</param>
    /// <param name="charsWritten">Characters written. Zero when this returns false.</param>
    /// <param name="letterCase">Letter case for A–F. Upper is the default.</param>
    public static bool TryToHex(ReadOnlySpan<byte> bytes, Span<char> destination, out int charsWritten, TextLetterCase letterCase = TextLetterCase.Upper)
    {
        charsWritten = 0;
        var needed = bytes.Length * 2;
        if (destination.Length < needed)
            return false;

        for (var i = 0; i < bytes.Length; i++) {
            var value = bytes[i];
            destination[i * 2] = Nibble(value >> 4, letterCase);
            destination[i * 2 + 1] = Nibble(value & 0xF, letterCase);
        }

        charsWritten = needed;
        return true;
    }

    /// <summary>Decodes even-length hex into <paramref name="destination" />. Either letter case is accepted. Whitespace is rejected.</summary>
    /// <param name="hex">Hex text. An odd length or a short destination returns false.</param>
    /// <param name="destination">Buffer that receives the bytes.</param>
    /// <param name="bytesWritten">Bytes written. Zero when the length or the destination fails before any byte is stored.</param>
    public static bool TryFromHex(ReadOnlySpan<char> hex, Span<byte> destination, out int bytesWritten)
    {
        bytesWritten = 0;
        if ((hex.Length & 1) != 0 || hex.Length / 2 > destination.Length)
            return false;

        for (var i = 0; i < hex.Length; i += 2) {
            var hi = HexValue(hex[i]);
            var lo = HexValue(hex[i + 1]);
            if (hi < 0 || lo < 0)
                return false;

            destination[bytesWritten++] = (byte)((hi << 4) | lo);
        }

        return true;
    }

    /// <summary>Decodes even-length hex. Empty input returns an empty array.</summary>
    /// <param name="hex">Hex text. Odd length or a non-hex character throws <see cref="FormatException" />.</param>
    public static byte[] FromHex(ReadOnlySpan<char> hex)
    {
        if (hex.IsEmpty)
            return [];

        if ((hex.Length & 1) != 0)
            throw new FormatException("Hex length must be even.");

        var buf = new byte[hex.Length / 2];
        if (!TryFromHex(hex, buf, out var written) || written != buf.Length)
            throw new FormatException("Invalid hexadecimal character.");

        return buf;
    }

    /// <inheritdoc cref="FromHex(ReadOnlySpan{char})" />
    public static byte[] FromHex(string hex)
    {
        ArgumentHelpers.ThrowIfNull(hex);
        return FromHex(hex.AsSpan());
    }

    /// <summary>Decodes <paramref name="bytes" />. Empty input returns <see cref="string.Empty" />. A null <paramref name="encoding" /> is UTF-8.</summary>
    /// <param name="bytes">Encoded bytes.</param>
    /// <param name="encoding">Encoding to use. Null uses UTF-8.</param>
    public static string GetString(ReadOnlySpan<byte> bytes, Encoding? encoding = null)
    {
        if (bytes.IsEmpty)
            return string.Empty;

        encoding = EncodingOrUtf8(encoding);
#if NET5_0_OR_GREATER
        return encoding.GetString(bytes);
#else
        return encoding.GetString(bytes.ToArray());
#endif
    }

    /// <inheritdoc cref="GetString(ReadOnlySpan{byte}, Encoding?)" />
    public static string GetString(byte[] bytes, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNull(bytes);
        return GetString(bytes.AsSpan(), encoding);
    }

    /// <summary>Encodes <paramref name="text" />. Empty input returns an empty array. A null <paramref name="encoding" /> is UTF-8.</summary>
    /// <param name="text">Text to encode.</param>
    /// <param name="encoding">Encoding to use. Null uses UTF-8.</param>
    public static byte[] GetBytes(ReadOnlySpan<char> text, Encoding? encoding = null)
    {
        if (text.IsEmpty)
            return [];

        encoding = EncodingOrUtf8(encoding);
#if NET5_0_OR_GREATER
        var encoded = new byte[encoding.GetByteCount(text)];
        var written = encoding.GetBytes(text, encoded);
        return written == encoded.Length ? encoded : encoded[..written];
#else
        return encoding.GetBytes(text.ToString());
#endif
    }

    /// <inheritdoc cref="GetBytes(ReadOnlySpan{char}, Encoding?)" />
    public static byte[] GetBytes(string text, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNull(text);
        return GetBytes(text.AsSpan(), encoding);
    }

    /// <summary>Writes encoded text into <paramref name="destination" />. Returns false when the span is too short. A null <paramref name="encoding" /> is UTF-8.</summary>
    /// <param name="text">Text to encode.</param>
    /// <param name="destination">Buffer that receives the bytes.</param>
    /// <param name="bytesWritten">Bytes written. Zero when this returns false.</param>
    /// <param name="encoding">Encoding to use. Null uses UTF-8.</param>
    public static bool TryGetBytes(ReadOnlySpan<char> text, Span<byte> destination, out int bytesWritten, Encoding? encoding = null)
    {
        bytesWritten = 0;
        encoding = EncodingOrUtf8(encoding);
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
        var encoded = encoding.GetBytes(text.ToString());
        if (destination.Length < encoded.Length)
            return false;

        encoded.CopyTo(destination);
        bytesWritten = encoded.Length;
        return true;
#endif
    }

    /// <summary>Writes decoded text into <paramref name="destination" />. Returns false when the span is too short. A null <paramref name="encoding" /> is UTF-8.</summary>
    /// <param name="bytes">Encoded bytes.</param>
    /// <param name="destination">Buffer that receives the characters.</param>
    /// <param name="charsWritten">Characters written. Zero when this returns false.</param>
    /// <param name="encoding">Encoding to use. Null uses UTF-8.</param>
    public static bool TryGetString(ReadOnlySpan<byte> bytes, Span<char> destination, out int charsWritten, Encoding? encoding = null)
    {
        charsWritten = 0;
        encoding = EncodingOrUtf8(encoding);
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
        var text = encoding.GetString(bytes.ToArray());
        if (destination.Length < text.Length)
            return false;

        text.AsSpan().CopyTo(destination);
        charsWritten = text.Length;
        return true;
#endif
    }

    private static Encoding EncodingOrUtf8(Encoding? encoding) => encoding ?? Encoding.UTF8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static char Nibble(int nibble, TextLetterCase letterCase) => (char)(nibble < 10 ? '0' + nibble : (letterCase == TextLetterCase.Upper ? 'A' : 'a') + (nibble - 10));

    private static int HexValue(char c)
        => c switch {
            >= '0' and <= '9' => c - '0',
            >= 'a' and <= 'f' => c - 'a' + 10,
            var _ => c is >= 'A' and <= 'F' ? c - 'A' + 10 : -1
        };
}