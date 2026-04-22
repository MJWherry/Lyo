using Lyo.Common.Enums;
using Lyo.Common.Identifiers;
using Lyo.Exceptions.Models;

namespace Lyo.Common.Tests;

public class LyoGuidTests
{
    private static bool AllUnique(Guid[] ids) => ids.Distinct().Count() == ids.Length;

    [Fact]
    public void CreateV4Bulk_ReturnsCorrectCount() => Assert.Equal(100, LyoGuid.CreateV4Bulk(100).Length);

    [Fact]
    public void CreateV4Bulk_AllUnique() => Assert.True(AllUnique(LyoGuid.CreateV4Bulk(1000)));

    [Fact]
    public void CreateV4Bulk_AllVersionFour() => Assert.All(LyoGuid.CreateV4Bulk(50), g => Assert.Equal('4', Version(g)));

    [Fact]
    public void CreateV4Bulk_ThrowsOnZero() => Assert.Throws<ArgumentOutsideRangeException>(() => LyoGuid.CreateV4Bulk(0));

    [Fact]
    public void CreateV6Bulk_ReturnsCorrectCount() => Assert.Equal(100, LyoGuid.CreateV6Bulk(100).Length);

    [Fact]
    public void CreateV6Bulk_AllUnique() => Assert.True(AllUnique(LyoGuid.CreateV6Bulk(1000)));

    [Fact]
    public void CreateV6Bulk_AllVersionSix() => Assert.All(LyoGuid.CreateV6Bulk(50), g => Assert.Equal('6', Version(g)));

    [Fact]
    public void CreateV7Bulk_ReturnsCorrectCount() => Assert.Equal(100, LyoGuid.CreateV7Bulk(100).Length);

    [Fact]
    public void CreateV7Bulk_AllUnique() => Assert.True(AllUnique(LyoGuid.CreateV7Bulk(1000)));

    [Fact]
    public void CreateV7Bulk_AllVersionSeven() => Assert.All(LyoGuid.CreateV7Bulk(50), g => Assert.Equal('7', Version(g)));

    [Fact]
    public void CreateCombPostgresBulk_ReturnsCorrectCount() => Assert.Equal(100, LyoGuid.CreateCombPostgresBulk(100).Length);

    [Fact]
    public void CreateCombPostgresBulk_AllUnique() => Assert.True(AllUnique(LyoGuid.CreateCombPostgresBulk(500)));

    [Fact]
    public void CreateCombSqlServerBulk_ReturnsCorrectCount() => Assert.Equal(100, LyoGuid.CreateCombSqlServerBulk(100).Length);

    [Fact]
    public void CreateCombSqlServerBulk_AllUnique() => Assert.True(AllUnique(LyoGuid.CreateCombSqlServerBulk(500)));

    [Fact]
    public void CreateBulk_V4_Dispatches() => Assert.Equal(10, LyoGuid.CreateBulk(GuidVersion.V4, 10).Length);

    [Fact]
    public void CreateBulk_V6_Dispatches() => Assert.Equal(10, LyoGuid.CreateBulk(GuidVersion.V6, 10).Length);

    [Fact]
    public void CreateBulk_V7_Dispatches() => Assert.Equal(10, LyoGuid.CreateBulk(GuidVersion.V7, 10).Length);

    [Fact]
    public void CreateBulk_V3_Throws() => Assert.Throws<ArgumentException>(() => LyoGuid.CreateBulk(GuidVersion.V3, 1));

    [Fact]
    public void CreateBulk_V5_Throws() => Assert.Throws<ArgumentException>(() => LyoGuid.CreateBulk(GuidVersion.V5, 1));

    /// <summary>Returns the UUID version digit from the standard formatted string (position 14 in "xxxxxxxx-xxxx-Vxxx-…").</summary>
    private static char Version(Guid g) => g.ToString()[14];

    /// <summary>Returns the UUID variant bits from the first byte of the third octet group.</summary>
    private static int Variant(Guid g)
    {
        var b = g.ToByteArray();
        // bytes[8] in ToByteArray() corresponds to rfc[8], stored as-is (_d field).
        return (b[8] >> 6) & 0x3; // 0b10 = 2 for RFC 4122 variant
    }

