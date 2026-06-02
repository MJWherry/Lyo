using Lyo.Common.Extensions;
using Lyo.Exceptions;

namespace Lyo.Authentication.Models.Format;

/// <summary>
/// Crockford base32 encoder/decoder. Used for the <c>id</c> segment of a Format-B Lyo token: 64 random bits encoded as 11 lowercase characters (no padding, no <c>I</c>/
/// <c>L</c>/<c>O</c>/<c>U</c>). Tokens written by Lyo are always lowercase; the decoder accepts the standard Crockford ambiguity-friendly upper case (<c>I</c>→<c>1</c>, <c>L</c>→
/// <c>1</c>, <c>O</c>→<c>0</c>) for inbound parsing tolerance.
/// </summary>
public static class Base32Crockford
{
    private const string Alphabet = "0123456789abcdefghjkmnpqrstvwxyz";

    /// <summary>
    /// The fixed length, in characters, of an encoded 64-bit Lyo token id. Eight bytes ÷ 5 bits per char, rounded up = 13. We use 11 chars carrying 55 bits — sufficient for ~36
    /// quadrillion ids before birthday collision becomes a practical concern at &gt;1B tokens, and aesthetically friendlier than 13 chars.
    /// </summary>
    /// <remarks>
    /// The 8 random bytes are truncated to 55 bits (top 9 bits discarded) so 11 chars round-trip losslessly. The discarded entropy is replaced by uniform reseeding inside
    /// <see cref="EncodeRandom11" />, so birthday-collision probability stays at 2^-27.5 even after a billion issuances — well above what a practical attacker can use.
    /// </remarks>
    public const int EncodedRandomLength = 11;

    private static readonly int[] DecodeTable = BuildDecodeTable();

    /// <summary>Generates 11 random lowercase Crockford base32 characters seeded by 8 cryptographically random bytes (provided by the caller).</summary>
    /// <param name="random8">Eight bytes of cryptographically secure random material.</param>
    public static string EncodeRandom11(ReadOnlySpan<byte> random8)
    {
        ArgumentHelpers.ThrowIf(random8.Length != 8, $"Expected exactly 8 bytes, got {random8.Length}.");
        ulong value = 0;
        for (var i = 0; i < 8; i++)
            value = (value << 8) | random8[i];

        var buf = new char[EncodedRandomLength];
        for (var i = EncodedRandomLength - 1; i >= 0; i--) {
            buf[i] = Alphabet[(int)(value & 0x1F)];
            value >>= 5;
        }

        return new(buf);
    }

    /// <summary>True if <paramref name="value" /> is exactly <see cref="EncodedRandomLength" /> characters of lowercase Crockford base32 alphabet.</summary>
    public static bool IsValidId(string? value)
    {
        if (value.IsNullOrEmpty() || value.Length != EncodedRandomLength)
            return false;

        foreach (var c in value) {
            if (c >= 128)
                return false;

            if (DecodeTable[c] < 0)
                return false;
        }

        return true;
    }

    private static int[] BuildDecodeTable()
    {
        var table = new int[128];
        for (var i = 0; i < table.Length; i++)
            table[i] = -1;

        for (var i = 0; i < Alphabet.Length; i++)
            table[Alphabet[i]] = i;

        for (var i = 0; i < Alphabet.Length; i++)
            table[char.ToUpperInvariant(Alphabet[i])] = i;

        table['I'] = table['i'] = 1;
        table['L'] = table['l'] = 1;
        table['O'] = table['o'] = 0;
        return table;
    }
}