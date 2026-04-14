using Lyo.Email.Models;
using Microsoft.Extensions.Options;

namespace Lyo.Email.Tests;

public class EmailServiceOptionsValidatorTests
{
    private readonly EmailServiceOptionsValidator _validator = new();

    [Fact]
    public void Validate_NullOptions_ReturnsFailure()
    {
        var result = _validator.Validate(null, null!);
        Assert.True(result.Failed);
        Assert.Contains("cannot be null", result.FailureMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_NullHost_ReturnsFailure()
    {
        var options = new EmailServiceOptions {
            Host = null!,
            Port = 587,
            DefaultFromAddress = "test@example.com",
            DefaultFromName = "Test"
        };

        var result = _validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Contains("Host is required", result.FailureMessage);
    }

    [Fact]
    public void Validate_EmptyHost_ReturnsFailure()
    {
        var options = new EmailServiceOptions {
            Host = "",
            Port = 587,
            DefaultFromAddress = "test@example.com",
            DefaultFromName = "Test"
        };

        var result = _validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Contains("Host is required", result.FailureMessage);
    }

    [Fact]
    public void Validate_WhitespaceHost_ReturnsFailure()
    {
        var options = new EmailServiceOptions {
            Host = "   ",
            Port = 587,
            DefaultFromAddress = "test@example.com",
            DefaultFromName = "Test"
        };

        var result = _validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Contains("Host is required", result.FailureMessage);
    }

    [Fact]
    public void Validate_InvalidPort_Zero_ReturnsFailure()
    {
        var options = new EmailServiceOptions {
            Host = "smtp.example.com",
            Port = 0,
            DefaultFromAddress = "test@example.com",
            DefaultFromName = "Test"
        };

        var result = _validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Contains("Port must be between", result.FailureMessage);
    }

    [Fact]
    public void Validate_InvalidPort_Negative_ReturnsFailure()
    {
        var options = new EmailServiceOptions {
            Host = "smtp.example.com",
            Port = -1,
            DefaultFromAddress = "test@example.com",
            DefaultFromName = "Test"
        };

        var result = _validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Contains("Port must be between", result.FailureMessage);
    }

    [Fact]
    public void Validate_InvalidPort_TooLarge_ReturnsFailure()
    {
        var options = new EmailServiceOptions {
            Host = "smtp.example.com",
            Port = 65536,
            DefaultFromAddress = "test@example.com",
            DefaultFromName = "Test"
        };

        var result = _validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Contains("Port must be between", result.FailureMessage);
    }

    [Fact]
    public void Validate_NullFromAddress_ReturnsFailure()
    {
        var options = new EmailServiceOptions {
            Host = "smtp.example.com",
            Port = 587,
            DefaultFromAddress = null!,
            DefaultFromName = "Test"
        };

        var result = _validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Contains("FromAddress is required", result.FailureMessage);
    }

    [Fact]
    public void Validate_EmptyFromAddress_ReturnsFailure()
    {
        var options = new EmailServiceOptions {
            Host = "smtp.example.com",
            Port = 587,
            DefaultFromAddress = "",
            DefaultFromName = "Test"
        };

        var result = _validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Contains("FromAddress is required", result.FailureMessage);
    }

    [Fact]
    public void Validate_NullFromName_ReturnsFailure()
    {
        var options = new EmailServiceOptions {
            Host = "smtp.example.com",
            Port = 587,
            DefaultFromAddress = "test@example.com",
            DefaultFromName = null!
        };

        var result = _validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Contains("FromName is required", result.FailureMessage);
    }

    [Fact]
    public void Validate_EmptyFromName_ReturnsFailure()
    {
        var options = new EmailServiceOptions {
            Host = "smtp.example.com",
            Port = 587,
            DefaultFromAddress = "test@example.com",
            DefaultFromName = ""
        };

        var result = _validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Contains("FromName is required", result.FailureMessage);
    }

    [Fact]
    public void Validate_ValidOptions_ReturnsSuccess()
    {
        var options = new EmailServiceOptions {
            Host = "smtp.example.com",
            Port = 587,
            UseSsl = true,
            DefaultFromAddress = "test@example.com",
            DefaultFromName = "Test Sender",
            Username = "testuser",
            Password = "testpass"
        };

        var result = _validator.Validate(null, options);
        Assert.False(result.Failed);
        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void Validate_ValidOptionsMinPort_ReturnsSuccess()
    {
        var options = new EmailServiceOptions {
            Host = "smtp.example.com",
            Port = 1,
            DefaultFromAddress = "test@example.com",
            DefaultFromName = "Test"
        };

        var result = _validator.Validate(null, options);
        Assert.False(result.Failed);
    }

    [Fact]
    public void Validate_ValidOptionsMaxPort_ReturnsSuccess()
    {
        var options = new EmailServiceOptions {
            Host = "smtp.example.com",
            Port = 65535,
            DefaultFromAddress = "test@example.com",
            DefaultFromName = "Test"
        };

        var result = _validator.Validate(null, options);
        Assert.False(result.Failed);
    }

    [Fact]
    public void Validate_MaxAttachmentCountPerEmailZero_ReturnsFailure()
    {
        var options = new EmailServiceOptions {
            Host = "smtp.example.com",
            Port = 587,
            DefaultFromAddress = "test@example.com",
            DefaultFromName = "Test",
            MaxAttachmentCountPerEmail = 0
        };

        var result = _validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Contains("MaxAttachmentCountPerEmail", string.Join(" ", result.Failures));
    }
}