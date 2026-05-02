using System.Diagnostics.CodeAnalysis;
using Lyo.Exceptions;

namespace Lyo.Common;

/// <summary>Uppercase hexadecimal encoding (same shape as <see cref="Convert.ToHexString(ReadOnlySpan{byte})" /> on .NET 5+).</summary>
public static class HexEncoding
{
    /// <summary>Encodes <paramref name="bytes" /> as uppercase hex with no separators. Returns <see cref="string.Empty" /> when length is 0.</summary>
    [return: NotNull]
    public static string ToHexString(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
            return string.Empty;
#if NET5_0_OR_GREATER
        return Convert.ToHexString(bytes);
#else
        var chars = new char[bytes.Length * 2];
        for (var i = 0; i < bytes.Length; i++) {
            var b = bytes[i];
            chars[i * 2] = NibbleToHexUpper(b >> 4);
            chars[i * 2 + 1] = NibbleToHexUpper(b & 0xF);
        }
        return new(chars);
#endif
    }

    /// <summary>Encodes <paramref name="bytes" /> as uppercase hex with no separators.</summary>
    [return: NotNull]
    public static string ToHexString(byte[] bytes)
    {
        ArgumentHelpers.ThrowIfNull(bytes);
        return ToHexString(bytes.AsSpan());
    }

#if !NET5_0_OR_GREATER
    private static char NibbleToHexUpper(int nibble) =>
        (char)(nibble < 10 ? '0' + nibble : 'A' + (nibble - 10));
#endif
}