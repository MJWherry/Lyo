namespace Lyo.RateLimit.Tests;

public sealed class LocalRateLimitServiceTests
{
    private const long Epoch = 1_700_000_000_000L;
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task TryAcquire_UnderLimit_Allows()
    {
        var decision = await New().TryAcquireAsync(Req(RateLimitWindow.Fixed, Epoch), TestCt);
        Assert.True(decision.Allowed);
        Assert.Null(decision.RetryAfter);
        Assert.Equal(3, decision.Remaining);
    }

    [Theory]
    [InlineData(RateLimitWindow.Fixed)]
    [InlineData(RateLimitWindow.Sliding)]
    [InlineData(RateLimitWindow.TokenBucket)]
    public async Task TryAcquire_AtPermitLimit_Allows_NextRejects(RateLimitWindow pattern)
    {
        var sut = New();
        for (var i = 0; i < 4; i++)
            Assert.True((await sut.TryAcquireAsync(Req(pattern, Epoch), TestCt)).Allowed);

        var denied = await sut.TryAcquireAsync(Req(pattern, Epoch), TestCt);
        Assert.False(denied.Allowed);
        Assert.Equal(0, denied.Remaining);
        Assert.NotNull(denied.RetryAfter);
    }

    [Fact]
    public async Task TryAcquire_OverLimit_RejectsWithRetryAfter()
    {
        var sut = New();
        for (var i = 0; i < 4; i++)
            await sut.TryAcquireAsync(Req(RateLimitWindow.Fixed, Epoch), TestCt);

        var denied = await sut.TryAcquireAsync(Req(RateLimitWindow.Fixed, Epoch), TestCt);
        Assert.False(denied.Allowed);
        Assert.Equal(Window, denied.RetryAfter);
    }

    [Fact]
    public async Task TryAcquire_Fixed_AtNextBoundary_AllowsFullBurst()
    {
        var sut = New();
        for (var i = 0; i < 4; i++)
            Assert.True((await sut.TryAcquireAsync(Req(RateLimitWindow.Fixed, Epoch + 9_000), TestCt)).Allowed);

        for (var i = 0; i < 4; i++)
            Assert.True((await sut.TryAcquireAsync(Req(RateLimitWindow.Fixed, Epoch + 10_000), TestCt)).Allowed);

        Assert.False((await sut.TryAcquireAsync(Req(RateLimitWindow.Fixed, Epoch + 10_000), TestCt)).Allowed);
    }

    [Fact]
    public async Task TryAcquire_Fixed_RetryAfter_IsTimeToNextId()
    {
        var sut = New();
        Assert.True((await sut.TryAcquireAsync(Req(RateLimitWindow.Fixed, Epoch + 9_900, limit: 1), TestCt)).Allowed);
        var denied = await sut.TryAcquireAsync(Req(RateLimitWindow.Fixed, Epoch + 9_950, limit: 1), TestCt);
        Assert.False(denied.Allowed);
        Assert.Equal(TimeSpan.FromMilliseconds(50), denied.RetryAfter);
    }

    [Fact]
    public async Task TryAcquire_Sliding_AtBoundary_StillRejects()
    {
        var sut = New();
        for (var i = 0; i < 4; i++)
            Assert.True((await sut.TryAcquireAsync(Req(RateLimitWindow.Sliding, Epoch), TestCt)).Allowed);

        var denied = await sut.TryAcquireAsync(Req(RateLimitWindow.Sliding, Epoch + 10_000), TestCt);
        Assert.False(denied.Allowed);
    }

    [Fact]
    public async Task TryAcquire_Sliding_HalfwayThroughNextWindow_AllowsExactlyHalf()
    {
        var sut = New();
        for (var i = 0; i < 4; i++)
            await sut.TryAcquireAsync(Req(RateLimitWindow.Sliding, Epoch), TestCt);

        var allowed = 0;
        for (var i = 0; i < 4; i++) {
            if ((await sut.TryAcquireAsync(Req(RateLimitWindow.Sliding, Epoch + 15_000), TestCt)).Allowed)
                allowed++;
        }

        Assert.Equal(2, allowed);
    }

    [Fact]
    public async Task TryAcquire_Sliding_SkippedWindow_DoesNotCountStalePrevious()
    {
        var sut = New();
        for (var i = 0; i < 4; i++)
            await sut.TryAcquireAsync(Req(RateLimitWindow.Sliding, Epoch), TestCt);

        for (var i = 0; i < 4; i++)
            Assert.True((await sut.TryAcquireAsync(Req(RateLimitWindow.Sliding, Epoch + 30_000), TestCt)).Allowed);
    }