    private static bool SortsAfter(Guid first, Guid second) => string.Compare(first.ToString("N"), second.ToString("N"), StringComparison.Ordinal) < 0;

    [Fact]
    public void V4_VersionNibbleIsCorrect() => Assert.Equal('4', Version(LyoGuid.CreateV4()));

    [Fact]
    public void V4_VariantBitsAreCorrect() => Assert.Equal(2, Variant(LyoGuid.CreateV4()));

    [Fact]
    public void V4_DifferentOnEachCall() => Assert.NotEqual(LyoGuid.CreateV4(), LyoGuid.CreateV4());

    [Fact]
    public void V3_VersionNibbleIsCorrect() => Assert.Equal('3', Version(LyoGuid.CreateV3(LyoGuid.Namespace.Dns, "test")));

    [Fact]
    public void V3_VariantBitsAreCorrect() => Assert.Equal(2, Variant(LyoGuid.CreateV3(LyoGuid.Namespace.Dns, "test")));

    [Fact]
    public void V3_IsDeterministic()
    {
        var a = LyoGuid.CreateV3(LyoGuid.Namespace.Dns, "www.example.com");
        var b = LyoGuid.CreateV3(LyoGuid.Namespace.Dns, "www.example.com");
        Assert.Equal(a, b);
    }

