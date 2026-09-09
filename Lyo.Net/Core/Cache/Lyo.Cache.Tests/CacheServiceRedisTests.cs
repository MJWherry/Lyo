using Lyo.Cache.Fusion;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Cache.Tests;

public class CacheServiceRedisTests : RedisContainerFixtureBase
{
    private ICacheService? _cacheService;
    private IServiceProvider? _serviceProvider;

    protected override ValueTask OnContainerStartedAsync(string connectionString, CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Register FusionCache with a Redis backplane (it owns the Redis connection).
        services.AddFusionCache(connectionString, options => options.Enabled = true, configureRedisBackplane: _ => { });
        _serviceProvider = services.BuildServiceProvider();
        _cacheService = _serviceProvider.GetRequiredService<ICacheService>();
        return ValueTask.CompletedTask;
    }

    protected override ValueTask OnContainerDisposingAsync(CancellationToken cancellationToken)
    {
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();

        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task GetOrSetAsync_WithRedisBackplane_CachesValue()
    {
        Assert.NotNull(_cacheService);
        var key = "redis-test-key-1";
        var expectedValue = "redis-cached-value";
        var callCount = 0;
        var result = await _cacheService.GetOrSetAsync<string>(
            key, async ct => {
                callCount++;
                await Task.Delay(10, ct);
                return expectedValue;
            }, token: TestContext.Current.CancellationToken);

        Assert.Equal(expectedValue, result);
        Assert.Equal(1, callCount);

        // Second call should read from cache.
        var cachedResult = await _cacheService.GetOrSetAsync<string>(
            key, _ => {
                callCount++;
                return Task.FromResult("different-value")!;
            }, token: TestContext.Current.CancellationToken);

        Assert.Equal(expectedValue, cachedResult);
        Assert.Equal(1, callCount); // Factory should not be called again
    }

    [Fact]
    public void Set_WithRedisBackplane_StoresValue()
    {
        Assert.NotNull(_cacheService);
        var key = "redis-test-key-2";
        var value = "redis-stored-value";
        _cacheService.Set(key, value);
        var result = _cacheService.GetOrSet<string>(key, _ => "default-value");
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task InvalidateCacheItem_WithRedisBackplane_RemovesItem()
    {
        Assert.NotNull(_cacheService);
        var key = "redis-test-key-3";
        var value = "redis-value-to-invalidate";
        _cacheService.Set(key, value);
        Assert.Equal(value, _cacheService.GetOrSet<string>(key, _ => "default"));
        await _cacheService.InvalidateCacheItem(key);
        Assert.Equal("default", _cacheService.GetOrSet<string>(key, _ => "default"));
    }

    [Fact]
    public async Task InvalidateCacheItemByTag_WithRedisBackplane_RemovesTaggedItems()
    {
        Assert.NotNull(_cacheService);
        var tag = "redis-test-tag";
        var key1 = "redis-tagged-key-1";
        var key2 = "redis-tagged-key-2";
        var key3 = "redis-tagged-key-3";
        _cacheService.Set(key1, "value1", [tag]);
        _cacheService.Set(key2, "value2", [tag]);
        _cacheService.Set(key3, "value3", ["other-tag"]);
        Assert.Equal("value1", _cacheService.GetOrSet<string>(key1, _ => "default"));
        Assert.Equal("value2", _cacheService.GetOrSet<string>(key2, _ => "default"));
        Assert.Equal("value3", _cacheService.GetOrSet<string>(key3, _ => "default"));
        await _cacheService.InvalidateCacheItemByTag(tag);
        Assert.Equal("default", _cacheService.GetOrSet<string>(key1, _ => "default"));
        Assert.Equal("default", _cacheService.GetOrSet<string>(key2, _ => "default"));
        Assert.Equal("value3", _cacheService.GetOrSet<string>(key3, _ => "default"));
    }

    [Fact]
    public async Task GetOrSetAsync_WithRedisBackplane_HandlesConcurrentAccess()
    {
        Assert.NotNull(_cacheService);
        var key = "redis-concurrent-key";
        var callCount = 0;
        var expectedValue = "concurrent-value";

        // Overlap several callers on the same key.
        var tasks = new List<Task<string>>();
        for (var i = 0; i < 10; i++) {
            tasks.Add(
                _cacheService.GetOrSetAsync<string>(
                        key, async ct => {
                            Interlocked.Increment(ref callCount);
                            await Task.Delay(50, ct);
                            return expectedValue;
                        }, token: TestContext.Current.CancellationToken)
                    .AsTask()!);
        }

        var results = await Task.WhenAll(tasks);

        // Every caller should see the same value.
        Assert.All(results, result => Assert.Equal(expectedValue, result));
        // Factory should run once, or a few times if a race slips through.
        Assert.True(callCount <= 3, $"Expected factory to be called 1-3 times, but was called {callCount} times");
    }

    [Fact]
    public async Task GetOrSetAsync_WithRedisBackplane_RespectsExpiration()
    {
        Assert.NotNull(_cacheService);
        var key = "redis-expiration-key";
        var shortExpiration = TimeSpan.FromMilliseconds(100);

        // Store via the value-based overload with a short TTL.
        await _cacheService.GetOrSetAsync(key, "value", options => options.Duration = shortExpiration, token: TestContext.Current.CancellationToken);
        Assert.Equal("value", _cacheService.GetOrSet<string>(key, _ => "default"));

        // Sleep until the TTL elapses.
        await Task.Delay(150, TestContext.Current.CancellationToken);

        // Factory should run again after expiry.
        var result = await _cacheService.GetOrSetAsync<string>(key, _ => Task.FromResult("new-value")!, token: TestContext.Current.CancellationToken);
        Assert.Equal("new-value", result);
    }
}