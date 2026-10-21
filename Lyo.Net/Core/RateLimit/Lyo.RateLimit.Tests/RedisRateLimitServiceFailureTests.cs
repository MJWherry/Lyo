using Lyo.RateLimit.Redis;
using StackExchange.Redis;

namespace Lyo.RateLimit.Tests;

public sealed class RedisRateLimitServiceFailureTests
{
    [Fact]
    public async Task TryAcquire_StoreError_FailOpenFalse_DeniesWithWindowRetryAfter()
    {
        var redis = new ThrowingRedis();
        var sut = new RedisRateLimitService(redis, new() { FailOpen = false });
        var decision = await sut.TryAcquireAsync(Req(), TestCt);
        Assert.False(decision.Allowed);
        Assert.Equal(TimeSpan.FromSeconds(10), decision.RetryAfter);
        Assert.Equal(0, redis.Writes);
    }

    [Fact]
    public async Task TryAcquire_StoreError_FailOpenTrue_AllowsWithoutWrite()
    {
        var redis = new ThrowingRedis();
        var sut = new RedisRateLimitService(redis, new() { FailOpen = true });
        var decision = await sut.TryAcquireAsync(Req(), TestCt);
        Assert.True(decision.Allowed);
        Assert.Equal(0, redis.Writes);
    }

    [Fact]
    public async Task TryAcquire_Cancelled_DoesNotDeny()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var redis = new ThrowingRedis();
        var sut = new RedisRateLimitService(redis, new());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.TryAcquireAsync(Req(), cts.Token).AsTask());
        Assert.Equal(0, redis.Calls);
    }

    private static RateLimitRequest Req() => new("uploads", "user", 4, TimeSpan.FromSeconds(10), RateLimitWindow.Fixed, 1_700_000_000_000L);

    private sealed class ThrowingRedis : IRateLimitRedisExecutor
    {
        public int Calls { get; private set; }

        public int Writes { get; }

        public Task<long[]> EvaluateAsync(string script, string key, RedisValue[] args, CancellationToken ct)
        {
            Calls++;
            ct.ThrowIfCancellationRequested();
            throw new RedisException("down");
        }
    }
}