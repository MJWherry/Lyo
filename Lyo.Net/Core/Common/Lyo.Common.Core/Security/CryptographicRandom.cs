using System.Security.Cryptography;
using Lyo.Exceptions;

namespace Lyo.Common.Core.Security;

/// <summary>
/// Cryptographically strong random helpers that work on netstandard2.0 (no dependency on <see cref="RandomNumberGenerator.GetBytes(int)" /> /
/// <see cref="RandomNumberGenerator.GetInt32(int, int)" />).
/// </summary>
public static class CryptographicRandom
{
    /// <summary>Array of <paramref name="length" /> cryptographically strong random bytes.</summary>
    public static byte[] GetBytes(int length)
    {
        ArgumentHelpers.ThrowIfNegative(length);
        if (length == 0)
            return [];

        var bytes = new byte[length];
        Fill(bytes);
        return bytes;
    }

    /// <summary>Writes cryptographically strong random bytes into <paramref name="buffer" />.</summary>
    public static void Fill(Span<byte> buffer)
    {
        if (buffer.IsEmpty)
            return;

#if NET10_0_OR_GREATER
        RandomNumberGenerator.Fill(buffer);
#else
        var arr = new byte[buffer.Length];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(arr);

        arr.AsSpan().CopyTo(buffer);
#endif
    }

    /// <summary>
    /// Builds a random string of <paramref name="length" /> characters drawn from <paramref name="alphabet" />. Rejection sampling keeps the distribution uniform even when the
    /// alphabet size does not divide 256 — naive <c>randomByte % alphabet.Length</c> favors the first characters.
    /// </summary>
    /// <param name="length">Number of characters to produce. Zero returns <see cref="string.Empty" />.</param>
    /// <param name="alphabet">Characters to choose from; must be non-empty. Duplicate characters simply weight themselves.</param>
    public static string GetString(int length, string alphabet)
    {
        ArgumentHelpers.ThrowIfNegative(length);
        ArgumentHelpers.ThrowIfNullOrEmpty(alphabet);
        if (length == 0)
            return string.Empty;

        var chars = new char[length];
        var limit = 256 - 256 % alphabet.Length;
        var buffer = new byte[length];
        var produced = 0;
        while (produced < length) {
            Fill(buffer);
            foreach (var b in buffer) {
                if (b >= limit)
                    continue;

                chars[produced++] = alphabet[b % alphabet.Length];
                if (produced == length)
                    break;
            }
        }

        return new(chars);
    }

    /// <summary>Random integer in <c>[<paramref name="fromInclusive" />, <paramref name="toExclusive" />)</c> with an approximately uniform distribution.</summary>
    public static int GetInt32(int fromInclusive, int toExclusive)
    {
        ArgumentHelpers.ThrowIf(fromInclusive >= toExclusive, "toExclusive must be greater than fromInclusive.", nameof(toExclusive));
        var range = (ulong)((long)toExclusive - fromInclusive);
        if (range == 1)
            return fromInclusive;

        var r = NextUInt64Exclusive(range);
        return (int)(fromInclusive + (long)r);
    }

    private static ulong NextUInt64Exclusive(ulong range)
    {
        ArgumentHelpers.ThrowIfLessThan(range, 2UL);
        var limit = ulong.MaxValue - ulong.MaxValue % range;
        var buf = new byte[8];
        ulong value;
        do {
            Fill(buf);
            value = BitConverter.ToUInt64(buf, 0);
        } while (value >= limit);

        return value % range;
    }
}