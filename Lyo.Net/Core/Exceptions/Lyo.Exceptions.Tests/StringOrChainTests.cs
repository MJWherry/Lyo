namespace Lyo.Exceptions.Tests;

public class StringOrChainTests
{
    [Fact]
    public void Or_FirstPresent_KeepsFirst() => Assert.Equal("a", "a".Or("b").OrDefault());

    [Fact]
    public void Or_FirstEmpty_UsesSecond() => Assert.Equal("b", "".Or("b").OrDefault());

    [Fact]
    public void Or_FirstNull_UsesSecond()
    {
        string? first = null;
        Assert.Equal("b", first.Or("b").OrDefault());
    }

    [Fact]
    public void Or_Whitespace_IsNotMissing() => Assert.Equal(" ", " ".Or("b").OrDefault());

    [Fact]
    public void OrIfWhiteSpace_Whitespace_IsMissing() => Assert.Equal("b", " ".OrIfWhiteSpace("b").OrDefault());

    [Fact]
    public void OrDefault_AllMissing_ReturnsDefault() => Assert.Equal("fallback", "".Or("").OrDefault("fallback"));

    [Fact]
    public void OrDefault_NoArgument_ReturnsEmpty() => Assert.Equal("", "".Or((string?)null).OrDefault());

    [Fact]
    public void Or_ChainsAcrossMultipleCandidates()
    {
        string? first = null;
        Assert.Equal("c", first.Or("").Or("c").OrDefault());
    }

    [Fact]
    public void OrIfWhiteSpace_ChainSkipsWhitespaceCandidates() => Assert.Equal("d", " ".OrIfWhiteSpace("  ").Or("d").OrDefault());

    [Fact]
    public void Or_FuncStarter_FirstMissing_InvokesFactory() => Assert.Equal("x", "".Or(() => "x").OrDefault());

    [Fact]
    public void Or_FuncStarter_FirstPresent_DoesNotInvokeFactory()
    {
        var invoked = false;
        var result = "a".Or(() => {
            invoked = true;
            return "x";
        }).OrDefault();
        Assert.Equal("a", result);
        Assert.False(invoked);
    }

    [Fact]
    public void Or_FuncContinuation_OnlyInvokedWhenMissing()
    {
        var invoked = false;
        var result = "".Or("b").Or(() => {
            invoked = true;
            return "x";
        }).OrDefault();
        Assert.Equal("b", result);
        Assert.False(invoked);
    }

    [Fact]
    public void OrIfWhiteSpace_FuncStarter_WhitespaceMissing_InvokesFactory()
        => Assert.Equal("x", " ".OrIfWhiteSpace(() => "x").OrDefault());

    [Fact]
    public void OrThrow_Resolved_ReturnsValue() => Assert.Equal("b", "".Or("b").OrThrow(() => new KeyNotFoundException("missing")));

    [Fact]
    public void OrThrow_AllMissing_ThrowsFactoryException()
        => Assert.Throws<KeyNotFoundException>(() => "".Or("").OrThrow(() => new KeyNotFoundException("missing")));

    [Fact]
    public void OrThrow_MessageFactory_AllMissing_ThrowsWithMessage()
    {
        var ex = Assert.Throws<InvalidDataException>(() => "".Or((string?)null).OrThrow(message => new InvalidDataException(message), "No value configured."));
        Assert.Equal("No value configured.", ex.Message);
    }

    [Fact]
    public void OrThrowIfWhiteSpace_ResolvedWhitespace_Throws()
        => Assert.Throws<KeyNotFoundException>(() => "".Or(" ").OrThrowIfWhiteSpace(() => new KeyNotFoundException("missing")));

    [Fact]
    public void OrThrowInvalidOperation_AllMissing_Throws()
        => Assert.Throws<InvalidOperationException>(() => "".Or("").OrThrowInvalidOperation());

    [Fact]
    public void OrThrowInvalidOperationIfWhiteSpace_ResolvedWhitespace_Throws()
        => Assert.Throws<InvalidOperationException>(() => "".Or(" ").OrThrowInvalidOperationIfWhiteSpace());

    [Fact]
    public void OrThrowArgument_AllMissing_Throws()
        => Assert.Throws<ArgumentException>(() => "".Or("").OrThrowArgument());

    [Fact]
    public void OrThrowKeyNotFound_Resolved_ReturnsValue() => Assert.Equal("v", "".Or("v").OrThrowKeyNotFound());

    [Fact]
    public void OrThrowNotSupported_AllMissing_Throws()
        => Assert.Throws<NotSupportedException>(() => "".Or("").OrThrowNotSupported());
}
