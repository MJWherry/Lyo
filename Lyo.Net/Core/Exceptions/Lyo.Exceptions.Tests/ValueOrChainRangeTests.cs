namespace Lyo.Exceptions.Tests;

public class ValueOrChainRangeTests
{
    [Fact]
    public void IfInRange_Int_InclusiveEnds_ReturnsChosen()
    {
        Assert.Equal("in", 1.IfInRange(1, 2, "in").OrDefault("out"));
        Assert.Equal("in", 2.IfInRange(1, 2, "in").OrDefault("out"));
    }

    [Fact]
    public void IfInRange_Int_Outside_ReturnsDefault() => Assert.Equal("out", 3.IfInRange(1, 2, "in").OrDefault("out"));

    [Fact]
    public void IfInRange_Long_TimestampWindow_ReturnsChosen()
    {
        long unix = 1_700_000_000;
        Assert.Equal("in", unix.IfInRange(1_600_000_000, 1_800_000_000, "in").OrDefault("out"));
    }

    [Fact]
    public void IfInRange_DateTime_InclusiveEnds_ReturnsChosen()
    {
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 12, 31);
        Assert.Equal("in", end.IfInRange(start, end, "in").OrDefault("out"));
    }

    [Fact]
    public void IfInRange_DateTime_Outside_ReturnsDefault()
    {
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 12, 31);
        var after = new DateTime(2025, 1, 1);
        Assert.Equal("out", after.IfInRange(start, end, "in").OrDefault("out"));
    }

    [Fact]
    public void IfInRange_DateTimeOffset_Inside_ReturnsChosen()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var value = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero);
        Assert.Equal("in", value.IfInRange(start, end, "in").OrDefault("out"));
    }

    [Fact]
    public void IfInRange_TimeSpan_Inside_ReturnsChosen() => Assert.Equal("in", TimeSpan.FromMinutes(30).IfInRange(TimeSpan.Zero, TimeSpan.FromHours(1), "in").OrDefault("out"));

    [Fact]
    public void IfInRange_DateOnly_Inside_ReturnsChosen()
    {
        var day = new DateOnly(2024, 7, 1);
        Assert.Equal("in", day.IfInRange(new(2024, 6, 1), new(2024, 8, 31), "in").OrDefault("out"));
    }

    [Fact]
    public void IfInRange_TimeOnly_Inside_ReturnsChosen() => Assert.Equal("in", new TimeOnly(12, 0).IfInRange(new(9, 0), new(17, 0), "in").OrDefault("out"));

    [Fact]
    public void IfInRange_String_Inside_ReturnsChosen() => Assert.Equal("in", "m".IfInRange("a", "z", "in").OrDefault("out"));

    [Fact]
    public void IfInRange_NullableDateTime_Null_ReturnsDefault()
    {
        DateTime? created = null;
        Assert.Equal("out", created.IfInRange(DateTime.MinValue, DateTime.MaxValue, "in").OrDefault("out"));
    }

    [Fact]
    public void IfInRange_ChosenNull_OrDefault_ReturnsNull()
    {
        string? chosen = null;
        Assert.Null(1.IfInRange(1, 2, chosen).OrDefault("fallback"));
    }

    [Fact]
    public void IfInRange_Func_Outside_DoesNotInvoke()
    {
        var invoked = false;
        var result = 5.IfInRange(
                1, 2, () => {
                    invoked = true;
                    return "in";
                })
            .OrDefault("out");

        Assert.Equal("out", result);
        Assert.False(invoked);
    }

    [Fact]
    public void IfNotInRange_Outside_ReturnsChosen() => Assert.Equal("out", 5.IfNotInRange(1, 2, "out").OrDefault("in"));

    [Fact]
    public void OrIfInRange_FirstMissSecondHit_ReturnsSecond() => Assert.Equal("mid", 3.IfInRange(1, 2, "low").OrIfInRange(3, 4, "mid").OrDefault("other"));

    [Fact]
    public void OrIfInRange_FirstHit_DoesNotInvokeLaterFactory()
    {
        var invoked = false;
        var result = 1.IfInRange(1, 2, "low")
            .OrIfInRange(
                3, 4, () => {
                    invoked = true;
                    return "mid";
                })
            .OrDefault("other");

        Assert.Equal("low", result);
        Assert.False(invoked);
    }

    [Fact]
    public void IfInRange_InvertedBounds_Throws() => Assert.Throws<ArgumentException>(() => 1.IfInRange(3, 1, "x"));

    [Fact]
    public void OrThrow_Open_ThrowsFactoryException() => Assert.Throws<KeyNotFoundException>(() => 5.IfInRange(1, 2, "in").OrThrow(() => new KeyNotFoundException("missing")));

    [Fact]
    public void OrThrow_Chosen_ReturnsValue() => Assert.Equal("in", 1.IfInRange(1, 2, "in").OrThrow(() => new KeyNotFoundException("missing")));

    [Fact]
    public void OrThrowInvalidOperation_Open_Throws() => Assert.Throws<InvalidOperationException>(() => 5.IfInRange(1, 2, "in").OrThrowInvalidOperation());
}