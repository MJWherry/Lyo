using Lyo.Common.Core.Conversion;
using Lyo.Common.Core.Enums;
using Lyo.Exceptions;

namespace Lyo.Hashing;

/// <summary>Hex encoding for digests and binary blobs. Plain encode and decode go through <see cref="ByteConvert" />.</summary>
public static class HexEncoding
{
    /// <summary>Encodes <paramref name="bytes" /> as hex with no separators. Empty input yields <see cref="string.Empty" />.</summary>
    public static string ToHexString(ReadOnlySpan<byte> bytes, TextLetterCase letterCase = TextLetterCase.Upper) => ByteConvert.ToHex(bytes, letterCase);

    /// <inheritdoc cref="ToHexString(ReadOnlySpan{byte}, TextLetterCase)" />
    public static string ToHexString(byte[] bytes, TextLetterCase letterCase = TextLetterCase.Upper)
    {
        ArgumentHelpers.ThrowIfNull(bytes);
        return ByteConvert.ToHex(bytes, letterCase);
    }

    /// <summary>Try-decode even-length hex (digit pairs). Uppercase or lowercase accepted.</summary>
    public static bool TryDecodeHex(ReadOnlySpan<char> hex, Span<byte> destination, out int bytesWritten) => ByteConvert.TryFromHex(hex, destination, out bytesWritten);

    /// <summary>Decode even-length hex; empty <paramref name="hex" /> yields an empty array.</summary>
    public static byte[] FromHex(ReadOnlySpan<char> hex) => ByteConvert.FromHex(hex);

    /// <inheritdoc cref="FromHex(ReadOnlySpan{char})" />
    public static byte[] FromHex(string hex)
    {
        ArgumentHelpers.ThrowIfNull(hex);
        return ByteConvert.FromHex(hex);
    }
}
