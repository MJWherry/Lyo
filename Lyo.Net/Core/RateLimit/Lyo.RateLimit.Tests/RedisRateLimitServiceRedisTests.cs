using Lyo.RateLimit.Redis;
using Lyo.Testing.Containers;
using StackExchange.Redis;

namespace Lyo.RateLimit.Tests;

public sealed class RedisRateLimitServiceRedisTests : RedisContainerFixtureBase
{
    private const long Epoch = 1_700_000_000_000L;
    private IConnectionMultiplexer? _firstMux;
    private IConnectionMultiplexer? _secondMux;
    private RedisRateLimitService? _first;
    private RedisRateLimitService? _second;

    protected override async ValueTask OnContainerStartedAsync(string connectionString, CancellationToken cancellationToken)
    {
        _firstMux = await ConnectionMultiplexer.ConnectAsync(connectionString);
        _secondMux = await ConnectionMultiplexer.ConnectAsync(connectionString);
        var options = new RedisRateLimitOptions();
        _first = new RedisRateLimitService(_firstMux, options);
        _second = new RedisRateLimitService(_secondMux, options);
    }

    protected override async ValueTask OnContainerDisposingAsync(CancellationToken cancellationToken)
    {
        if (_firstMux is not null)
            await _firstMux.DisposeAsync();

        if (_secondMux is not null)
            await _secondMux.DisposeAsync();
    }

    [Theory]
    [InlineData(RateLimitWindow.Fixed)]
    [InlineData(RateLimitWindow.Sliding)]
    [InlineData(RateLimitWindow.TokenBucket)]
    public async Task TryAcquire_SecondInstance_SeesFirstInstanceConsumption(RateLimitWindow pattern)
    {
        var policy = "share-" + pattern;
        for (var i = 0; i < 4; i++)
            Assert.True((await First().TryAcquireAsync(Req(pattern, Epoch, policy), TestCt)).Allowed);

        Assert.False((await Second().TryAcquireAsync(Req(pattern, Epoch, policy), TestCt)).Allowed);
    }

    [Fact]
    public async Task TryAcquire_Fixed_SharedBoundary_AllowsFullBurst()
    {
        var policy = "fixed-boundary";
        for (var i = 0; i < 4; i++)
            Assert.True((await First().TryAcquireAsync(Req(RateLimitWindow.Fixed, Epoch + 9_000, policy), TestCt)).Allowed);

        for (var i = 0; i < 4; i++)
            Assert.True((await Second().TryAcquireAsync(Req(RateLimitWindow.Fixed, Epoch + 10_000, policy), TestCt)).Allowed);
    }

    [Fact]
    public async Task TryAcquire_Sliding_SharedBoundary_StillRejects()
    {
        var policy = "sliding-boundary";
        for (var i = 0; i < 4; i++)
            Assert.True((await First().TryAcquireAsync(Req(RateLimitWindow.Sliding, Epoch, policy), TestCt)).Allowed);

        Assert.False((await Second().TryAcquireAsync(Req(RateLimitWindow.Sliding, Epoch + 10_000, policy), TestCt)).Allowed);
    }

    [Fact]
    public async Task TryAcquire_TokenBucket_SharedHalfway_RefillsHalf()
    {
        var policy = "token-half";
        for (var i = 0; i < 4; i++)
            Assert.True((await First().TryAcquireAsync(Req(RateLimitWindow.TokenBucket, Epoch, policy), TestCt)).Allowed);

        var allowed = 0;
        for (var i = 0; i < 4; i++) {
            if ((await Second().TryAcquireAsync(Req(RateLimitWindow.TokenBucket, Epoch + 5_000, policy), TestCt)).Allowed)
                allowed++;
        }

        Assert.Equal(2, allowed);
    }

    private RedisRateLimitService First() => _first ?? throw new InvalidOperationException("Redis is not started.");

    private RedisRateLimitService Second() => _second ?? throw new InvalidOperationException("Redis is not started.");

    private static RateLimitRequest Req(RateLimitWindow pattern, long now, string policy)
        => new(policy, "user", 4, TimeSpan.FromSeconds(10), pattern, now);
}