    [Fact]
    public void V3_DifferentNamesProduceDifferentGuids()
    {
        var a = LyoGuid.CreateV3(LyoGuid.Namespace.Dns, "a.example.com");
        var b = LyoGuid.CreateV3(LyoGuid.Namespace.Dns, "b.example.com");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void V3_DifferentNamespacesProduceDifferentGuids()
    {
        var a = LyoGuid.CreateV3(LyoGuid.Namespace.Dns, "example.com");
        var b = LyoGuid.CreateV3(LyoGuid.Namespace.Url, "example.com");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void V3_NullNameThrows() => Assert.Throws<ArgumentNullException>(() => LyoGuid.CreateV3(LyoGuid.Namespace.Dns, null!));

    [Fact]
    public void V5_VersionNibbleIsCorrect() => Assert.Equal('5', Version(LyoGuid.CreateV5(LyoGuid.Namespace.Dns, "test")));

    [Fact]
    public void V5_VariantBitsAreCorrect() => Assert.Equal(2, Variant(LyoGuid.CreateV5(LyoGuid.Namespace.Dns, "test")));

    [Fact]
    public void V5_IsDeterministic()
    {
        var a = LyoGuid.CreateV5(LyoGuid.Namespace.Url, "https://example.com");
        var b = LyoGuid.CreateV5(LyoGuid.Namespace.Url, "https://example.com");
        Assert.Equal(a, b);
    }

    [Fact]
    public void V5_DiffersFromV3ForSameInput()
    {
        var v3 = LyoGuid.CreateV3(LyoGuid.Namespace.Dns, "example.com");
        var v5 = LyoGuid.CreateV5(LyoGuid.Namespace.Dns, "example.com");
        Assert.NotEqual(v3, v5);
    }

    [Fact]
    public void V5_NullNameThrows() => Assert.Throws<ArgumentNullException>(() => LyoGuid.CreateV5(LyoGuid.Namespace.Dns, null!));

    [Fact]
    public void V6_VersionNibbleIsCorrect() => Assert.Equal('6', Version(LyoGuid.CreateV6()));

    [Fact]
    public void V6_VariantBitsAreCorrect() => Assert.Equal(2, Variant(LyoGuid.CreateV6()));

    [Fact]
    public void V6_DifferentOnEachCall() => Assert.NotEqual(LyoGuid.CreateV6(), LyoGuid.CreateV6());

    [Fact]
    public void V6_TimestampRoundTrips()
    {
        var ts = new DateTimeOffset(2025, 6, 15, 12, 30, 0, TimeSpan.Zero);
        var guid = LyoGuid.CreateV6(ts);
        var recovered = LyoGuid.GetTimestamp(guid);
        // V6 has ms precision (rounded down from 100-ns intervals).
        Assert.Equal(ts.ToUnixTimeMilliseconds(), recovered.ToUnixTimeMilliseconds());
    }

    [Fact]
    public void V6_LaterTimestampSortsLater()
    {
        var t1 = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var t2 = t1.AddMilliseconds(1);
        var g1 = LyoGuid.CreateV6(t1);
        var g2 = LyoGuid.CreateV6(t2);
        Assert.True(SortsAfter(g1, g2), "Earlier V6 GUID should sort before later one.");
    }

    [Fact]
    public void V7_VersionNibbleIsCorrect() => Assert.Equal('7', Version(LyoGuid.CreateV7()));

    [Fact]
    public void V7_VariantBitsAreCorrect() => Assert.Equal(2, Variant(LyoGuid.CreateV7()));

    [Fact]
    public void V7_DifferentOnEachCall() => Assert.NotEqual(LyoGuid.CreateV7(), LyoGuid.CreateV7());

    [Fact]
    public void V7_TimestampRoundTrips()
    {
        var ts = new DateTimeOffset(2025, 6, 15, 12, 30, 45, 123, TimeSpan.Zero);
        var guid = LyoGuid.CreateV7(ts);
        var recovered = LyoGuid.GetTimestamp(guid);
        Assert.Equal(ts.ToUnixTimeMilliseconds(), recovered.ToUnixTimeMilliseconds());
    }

    [Fact]
    public void V7_LaterTimestampSortsLater()
    {
        var t1 = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var t2 = t1.AddMilliseconds(1);
        var g1 = LyoGuid.CreateV7(t1);
        var g2 = LyoGuid.CreateV7(t2);
        Assert.True(SortsAfter(g1, g2), "Earlier V7 GUID should sort before later one.");
    }

    [Fact]
    public void GetTimestamp_OnV4_Throws()
    {
        var v4 = LyoGuid.CreateV4();
        Assert.Throws<ArgumentException>(() => LyoGuid.GetTimestamp(v4));
    }

    [Fact]
    public void GetTimestamp_OnV3_Throws()
    {
        var v3 = LyoGuid.CreateV3(LyoGuid.Namespace.Dns, "test");
        Assert.Throws<ArgumentException>(() => LyoGuid.GetTimestamp(v3));
    }

    [Theory]
    [InlineData(GuidVersion.V4, '4')]
    [InlineData(GuidVersion.V6, '6')]
    [InlineData(GuidVersion.V7, '7')]
    public void Create_DispatchesToCorrectVersion(GuidVersion version, char expectedVersionChar) => Assert.Equal(expectedVersionChar, Version(LyoGuid.Create(version)));

    [Theory]
    [InlineData(GuidVersion.V3)]
    [InlineData(GuidVersion.V5)]
    public void Create_NameBasedVersions_Throw(GuidVersion version) => Assert.Throws<ArgumentException>(() => LyoGuid.Create(version));

    [Fact]
    public void CombPostgres_DifferentOnEachCall() => Assert.NotEqual(LyoGuid.CreateCombPostgres(), LyoGuid.CreateCombPostgres());

    [Fact]
    public void CombPostgres_LaterTimestampSortsLater()
    {
        var t1 = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var t2 = t1.AddMilliseconds(1);
        var g1 = LyoGuid.CreateCombPostgres(t1);
        var g2 = LyoGuid.CreateCombPostgres(t2);
        Assert.True(SortsAfter(g1, g2), "Earlier COMB-Postgres GUID should sort before later one.");
    }

    [Fact]
    public void CombSqlServer_DifferentOnEachCall() => Assert.NotEqual(LyoGuid.CreateCombSqlServer(), LyoGuid.CreateCombSqlServer());

    [Fact]
    public void CombSqlServer_TimestampInTrailingBytes()
    {
        var ts = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var ms = ts.ToUnixTimeMilliseconds();
        var guid = LyoGuid.CreateCombSqlServer(ts);
        var b = guid.ToByteArray();
        // Bytes 10-15 are stored as-is and carry the timestamp big-endian.
        var recovered = ((long)b[10] << 40) | ((long)b[11] << 32) | ((long)b[12] << 24) | ((long)b[13] << 16) | ((long)b[14] << 8) | b[15];
        Assert.Equal(ms, recovered);
    }
}