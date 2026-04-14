namespace Lyo.Common.Tests;

public class EnsureTests
{
    [Fact]
    public void NotNull_WithNonNullValue_ReturnsSuccess()
    {
        var result = Ensure.NotNull("hello");
        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Data);
    }

    [Fact]
    public void NotNull_WithNullValue_ReturnsFailure()
    {
        string? nullStr = null;
        var result = Ensure.NotNull(nullStr);
        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors!);
        Assert.Equal("NULL_VALUE", result.Errors![0].Code);
    }

    [Fact]
    public void NotEmpty_WithNonEmptyString_ReturnsSuccess()
    {
        var result = Ensure.NotEmpty("abc");
        Assert.True(result.IsSuccess);
        Assert.Equal("abc", result.Data);
    }

    [Fact]
    public void NotEmpty_WithEmptyString_ReturnsFailure()
    {
        var result = Ensure.NotEmpty("");
        Assert.False(result.IsSuccess);
        Assert.Equal("EMPTY_STRING", result.Errors![0].Code);
    }

    [Fact]
    public void NotEmpty_WithNullString_ReturnsFailure()
    {
        var result = Ensure.NotEmpty(null);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void NotWhiteSpace_WithWhiteSpace_ReturnsFailure()
    {
        var result = Ensure.NotWhiteSpace("   ");
        Assert.False(result.IsSuccess);
        Assert.Equal("WHITESPACE_STRING", result.Errors![0].Code);
    }

    [Fact]
    public void InRange_WithValueInRange_ReturnsSuccess()
    {
        var result = Ensure.InRange(5, 1, 10);
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Data);
    }

    [Fact]
    public void InRange_WithValueBelowMin_ReturnsFailure()
    {
        var result = Ensure.InRange(0, 1, 10);
        Assert.False(result.IsSuccess);
        Assert.Equal("OUT_OF_RANGE", result.Errors![0].Code);
    }

    [Fact]
    public void InRange_WithValueAboveMax_ReturnsFailure()
    {
        var result = Ensure.InRange(11, 1, 10);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void That_WithConditionTrue_ReturnsSuccess()
    {
        var result = Ensure.That(42, x => x > 0, "INVALID", "Must be positive");
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Data);
    }

    [Fact]
    public void That_WithConditionFalse_ReturnsFailure()
    {
        var result = Ensure.That(-1, x => x > 0, "INVALID", "Must be positive");
        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID", result.Errors![0].Code);
        Assert.Equal("Must be positive", result.Errors![0].Message);
    }
}