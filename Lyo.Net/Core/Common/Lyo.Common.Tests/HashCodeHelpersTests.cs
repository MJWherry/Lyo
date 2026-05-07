namespace Lyo.Common.Tests;

public class HashCodeHelpersTests
{
    [Fact]
    public void Combine_WithSameInputs_ReturnsSameHash()
    {
        var first = HashCodeHelpers.Combine("abc", 123, null);
        var second = HashCodeHelpers.Combine("abc", 123, null);
        Assert.Equal(first, second);
    }

    [Fact]
    public void Combine_WithDifferentInputs_ReturnsDifferentHash()
    {
        var first = HashCodeHelpers.Combine("abc", 123);
        var second = HashCodeHelpers.Combine("abc", 124);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Combine_WithNullValues_DoesNotThrow()
    {
        var hash = HashCodeHelpers.Combine(null, null);
        Assert.IsType<int>(hash);
    }
}