using Lyo.Common.Security;
using Lyo.Exceptions;

namespace Lyo.Common.Identifiers;

/// <summary>
/// KSUID (K-Sortable Unique Identifier) generator. A KSUID is a 27-character base62 string encoding 20 bytes: a 32-bit second-precision timestamp (relative to
/// 2014-05-13T16:53:20Z) followed by 128 random bits. KSUIDs sort lexicographically by creation time and are safe to use in URLs.
/// </summary>
public static class Ksuid
{
    // Seconds between the Unix epoch (1970-01-01) and the KSUID epoch (2014-05-13T16:53:20Z).
    private const long EpochOffsetSeconds = 1_400_000_000L;

    // base62: digits, uppercase, lowercase — chosen for natural lexicographic sort order.
    private const string Base62Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    private const int StringLength = 27;
    private const int PayloadLength = 20; // 4-byte timestamp + 16-byte random

    // O(1) decode table indexed by ASCII value (0-127). 255 = invalid.
    private static ReadOnlySpan<byte> DecodeMap
        => [
            // 0-47
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255,
            // '0'=48..'9'=57 → 0..9
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
            // ':'=58..'@'=64
            255, 255, 255, 255, 255, 255, 255,
            // 'A'=65..'Z'=90 → 10..35
            10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
            20, 21, 22, 23, 24, 25, 26, 27, 28, 29,
            30, 31, 32, 33, 34, 35,
            // '['=91..'`'=96
            255, 255, 255, 255, 255, 255,
            // 'a'=97..'z'=122 → 36..61
            36, 37, 38, 39, 40, 41, 42, 43, 44, 45,
            46, 47, 48, 49, 50, 51, 52, 53, 54, 55,
            56, 57, 58, 59, 60, 61,
            // '{'=123..DEL=127
            255, 255, 255, 255, 255
        ];

    /// <summary>Creates a new KSUID string using the current UTC time.</summary>
    public static string Create() => Create(DateTimeOffset.UtcNow);

    /// <summary>Creates a KSUID string with an explicit <paramref name="timestamp" /> (useful for testing).</summary>
    public static string Create(DateTimeOffset timestamp)
    {
        var secs = Math.Max(0L, timestamp.ToUnixTimeSeconds() - EpochOffsetSeconds);
        Span<byte> bytes = stackalloc byte[20]; // stays on the stack
        bytes[0] = (byte)(secs >> 24);
        bytes[1] = (byte)(secs >> 16);
        bytes[2] = (byte)(secs >> 8);
        bytes[3] = (byte)secs;
        CryptographicRandom.Fill(bytes[4..]);
        return EncodeBase62(bytes);
    }

    /// <summary>
    /// Generates <paramref name="count" /> KSUID strings in a single batch using the current UTC time. All KSUIDs share the same second-precision timestamp captured at batch
    /// start. Uses one RNG fill for all <paramref name="count" /> × 16 random bytes.
    /// </summary>
    public static string[] CreateBulk(int count)
    {
        ArgumentHelpers.ThrowIfNegativeOrZero(count, nameof(count));
        var secs = Math.Max(0L, DateTimeOffset.UtcNow.ToUnixTimeSeconds() - EpochOffsetSeconds);
        var result = new string[count];
        var buf = new byte[count * 16]; // 16 random bytes per KSUID
        CryptographicRandom.Fill(buf);

        // Reusable 20-byte payload buffer: 4-byte timestamp (constant) + 16-byte random slice.
        Span<byte> payload = stackalloc byte[20];
        payload[0] = (byte)(secs >> 24);
        payload[1] = (byte)(secs >> 16);
        payload[2] = (byte)(secs >> 8);
        payload[3] = (byte)secs;
        for (var i = 0; i < count; i++) {
            buf.AsSpan(i * 16, 16).CopyTo(payload[4..]);
            result[i] = EncodeBase62(payload);
        }

        return result;
    }

    /// <summary>Extracts the UTC timestamp (second precision) from a KSUID string.</summary>
    /// <exception cref="ArgumentException">The string is not a valid 27-character KSUID.</exception>
    public static DateTimeOffset GetTimestamp(string ksuid)
    {
        ArgumentHelpers.ThrowIf(string.IsNullOrEmpty(ksuid) || ksuid.Length != StringLength, $"KSUID must be exactly {StringLength} characters.", nameof(ksuid));
        Span<byte> bytes = stackalloc byte[PayloadLength];
        DecodeBase62(ksuid, bytes);
        var secs = ((long)bytes[0] << 24) | ((long)bytes[1] << 16) | ((long)bytes[2] << 8) | bytes[3];
        return DateTimeOffset.FromUnixTimeSeconds(secs + EpochOffsetSeconds);
    }

    private static string EncodeBase62(ReadOnlySpan<byte> bytes)
    {
        // Treat 20 bytes as a 160-bit big-endian unsigned integer stored as five 32-bit words,
        // then perform repeated division by 62, filling the result right-to-left.
        Span<uint> words = stackalloc uint[5];
        for (var i = 0; i < 5; i++)
            words[i] = (uint)((bytes[i * 4] << 24) | (bytes[i * 4 + 1] << 16) | (bytes[i * 4 + 2] << 8) | bytes[i * 4 + 3]);

        var result = new char[StringLength];
        for (var i = StringLength - 1; i >= 0; i--) {
            ulong remainder = 0;
            for (var j = 0; j < 5; j++) {
                var cur = (remainder << 32) | words[j];
                words[j] = (uint)(cur / 62);
                remainder = cur % 62;
            }

            result[i] = Base62Alphabet[(int)remainder];
        }

        return new(result);
    }

    private static void DecodeBase62(string ksuid, Span<byte> output)
    {
        Span<uint> words = stackalloc uint[5];
        foreach (var c in ksuid) {
            if (c >= 128)
                throw new ArgumentException($"Invalid KSUID character '{c}'.", nameof(ksuid));

            var value = DecodeMap[c];
            if (value == 255)
                throw new ArgumentException($"Invalid KSUID character '{c}'.", nameof(ksuid));

            ulong carry = value;
            for (var j = 4; j >= 0; j--) {
                var cur = (ulong)words[j] * 62 + carry;
                words[j] = (uint)(cur & 0xFFFF_FFFF);
                carry = cur >> 32;
            }
        }

        for (var i = 0; i < 5; i++) {
            output[i * 4] = (byte)(words[i] >> 24);
            output[i * 4 + 1] = (byte)(words[i] >> 16);
            output[i * 4 + 2] = (byte)(words[i] >> 8);
            output[i * 4 + 3] = (byte)words[i];
        }
    }
}