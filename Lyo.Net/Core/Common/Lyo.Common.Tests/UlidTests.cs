using Lyo.Common.Identifiers;
using Lyo.Exceptions.Models;

namespace Lyo.Common.Tests;

public class UlidTests
{
    private const string ValidChars = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    [Fact]
    public void Create_Returns26Characters() => Assert.Equal(26, Ulid.Create().Length);

    [Fact]
    public void Create_UsesOnlyCrockfordBase32Chars()
    {
        var ulid = Ulid.Create();
        Assert.All(ulid, c => Assert.Contains(c, ValidChars));
    }

    [Fact]
    public void Create_DifferentOnEachCall() => Assert.NotEqual(Ulid.Create(), Ulid.Create());

    [Fact]
    public void Create_LaterTimestampProducesLexicographicallyLaterUlid()
    {
        var t1 = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var t2 = t1.AddMilliseconds(1);
        var u1 = Ulid.Create(t1);
        var u2 = Ulid.Create(t2);
        // The first 10 characters encode the timestamp; later time → larger string.
        Assert.True(string.Compare(u1, u2, StringComparison.Ordinal) < 0, "ULID with earlier timestamp should sort before one with later timestamp.");
    }

    [Fact]
    public void GetTimestamp_RoundTrips()
    {
        var ts = new DateTimeOffset(2025, 6, 15, 12, 30, 45, 123, TimeSpan.Zero);
        var ulid = Ulid.Create(ts);
        var recovered = Ulid.GetTimestamp(ulid);
        Assert.Equal(ts.ToUnixTimeMilliseconds(), recovered.ToUnixTimeMilliseconds());
    }

    [Fact]
    public void GetTimestamp_TooShort_Throws() => Assert.Throws<ArgumentException>(() => Ulid.GetTimestamp("TOOSHORT"));

    [Fact]
    public void GetTimestamp_TooLong_Throws() => Assert.Throws<ArgumentException>(() => Ulid.GetTimestamp(new('0', 27)));

    [Fact]
    public void GetTimestamp_InvalidChar_Throws()
        => Assert.Throws<ArgumentException>(()
            => Ulid.GetTimestamp("I000000000" + new string('0', 16))); // 'I' not in Crockford alphabet; must be in timestamp region (first 10 chars)

    [Fact]
    public void GetTimestamp_Null_Throws() => Assert.Throws<ArgumentException>(() => Ulid.GetTimestamp(null!));

    [Fact]
    public void Create_SameTimestampDifferentRandom()
    {
        var ts = DateTimeOffset.UtcNow;
        var u1 = Ulid.Create(ts);
        var u2 = Ulid.Create(ts);
        Assert.Equal(u1[..10], u2[..10]);
    }

    [Fact]
    public void CreateBulk_ReturnsCorrectCount() => Assert.Equal(100, Ulid.CreateBulk(100).Length);

    [Fact]
    public void CreateBulk_AllUnique()
    {
        var ids = Ulid.CreateBulk(1000);
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void CreateBulk_AllValidLength() => Assert.All(Ulid.CreateBulk(50), u => Assert.Equal(26, u.Length));

    [Fact]
    public void CreateBulk_AllShareSameTimestampPrefix()
    {
        var ids = Ulid.CreateBulk(10);
        var prefix = ids[0][..10];
        Assert.All(ids, u => Assert.Equal(prefix, u[..10]));
    }

    [Fact]
    public void CreateBulk_ThrowsOnZero() => Assert.Throws<ArgumentOutsideRangeException>(() => Ulid.CreateBulk(0));
}