using Lyo.Common.Builders;

namespace Lyo.Common.Tests;

public class ResultTests
{
    [Fact]
    public void Success_WithData_ReturnsSuccessfulResult()
    {
        var result = Result<string>.Success("hello");
        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Data);
        Assert.Null(result.Errors);
    }

    [Fact]
    public void Failure_WithMessageAndCode_ReturnsFailedResult()
    {
        var result = Result<string>.Failure("Something went wrong", "ERR_001");
        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Single(result.Errors!);
        Assert.Equal("Something went wrong", result.Errors![0].Message);
        Assert.Equal("ERR_001", result.Errors![0].Code);
    }

    [Fact]
    public void Failure_WithError_ReturnsFailedResult()
    {
        var error = ErrorBuilder.Create().WithMessage("Bad").WithCode("BAD").Build();
        var result = Result<int>.Failure(error);
        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors!);
        Assert.Equal("Bad", result.Errors![0].Message);
    }

    [Fact]
    public void TryGetValue_OnSuccess_ReturnsTrueAndValue()
    {
        var result = Result<int>.Success(42);
        Assert.True(result.TryGetValue(out var value));
        Assert.Equal(42, value);
    }

    [Fact]
    public void TryGetValue_OnFailure_ReturnsFalse()
    {
        var result = Result<int>.Failure("fail", "CODE");
        Assert.False(result.TryGetValue(out var value));
        Assert.Equal(0, value);
    }

    [Fact]
    public void ValueOrThrow_OnSuccess_ReturnsValue()
    {
        var result = Result<string>.Success("ok");
        Assert.Equal("ok", result.ValueOrThrow());
    }

    [Fact]
    public void ValueOrThrow_OnFailure_Throws()
    {
        var result = Result<string>.Failure("fail", "CODE");
        Assert.Throws<InvalidOperationException>(() => result.ValueOrThrow());
    }

    [Fact]
    public void ValueOrDefault_OnSuccess_ReturnsData()
    {
        var result = Result<int>.Success(10);
        Assert.Equal(10, result.ValueOrDefault(0));
    }

    [Fact]
    public void ValueOrDefault_OnFailure_ReturnsDefault()
    {
        var result = Result<int>.Failure("fail", "CODE");
        Assert.Equal(99, result.ValueOrDefault(99));
    }

    [Fact]
    public void Match_OnSuccess_CallsOnSuccess()
    {
        var result = Result<int>.Success(5);
        var output = result.Match(v => v * 2, _ => -1);
        Assert.Equal(10, output);
    }

    [Fact]
    public void Match_OnFailure_CallsOnFailure()
    {
        var result = Result<int>.Failure("fail", "CODE");
        var output = result.Match(v => v, errs => errs.Count);
        Assert.Equal(1, output);
    }
}