namespace Lyo.Exceptions.Tests;

public class ValueOrChainComparisonTests
{
    [Fact]
    public void IfGreaterThan_Above_ReturnsChosen() => Assert.Equal("yes", 6.IfGreaterThan(5, "yes").OrDefault("no"));

    [Fact]
    public void IfGreaterThan_Equal_ReturnsDefault() => Assert.Equal("no", 5.IfGreaterThan(5, "yes").OrDefault("no"));

    [Fact]
    public void IfGreaterThanOrEqual_Equal_ReturnsChosen() => Assert.Equal("yes", 5.IfGreaterThanOrEqual(5, "yes").OrDefault("no"));

    [Fact]
    public void IfLessThan_Below_ReturnsChosen() => Assert.Equal("yes", 4.IfLessThan(5, "yes").OrDefault("no"));

    [Fact]
    public void IfLessThanOrEqual_Equal_ReturnsChosen() => Assert.Equal("yes", 5.IfLessThanOrEqual(5, "yes").OrDefault("no"));

    [Fact]
    public void IfGreaterThan_DateTime_AfterThreshold_ReturnsChosen()
    {
        var threshold = new DateTime(2024, 6, 1);
        var after = new DateTime(2024, 6, 2);
        Assert.Equal("after", after.IfGreaterThan(threshold, "after").OrDefault("not"));
    }

    [Fact]
    public void IfEqual_Match_ReturnsChosen() => Assert.Equal("yes", 3.IfEqual(3, "yes").OrDefault("no"));

    [Fact]
    public void IfNotEqual_Differs_ReturnsChosen() => Assert.Equal("yes", 3.IfNotEqual(4, "yes").OrDefault("no"));

    [Fact]
    public void IfZero_Zero_ReturnsChosen() => Assert.Equal("yes", 0.IfZero("yes").OrDefault("no"));

    [Fact]
    public void IfZero_NonZero_ReturnsDefault() => Assert.Equal("no", 1.IfZero("yes").OrDefault("no"));

    [Fact]
    public void IfNegative_TimeSpan_ReturnsChosen() => Assert.Equal("yes", TimeSpan.FromMinutes(-1).IfNegative("yes").OrDefault("no"));

    [Fact]
    public void IfNegativeOrZero_Zero_ReturnsChosen() => Assert.Equal("yes", 0.IfNegativeOrZero("yes").OrDefault("no"));

    [Fact]
    public void IfPositive_AboveZero_ReturnsChosen() => Assert.Equal("yes", 1.IfPositive("yes").OrDefault("no"));

    [Fact]
    public void IfPositiveOrZero_Zero_ReturnsChosen() => Assert.Equal("yes", 0.IfPositiveOrZero("yes").OrDefault("no"));

    [Fact]
    public void OrIfGreaterThan_FirstMissSecondHit_ReturnsSecond() => Assert.Equal("second", 5.IfGreaterThan(10, "first").OrIfGreaterThan(4, "second").OrDefault("no"));

    [Fact]
    public void If_PredicateFalse_OrWhenTrue_ReturnsSecond() => Assert.Equal("second", 1.If(v => v > 10, "first").OrWhen(true, "second").OrDefault("no"));
}