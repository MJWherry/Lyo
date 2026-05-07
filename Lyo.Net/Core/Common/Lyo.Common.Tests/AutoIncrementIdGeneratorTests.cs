using Lyo.Common.Identifiers;

namespace Lyo.Common.Tests;

public class AutoIncrementIdGeneratorTests
{
    [Fact]
    public void Next_Int_DefaultStartReturnsOne()
    {
        var gen = new AutoIncrementIdGenerator<int>();
        Assert.Equal(1, gen.Next());
        Assert.Equal(1, gen.Current);
    }

    [Fact]
    public void Next_Long_DefaultStartReturnsOne()
    {
        var gen = new AutoIncrementIdGenerator<long>();
        Assert.Equal(1L, gen.Next());
        Assert.Equal(1L, gen.Current);
    }

    [Fact]
    public void Next_UInt_DefaultStartReturnsOne()
    {
        var gen = new AutoIncrementIdGenerator<uint>();
        Assert.Equal(1u, gen.Next());
        Assert.Equal(1u, gen.Current);
    }

    [Fact]
    public void Next_ULong_DefaultStartReturnsOne()
    {
        var gen = new AutoIncrementIdGenerator<ulong>();
        Assert.Equal(1ul, gen.Next());
        Assert.Equal(1ul, gen.Current);
    }

    [Fact]
    public void SetCurrent_Int_AppliesToNext()
    {
        var gen = new AutoIncrementIdGenerator<int>();
        gen.SetCurrent(41);
        Assert.Equal(42, gen.Next());
    }

    [Fact]
    public void SetCurrent_Long_AppliesToNext()
    {
        var gen = new AutoIncrementIdGenerator<long>();
        gen.SetCurrent(41L);
        Assert.Equal(42L, gen.Next());
    }

    [Fact]
    public void SetCurrent_UInt_AppliesToNext()
    {
        var gen = new AutoIncrementIdGenerator<uint>();
        gen.SetCurrent(41u);
        Assert.Equal(42u, gen.Next());
    }

    [Fact]
    public void SetCurrent_ULong_AppliesToNext()
    {
        var gen = new AutoIncrementIdGenerator<ulong>();
        gen.SetCurrent(41ul);
        Assert.Equal(42ul, gen.Next());
    }

    [Fact]
    public void Constructor_UnsupportedType_Throws()
        => Assert.Throws<NotSupportedException>(() => new AutoIncrementIdGenerator<short>());

    [Fact]
    public void Next_Int_IsMonotonic()
    {
        var gen = new AutoIncrementIdGenerator<int>();
        var prev = gen.Next();
        for (var i = 0; i < 100; i++) {
            var current = gen.Next();
            Assert.True(current > prev);
            prev = current;
        }
    }

    [Fact]
    public void Next_Long_ParallelCalls_AreUniqueAndComplete()
    {
        var gen = new AutoIncrementIdGenerator<long>();
        const int count = 5000;
        var results = new long[count];

        Parallel.For(0, count, i => { results[i] = gen.Next(); });

        Assert.Equal(count, results.Distinct().Count());
        Assert.Equal(count, gen.Current);
        Assert.Equal(1L, results.Min());
        Assert.Equal(count, results.Max());
    }

    [Fact]
    public void Next_Int_ThrowsOnOverflowAtMax()
    {
        var gen = new AutoIncrementIdGenerator<int>(int.MaxValue);
        Assert.Throws<OverflowException>(() => gen.Next());
    }

    [Fact]
    public void Next_Long_ThrowsOnOverflowAtMax()
    {
        var gen = new AutoIncrementIdGenerator<long>(long.MaxValue);
        Assert.Throws<OverflowException>(() => gen.Next());
    }

    [Fact]
    public void Next_UInt_ThrowsOnOverflowAtMax()
    {
        var gen = new AutoIncrementIdGenerator<uint>(uint.MaxValue);
        Assert.Throws<OverflowException>(() => gen.Next());
    }

    [Fact]
    public void Next_ULong_ThrowsOnOverflowAtMax()
    {
        var gen = new AutoIncrementIdGenerator<ulong>(ulong.MaxValue);
        Assert.Throws<OverflowException>(() => gen.Next());
    }

    [Fact]
    public void Shared_IsNotNull()
    {
        Assert.NotNull(AutoIncrementIdGenerator<int>.Shared);
        Assert.NotNull(AutoIncrementIdGenerator<long>.Shared);
        Assert.NotNull(AutoIncrementIdGenerator<uint>.Shared);
        Assert.NotNull(AutoIncrementIdGenerator<ulong>.Shared);
    }
}
