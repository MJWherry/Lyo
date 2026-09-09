using System.Security.Cryptography;
using System.Text;
using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Security;
using Lyo.Exceptions;

namespace Lyo.Common.Core.Identifiers;

/// <summary>UUID factory for RFC 9562 versions and database-optimised COMB variants.</summary>
public static class LyoGuid
{
    // 100-nanosecond ticks from the Gregorian UUID epoch (1582-10-15) to the Unix epoch (1970-01-01).
    private const long GregorianEpochOffset = 0x01B21DD213814000L;

    /// <summary>Mints a time-based GUID of <paramref name="version" /> using the current UTC time.
    /// <para>V3 and V5 are name-based; call <see cref="CreateV3" /> and <see cref="CreateV5" /> directly.</para>
    /// </summary>
    public static Guid Create(GuidVersion version)
        => version switch {
            GuidVersion.V4 => CreateV4(),
            GuidVersion.V6 => CreateV6(),
            GuidVersion.V7 => CreateV7(),
            GuidVersion.CombPostgres => CreateCombPostgres(),
            GuidVersion.CombSqlServer => CreateCombSqlServer(),
            GuidVersion.V3 or GuidVersion.V5 => throw new ArgumentException($"{version} is name-based; call CreateV3() or CreateV5() with a namespace and name.", nameof(version)),
            var _ => throw new ArgumentOutOfRangeException(nameof(version), version, null)
        };

    /// <summary>Mints a version 4 (fully random) GUID.</summary>
    public static Guid CreateV4() => Guid.NewGuid();

    /// <summary>Mints a version 3 (MD5 name-based) GUID per RFC 9562. The same <paramref name="ns" /> and <paramref name="name" /> always produce the same GUID.</summary>
    public static Guid CreateV3(Guid ns, string name)
    {
        ArgumentHelpers.ThrowIfNull(name);
        var input = BuildNameInput(ns, name);
        byte[] hash;
        using (var md5 = MD5.Create())
            hash = md5.ComputeHash(input);

        return FromHashBytes(hash, 0x30);
    }

    /// <summary>Mints a version 5 (SHA-1 name-based) GUID per RFC 9562. The same <paramref name="ns" /> and <paramref name="name" /> always produce the same GUID.</summary>
    public static Guid CreateV5(Guid ns, string name)
    {
        ArgumentHelpers.ThrowIfNull(name);
        var input = BuildNameInput(ns, name);
        byte[] hash;
        using (var sha1 = SHA1.Create())
            hash = sha1.ComputeHash(input);

        return FromHashBytes(hash, 0x50);
    }

    /// <summary>Mints a version 6 (time-ordered, Gregorian epoch) GUID per RFC 9562 using the current UTC time.</summary>
    public static Guid CreateV6() => CreateV6(DateTimeOffset.UtcNow);

    /// <summary>Mints a version 6 GUID with an explicit <paramref name="timestamp" /> (useful for testing).</summary>
    public static Guid CreateV6(DateTimeOffset timestamp) => BuildV6(timestamp.ToUnixTimeMilliseconds());

