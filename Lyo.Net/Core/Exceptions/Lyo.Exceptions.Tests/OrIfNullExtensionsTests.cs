namespace Lyo.Exceptions.Tests;

public class OrIfNullExtensionsTests
{
    [Fact]
    public void OrIfNull_Reference_FirstPresent_ReturnsFirst() => Assert.Equal("a", "a".OrIfNull("b"));

    [Fact]
    public void OrIfNull_Reference_FirstNull_UsesSecond()
    {
        string? first = null;
        Assert.Equal("b", first.OrIfNull("b"));
    }

    [Fact]
    public void OrIfNull_Reference_Chain_SkipsNullsUntilValue()
    {
        string? first = null;
        string? second = null;
        Assert.Equal("c", first.OrIfNull(second).OrIfNull("c"));
    }

    [Fact]
    public void OrIfNull_Reference_AllNull_ReturnsNull()
    {
        string? first = null;
        string? second = null;
        string? third = null;
        Assert.Null(first.OrIfNull(second).OrIfNull(third));
    }

    [Fact]
    public void OrIfNull_Reference_LastNonNull_FlowsToNonNullReturn()
    {
        string? first = null;
        string? second = null;
        string result = first.OrIfNull(second).OrIfNull("c");
        Assert.Equal("c", result);
    }

    [Fact]
    public void OrIfNull_Func_FirstPresent_DoesNotInvoke()
    {
        var invoked = false;
        var result = "a".OrIfNull(() => {
                invoked = true;
                return "x";
            });

        Assert.Equal("a", result);
        Assert.False(invoked);
    }

    [Fact]
    public void OrIfNull_Func_FirstNull_InvokesOnce()
    {
        string? first = null;
        var calls = 0;
        var result = first.OrIfNull(() => {
                calls++;
                return "x";
            });

        Assert.Equal("x", result);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void OrIfNull_NullableStruct_Zero_IsPresent()
    {
        int? value = 0;
        Assert.Equal(0, value.OrIfNull(5));
    }

    [Fact]
    public void OrIfNull_NullableStruct_Null_UsesNext()
    {
        int? value = null;
        Assert.Equal(5, value.OrIfNull(5));
    }

    [Fact]
    public void OrDefault_Reference_Null_ReturnsFallback()
    {
        string? value = null;
        Assert.Equal("fallback", value.OrDefault("fallback"));
    }

    [Fact]
    public void OrDefault_Func_Present_DoesNotInvoke()
    {
        var invoked = false;
        string? value = "a";
        var result = value.OrDefault(() => {
                invoked = true;
                return "x";
            });

        Assert.Equal("a", result);
        Assert.False(invoked);
    }

    [Fact]
    public void OrDefault_NullableStruct_Null_ReturnsFallback()
    {
        int? value = null;
        Assert.Equal(7, value.OrDefault(7));
    }

    [Fact]
    public void OrIfNull_AllNull_OrThrowInvalidOperation_Throws()
    {
        string? value = null;
        Assert.Throws<InvalidOperationException>(() => value.OrIfNull((string?)null).OrThrowInvalidOperation());
    }
}
