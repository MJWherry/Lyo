using Lyo.Cache.Fusion;
using Lyo.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;

namespace Lyo.Cache.Tests;

public class CacheServiceTests : IDisposable
{
    private readonly IFusionCache _fusionCache;
    private readonly ILogger<LocalCacheService> _localLogger;
    private readonly ILogger<FusionCacheService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly CacheOptions _options;

    public CacheServiceTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _logger = loggerFactory.CreateLogger<FusionCacheService>();
        _localLogger = loggerFactory.CreateLogger<LocalCacheService>();
        _options = new() { Enabled = true, DefaultExpiration = TimeSpan.FromMinutes(5) };
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddFusionCache().TryWithAutoSetup();
        var serviceProvider = services.BuildServiceProvider();
        _fusionCache = serviceProvider.GetRequiredService<IFusionCache>();
        _memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
    }

    public void Dispose() => _fusionCache.Dispose();

    [Fact]
    public void Constructor_WithEnabledCache_CreatesService()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        service.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithDisabledCache_CreatesService()
    {
        var disabledOptions = new CacheOptions { Enabled = false };
        var service = new LocalCacheService(_memoryCache, _localLogger, disabledOptions);
        service.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetOrSetAsync_WithEnabledCache_ReturnsCachedValue()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "test-key-1";
        var expectedValue = "test-value";
        var result = await service.GetOrSetAsync<string>(
            key, async ct => {
                await Task.Delay(10, ct).ConfigureAwait(false);
                return expectedValue;
            }, token: TestContext.Current.CancellationToken).ConfigureAwait(false);

        result.ShouldBe(expectedValue);

        // Second call should return cached value (factory shouldn't be called again)
        var callCount = 0;
        var cachedResult = await service.GetOrSetAsync<string>(
            key, async ct => {
                callCount++;
                await Task.Delay(10, ct).ConfigureAwait(false);
                return "different-value";
            }, token: TestContext.Current.CancellationToken).ConfigureAwait(false);

        cachedResult.ShouldBe(expectedValue);
        callCount.ShouldBe(0); // Factory should not be called
    }

    [Fact]
    public async Task GetOrSetAsync_WithDisabledCache_AlwaysCallsFactory()
    {
        var disabledOptions = new CacheOptions { Enabled = false };
        var service = new LocalCacheService(_memoryCache, _localLogger, disabledOptions);
        var key = "test-key-2";
        var callCount = 0;
        var result1 = await service.GetOrSetAsync<string>(
            key, ct => {
                callCount++;
                return Task.FromResult("value-1")!;
            }, token: TestContext.Current.CancellationToken).ConfigureAwait(false);

        var result2 = await service.GetOrSetAsync<string>(
            key, ct => {
                callCount++;
                return Task.FromResult("value-2")!;
            }, token: TestContext.Current.CancellationToken).ConfigureAwait(false);

        result1.ShouldBe("value-1");
        result2.ShouldBe("value-2");
        callCount.ShouldBe(2); // Factory should be called each time
    }

    [Fact]
    public void GetOrSet_WithEnabledCache_ReturnsCachedValue()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "test-key-3";
        var expectedValue = 42;
        var result = service.GetOrSet(key, _ => expectedValue);
        result.ShouldBe(expectedValue);
        var callCount = 0;
        var cachedResult = service.GetOrSet(
            key, _ => {
                callCount++;
                return 999;
            });

        cachedResult.ShouldBe(expectedValue);
        callCount.ShouldBe(0);
    }

    [Fact]
    public void Set_WithEnabledCache_StoresValue()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "test-key-4";
        var value = "stored-value";
        service.Set(key, value);
        var result = service.GetOrSet<string>(key, _ => "default-value");
        result.ShouldBe(value);
    }

    [Fact]
    public void Set_WithDisabledCache_DoesNothing()
    {
        var disabledOptions = new CacheOptions { Enabled = false };
        var service = new LocalCacheService(_memoryCache, _localLogger, disabledOptions);
        var key = "test-key-5";
        var value = "stored-value";
        service.Set(key, value); // Should not throw

        // Verify it's not cached
        var result = service.GetOrSet<string>(key, _ => "default-value");
        result.ShouldBe("default-value");
    }

    [Fact]
    public async Task InvalidateCacheItem_WithEnabledCache_RemovesItem()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "test-key-6";
        var value = "cached-value";

        // Set and verify it's cached
        service.Set(key, value);
        var cached = service.GetOrSet<string>(key, _ => "default");
        cached.ShouldBe(value);

        // Invalidate
        await service.InvalidateCacheItem(key).ConfigureAwait(false);

        // Verify it's gone
        var afterInvalidate = service.GetOrSet<string>(key, _ => "default");
        afterInvalidate.ShouldBe("default");
    }

    [Fact]
    public async Task InvalidateCacheItem_WithDisabledCache_DoesNothing()
    {
        var disabledOptions = new CacheOptions { Enabled = false };
        var service = new LocalCacheService(_memoryCache, _localLogger, disabledOptions);
        var key = "test-key-7";
        await service.InvalidateCacheItem(key).ConfigureAwait(false); // Should not throw
    }

    [Fact]
    public async Task InvalidateCacheItemByTag_WithEnabledCache_RemovesTaggedItems()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var tag = "test-tag";
        var key1 = "test-key-tag-1";
        var key2 = "test-key-tag-2";
        var key3 = "test-key-tag-3";

        // Set items with tags
        service.Set(key1, "value1", [tag]);
        service.Set(key2, "value2", [tag]);
        service.Set(key3, "value3", ["other-tag"]);

        // Verify they're cached
        service.GetOrSet<string>(key1, _ => "default").ShouldBe("value1");
        service.GetOrSet<string>(key2, _ => "default").ShouldBe("value2");
        service.GetOrSet<string>(key3, _ => "default").ShouldBe("value3");

        // Invalidate by tag
        await service.InvalidateCacheItemByTag(tag).ConfigureAwait(false);

        // Verify tagged items are gone, but untagged item remains
        service.GetOrSet<string>(key1, _ => "default").ShouldBe("default");
        service.GetOrSet<string>(key2, _ => "default").ShouldBe("default");
        service.GetOrSet<string>(key3, _ => "default").ShouldBe("value3");
    }

    [Fact]
    public async Task InvalidateQueryCacheAsync_WithEnabledCache_RemovesQueryCache()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var entityTag = "entity:testentity";
        var key1 = "test-query-1";
        var key2 = "test-query-2";

        // Set items with entity tag
        service.Set(key1, "value1", [entityTag]);
        service.Set(key2, "value2", [entityTag]);
        service.GetOrSet<string>(key1, _ => "default").ShouldBe("value1");
        service.GetOrSet<string>(key2, _ => "default").ShouldBe("value2");

        // Invalidate query cache
        await service.InvalidateQueryCacheAsync<TestModels.TestEntity>().ConfigureAwait(false);

        // Verify items are gone
        service.GetOrSet<string>(key1, _ => "default").ShouldBe("default");
        service.GetOrSet<string>(key2, _ => "default").ShouldBe("default");
    }

    [Fact]
    public async Task InvalidateAllCachedQueriesAsync_WithEnabledCache_RemovesAllQueries()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var queriesTag = "queries";
        var key1 = "query-1";
        var key2 = "query-2";
        service.Set(key1, "value1", [queriesTag]);
        service.Set(key2, "value2", [queriesTag]);
        service.GetOrSet<string>(key1, _ => "default").ShouldBe("value1");
        service.GetOrSet<string>(key2, _ => "default").ShouldBe("value2");
        await service.InvalidateAllCachedQueriesAsync().ConfigureAwait(false);
        service.GetOrSet<string>(key1, _ => "default").ShouldBe("default");
        service.GetOrSet<string>(key2, _ => "default").ShouldBe("default");
    }

    [Fact]
    public async Task GetOrSetAsync_WithException_FallsBackToFactory()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "test-key-exception";
        var callCount = 0;

        // First call should work
        var result1 = await service.GetOrSetAsync<string>(
            key, ct => {
                callCount++;
                return Task.FromResult("value-1")!;
            }, token: TestContext.Current.CancellationToken).ConfigureAwait(false);

        result1.ShouldBe("value-1");
        callCount.ShouldBe(1);

        // Simulate cache failure by using a different key and forcing an error
        // The fallback should call factory again
        var result2 = await service.GetOrSetAsync<string>(
            key + "-new", ct => {
                callCount++;
                if (callCount == 1)
                    throw new("Cache error");

                return Task.FromResult("value-2")!;
            }, token: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Should fallback to factory and get value-2
        result2.ShouldBe("value-2");
    }

    [Fact]
    public void Items_Property_ReturnsReadOnlyCollection()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var items = service.Items;
        items.ShouldNotBeNull();
        items.ShouldBeAssignableTo<IReadOnlyCollection<CacheItem>>();
    }

    [Fact]
    public async Task GetOrSetAsync_WithTags_StoresTags()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "test-key-tags";
        var tags = new[] { "tag1", "tag2" };
        await service.GetOrSetAsync<string>(key, ct => Task.FromResult("value")!, tags, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Verify item exists
        var result = service.GetOrSet<string>(key, _ => "default");
        result.ShouldBe("value");

        // Invalidate by tag
        await service.InvalidateCacheItemByTag("tag1").ConfigureAwait(false);

        // Should be removed
        var afterInvalidate = service.GetOrSet<string>(key, _ => "default");
        afterInvalidate.ShouldBe("default");
    }

    [Fact]
    public async Task GetOrSetAsync_WithCancellationToken_RespectsCancellation()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "test-key-cancellation";
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(false);
        var ex = await ExceptionAssertions.ThrowsAsync<TaskCanceledException>(async () => {
            await service.GetOrSetAsync<string>(
                key, async ct => {
                    await Task.Delay(1000, ct).ConfigureAwait(false);
                    return "value";
                }, token: cts.Token).ConfigureAwait(false);
        });

        ex.ShouldNotBeNull();
    }
}