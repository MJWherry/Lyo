using Lyo.Common.Security;
using Lyo.Exceptions;

namespace Lyo.Common.Identifiers;

/// <summary>
/// ULID (Universally Unique Lexicographically Sortable Identifier) generator. A ULID is a 26-character Crockford base32 string: 10 chars of 48-bit millisecond timestamp
/// followed by 16 chars of 80-bit randomness. ULIDs sort lexicographically by creation time.
/// </summary>
public static class Ulid
{
    // Crockford base32 — no I, L, O, U to avoid ambiguous characters.
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    // O(1) decode table indexed by ASCII value (0-127). 255 = invalid.
    // Handles both uppercase and lowercase input.
    private static ReadOnlySpan<byte> DecodeMap
        => [
            // 0-47
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255,
            // '0'=48..'9'=57
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
            // ':'=58..'@'=64
            255, 255, 255, 255, 255, 255, 255,
            // 'A'=65..'Z'=90  (I, L, O, U excluded by Crockford)
            10, 11, 12, 13, 14, 15, 16, 17, 255, 18,
            19, 255, 20, 21, 255, 22, 23, 24, 25, 26,
            255, 27, 28, 29, 30, 31,
            // '['=91..'`'=96
            255, 255, 255, 255, 255, 255,
            // 'a'=97..'z'=122 (same values as uppercase)
            10, 11, 12, 13, 14, 15, 16, 17, 255, 18,
            19, 255, 20, 21, 255, 22, 23, 24, 25, 26,
            255, 27, 28, 29, 30, 31,
            // '{'=123..DEL=127
            255, 255, 255, 255, 255
        ];

    /// <summary>Creates a new ULID string using the current UTC time.</summary>
    public static string Create() => Create(DateTimeOffset.UtcNow);

    /// <summary>Creates a ULID string with an explicit <paramref name="timestamp" /> (useful for testing).</summary>
    public static string Create(DateTimeOffset timestamp)
    {
        var ms = timestamp.ToUnixTimeMilliseconds();
        Span<byte> rand = stackalloc byte[10]; // 80 random bits — stays on the stack
        CryptographicRandom.Fill(rand);
        return BuildString(ms, rand);
    }

    /// <summary>
    /// Generates <paramref name="count" /> ULID strings in a single batch using the current UTC time. All ULIDs share the same millisecond timestamp captured at batch start.
    /// Uses one RNG fill for all <paramref name="count" /> × 10 random bytes.
    /// </summary>
    public static string[] CreateBulk(int count)
    {
        ArgumentHelpers.ThrowIfNegativeOrZero(count, nameof(count));
        var ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var result = new string[count];
        var buf = new byte[count * 10]; // 10 random bytes per ULID
        CryptographicRandom.Fill(buf);
        for (var i = 0; i < count; i++)
            result[i] = BuildString(ms, buf.AsSpan(i * 10, 10));

        return result;
    }

    /// <summary>Extracts the UTC timestamp embedded in a ULID string.</summary>
    /// <exception cref="ArgumentException">The string is not a valid 26-character ULID.</exception>
    public static DateTimeOffset GetTimestamp(string ulid)
    {
        ArgumentHelpers.ThrowIf(ulid is null || ulid.Length != 26, "ULID must be exactly 26 characters.", nameof(ulid));

        long ms = 0;
        for (var i = 0; i < 10; i++)
            ms = (ms << 5) | DecodeChar(ulid[i], i);

        return DateTimeOffset.FromUnixTimeMilliseconds(ms);
    }

    private static string BuildString(long ms, ReadOnlySpan<byte> rand)
    {
        // 26 Crockford base32 chars: 10 × 5-bit timestamp (MSB first) + 16 × 5-bit random.
        var chars = new char[26];
        chars[0] = Alphabet[(int)((ms >> 45) & 0x1F)];
        chars[1] = Alphabet[(int)((ms >> 40) & 0x1F)];
        chars[2] = Alphabet[(int)((ms >> 35) & 0x1F)];
        chars[3] = Alphabet[(int)((ms >> 30) & 0x1F)];
        chars[4] = Alphabet[(int)((ms >> 25) & 0x1F)];
        chars[5] = Alphabet[(int)((ms >> 20) & 0x1F)];
        chars[6] = Alphabet[(int)((ms >> 15) & 0x1F)];
        chars[7] = Alphabet[(int)((ms >> 10) & 0x1F)];
        chars[8] = Alphabet[(int)((ms >> 5) & 0x1F)];
        chars[9] = Alphabet[(int)(ms & 0x1F)];

        // Pack 10 bytes (80 bits) into 16 × 5-bit base32 characters.
        chars[10] = Alphabet[rand[0] >> 3];
        chars[11] = Alphabet[((rand[0] & 0x07) << 2) | ((rand[1] >> 6) & 0x03)];
        chars[12] = Alphabet[(rand[1] >> 1) & 0x1F];
        chars[13] = Alphabet[((rand[1] & 0x01) << 4) | ((rand[2] >> 4) & 0x0F)];
        chars[14] = Alphabet[((rand[2] & 0x0F) << 1) | ((rand[3] >> 7) & 0x01)];
        chars[15] = Alphabet[(rand[3] >> 2) & 0x1F];
        chars[16] = Alphabet[((rand[3] & 0x03) << 3) | ((rand[4] >> 5) & 0x07)];
        chars[17] = Alphabet[rand[4] & 0x1F];
        chars[18] = Alphabet[rand[5] >> 3];
        chars[19] = Alphabet[((rand[5] & 0x07) << 2) | ((rand[6] >> 6) & 0x03)];
        chars[20] = Alphabet[(rand[6] >> 1) & 0x1F];
        chars[21] = Alphabet[((rand[6] & 0x01) << 4) | ((rand[7] >> 4) & 0x0F)];
        chars[22] = Alphabet[((rand[7] & 0x0F) << 1) | ((rand[8] >> 7) & 0x01)];
        chars[23] = Alphabet[(rand[8] >> 2) & 0x1F];
        chars[24] = Alphabet[((rand[8] & 0x03) << 3) | ((rand[9] >> 5) & 0x07)];
        chars[25] = Alphabet[rand[9] & 0x1F];
        return new(chars);
    }

    private static int DecodeChar(char c, int position)
    {
        ArgumentHelpers.ThrowIf(c >= 128, $"Invalid ULID character '{c}' at position {position}.", "ulid");
        var v = DecodeMap[c];
        ArgumentHelpers.ThrowIf(v == 255, $"Invalid ULID character '{c}' at position {position}.", "ulid");

        return v;
    }
}