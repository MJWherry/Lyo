using Lyo.Common.Enums;

namespace Lyo.Common.Tests;

public class ResultExtensionsTests
{

    [Fact]
    public void Then_OnSuccess_ChainsToNext()
    {
        var result = Result<int>.Success(5).Then(x => Result<string>.Success(x.ToString()));
        Assert.True(result.IsSuccess);
        Assert.Equal("5", result.Data);
    }

    [Fact]
    public void Then_OnFailure_ShortCircuits()
    {
        var called = false;
        var result = Result<int>.Failure("fail", "CODE")
            .Then(x => { called = true; return Result<string>.Success("ok"); });
        Assert.False(called);
        Assert.False(result.IsSuccess);
        Assert.Equal("CODE", result.Errors![0].Code);
    }

    [Fact]
    public void OnSuccess_OnSuccess_ExecutesAction()
    {
        var called = false;
        Result<int>.Success(1).OnSuccess(_ => called = true);
        Assert.True(called);
    }

    [Fact]
    public void OnFailure_OnFailure_ExecutesAction()
    {
        var called = false;
        Result<int>.Failure("fail", "CODE").OnFailure(_ => called = true);
        Assert.True(called);
    }

    [Fact]
    public void OnSuccess_WithFunc_TransformsResult()
    {
        var result = Result<int>.Success(3).OnSuccess(x => Result<string>.Success((x * 2).ToString()));
        Assert.True(result.IsSuccess);
        Assert.Equal("6", result.Data);
    }

    [Fact]
    public void GetErrorMessages_ReturnsAllDistinctMessages()
    {
        var result = Result<int>.Failure([
            new Error("msg1", "A"),
            new Error("msg2", "B"),
        ]);
        var messages = result.GetErrorMessages();
        Assert.Contains("msg1", messages);
        Assert.Contains("msg2", messages);
    }

    [Fact]
    public void GetErrorCodes_ReturnsAllDistinctCodes()
    {
        var result = Result<int>.Failure([
            new Error("msg1", "A"),
            new Error("msg2", "A"),
            new Error("msg3", "B"),
        ]);
        var codes = result.GetErrorCodes();
        Assert.Equal(2, codes.Count);
    }

    [Fact]
    public void GetFirstError_ReturnsFirstError()
    {
        var result = Result<int>.Failure([new Error("first", "FIRST"), new Error("second", "SECOND")]);
        Assert.Equal("FIRST", result.GetFirstError()?.Code);
    }

    [Fact]
    public void GetFirstError_ReturnsNull_OnSuccess()
    {
        Assert.Null(Result<int>.Success(1).GetFirstError());
    }

    [Fact]
    public void HasSeverity_ReturnsTrueForMatchingSeverity()
    {
        var result = Result<int>.Failure(new Error("crit", "CODE") { Severity = ErrorSeverity.Critical });
        Assert.True(result.HasSeverity(ErrorSeverity.Critical));
        Assert.False(result.HasSeverity(ErrorSeverity.Warning));
    }

    [Fact]
    public void Combine_AllSuccess_ReturnsCombinedList()
    {
        var results = new[] { Result<int>.Success(1), Result<int>.Success(2), Result<int>.Success(3) };
        var combined = results.Combine();
        Assert.True(combined.IsSuccess);
        Assert.Equal([1, 2, 3], combined.Data);
    }

    [Fact]
    public void Combine_AnyFailure_ReturnsAllErrors()
    {
        var results = new[] { Result<int>.Success(1), Result<int>.Failure("fail", "A"), Result<int>.Failure("fail2", "B") };
        var combined = results.Combine();
        Assert.False(combined.IsSuccess);
        Assert.Equal(2, combined.Errors!.Count);
    }

    [Fact]
    public void FirstSuccess_ReturnsFirstSuccessfulResult()
    {
        var results = new[] {
            Result<int>.Failure("fail", "A"),
            Result<int>.Success(42),
            Result<int>.Success(99),
        };
        var first = results.FirstSuccess();
        Assert.True(first.IsSuccess);
        Assert.Equal(42, first.Data);
    }

    [Fact]
    public void MapError_OnFailure_TransformsErrors()
    {
        var result = Result<int>.Failure("original", "CODE")
            .MapError(errors => [new Error("transformed", "NEW_CODE")]);
        Assert.False(result.IsSuccess);
        Assert.Equal("NEW_CODE", result.Errors![0].Code);
        Assert.Equal("transformed", result.Errors[0].Message);
    }

    [Fact]
    public void MapError_OnSuccess_ReturnsOriginalUnchanged()
    {
        var original = Result<int>.Success(5);
        var result = original.MapError(_ => [new Error("never", "NEVER")]);
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Data);
    }

    [Fact]
    public void WithRequest_AddsRequestToResult()
    {
        var result = Result<int>.Success(42).WithRequest("myRequest");
        Assert.True(result.IsSuccess);
        Assert.Equal("myRequest", result.Request);
        Assert.Equal(42, result.Data);
    }

    [Fact]
    public void ToResult_StripsRequestFromPairedResult()
    {
        var paired = Result<string, int>.Success("req", 7);
        var plain = paired.ToResult();
        Assert.True(plain.IsSuccess);
        Assert.Equal(7, plain.Data);
    }

    [Fact]
    public void Combine_TwoResults_BothSuccess_ReturnsTuple()
    {
        var r1 = Result<int>.Success(1);
        var r2 = Result<string>.Success("hello");
        var combined = r1.Combine(r2);
        Assert.True(combined.IsSuccess);
        Assert.Equal((1, "hello"), combined.Data);
    }

    [Fact]
    public void Combine_TwoResults_OneFailure_CollectsErrors()
    {
        var r1 = Result<int>.Success(1);
        var r2 = Result<string>.Failure("fail", "CODE");
        var combined = r1.Combine(r2);
        Assert.False(combined.IsSuccess);
        Assert.Single(combined.Errors!);
    }
}
