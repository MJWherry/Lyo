using Lyo.Common.Builders;

namespace Lyo.Common.Tests;

public class BulkResultTests
{

    [Fact]
    public void Counts_AreCorrect()
    {
        var bulk = BulkResult<int>.FromResults([
            Result<int>.Success(1),
            Result<int>.Success(2),
            Result<int>.Failure("fail", "CODE"),
        ]);

        Assert.Equal(3, bulk.TotalCount);
        Assert.Equal(2, bulk.SuccessCount);
        Assert.Equal(1, bulk.FailureCount);
    }

    [Fact]
    public void IsCompleteSuccess_WhenAllSucceed()
    {
        var bulk = BulkResult<int>.FromData([1, 2, 3]);
        Assert.True(bulk.IsCompleteSuccess);
        Assert.False(bulk.IsCompleteFailure);
        Assert.False(bulk.HasPartialSuccess);
    }

    [Fact]
    public void IsCompleteFailure_WhenAllFail()
    {
        var bulk = BulkResult<int>.FromErrors([
            new Error("a", "A"),
            new Error("b", "B"),
        ]);
        Assert.True(bulk.IsCompleteFailure);
        Assert.False(bulk.IsCompleteSuccess);
        Assert.True(bulk.HasErrors);
    }

    [Fact]
    public void HasPartialSuccess_WhenMixed()
    {
        var bulk = BulkResult<int>.FromResults([
            Result<int>.Success(1),
            Result<int>.Failure("fail", "CODE"),
        ]);
        Assert.True(bulk.HasPartialSuccess);
        Assert.True(bulk.HasErrors);
    }

    [Fact]
    public void SuccessfulResults_ReturnsOnlySuccesses()
    {
        var bulk = BulkResult<string>.FromResults([
            Result<string>.Success("a"),
            Result<string>.Failure("err", "E"),
            Result<string>.Success("b"),
        ]);
        Assert.Equal(2, bulk.SuccessfulResults.Count);
        Assert.Equal(["a", "b"], bulk.SuccessfulData);
    }

    [Fact]
    public void FailedResults_ReturnsOnlyFailures()
    {
        var bulk = BulkResult<string>.FromResults([
            Result<string>.Success("ok"),
            Result<string>.Failure("fail", "CODE"),
        ]);
        Assert.Single(bulk.FailedResults);
        Assert.Equal("CODE", bulk.FirstError?.Code);
    }

    [Fact]
    public void ErrorCodes_AreDistinctAcrossAllFailures()
    {
        var bulk = BulkResult<int>.FromResults([
            Result<int>.Failure("msg1", "A"),
            Result<int>.Failure("msg2", "A"),
            Result<int>.Failure("msg3", "B"),
        ]);
        Assert.Equal(2, bulk.ErrorCodes.Count);
        Assert.Contains("A", bulk.ErrorCodes);
        Assert.Contains("B", bulk.ErrorCodes);
    }

    [Fact]
    public void IResult_Errors_ReturnsNullOnCompleteSuccess()
    {
        BulkResult<int> bulk = BulkResult<int>.FromData([1, 2]);
        IResult iResult = bulk;
        Assert.Null(iResult.Errors);
    }

    [Fact]
    public void IResult_Errors_ReturnsErrorsOnFailure()
    {
        var bulk = BulkResult<int>.FromErrors([new Error("e", "CODE")]);
        IResult iResult = bulk;
        Assert.NotNull(iResult.Errors);
    }

    [Fact]
    public void BulkResultBuilder_BuildsCorrectly()
    {
        var bulk = BulkResultBuilder<int>.Create()
            .AddSuccess(1)
            .AddSuccess(2)
            .AddFailure(new Error("fail", "F"))
            .Build();

        Assert.Equal(3, bulk.TotalCount);
        Assert.Equal(2, bulk.SuccessCount);
        Assert.Equal(1, bulk.FailureCount);
    }

    [Fact]
    public void BulkResultBuilder_AddRange_AddsAll()
    {
        var results = new[] { Result<int>.Success(10), Result<int>.Success(20) };
        var bulk = BulkResultBuilder<int>.Create().AddRange(results).Build();
        Assert.Equal(2, bulk.SuccessCount);
    }

    [Fact]
    public void BulkResultBuilder_AddEach_ProjectsItems()
    {
        var bulk = BulkResultBuilder<string>.Create()
            .AddEach(["a", "b", "c"], s => Result<string>.Success(s.ToUpper()))
            .Build();

        Assert.Equal(3, bulk.SuccessCount);
        Assert.Contains("A", bulk.SuccessfulData);
    }

    [Fact]
    public void BulkResultBuilder_ImplicitConversion_Works()
    {
        BulkResult<int> bulk = BulkResultBuilder<int>.Create().AddSuccess(42);
        Assert.Equal(1, bulk.SuccessCount);
    }

    [Fact]
    public void BulkResultBuilder_Count_ReflectsAdded()
    {
        var builder = BulkResultBuilder<int>.Create().AddSuccess(1).AddSuccess(2);
        Assert.Equal(2, builder.Count);
    }

    [Fact]
    public void PairedBulkResult_SuccessfulRequests_Correct()
    {
        var bulk = new BulkResult<string, int>([
            Result<string, int>.Success("req1", 1),
            Result<string, int>.Failure("req2", new Error("fail", "F")),
        ]);

        Assert.Single(bulk.SuccessfulRequests);
        Assert.Equal("req1", bulk.SuccessfulRequests[0]);
        Assert.Single(bulk.FailedRequests);
        Assert.Equal("req2", bulk.FailedRequests[0]);
    }

    [Fact]
    public void PairedBulkResultBuilder_BuildsCorrectly()
    {
        var bulk = BulkResultBuilder<string, int>.Create()
            .AddSuccess("r1", 100)
            .AddFailure("r2", new Error("fail", "ERR"))
            .Build();

        Assert.Equal(1, bulk.SuccessCount);
        Assert.Equal(1, bulk.FailureCount);
        Assert.Equal("r1", bulk.SuccessfulRequests[0]);
    }
}
