using Lyo.Cache.Fusion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Lyo.Cache.Tests;

public class CacheServiceRedisTests : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:7-alpine").Build();
    private ICacheService? _cacheService;
    private IServiceProvider? _serviceProvider;

    public async ValueTask InitializeAsync()
    {
        await _redisContainer.StartAsync().ConfigureAwait(false);
        var services = new ServiceCollection();
        services.AddLogging();

        // Add FusionCache with Redis backplane (registers Redis connection internally)
        services.AddFusionCache(_redisContainer.GetConnectionString(), options => options.Enabled = true, configureRedisBackplane: _ => { });
        _serviceProvider = services.BuildServiceProvider();
        _cacheService = _serviceProvider.GetRequiredService<ICacheService>();
    }

    public async ValueTask DisposeAsync()
    {
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();

        await _redisContainer.DisposeAsync().ConfigureAwait(false);
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
                await Task.Delay(10, ct).ConfigureAwait(false);
                return expectedValue;
            }, token: TestContext.Current.CancellationToken).ConfigureAwait(false);

        Assert.Equal(expectedValue, result);
        Assert.Equal(1, callCount);

        // Second call should use cache
        var cachedResult = await _cacheService.GetOrSetAsync<string>(
            key, _ => {
                callCount++;
                return Task.FromResult("different-value")!;
            }, token: TestContext.Current.CancellationToken).ConfigureAwait(false);

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
        await _cacheService.InvalidateCacheItem(key).ConfigureAwait(false);
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
        await _cacheService.InvalidateCacheItemByTag(tag).ConfigureAwait(false);
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

        // Simulate concurrent access
        var tasks = new List<Task<string>>();
        for (var i = 0; i < 10; i++) {
            tasks.Add(
                _cacheService.GetOrSetAsync<string>(
                        key, async ct => {
                            Interlocked.Increment(ref callCount);
                            await Task.Delay(50, ct).ConfigureAwait(false);
                            return expectedValue;
                        }, token: TestContext.Current.CancellationToken)
                    .AsTask()!);
        }

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        // All should return the same value
        Assert.All(results, result => Assert.Equal(expectedValue, result));
        // Factory should only be called once (or very few times due to race condition)
        Assert.True(callCount <= 3, $"Expected factory to be called 1-3 times, but was called {callCount} times");
    }

    [Fact]
    public async Task GetOrSetAsync_WithRedisBackplane_RespectsExpiration()
    {
        Assert.NotNull(_cacheService);
        var key = "redis-expiration-key";
        var shortExpiration = TimeSpan.FromMilliseconds(100);

        // Set with short expiration using value-based overload
        await _cacheService.GetOrSetAsync(key, "value", options => options.Duration = shortExpiration, token: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal("value", _cacheService.GetOrSet<string>(key, _ => "default"));

        // Wait for expiration
        await Task.Delay(150, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Should call factory (cache expired)
        var result = await _cacheService.GetOrSetAsync<string>(key, ct => Task.FromResult("new-value")!, token: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal("new-value", result);
    }

    [Fact]
    public void AddRedisConnection_WithConnectionString_RegistersConnection()
    {
        var services = new ServiceCollection();
        services.AddRedisConnection("localhost:6379");

        // Verify service is registered without actually connecting
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IConnectionMultiplexer));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddRedisConnection_WithConfiguration_RegistersConnection()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "Redis:ConnectionString", "localhost:6379" } }).Build();
        services.AddRedisConnectionFromConfiguration(configuration);

        // Verify service is registered without actually connecting
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IConnectionMultiplexer));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }
}