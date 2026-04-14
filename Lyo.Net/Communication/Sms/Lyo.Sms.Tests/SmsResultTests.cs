using Lyo.Common;
using Lyo.Sms.Models;
using Lyo.Sms.Twilio;

namespace Lyo.Sms.Tests;

public class SmsResultTests
{
    [Fact]
    public void Success_CreatesSuccessResult()
    {
        var request = new SmsRequest { To = "+1234567890", From = "+1987654321", Body = "Test message" };
        var result = Result<SmsRequest>.Success(request);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("+1234567890", result.Data!.To);
        Assert.Equal("+1987654321", result.Data.From);
        Assert.Null(result.Errors);
    }

    [Fact]
    public void Failure_CreatesFailureResult()
    {
        var request = new SmsRequest { To = "+1234567890", From = "+1987654321", Body = "Test message" };
        var exception = new Exception("Test error");
        var result = Result<SmsRequest>.Failure(exception, "TEST_ERROR");
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Errors);
        Assert.Single(result.Errors!);
        Assert.Equal("Test error", result.Errors![0].Message);
        Assert.Same(exception, result.Errors[0].Exception);
    }

    [Fact]
    public void TwilioSmsResult_FromException_CreatesFailureResult()
    {
        var request = new SmsRequest { To = "+1234567890", From = "+1987654321", Body = "Test message" };
        var exception = new Exception("Test error");
        var result = TwilioSmsResult.FromException(exception, request, "AC123");
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Errors);
        Assert.Equal("AC123", result.AccountSid);
        Assert.Same(exception, result.Errors![0].Exception);
    }

    [Fact]
    public void TwilioSmsResult_FromError_CreatesFailureResult()
    {
        var request = new SmsRequest { To = "+1234567890", From = "+1987654321", Body = "Test message" };
        var result = TwilioSmsResult.FromError("Custom error", "CUSTOM_ERROR", request, null, "AC123");
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Errors);
        Assert.Equal("AC123", result.AccountSid);
        Assert.Equal("Custom error", result.Errors![0].Message);
        Assert.Equal("CUSTOM_ERROR", result.Errors[0].Code);
    }

    [Fact]
    public void ToString_Success_IncludesRelevantInfo()
    {
        var request = new SmsRequest { To = "+1234567890", From = "+1987654321", Body = "Test message" };
        var result = Result<SmsRequest>.Success(request);
        var str = result.ToString();
        Assert.Contains("+1234567890", str);
        Assert.Contains("+1987654321", str);
    }

    [Fact]
    public void ToString_Failure_IncludesErrorInfo()
    {
        var request = new SmsRequest { To = "+1234567890", From = "+1987654321", Body = "Test message" };
        var exception = new Exception("Test error");
        var result = Result<SmsRequest>.Failure(exception, "TEST_ERROR");
        var str = result.ToString();
        Assert.Contains("Test error", str);
        Assert.Contains("TEST_ERROR", str);
    }
}