    [Fact]
    public async Task TryAcquire_Sliding_BothWindowsFull_RetryAfterExceedsWindow()
    {
        var sut = New();
        for (var i = 0; i < 4; i++)
            await sut.TryAcquireAsync(Req(RateLimitWindow.Sliding, Epoch), TestCt);

        var denied = await sut.TryAcquireAsync(Req(RateLimitWindow.Sliding, Epoch), TestCt);
        Assert.False(denied.Allowed);
        Assert.NotNull(denied.RetryAfter);
        Assert.True(denied.RetryAfter > Window);
        Assert.Equal(TimeSpan.FromMilliseconds(12_500), denied.RetryAfter);
    }

    [Fact]
    public async Task TryAcquire_TokenBucket_Halfway_RefillsExactlyHalf()
    {
        var sut = New();
        for (var i = 0; i < 4; i++)
            Assert.True((await sut.TryAcquireAsync(Req(RateLimitWindow.TokenBucket, Epoch), TestCt)).Allowed);

        var allowed = 0;
        for (var i = 0; i < 4; i++) {
            if ((await sut.TryAcquireAsync(Req(RateLimitWindow.TokenBucket, Epoch + 5_000), TestCt)).Allowed)
                allowed++;
        }

        Assert.Equal(2, allowed);
    }

    [Fact]
    public async Task TryAcquire_TokenBucket_SpendNearTick_RefillsFromLastUpdate()
    {
        var sut = New();
        var window = TimeSpan.FromSeconds(2);
        Assert.True((await sut.TryAcquireAsync(Req(RateLimitWindow.TokenBucket, Epoch + 1_900, limit: 1, window: window), TestCt)).Allowed);
        Assert.False((await sut.TryAcquireAsync(Req(RateLimitWindow.TokenBucket, Epoch + 2_000, limit: 1, window: window), TestCt)).Allowed);
        Assert.True((await sut.TryAcquireAsync(Req(RateLimitWindow.TokenBucket, Epoch + 3_900, limit: 1, window: window), TestCt)).Allowed);
    }

    [Fact]
    public async Task TryAcquire_OtherPartition_DoesNotShareCount()
    {
        var sut = New();
        for (var i = 0; i < 4; i++)
            await sut.TryAcquireAsync(Req(RateLimitWindow.Fixed, Epoch), TestCt);

        Assert.True((await sut.TryAcquireAsync(Req(RateLimitWindow.Fixed, Epoch, partition: "other"), TestCt)).Allowed);
    }

    [Fact]
    public async Task TryAcquire_OtherPolicy_DoesNotShareCount()
    {
        var sut = New();
        for (var i = 0; i < 4; i++)
            await sut.TryAcquireAsync(Req(RateLimitWindow.Fixed, Epoch), TestCt);

        Assert.True((await sut.TryAcquireAsync(Req(RateLimitWindow.Fixed, Epoch, policy: "other"), TestCt)).Allowed);
    }

    [Fact]
    public async Task TryAcquire_DifferentLimit_FailsClosed()
    {
        var sut = New();
        Assert.True((await sut.TryAcquireAsync(Req(RateLimitWindow.Fixed, Epoch), TestCt)).Allowed);
        var denied = await sut.TryAcquireAsync(Req(RateLimitWindow.Fixed, Epoch, limit: 2), TestCt);
        Assert.False(denied.Allowed);
        Assert.Equal(Window, denied.RetryAfter);

        for (var i = 0; i < 3; i++)
            Assert.True((await sut.TryAcquireAsync(Req(RateLimitWindow.Fixed, Epoch), TestCt)).Allowed);

        Assert.False((await sut.TryAcquireAsync(Req(RateLimitWindow.Fixed, Epoch), TestCt)).Allowed);
    }

    [Fact]
    public async Task TryAcquire_ConcurrentSamePartition_DoesNotExceedLimit()
    {
        var sut = New();
        var allowed = 0;
        var tasks = Enumerable.Range(0, 32).Select(async _ => {
            var decision = await sut.TryAcquireAsync(Req(RateLimitWindow.TokenBucket, Epoch), TestCt);
            if (decision.Allowed)
                Interlocked.Increment(ref allowed);
        });
        await Task.WhenAll(tasks);
        Assert.Equal(4, allowed);
    }

    [Fact]
    public async Task TryAcquire_Cancelled_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var sut = New();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.TryAcquireAsync(Req(RateLimitWindow.Fixed, Epoch), cts.Token).AsTask());
        Assert.True((await sut.TryAcquireAsync(Req(RateLimitWindow.Fixed, Epoch), TestCt)).Allowed);
    }

    private static LocalRateLimitService New() => new(new RateLimitOptions());

    private static RateLimitRequest Req(
        RateLimitWindow pattern,
        long now,
        string policy = "uploads",
        string partition = "user",
        int limit = 4,
        TimeSpan? window = null)
        => new(policy, partition, limit, window ?? Window, pattern, now);
}
