using System.Security.Cryptography;

namespace Lyo.Common.Security;

/// <summary>Cryptographically secure random helpers compatible with netstandard2.0 (no dependency on <see cref="RandomNumberGenerator.GetBytes(int)"/> / <see cref="RandomNumberGenerator.GetInt32(int, int)"/>).</summary>
public static class CryptographicRandom
{
    /// <summary>Returns an array of <paramref name="length"/> cryptographically strong random bytes.</summary>
    public static byte[] GetBytes(int length)
    {
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));
        if (length == 0)
            return Array.Empty<byte>();

        var bytes = new byte[length];
        Fill(bytes);
        return bytes;
    }

    /// <summary>Fills <paramref name="buffer"/> with cryptographically strong random bytes.</summary>
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

    /// <summary>Returns a random integer in <c>[<paramref name="fromInclusive"/>, <paramref name="toExclusive"/>)</c> with an approximately uniform distribution.</summary>
    public static int GetInt32(int fromInclusive, int toExclusive)
    {
        if (fromInclusive >= toExclusive)
            throw new ArgumentOutOfRangeException(nameof(toExclusive));

        var range = (ulong)((long)toExclusive - fromInclusive);
        if (range == 1)
            return fromInclusive;

        var r = NextUInt64Exclusive(range);
        return (int)((long)fromInclusive + (long)r);
    }

    private static ulong NextUInt64Exclusive(ulong range)
    {
        if (range < 2)
            throw new ArgumentOutOfRangeException(nameof(range));

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