    /// <summary>
    /// Mints a version 7 (time-ordered, Unix-ms epoch) GUID per RFC 9562 using the current UTC time. On .NET 9+ delegates to <see cref="Guid.CreateVersion7()" /> which
    /// provides sub-millisecond monotonicity; earlier targets use a fully-random rand_a/rand_b region.
    /// </summary>
    public static Guid CreateV7()
#if NET9_0_OR_GREATER
        => Guid.CreateVersion7(); // no-arg form uses the runtime monotonic counter
#else
        => BuildV7(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
#endif

    /// <summary>Mints a version 7 GUID with an explicit <paramref name="timestamp" /> (useful for testing).</summary>
    public static Guid CreateV7(DateTimeOffset timestamp)
#if NET9_0_OR_GREATER
        => Guid.CreateVersion7(timestamp);
#else
        => BuildV7(timestamp.ToUnixTimeMilliseconds());
#endif

    /// <summary>UTC timestamp embedded in a version 6 or version 7 GUID. Both versions have millisecond precision.</summary>
    /// <exception cref="ArgumentException">The GUID is not version 6 or 7.</exception>
    public static DateTimeOffset GetTimestamp(Guid guid)
    {
        // Use ToByteArray() indices so we do not allocate a second RFC-reordered array.
        // ToByteArray() layout: _a LE (b[0..3] = rfc[3,2,1,0]), _b LE (b[4..5] = rfc[5,4]),
        //                       _c LE (b[6..7] = rfc[7,6]),      _d.._k unchanged (b[8..15] = rfc[8..15]).
        // Version nibble: rfc[6] high nibble equals b[7] high nibble.
        var b = guid.ToByteArray();
        var version = (b[7] >> 4) & 0xF;
        return version switch {
            6 => ExtractTimestampV6(b),
            7 => ExtractTimestampV7(b),
            var _ => throw new ArgumentException($"GUID is version {version}; only version 6 and 7 carry an extractable timestamp.", nameof(guid))
        };
    }

    /// <summary>Mints a COMB GUID optimised for PostgreSQL B-tree ordering (timestamp in the leading 6 bytes).</summary>
    public static Guid CreateCombPostgres() => CreateCombPostgres(DateTimeOffset.UtcNow);

    /// <summary>Mints a PostgreSQL COMB GUID with an explicit <paramref name="timestamp" /> (useful for testing).</summary>
    public static Guid CreateCombPostgres(DateTimeOffset timestamp)
    {
        Span<byte> rfc = stackalloc byte[16];
        CryptographicRandom.Fill(rfc);
        WriteTimestampRfc(rfc, timestamp.ToUnixTimeMilliseconds());
        return FromRfcBytes(rfc);
    }

    /// <summary>Mints a COMB GUID optimised for SQL Server uniqueidentifier ordering (timestamp in the trailing 6 bytes).</summary>
    public static Guid CreateCombSqlServer() => CreateCombSqlServer(DateTimeOffset.UtcNow);

    /// <summary>Mints a SQL Server COMB GUID with an explicit <paramref name="timestamp" /> (useful for testing).</summary>
    public static Guid CreateCombSqlServer(DateTimeOffset timestamp)
    {
        Span<byte> rfc = stackalloc byte[16];
        CryptographicRandom.Fill(rfc);
        var ms = timestamp.ToUnixTimeMilliseconds();
        rfc[10] = (byte)(ms >> 40);
        rfc[11] = (byte)(ms >> 32);
        rfc[12] = (byte)(ms >> 24);
        rfc[13] = (byte)(ms >> 16);
        rfc[14] = (byte)(ms >> 8);
        rfc[15] = (byte)ms;
        return FromRfcBytes(rfc);
    }

    /// <summary>Mints <paramref name="count" /> GUIDs of <paramref name="version" /> in one batch.
    /// <para>V3 and V5 are name-based; call <see cref="CreateV3" /> and <see cref="CreateV5" /> directly.</para>
    /// </summary>
    public static Guid[] CreateBulk(GuidVersion version, int count)
        => version switch {
            GuidVersion.V4 => CreateV4Bulk(count),
            GuidVersion.V6 => CreateV6Bulk(count),
            GuidVersion.V7 => CreateV7Bulk(count),
            GuidVersion.CombPostgres => CreateCombPostgresBulk(count),
            GuidVersion.CombSqlServer => CreateCombSqlServerBulk(count),
            GuidVersion.V3 or GuidVersion.V5 => throw new ArgumentException($"{version} is name-based; use CreateV3() or CreateV5() individually.", nameof(version)),
            var _ => throw new ArgumentOutOfRangeException(nameof(version), version, null)
        };

    /// <summary>Mints <paramref name="count" /> version 4 (fully random) GUIDs in one batch. Uses one RNG fill for all <paramref name="count" /> × 16 bytes.</summary>
    public static Guid[] CreateV4Bulk(int count)
    {
        ArgumentHelpers.ThrowIfNegativeOrZero(count);
        var result = new Guid[count];
        var buf = new byte[count * 16];
        CryptographicRandom.Fill(buf);
        for (var i = 0; i < count; i++) {
            var off = i * 16;
            buf[off + 6] = (byte)(0x40 | (buf[off + 6] & 0x0F)); // ver = 4
            buf[off + 8] = (byte)(0x80 | (buf[off + 8] & 0x3F)); // var = 10xx
            result[i] = FromRfcBytes(buf.AsSpan(off, 16));
        }

        return result;
    }

    /// <summary>
    /// Mints <paramref name="count" /> version 6 (time-ordered, Gregorian epoch) GUIDs in one batch. All GUIDs share the same millisecond timestamp captured at batch start.
    /// </summary>
    public static Guid[] CreateV6Bulk(int count)
    {
        ArgumentHelpers.ThrowIfNegativeOrZero(count);
        var t = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 10_000L + GregorianEpochOffset;
        var result = new Guid[count];
        var buf = new byte[count * 16];
        CryptographicRandom.Fill(buf);
        for (var i = 0; i < count; i++) {
            var off = i * 16;
            buf[off] = (byte)(t >> 52);
            buf[off + 1] = (byte)(t >> 44);
            buf[off + 2] = (byte)(t >> 36);
            buf[off + 3] = (byte)(t >> 28);
            buf[off + 4] = (byte)(t >> 20);
            buf[off + 5] = (byte)(t >> 12);
            buf[off + 6] = (byte)(0x60 | ((t >> 8) & 0x0F));
            buf[off + 7] = (byte)(t & 0xFF);
            buf[off + 8] = (byte)(0x80 | (buf[off + 8] & 0x3F));
            result[i] = FromRfcBytes(buf.AsSpan(off, 16));
        }

        return result;
    }

    /// <summary>
    /// Mints <paramref name="count" /> version 7 (time-ordered, Unix-ms epoch) GUIDs in one batch. All GUIDs share the same millisecond timestamp captured at batch start.
    /// </summary>
    public static Guid[] CreateV7Bulk(int count)
    {
        ArgumentHelpers.ThrowIfNegativeOrZero(count);
        var ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var result = new Guid[count];
        var buf = new byte[count * 16];
        CryptographicRandom.Fill(buf);
        for (var i = 0; i < count; i++) {
            var off = i * 16;
            buf[off] = (byte)(ms >> 40);
            buf[off + 1] = (byte)(ms >> 32);
            buf[off + 2] = (byte)(ms >> 24);
            buf[off + 3] = (byte)(ms >> 16);
            buf[off + 4] = (byte)(ms >> 8);
            buf[off + 5] = (byte)ms;
            buf[off + 6] = (byte)(0x70 | (buf[off + 6] & 0x0F));
            buf[off + 8] = (byte)(0x80 | (buf[off + 8] & 0x3F));
            result[i] = FromRfcBytes(buf.AsSpan(off, 16));
        }

        return result;
    }

    /// <summary>Mints <paramref name="count" /> PostgreSQL COMB GUIDs in one batch. All GUIDs share the same millisecond timestamp captured at batch start.</summary>
    public static Guid[] CreateCombPostgresBulk(int count)
    {
        ArgumentHelpers.ThrowIfNegativeOrZero(count);
        var ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var result = new Guid[count];
        var buf = new byte[count * 16];
        CryptographicRandom.Fill(buf);
        for (var i = 0; i < count; i++) {
            var off = i * 16;
            buf[off] = (byte)(ms >> 40);
            buf[off + 1] = (byte)(ms >> 32);
            buf[off + 2] = (byte)(ms >> 24);
            buf[off + 3] = (byte)(ms >> 16);
            buf[off + 4] = (byte)(ms >> 8);
            buf[off + 5] = (byte)ms;
            result[i] = FromRfcBytes(buf.AsSpan(off, 16));
        }

        return result;
    }

    /// <summary>Mints <paramref name="count" /> SQL Server COMB GUIDs in one batch. All GUIDs share the same millisecond timestamp captured at batch start.</summary>
    public static Guid[] CreateCombSqlServerBulk(int count)
    {
        ArgumentHelpers.ThrowIfNegativeOrZero(count);
        var ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var result = new Guid[count];
        var buf = new byte[count * 16];
        CryptographicRandom.Fill(buf);
        for (var i = 0; i < count; i++) {
            var off = i * 16;
            buf[off + 10] = (byte)(ms >> 40);
            buf[off + 11] = (byte)(ms >> 32);
            buf[off + 12] = (byte)(ms >> 24);
            buf[off + 13] = (byte)(ms >> 16);
            buf[off + 14] = (byte)(ms >> 8);
            buf[off + 15] = (byte)ms;
            result[i] = FromRfcBytes(buf.AsSpan(off, 16));
        }

        return result;
    }

    private static Guid BuildV6(long unixMs)
    {
        var t = unixMs * 10_000L + GregorianEpochOffset;
        Span<byte> rfc = stackalloc byte[16];
        CryptographicRandom.Fill(rfc);
        rfc[0] = (byte)(t >> 52);
        rfc[1] = (byte)(t >> 44);
        rfc[2] = (byte)(t >> 36);
        rfc[3] = (byte)(t >> 28);
        rfc[4] = (byte)(t >> 20);
        rfc[5] = (byte)(t >> 12);
        rfc[6] = (byte)(0x60 | ((t >> 8) & 0x0F));
        rfc[7] = (byte)(t & 0xFF);
        rfc[8] = (byte)(0x80 | (rfc[8] & 0x3F));
        return FromRfcBytes(rfc);
    }

    private static Guid BuildV7(long unixMs)
    {
        Span<byte> rfc = stackalloc byte[16];
        CryptographicRandom.Fill(rfc);
        WriteTimestampRfc(rfc, unixMs);
        rfc[6] = (byte)(0x70 | (rfc[6] & 0x0F));
        rfc[8] = (byte)(0x80 | (rfc[8] & 0x3F));
        return FromRfcBytes(rfc);
    }

    private static Guid FromHashBytes(byte[] hash, byte versionNibble)
    {
        Span<byte> rfc = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(rfc);
        rfc[6] = (byte)(versionNibble | (rfc[6] & 0x0F));
        rfc[8] = (byte)(0x80 | (rfc[8] & 0x3F));
        return FromRfcBytes(rfc);
    }

    private static byte[] BuildNameInput(Guid ns, string name)
    {
        var nsBytes = ToRfcBytes(ns);
        var nameBytes = Encoding.UTF8.GetBytes(name);
        var input = new byte[nsBytes.Length + nameBytes.Length];
        nsBytes.CopyTo(input, 0);
        nameBytes.CopyTo(input, nsBytes.Length);
        return input;
    }

    /// <summary>
    /// Builds a <see cref="Guid" /> from 16 bytes in RFC 4122 big-endian order using the <c>(int, short, short, ...)</c> constructor, skipping the byte-group reversal
    /// that <c>new Guid(byte[])</c> applies to the first 8 bytes. Accepts <see cref="ReadOnlySpan{T}" /> so callers can pass stack-allocated buffers.
    /// </summary>
    private static Guid FromRfcBytes(ReadOnlySpan<byte> rfc)
    {
        var a = (rfc[0] << 24) | (rfc[1] << 16) | (rfc[2] << 8) | rfc[3];
        var b = (short)((rfc[4] << 8) | rfc[5]);
        var c = (short)((rfc[6] << 8) | rfc[7]);
        return new(a, b, c, rfc[8], rfc[9], rfc[10], rfc[11], rfc[12], rfc[13], rfc[14], rfc[15]);
    }

    /// <summary>16-byte RFC 4122 big-endian layout of a <see cref="Guid" /> (used for V3/V5 only).</summary>
    private static byte[] ToRfcBytes(Guid guid)
    {
        var b = guid.ToByteArray();
        return [
            b[3], b[2], b[1], b[0], b[5], b[4], b[7], b[6], b[8], b[9],
            b[10], b[11], b[12], b[13], b[14], b[15]
        ];
    }

    private static void WriteTimestampRfc(Span<byte> rfc, long ms)
    {
        rfc[0] = (byte)(ms >> 40);
        rfc[1] = (byte)(ms >> 32);
        rfc[2] = (byte)(ms >> 24);
        rfc[3] = (byte)(ms >> 16);
        rfc[4] = (byte)(ms >> 8);
        rfc[5] = (byte)ms;
    }

    private static DateTimeOffset ExtractTimestampV6(byte[] b)
    {
        // rfc[0..7] maps to b[3], b[2], b[1], b[0], b[5], b[4], b[7], b[6]
        var t = ((long)b[3] << 52) | ((long)b[2] << 44) | ((long)b[1] << 36) | ((long)b[0] << 28) | ((long)b[5] << 20) | ((long)b[4] << 12) | ((long)(b[7] & 0x0F) << 8) | b[6];
        return DateTimeOffset.FromUnixTimeMilliseconds((t - GregorianEpochOffset) / 10_000L);
    }

    private static DateTimeOffset ExtractTimestampV7(byte[] b)
    {
        // rfc[0..5] maps to b[3], b[2], b[1], b[0], b[5], b[4]
        var ms = ((long)b[3] << 40) | ((long)b[2] << 32) | ((long)b[1] << 24) | ((long)b[0] << 16) | ((long)b[5] << 8) | b[4];
        return DateTimeOffset.FromUnixTimeMilliseconds(ms);
    }

    /// <summary>Well-known UUID namespaces from RFC 4122 Appendix C, for use with <see cref="CreateV3" /> and <see cref="CreateV5" />.</summary>
    public static class Namespace
    {
        public static readonly Guid Dns = new("6ba7b810-9dad-11d1-80b4-00c04fd430c8");
        public static readonly Guid Url = new("6ba7b811-9dad-11d1-80b4-00c04fd430c8");
        public static readonly Guid Oid = new("6ba7b812-9dad-11d1-80b4-00c04fd430c8");
        public static readonly Guid X500 = new("6ba7b814-9dad-11d1-80b4-00c04fd430c8");
    }
}