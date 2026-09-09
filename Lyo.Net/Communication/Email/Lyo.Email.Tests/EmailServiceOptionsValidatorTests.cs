using Lyo.Common.Metadata.Records;
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingHost_ReturnsFailure(string? host)
    {
        var options = new EmailServiceOptions {
            Host = host!,
            Port = PortInfo.SmtpSubmission,
            DefaultFromAddress = "test@example.com",
            DefaultFromName = "Test"
        };

        var result = _validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Contains("Host is required", result.FailureMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    public void Validate_InvalidPort_ReturnsFailure(int port)
    {
        var options = new EmailServiceOptions {
            Host = "smtp.example.com",
            Port = port,
            DefaultFromAddress = "test@example.com",
            DefaultFromName = "Test"
        };

        var result = _validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Contains("Port must be between", result.FailureMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingFromAddress_ReturnsFailure(string? fromAddress)
    {
        var options = new EmailServiceOptions {
            Host = "smtp.example.com",
            Port = PortInfo.SmtpSubmission,
            DefaultFromAddress = fromAddress!,
            DefaultFromName = "Test"
        };

        var result = _validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Contains("FromAddress is required", result.FailureMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingFromName_ReturnsFailure(string? fromName)
    {
        var options = new EmailServiceOptions {
            Host = "smtp.example.com",
            Port = PortInfo.SmtpSubmission,
            DefaultFromAddress = "test@example.com",
            DefaultFromName = fromName!
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
            Port = PortInfo.SmtpSubmission,
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
            Port = PortInfo.SmtpSubmission,
            DefaultFromAddress = "test@example.com",
            DefaultFromName = "Test",
            MaxAttachmentCountPerEmail = 0
        };

        var result = _validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Contains("MaxAttachmentCountPerEmail", string.Join(" ", result.Failures));
    }
}