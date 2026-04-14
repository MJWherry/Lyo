using Lyo.Common.Builders;

namespace Lyo.Common.Tests;

public class ErrorBuilderTests
{
    [Fact]
    public void Create_WithMessageAndCode_BuildsError()
    {
        var error = ErrorBuilder.Create().WithMessage("Something failed").WithCode("ERR_001").Build();
        Assert.Equal("Something failed", error.Message);
        Assert.Equal("ERR_001", error.Code);
    }

    [Fact]
    public void WithMetadata_AddsMetadata()
    {
        var error = ErrorBuilder.Create().WithMessage("Fail").WithCode("ERR").WithMetadata("Key", "Value").WithMetadata("Count", 42).Build();
        Assert.NotNull(error.Metadata);
        Assert.Equal("Value", error.Metadata["Key"]);
        Assert.Equal(42, error.Metadata["Count"]);
    }

    [Fact]
    public void FromException_CreatesErrorFromException()
    {
        var ex = new InvalidOperationException("Invalid state");
        var error = ErrorBuilder.FromException(ex, "CUSTOM_CODE").Build();
        Assert.Equal("Invalid state", error.Message);
        Assert.Equal("CUSTOM_CODE", error.Code);
    }

    [Fact]
    public void FromException_WithoutCode_UsesExceptionTypeName()
    {
        var ex = new ArgumentException("Bad arg");
        var error = ErrorBuilder.FromException(ex).Build();
        Assert.Equal("ArgumentException", error.Code);
    }

    [Fact]
    public void WithInnerError_AddsInnerError()
    {
        var inner = ErrorBuilder.Create().WithMessage("Inner").WithCode("INNER").Build();
        var error = ErrorBuilder.Create().WithMessage("Outer").WithCode("OUTER").WithInnerError(inner).Build();
        Assert.NotNull(error.InnerError);
        Assert.Equal("Inner", error.InnerError.Message);
        Assert.Equal("INNER", error.InnerError.Code);
    }
}