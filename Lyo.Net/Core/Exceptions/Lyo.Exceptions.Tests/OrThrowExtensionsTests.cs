namespace Lyo.Exceptions.Tests;

public class OrThrowExtensionsTests
{
    [Fact]
    public void OrThrowInvalidOperation_Reference_NonNull_ReturnsSameInstance()
    {
        var instance = new object();
        Assert.Same(instance, instance.OrThrowInvalidOperation());
    }

    [Fact]
    public void OrThrowInvalidOperation_Reference_Null_ThrowsWithDefaultMessage()
    {
        object? value = null;
        var ex = Assert.Throws<InvalidOperationException>(() => value.OrThrowInvalidOperation());
        Assert.Equal("A required value was null.", ex.Message);
    }

    [Fact]
    public void OrThrowInvalidOperation_Reference_Null_WithCustomMessage_UsesMessage()
    {
        object? value = null;
        var ex = Assert.Throws<InvalidOperationException>(() => value.OrThrowInvalidOperation("Missing dependency."));
        Assert.Equal("Missing dependency.", ex.Message);
    }

    [Fact]
    public void OrThrowInvalidOperation_NullableStruct_HasValue_ReturnsValue()
    {
        int? value = 42;
        Assert.Equal(42, value.OrThrowInvalidOperation());
    }

    [Fact]
    public void OrThrowInvalidOperation_NullableStruct_Null_ThrowsWithDefaultMessage()
    {
        int? value = null;
        var ex = Assert.Throws<InvalidOperationException>(() => value.OrThrowInvalidOperation());
        Assert.Equal("A required non-null value was missing.", ex.Message);
    }

    [Fact]
    public void OrThrow_Reference_NonNull_ReturnsSameInstance()
    {
        var instance = new object();
        Assert.Same(instance, instance.OrThrow(message => new InvalidDataException(message)));
    }

    [Fact]
    public void OrThrow_Reference_Null_ThrowsFactoryExceptionWithDefaultMessage()
    {
        object? value = null;
        var ex = Assert.Throws<InvalidDataException>(() => value.OrThrow(message => new InvalidDataException(message)));
        Assert.Equal("A required value was null.", ex.Message);
    }

    [Fact]
    public void OrThrow_NullableStruct_Null_ThrowsFactoryException()
    {
        int? value = null;
        var ex = Assert.Throws<ApplicationException>(() => value.OrThrow(message => new ApplicationException(message)));
        Assert.Equal("A required non-null value was missing.", ex.Message);
    }

    [Fact]
    public void OrThrow_NullableStruct_HasValue_ReturnsValue()
    {
        int? value = 7;
        Assert.Equal(7, value.OrThrow(message => new ApplicationException(message)));
    }

    [Fact]
    public void OrThrow_String_Value_ReturnsValue() => Assert.Equal("value", "value".OrThrow(() => new KeyNotFoundException("missing")));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void OrThrow_String_Missing_ThrowsFactoryException(string? value) => Assert.Throws<KeyNotFoundException>(() => value.OrThrow(() => new KeyNotFoundException("missing")));

    [Fact]
    public void OrThrow_String_Whitespace_ReturnsWhitespace() => Assert.Equal("  ", "  ".OrThrow(() => new KeyNotFoundException("missing")));

    [Fact]
    public void OrThrowIfWhiteSpace_String_Whitespace_Throws() => Assert.Throws<KeyNotFoundException>(() => "  ".OrThrowIfWhiteSpace(() => new KeyNotFoundException("missing")));

    [Fact]
    public void OrThrowIfWhiteSpace_String_Value_ReturnsValue() => Assert.Equal("value", "value".OrThrowIfWhiteSpace(() => new KeyNotFoundException("missing")));

    [Fact]
    public void OrThrow_String_MessageFactory_Empty_ThrowsWithDefaultMessage()
    {
        var ex = Assert.Throws<InvalidDataException>(() => "".OrThrow(message => new InvalidDataException(message)));
        Assert.Equal("The string value cannot be null or empty.", ex.Message);
    }

    [Fact]
    public void OrThrowIfWhiteSpace_String_MessageFactory_Whitespace_ThrowsWithCustomMessage()
    {
        var ex = Assert.Throws<InvalidDataException>(() => " ".OrThrowIfWhiteSpace(message => new InvalidDataException(message), "Setting missing."));
        Assert.Equal("Setting missing.", ex.Message);
    }

    [Fact]
    public void OrThrowInvalidOperation_String_Empty_ThrowsWithDefaultMessage()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => "".OrThrowInvalidOperation());
        Assert.Equal("The string value cannot be null or empty.", ex.Message);
    }

    [Fact]
    public void OrThrowInvalidOperation_String_Value_ReturnsValue() => Assert.Equal("value", "value".OrThrowInvalidOperation());

    [Fact]
    public void OrThrowInvalidOperationIfWhiteSpace_Whitespace_ThrowsWithDefaultMessage()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => " ".OrThrowInvalidOperationIfWhiteSpace());
        Assert.Equal("The string value cannot be null, empty, or whitespace.", ex.Message);
    }

    [Fact]
    public void OrThrowArgument_Empty_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => "".OrThrowArgument());
        Assert.Equal("The argument value cannot be null or empty.", ex.Message);
    }

    [Fact]
    public void OrThrowArgument_Value_ReturnsValue() => Assert.Equal("v", "v".OrThrowArgument());

    [Fact]
    public void OrThrowKeyNotFound_Null_Throws()
    {
        string? value = null;
        var ex = Assert.Throws<KeyNotFoundException>(() => value.OrThrowKeyNotFound());
        Assert.Equal("The requested configuration or lookup value was not found.", ex.Message);
    }

    [Fact]
    public void OrThrowNotSupported_Empty_Throws()
    {
        var ex = Assert.Throws<NotSupportedException>(() => "".OrThrowNotSupported("Provider not supported."));
        Assert.Equal("Provider not supported.", ex.Message);
    }

    [Fact]
    public void OrThrowNotSupported_Value_ReturnsValue() => Assert.Equal("v", "v".OrThrowNotSupported());
}