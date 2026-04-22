using Lyo.Common.Identifiers;
using Lyo.Exceptions.Models;

namespace Lyo.Common.Tests;

public class NanoIdTests
{
    [Fact]
    public void Create_DefaultLength_Returns21Characters() =>
        Assert.Equal(NanoId.DefaultSize, NanoId.Create().Length);

    [Fact]
    public void Create_CustomLength_ReturnsCorrectLength()
    {
        Assert.Equal(10, NanoId.Create(10).Length);
        Assert.Equal(50, NanoId.Create(50).Length);
    }

    [Fact]
    public void Create_UsesOnlyDefaultAlphabetChars()
    {
        var id = NanoId.Create();
        Assert.All(id, c => Assert.Contains(c, NanoId.DefaultAlphabet));
    }

    [Fact]
    public void Create_CustomAlphabet_UsesOnlyThoseChars()
    {
        const string alphabet = "abc";
        var id = NanoId.Create(alphabet, 30);
        Assert.Equal(30, id.Length);
        Assert.All(id, c => Assert.Contains(c, alphabet));
    }

    [Fact]
    public void Create_DifferentOnEachCall() => Assert.NotEqual(NanoId.Create(), NanoId.Create());

    [Fact]
    public void Create_NonPowerOfTwoAlphabet_StillProducesCorrectLength()
    {
        // Alphabet of 10 chars (not a power of 2) exercises rejection sampling.
        const string alphabet = "0123456789";
        var id = NanoId.Create(alphabet, 20);
        Assert.Equal(20, id.Length);
        Assert.All(id, c => Assert.Contains(c, alphabet));
    }

    [Fact]
    public void Create_SingleCharAlphabet_ReturnsRepeatedChar()
    {
        var id = NanoId.Create("X", 5);
        Assert.Equal("XXXXX", id);
    }

    [Fact]
    public void Create_ZeroSize_Throws() =>
        Assert.Throws<ArgumentOutsideRangeException>(() => NanoId.Create(0));

    [Fact]
    public void Create_NegativeSize_Throws() =>
        Assert.Throws<ArgumentOutsideRangeException>(() => NanoId.Create(-1));

    [Fact]
    public void Create_EmptyAlphabet_Throws() =>
        Assert.Throws<ArgumentException>(() => NanoId.Create("", 10));

    [Fact]
    public void Create_AlphabetOver255Chars_Throws() =>
        Assert.Throws<ArgumentException>(() => NanoId.Create(new string('a', 256), 10));

    [Fact]
    public void Create_UniformDistribution_NoCharExcludedForPowerOfTwoAlphabet()
    {
        var counts = new Dictionary<char, int>();
        for (var i = 0; i < 1000; i++)
            foreach (var c in NanoId.Create(21))
                counts[c] = counts.GetValueOrDefault(c) + 1;

        foreach (var c in NanoId.DefaultAlphabet)
            Assert.True(counts.GetValueOrDefault(c) > 0, $"Character '{c}' never appeared.");
    }

    [Fact]
    public void CreateBulk_ReturnsCorrectCount() => Assert.Equal(100, NanoId.CreateBulk(100).Length);

    [Fact]
    public void CreateBulk_AllDefaultLength() => Assert.All(NanoId.CreateBulk(50), id => Assert.Equal(NanoId.DefaultSize, id.Length));

    [Fact]
    public void CreateBulk_AllUnique()
    {
        var ids = NanoId.CreateBulk(1000);
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void CreateBulk_AllValidChars() =>
        Assert.All(NanoId.CreateBulk(50), id => Assert.All(id, c => Assert.Contains(c, NanoId.DefaultAlphabet)));

    [Fact]
    public void CreateBulk_CustomSize_ReturnsCorrectLength() =>
        Assert.All(NanoId.CreateBulk(20, 10), id => Assert.Equal(10, id.Length));

    [Fact]
    public void CreateBulk_CustomAlphabet_UsesOnlyThoseChars()
    {
        const string alphabet = "abc";
        Assert.All(NanoId.CreateBulk(20, alphabet, 15), id => Assert.All(id, c => Assert.Contains(c, alphabet)));
    }

    [Fact]
    public void CreateBulk_ThrowsOnZero() => Assert.Throws<ArgumentOutOfRangeException>(() => NanoId.CreateBulk(0));
}
