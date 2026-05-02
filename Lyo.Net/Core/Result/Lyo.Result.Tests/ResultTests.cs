using System.Collections;
using Lyo.Result.Builders;
using Lyo.Result.Interfaces;

namespace Lyo.Result.Tests;

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
        Assert.Single((IEnumerable)result.Errors!);
        Assert.Equal("Something went wrong", result.Errors![0].Message);
        Assert.Equal("ERR_001", result.Errors![0].Code);
    }

    [Fact]
    public void Failure_WithError_ReturnsFailedResult()
    {
        var error = ErrorBuilder.Create().WithMessage("Bad").WithCode("BAD").Build();
        var result = Result<int>.Failure(error);
        Assert.False(result.IsSuccess);
        Assert.Single((IEnumerable)result.Errors!);
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
        Assert.Throws<InvalidOperationException>(result.ValueOrThrow);
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
    public void ValueOrDefault_Factory_OnFailure_CallsFactory()
    {
        var result = Result<int>.Failure("fail", "CODE");
        Assert.Equal(7, result.ValueOrDefault(() => 7));
    }

    [Fact]
    public void Map_OnSuccess_TransformsValue()
    {
        var result = Result<int>.Success(5).Map(x => x * 2);
        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Data);
    }

    [Fact]
    public void Map_OnFailure_PropagatesErrors()
    {
        var original = Result<int>.Failure("fail", "CODE");
        var mapped = original.Map(x => x.ToString());
        Assert.False(mapped.IsSuccess);
        Assert.Single((IEnumerable)mapped.Errors!);
        Assert.Equal("CODE", mapped.Errors![0].Code);
    }

    [Fact]
    public async Task MapAsync_OnSuccess_TransformsValue()
    {
        var result = await Result<int>.Success(3).MapAsync(x => Task.FromResult(x * 10));
        Assert.True(result.IsSuccess);
        Assert.Equal(30, result.Data);
    }

    [Fact]
    public async Task MapAsync_OnFailure_PropagatesErrors()
    {
        var result = await Result<int>.Failure("fail", "CODE").MapAsync(x => Task.FromResult(x.ToString()));
        Assert.False(result.IsSuccess);
        Assert.Equal("CODE", result.Errors![0].Code);
    }

    [Fact]
    public void Tap_OnSuccess_ExecutesAction_AndReturnsOriginal()
    {
        var called = false;
        var result = Result<int>.Success(99).Tap(_ => called = true);
        Assert.True(called);
        Assert.True(result.IsSuccess);
        Assert.Equal(99, result.Data);
    }

    [Fact]
    public void Tap_OnFailure_DoesNotExecuteAction()
    {
        var called = false;
        Result<int>.Failure("fail", "CODE").Tap(_ => called = true);
        Assert.False(called);
    }

    [Fact]
    public async Task TapAsync_OnSuccess_ExecutesAction()
    {
        var called = false;
        await Result<int>.Success(1)
            .TapAsync(_ => {
                called = true;
                return Task.CompletedTask;
            });

        Assert.True(called);
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

    [Fact]
    public void Recover_OnFailure_ReturnsFallback()
    {
        var result = Result<int>.Failure("fail", "CODE");
        Assert.Equal(42, result.Recover(42));
    }

    [Fact]
    public void RecoverWith_OnFailure_ReturnsNewResult()
    {
        var result = Result<int>.Failure("fail", "CODE");
        var recovered = result.RecoverWith(_ => Result<int>.Success(5));
        Assert.True(recovered.IsSuccess);
        Assert.Equal(5, recovered.Data);
    }

    [Fact]
    public void Where_PredicateTrue_RemainsSuccess()
    {
        var result = Result<int>.Success(10).Where(x => x > 0, "NEG", "Must be positive");
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Where_PredicateFalse_BecomesFailure()
    {
        var result = Result<int>.Success(-1).Where(x => x > 0, "NEG", "Must be positive");
        Assert.False(result.IsSuccess);
        Assert.Equal("NEG", result.Errors![0].Code);
    }

    [Fact]
    public void ImplicitFromValue_CreatesSuccess()
    {
        Result<string> result = "hello";
        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Data);
    }

    [Fact]
    public void ImplicitFromError_CreatesFailure()
    {
        Result<string> result = new Error("fail", "CODE");
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void IResult_Success_IsSuccess_True()
    {
        IResult result = Result<int>.Success(1);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Errors);
    }

    [Fact]
    public void IResultT_Success_Data_IsAccessible()
    {
        IResult<int> result = Result<int>.Success(42);
        Assert.Equal(42, result.Data);
    }

    [Fact]
    public void IResult_Failure_Errors_Populated()
    {
        IResult result = Result<int>.Failure("msg", "CODE");
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Errors);
        Assert.Single(result.Errors!);
    }

    [Fact]
    public void GetAllErrors_FlattensInnerErrors()
    {
        var inner = new Error("inner msg", "INNER");
        var outer = new Error("outer msg", "OUTER", innerError: inner);
        var result = Result<int>.Failure(outer);
        var allErrors = result.GetAllErrors();
        Assert.Equal(2, allErrors.Count);
        Assert.Contains(allErrors, e => e.Code == "INNER");
        Assert.Contains(allErrors, e => e.Code == "OUTER");
    }
}