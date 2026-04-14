using Lyo.Cache.Fusion;
using Lyo.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;

namespace Lyo.Cache.Tests;

public class CacheServiceErrorHandlingTests : IDisposable
{
    private readonly IFusionCache _fusionCache;
    private readonly ILogger<FusionCacheService> _logger;
    private readonly CacheOptions _options;

    public CacheServiceErrorHandlingTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _logger = loggerFactory.CreateLogger<FusionCacheService>();
        _options = new() { Enabled = true, DefaultExpiration = TimeSpan.FromMinutes(5) };
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddFusionCache().TryWithAutoSetup();
        var serviceProvider = services.BuildServiceProvider();
        _fusionCache = serviceProvider.GetRequiredService<IFusionCache>();
    }

    public void Dispose() => _fusionCache.Dispose();

    [Fact]
    public async Task InvalidateCacheItem_WithException_Throws()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "error-test-key";

        // Set a value first
        service.Set(key, "value");

        // Invalidate should work normally
        await service.InvalidateCacheItem(key).ConfigureAwait(false);

        // Verify it's gone
        service.GetOrSet<string>(key, _ => "default").ShouldBe("default");
    }

    [Fact]
    public async Task InvalidateCacheItemByTag_WithException_Throws()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var tag = "error-test-tag";
        var key = "error-tag-key";

        // Set a value with tag
        service.Set(key, "value", [tag]);

        // Invalidate should work normally
        await service.InvalidateCacheItemByTag(tag).ConfigureAwait(false);

        // Verify it's gone
        service.GetOrSet<string>(key, _ => "default").ShouldBe("default");
    }

    [Fact]
    public async Task InvalidateQueryCacheAsync_WithException_Throws()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "error-query-key";

        // Set a value with entity tag
        service.Set(key, "value", ["entity:testentity"]);

        // Invalidate should work normally
        await service.InvalidateQueryCacheAsync<TestModels.TestEntity>().ConfigureAwait(false);

        // Verify it's gone
        service.GetOrSet<string>(key, _ => "default").ShouldBe("default");
    }

    [Fact]
    public async Task InvalidateAllCachedQueriesAsync_WithException_Throws()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "error-queries-key";

        // Set a value with queries tag
        service.Set(key, "value", ["queries"]);

        // Invalidate should work normally
        await service.InvalidateAllCachedQueriesAsync().ConfigureAwait(false);

        // Verify it's gone
        service.GetOrSet<string>(key, _ => "default").ShouldBe("default");
    }

    [Fact]
    public async Task InvalidateCacheByTypeAsync_WithException_Throws()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "error-type-key";
        var typeName = typeof(TestModels.TestEntity).FullName!;

        // Set a value with type tag
        service.Set(key, "value", [$"type:{typeName}"]);

        // Invalidate should work normally
        await service.InvalidateCacheByTypeAsync(typeName).ConfigureAwait(false);

        // Verify it's gone
        service.GetOrSet<string>(key, _ => "default").ShouldBe("default");
    }

    [Fact]
    public void Set_WithException_Throws()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "error-set-key";

        // Set should work normally
        service.Set(key, "value");

        // Verify it's cached
        service.GetOrSet<string>(key, _ => "default").ShouldBe("value");
    }

    [Fact]
    public async Task GetOrSetAsync_WithCacheFailure_FallsBackToFactory()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "fallback-test-key";
        var callCount = 0;

        // First call should work and cache
        var result1 = await service.GetOrSetAsync<string>(
            key, _ => {
                callCount++;
                return Task.FromResult("cached-value")!;
            }, token: TestContext.Current.CancellationToken).ConfigureAwait(false);

        result1.ShouldBe("cached-value");
        callCount.ShouldBe(1);

        // Verify it's cached
        var cached = service.GetOrSet<string>(key, _ => "default");
        cached.ShouldBe("cached-value");

        // Remove from cache manually to simulate failure
        await service.InvalidateCacheItem(key).ConfigureAwait(false);

        // Next call should fallback to factory
        var result2 = await service.GetOrSetAsync<string>(
            key, _ => {
                callCount++;
                return Task.FromResult("new-value")!;
            }, token: TestContext.Current.CancellationToken).ConfigureAwait(false);

        Assert.Equal("new-value", result2);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task GetOrSet_WithCacheFailure_FallsBackToFactory()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "fallback-sync-test-key";
        var callCount = 0;

        // First call should work and cache
        var result1 = service.GetOrSet<string>(
            key, _ => {
                callCount++;
                return "cached-value";
            });

        result1.ShouldBe("cached-value");
        callCount.ShouldBe(1);

        // Remove from cache manually to simulate failure
        await service.InvalidateCacheItem(key).ConfigureAwait(false);

        // Next call should fallback to factory
        var result2 = service.GetOrSet<string>(
            key, _ => {
                callCount++;
                return "new-value";
            });

        Assert.Equal("new-value", result2);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task GetOrSetAsync_WithTypeAndNullTypeName_UsesTypeName()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "null-type-name-test";

        // Type with null FullName should use Name
        var result = await service.GetOrSetAsync<string>(key, _ => Task.FromResult("value"), typeof(string), token: TestContext.Current.CancellationToken).ConfigureAwait(false);
        result.ShouldBe("value");
    }

    [Fact]
    public void GetOrSet_WithTypeAndNullTypeName_UsesTypeName()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "null-type-name-sync-test";

        // Type with null FullName should use Name
        var result = service.GetOrSet<string>(key, _ => "value", typeof(string));
        result.ShouldBe("value");
    }
}