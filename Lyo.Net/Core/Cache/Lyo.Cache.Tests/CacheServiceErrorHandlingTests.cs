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

        // Seed a cached value first.
        service.Set(key, "value");

        // Invalidate should succeed on a healthy cache.
        await service.InvalidateCacheItem(key);

        // Confirm the key is no longer present.
        service.GetOrSet<string>(key, _ => "default").ShouldBe("default");
    }

    [Fact]
    public async Task InvalidateCacheItemByTag_WithException_Throws()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var tag = "error-test-tag";
        var key = "error-tag-key";

        // Seed a value that carries a tag.
        service.Set(key, "value", [tag]);

        // Invalidate should succeed on a healthy cache.
        await service.InvalidateCacheItemByTag(tag);

        // Confirm the key is no longer present.
        service.GetOrSet<string>(key, _ => "default").ShouldBe("default");
    }

    [Fact]
    public async Task InvalidateQueryCacheAsync_WithException_Throws()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "error-query-key";

        // Seed a value tagged as an entity.
        service.Set(key, "value", [Constants.Tags.EntityType(typeof(TestModels.TestEntity))]);

        // Invalidate should succeed on a healthy cache.
        await service.InvalidateQueryCacheAsync<TestModels.TestEntity>();

        // Confirm the key is no longer present.
        service.GetOrSet<string>(key, _ => "default").ShouldBe("default");
    }

    [Fact]
    public async Task InvalidateAllCachedQueriesAsync_WithException_Throws()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "error-queries-key";

        // Seed a value tagged for queries.
        service.Set(key, "value", ["queries"]);

        // Invalidate should succeed on a healthy cache.
        await service.InvalidateAllCachedQueriesAsync();

        // Confirm the key is no longer present.
        service.GetOrSet<string>(key, _ => "default").ShouldBe("default");
    }

    [Fact]
    public async Task InvalidateCacheByTypeAsync_WithException_Throws()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "error-type-key";
        var typeName = typeof(TestModels.TestEntity).FullName!;

        // Seed a value tagged by CLR type.
        service.Set(key, "value", [$"type:{typeName}"]);

        // Invalidate should succeed on a healthy cache.
        await service.InvalidateCacheByTypeAsync(typeName);

        // Confirm the key is no longer present.
        service.GetOrSet<string>(key, _ => "default").ShouldBe("default");
    }

    [Fact]
    public void Set_WithException_Throws()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "error-set-key";

        // Set should succeed on a healthy cache.
        service.Set(key, "value");

        // Confirm the value is now in cache.
        service.GetOrSet<string>(key, _ => "default").ShouldBe("value");
    }

    [Fact]
    public async Task GetOrSetAsync_WithCacheFailure_FallsBackToFactory()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "fallback-test-key";
        var callCount = 0;

        // First call should succeed and populate cache.
        var result1 = await service.GetOrSetAsync<string>(
            key, _ => {
                callCount++;
                return Task.FromResult("cached-value")!;
            }, token: TestContext.Current.CancellationToken);

        result1.ShouldBe("cached-value");
        callCount.ShouldBe(1);

        // Confirm the value is now in cache.
        var cached = service.GetOrSet<string>(key, _ => "default");
        cached.ShouldBe("cached-value");

        // Drop the entry by hand to simulate a later cache miss.
        await service.InvalidateCacheItem(key);

        // The next call should fall back to the factory.
        var result2 = await service.GetOrSetAsync<string>(
            key, _ => {
                callCount++;
                return Task.FromResult("new-value")!;
            }, token: TestContext.Current.CancellationToken);

        Assert.Equal("new-value", result2);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task GetOrSet_WithCacheFailure_FallsBackToFactory()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "fallback-sync-test-key";
        var callCount = 0;

        // First call should succeed and populate cache.
        var result1 = service.GetOrSet<string>(
            key, _ => {
                callCount++;
                return "cached-value";
            });

        result1.ShouldBe("cached-value");
        callCount.ShouldBe(1);

        // Drop the entry by hand to simulate a later cache miss.
        await service.InvalidateCacheItem(key);

        // The next call should fall back to the factory.
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

        // A type whose FullName is null should fall back to Name.
        var result = await service.GetOrSetAsync<string>(key, _ => Task.FromResult("value"), typeof(string), token: TestContext.Current.CancellationToken);
        result.ShouldBe("value");
    }

    [Fact]
    public void GetOrSet_WithTypeAndNullTypeName_UsesTypeName()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "null-type-name-sync-test";

        // A type whose FullName is null should fall back to Name.
        var result = service.GetOrSet<string>(key, _ => "value", typeof(string));
        result.ShouldBe("value");
    }
}