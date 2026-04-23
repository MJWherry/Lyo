using Lyo.Common;

namespace Lyo.Result.Tests;

public class AsyncResultExtensionsTests
{

    [Fact]
    public async Task ThenAsync_OnSuccess_ChainsToNext()
    {
        var result = await Result<int>.Success(5)
            .ThenAsync(x => Task.FromResult(Result<string>.Success(x.ToString())), ct: TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Equal("5", result.Data);
    }

    [Fact]
    public async Task ThenAsync_OnFailure_ShortCircuits()
    {
        var called = false;
        var result = await Result<int>.Failure("fail", "CODE")
            .ThenAsync(x => { called = true; return Task.FromResult(Result<string>.Success("ok")); }, ct: TestContext.Current.CancellationToken);
        Assert.False(called);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ThenAsync_WithCancellationToken_PassesTokenToDelegate()
    {
        using var cts = new CancellationTokenSource();
        var receivedToken = CancellationToken.None;

        await Result<int>.Success(1)
            .ThenAsync(async (val, ct) => {
                receivedToken = ct;
                await Task.Delay(1, ct);
                return Result<int>.Success(val + 1);
            }, cts.Token);

        Assert.Equal(cts.Token, receivedToken);
    }

    [Fact]
    public async Task ThenAsync_CancelledToken_ThrowsBeforeExecution()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Result<int>.Success(1)
                .ThenAsync((val, ct) => Task.FromResult(Result<int>.Success(val)), cts.Token));
    }

    [Fact]
    public async Task ThenAsync_TaskOverload_ChainsCorrectly()
    {
        var result = await Task.FromResult(Result<int>.Success(10))
            .ThenAsync(x => Task.FromResult(Result<string>.Success(x.ToString())), ct: TestContext.Current.CancellationToken);
        Assert.Equal("10", result.Data);
    }

    [Fact]
    public async Task OnSuccessAsync_OnSuccess_ExecutesAction()
    {
        var called = false;
        await Result<int>.Success(1).OnSuccessAsync(_ => { called = true; return Task.CompletedTask; }, ct: TestContext.Current.CancellationToken);
        Assert.True(called);
    }

    [Fact]
    public async Task OnSuccessAsync_OnFailure_DoesNotExecute()
    {
        var called = false;
        await Result<int>.Failure("fail", "CODE").OnSuccessAsync(_ => { called = true; return Task.CompletedTask; }, ct: TestContext.Current.CancellationToken);
        Assert.False(called);
    }

    [Fact]
    public async Task OnFailureAsync_OnFailure_ExecutesAction()
    {
        var called = false;
        await Result<int>.Failure("fail", "CODE")
            .OnFailureAsync(_ => { called = true; return Task.CompletedTask; }, ct: TestContext.Current.CancellationToken);
        Assert.True(called);
    }

    [Fact]
    public async Task OnFailureAsync_OnSuccess_DoesNotExecute()
    {
        var called = false;
        await Result<int>.Success(1).OnFailureAsync(_ => { called = true; return Task.CompletedTask; }, ct: TestContext.Current.CancellationToken);
        Assert.False(called);
    }

    [Fact]
    public async Task WhenAll_AllSuccess_ReturnsCombinedList()
    {
        var combined = await AsyncResultExtensions.WhenAll(
            Task.FromResult(Result<int>.Success(1)),
            Task.FromResult(Result<int>.Success(2)),
            Task.FromResult(Result<int>.Success(3)));
        Assert.True(combined.IsSuccess);
        Assert.Equal(3, combined.Data!.Count);
    }

    [Fact]
    public async Task WhenAll_AnyFailure_ReturnsAllErrors()
    {
        var combined = await AsyncResultExtensions.WhenAll(
            Task.FromResult(Result<int>.Success(1)),
            Task.FromResult(Result<int>.Failure("fail1", "A")),
            Task.FromResult(Result<int>.Failure("fail2", "B")));
        Assert.False(combined.IsSuccess);
        Assert.Equal(2, combined.Errors!.Count);
    }

    [Fact]
    public async Task FirstSuccess_ReturnsFirstSuccessfulResult()
    {
        var result = await AsyncResultExtensions.FirstSuccess(
            Task.FromResult(Result<int>.Failure("fail", "A")),
            Task.FromResult(Result<int>.Success(42)),
            Task.FromResult(Result<int>.Success(99)));
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Data);
    }

    [Fact]
    public async Task FirstSuccess_AllFail_ReturnsAllErrors()
    {
        var result = await AsyncResultExtensions.FirstSuccess(
            Task.FromResult(Result<int>.Failure("f1", "A")),
            Task.FromResult(Result<int>.Failure("f2", "B")));
        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.Errors!.Count);
    }

    [Fact]
    public async Task MapAsync_TaskOverload_TransformsValue()
    {
        var result = await Task.FromResult(Result<int>.Success(5))
            .MapAsync(x => Task.FromResult(x * 3), ct: TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Equal(15, result.Data);
    }

    [Fact]
    public async Task TapAsync_TaskOverload_ExecutesSideEffect()
    {
        var called = false;
        var result = await Task.FromResult(Result<int>.Success(7))
            .TapAsync(_ => { called = true; return Task.CompletedTask; }, ct: TestContext.Current.CancellationToken);
        Assert.True(called);
        Assert.Equal(7, result.Data);
    }

    [Fact]
    public async Task CombineAsync_BothSuccess_ReturnsTuple()
    {
        var result = await Task.FromResult(Result<int>.Success(1))
            .CombineAsync(Task.FromResult(Result<string>.Success("hello")));
        Assert.True(result.IsSuccess);
        Assert.Equal((1, "hello"), result.Data);
    }

    [Fact]
    public async Task CombineAsync_OneFailure_CollectsErrors()
    {
        var result = await Task.FromResult(Result<int>.Success(1))
            .CombineAsync(Task.FromResult(Result<string>.Failure("fail", "CODE")));
        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors!);
    }
}
