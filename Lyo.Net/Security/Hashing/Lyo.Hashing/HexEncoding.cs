using System.Runtime.CompilerServices;
using Lyo.Common.Enums;
using Lyo.Exceptions;

namespace Lyo.Hashing;

/// <summary>Hexadecimal encoding for digests and binary blobs.</summary>
public static class HexEncoding
{
    /// <summary>Encodes <paramref name="bytes" /> as hex with no separators. Empty input yields <see cref="string.Empty" />.</summary>
    public static string ToHexString(ReadOnlySpan<byte> bytes, TextLetterCase letterCase = TextLetterCase.Upper)
    {
        if (bytes.IsEmpty)
            return string.Empty;

        return letterCase == TextLetterCase.Upper ? ToHexStringUpper(bytes) : ToHexStringLower(bytes);
    }

    /// <inheritdoc cref="ToHexString(ReadOnlySpan{byte}, TextLetterCase)" />
    public static string ToHexString(byte[] bytes, TextLetterCase letterCase = TextLetterCase.Upper)
    {
        ArgumentHelpers.ThrowIfNull(bytes);
        return ToHexString(bytes.AsSpan(), letterCase);
    }

#if NET5_0_OR_GREATER
    private static string ToHexStringUpper(ReadOnlySpan<byte> bytes) => Convert.ToHexString(bytes);
#else
    private static string ToHexStringUpper(ReadOnlySpan<byte> bytes)
    {
        var chars = new char[bytes.Length * 2];
        for (var i = 0; i < bytes.Length; i++) {
            var b = bytes[i];
            chars[i * 2] = NibbleToHexUpper(b >> 4);
            chars[i * 2 + 1] = NibbleToHexUpper(b & 0xF);
        }

        return new(chars);
    }
#endif

    private static string ToHexStringLower(ReadOnlySpan<byte> bytes)
    {
        var chars = new char[bytes.Length * 2];
        for (var i = 0; i < bytes.Length; i++) {
            var b = bytes[i];
            chars[i * 2] = NibbleToHexLower(b >> 4);
            chars[i * 2 + 1] = NibbleToHexLower(b & 0xF);
        }

        return new(chars);
    }

#if !NET5_0_OR_GREATER
    private static char NibbleToHexUpper(int nibble) => (char)(nibble < 10 ? '0' + nibble : 'A' + (nibble - 10));
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static char NibbleToHexLower(int nibble) => (char)(nibble < 10 ? '0' + nibble : 'a' + (nibble - 10));

    /// <summary>Try decode even-length hexadecimal (digit pairs). Accepts uppercase or lowercase.</summary>
    public static bool TryDecodeHex(ReadOnlySpan<char> hex, Span<byte> destination, out int bytesWritten)
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

    /// <summary>Decode even-length hexadecimal; returns empty array when <paramref name="hex" /> is empty.</summary>
    public static byte[] FromHex(ReadOnlySpan<char> hex)
    {
        if (hex.IsEmpty)
            return [];

        if ((hex.Length & 1) != 0)
            throw new FormatException("Hex length must be even.");

        var buf = new byte[hex.Length / 2];
        if (!TryDecodeHex(hex, buf, out var written) || written != buf.Length)
            throw new FormatException("Invalid hexadecimal character.");

        return buf;
    }

    /// <inheritdoc cref="FromHex(ReadOnlySpan{char})" />
    public static byte[] FromHex(string hex)
    {
        ArgumentHelpers.ThrowIfNull(hex);
        return FromHex(hex.AsSpan());
    }

    private static int HexValue(char c)
    {
        if (c is >= '0' and <= '9')
            return c - '0';

        if (c is >= 'a' and <= 'f')
            return c - 'a' + 10;

        return c is >= 'A' and <= 'F' ? c - 'A' + 10 : -1;
    }
}