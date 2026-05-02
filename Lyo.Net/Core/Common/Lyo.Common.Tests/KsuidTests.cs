using Lyo.Common.Identifiers;
using Lyo.Exceptions.Models;

namespace Lyo.Common.Tests;

public class KsuidTests
{
    private const string Base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    [Fact]
    public void Create_Returns27Characters() => Assert.Equal(27, Ksuid.Create().Length);

    [Fact]
    public void Create_UsesOnlyBase62Chars()
    {
        var id = Ksuid.Create();
        Assert.All(id, c => Assert.Contains(c, Base62Chars));
    }

    [Fact]
    public void Create_DifferentOnEachCall() => Assert.NotEqual(Ksuid.Create(), Ksuid.Create());

    [Fact]
    public void Create_LaterTimestampProducesLexicographicallyLaterKsuid()
    {
        var t1 = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var t2 = t1.AddSeconds(1);
        var k1 = Ksuid.Create(t1);
        var k2 = Ksuid.Create(t2);
        Assert.True(string.Compare(k1, k2, StringComparison.Ordinal) < 0, "KSUID with earlier timestamp should sort before one with later timestamp.");
    }

    [Fact]
    public void GetTimestamp_RoundTrips_ToSecondPrecision()
    {
        // KSUID has second precision, so sub-second components are lost.
        var ts = new DateTimeOffset(2025, 6, 15, 12, 30, 45, 0, TimeSpan.Zero);
        var id = Ksuid.Create(ts);
        var recovered = Ksuid.GetTimestamp(id);
        Assert.Equal(ts.ToUnixTimeSeconds(), recovered.ToUnixTimeSeconds());
    }

    [Fact]
    public void GetTimestamp_TooShort_Throws() => Assert.Throws<ArgumentException>(() => Ksuid.GetTimestamp("TOOSHORT"));

    [Fact]
    public void GetTimestamp_TooLong_Throws() => Assert.Throws<ArgumentException>(() => Ksuid.GetTimestamp(new('0', 28)));

    [Fact]
    public void GetTimestamp_InvalidChar_Throws() => Assert.Throws<ArgumentException>(() => Ksuid.GetTimestamp(new('!', 27)));

    [Fact]
    public void GetTimestamp_Null_Throws() => Assert.Throws<ArgumentException>(() => Ksuid.GetTimestamp(null!));

    [Fact]
    public void Create_SameTimestampDifferentRandom()
    {
        var ts = DateTimeOffset.UtcNow;
        var k1 = Ksuid.Create(ts);
        var k2 = Ksuid.Create(ts);
        // Same second-precision timestamp means the first ~7 chars should be very close
        // (may differ only by random carry), but the full IDs almost certainly differ.
        Assert.NotEqual(k1, k2);
    }

    [Fact]
    public void Create_BeforeEpoch_DoesNotThrow()
    {
        var priorToEpoch = new DateTimeOffset(2010, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var id = Ksuid.Create(priorToEpoch);
        Assert.Equal(27, id.Length);
    }

    [Fact]
    public void CreateBulk_ReturnsCorrectCount() => Assert.Equal(100, Ksuid.CreateBulk(100).Length);

    [Fact]
    public void CreateBulk_AllUnique()
    {
        var ids = Ksuid.CreateBulk(500);
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void CreateBulk_AllValidLength() => Assert.All(Ksuid.CreateBulk(50), id => Assert.Equal(27, id.Length));

    [Fact]
    public void CreateBulk_AllShareSameTimestampPrefix()
    {
        // All KSUID in a batch are generated within the same second, so the first
        // character(s) encoding the timestamp should be identical.
        var ids = Ksuid.CreateBulk(10);
        var prefix = ids[0][..4];
        Assert.All(ids, id => Assert.Equal(prefix, id[..4]));
    }

    [Fact]
    public void CreateBulk_ThrowsOnZero() => Assert.Throws<ArgumentOutsideRangeException>(() => Ksuid.CreateBulk(0));
}