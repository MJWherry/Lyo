using Lyo.Result.Builders;
using Lyo.Result.Enums;

namespace Lyo.Result.Tests;

public class ErrorTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var error = new Error("message", "CODE");
        Assert.Equal("message", error.Message);
        Assert.Equal("CODE", error.Code);
        Assert.Equal(ErrorSeverity.Error, error.Severity);
        Assert.Equal(ErrorType.Generic, error.Type);
    }

    [Fact]
    public void Constructor_FromException_UsesExceptionMessage()
    {
        var ex = new InvalidOperationException("boom");
        var error = new Error(null, "EX_CODE", exception: ex);
        Assert.Equal("boom", error.Message);
        Assert.Equal("EX_CODE", error.Code);
    }

    [Fact]
    public void Validation_SetsCorrectTypeAndSeverity()
    {
        var error = Error.Validation("Field is invalid", "FIELD_INVALID");
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal(ErrorSeverity.Warning, error.Severity);
        Assert.Equal("Field is invalid", error.Message);
        Assert.Equal("FIELD_INVALID", error.Code);
    }

    [Fact]
    public void Validation_DefaultCode_IsValidationFailed()
    {
        var error = Error.Validation("invalid");
        Assert.Equal(ValidationErrorCodes.ValidationFailed, error.Code);
    }

    [Fact]
    public void NotFound_SetsCorrectType()
    {
        var error = Error.NotFound("User not found");
        Assert.Equal(ErrorType.NotFound, error.Type);
        Assert.Equal(ValidationErrorCodes.NotFound, error.Code);
        Assert.Equal(ErrorSeverity.Error, error.Severity);
    }

    [Fact]
    public void Conflict_SetsCorrectType()
    {
        var error = Error.Conflict("Already exists");
        Assert.Equal(ErrorType.Conflict, error.Type);
        Assert.Equal(ValidationErrorCodes.Conflict, error.Code);
    }

    [Fact]
    public void Unauthorized_HasDefaultMessage()
    {
        var error = Error.Unauthorized();
        Assert.Equal(ErrorType.Unauthorized, error.Type);
        Assert.Equal(ValidationErrorCodes.Unauthorized, error.Code);
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public void Forbidden_HasDefaultMessage()
    {
        var error = Error.Forbidden();
        Assert.Equal(ErrorType.Forbidden, error.Type);
        Assert.Equal(ErrorSeverity.Error, error.Severity);
    }

    [Fact]
    public void Internal_WithException_IncludesExceptionInfo()
    {
        var ex = new Exception("crash");
        var error = Error.Internal(exception: ex);
        Assert.Equal(ErrorType.InternalError, error.Type);
        Assert.Equal(ErrorSeverity.Critical, error.Severity);
        Assert.Equal(ex, error.Exception);
    }

    [Fact]
    public void Timeout_SetsCorrectType()
    {
        var error = Error.Timeout();
        Assert.Equal(ErrorType.Timeout, error.Type);
        Assert.Equal(ValidationErrorCodes.Timeout, error.Code);
    }

    [Fact]
    public void ServiceUnavailable_SetsCorrectType()
    {
        var error = Error.ServiceUnavailable();
        Assert.Equal(ErrorType.ServiceUnavailable, error.Type);
        Assert.Equal(ErrorSeverity.Critical, error.Severity);
    }

    [Fact]
    public void FromException_WithInnerException_CreatesChain()
    {
        var inner = new ArgumentException("bad arg");
        var outer = new InvalidOperationException("invalid state", inner);
        var error = Error.FromException(outer, "OUTER_CODE");
        Assert.Equal("OUTER_CODE", error.Code);
        Assert.NotNull(error.InnerError);
        Assert.Equal("bad arg", error.InnerError!.Message);
    }

    [Fact]
    public void ErrorBuilder_Create_WithMessageAndCode_BuildsError()
    {
        var error = ErrorBuilder.Create().WithMessage("Something failed").WithCode("ERR_001").Build();
        Assert.Equal("Something failed", error.Message);
        Assert.Equal("ERR_001", error.Code);
    }

    [Fact]
    public void ErrorBuilder_WithMetadata_AddsMetadata()
    {
        var error = ErrorBuilder.Create().WithMessage("Fail").WithCode("ERR").WithMetadata("Key", "Value").WithMetadata("Count", 42).Build();
        Assert.NotNull(error.Metadata);
        Assert.Equal("Value", error.Metadata["Key"]);
        Assert.Equal(42, error.Metadata["Count"]);
    }

    [Fact]
    public void ErrorBuilder_FromException_CreatesErrorFromException()
    {
        var ex = new InvalidOperationException("Invalid state");
        var error = ErrorBuilder.FromException(ex, "CUSTOM_CODE").Build();
        Assert.Equal("Invalid state", error.Message);
        Assert.Equal("CUSTOM_CODE", error.Code);
    }

    [Fact]
    public void ErrorBuilder_FromException_WithoutCode_UsesExceptionTypeName()
    {
        var ex = new ArgumentException("Bad arg");
        var error = ErrorBuilder.FromException(ex).Build();
        Assert.Equal("ArgumentException", error.Code);
    }

    [Fact]
    public void ErrorBuilder_WithInnerError_AddsInnerError()
    {
        var inner = ErrorBuilder.Create().WithMessage("Inner").WithCode("INNER").Build();
        var error = ErrorBuilder.Create().WithMessage("Outer").WithCode("OUTER").WithInnerError(inner).Build();
        Assert.NotNull(error.InnerError);
        Assert.Equal("Inner", error.InnerError!.Message);
        Assert.Equal("INNER", error.InnerError!.Code);
    }

    [Fact]
    public void ErrorBuilder_WithType_SetsType()
    {
        var error = ErrorBuilder.Create().WithMessage("Not found").WithCode("NF").WithType(ErrorType.NotFound).Build();
        Assert.Equal(ErrorType.NotFound, error.Type);
    }

    [Fact]
    public void ErrorBuilder_WithSeverity_SetsSeverity()
    {
        var error = ErrorBuilder.Create().WithMessage("Warning").WithCode("W").WithSeverity(ErrorSeverity.Warning).Build();
        Assert.Equal(ErrorSeverity.Warning, error.Severity);
    }
}