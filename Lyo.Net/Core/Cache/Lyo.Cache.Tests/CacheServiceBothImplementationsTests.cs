using Lyo.Cache.Fusion;
using Lyo.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;

namespace Lyo.Cache.Tests;

/// <summary>Runs core cache behavior tests against both LocalCacheService and FusionCacheService.</summary>
public class CacheServiceBothImplementationsTests : IDisposable
{
    private readonly IFusionCache _fusionCache;
    private readonly ILogger<FusionCacheService> _fusionLogger;
    private readonly ILogger<LocalCacheService> _localLogger;
    private readonly IMemoryCache _memoryCache;
    private readonly CacheOptions _options;

    public static IEnumerable<object[]> CacheImplementations => [["Local"], ["Fusion"]];

    public CacheServiceBothImplementationsTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _fusionLogger = loggerFactory.CreateLogger<FusionCacheService>();
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

    private ICacheService CreateCacheService(string implementation)
        => implementation switch {
            "Local" => new LocalCacheService(_memoryCache, _localLogger, _options),
            "Fusion" => new FusionCacheService(_fusionCache, _fusionLogger, _options),
            var _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public async Task GetOrSetAsync_ReturnsCachedValue(string implementation)
    {
        var service = CreateCacheService(implementation);
        var key = $"both-getorset-async-{implementation}";
        var expectedValue = "cached-value";
        var result = await service.GetOrSetAsync<string>(
            key, async ct => {
                await Task.Delay(5, ct).ConfigureAwait(false);
                return expectedValue;
            }, token: TestContext.Current.CancellationToken).ConfigureAwait(false);

        result.ShouldBe(expectedValue);
        var callCount = 0;
        var cachedResult = await service.GetOrSetAsync<string>(
            key, async ct => {
                callCount++;
                return "different";
            }, token: TestContext.Current.CancellationToken).ConfigureAwait(false);

        cachedResult.ShouldBe(expectedValue);
        callCount.ShouldBe(0);
    }

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public void GetOrSet_ReturnsCachedValue(string implementation)
    {
        var service = CreateCacheService(implementation);
        var key = $"both-getorset-{implementation}";
        var expectedValue = 123;
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

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public void Set_StoresValue(string implementation)
    {
        var service = CreateCacheService(implementation);
        var key = $"both-set-{implementation}";
        var value = "stored";
        service.Set(key, value);
        var result = service.GetOrSet<string>(key, _ => "default");
        result.ShouldBe(value);
    }

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public async Task InvalidateCacheItem_RemovesItem(string implementation)
    {
        var service = CreateCacheService(implementation);
        var key = $"both-invalidate-{implementation}";
        service.Set(key, "value");
        service.GetOrSet<string>(key, _ => "default").ShouldBe("value");
        await service.InvalidateCacheItem(key).ConfigureAwait(false);
        service.GetOrSet<string>(key, _ => "default").ShouldBe("default");
    }

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public async Task InvalidateCacheItemByTag_RemovesTaggedItems(string implementation)
    {
        var service = CreateCacheService(implementation);
        var tag = $"both-tag-{implementation}";
        var key1 = $"both-tag-key1-{implementation}";
        var key2 = $"both-tag-key2-{implementation}";
        var key3 = $"both-tag-key3-{implementation}";
        await service.GetOrSetAsync<string>(key1, ct => Task.FromResult("v1")!, [tag], TestContext.Current.CancellationToken).ConfigureAwait(false);
        await service.GetOrSetAsync<string>(key2, ct => Task.FromResult("v2")!, [tag], TestContext.Current.CancellationToken).ConfigureAwait(false);
        await service.GetOrSetAsync<string>(key3, ct => Task.FromResult("v3")!, ["other-tag"], TestContext.Current.CancellationToken).ConfigureAwait(false);
        service.GetOrSet<string>(key1, _ => "default").ShouldBe("v1");
        service.GetOrSet<string>(key2, _ => "default").ShouldBe("v2");
        service.GetOrSet<string>(key3, _ => "default").ShouldBe("v3");
        await service.InvalidateCacheItemByTag(tag).ConfigureAwait(false);
        service.GetOrSet<string>(key1, _ => "default").ShouldBe("default");
        service.GetOrSet<string>(key2, _ => "default").ShouldBe("default");
        service.GetOrSet<string>(key3, _ => "default").ShouldBe("v3");
    }

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public async Task GetOrSetAsync_WithFactoryReturningTags_StoresTags(string implementation)
    {
        var service = CreateCacheService(implementation);
        var tag = $"both-factory-tags-{implementation}";
        var key = $"both-factory-tags-key-{implementation}";
        await service.GetOrSetAsync<string>(key, ct => Task.FromResult<(string?, string[]?)>(("value", [tag])), token: TestContext.Current.CancellationToken).ConfigureAwait(false);
        service.GetOrSet<string>(key, _ => "default").ShouldBe("value");
        await service.InvalidateCacheItemByTag(tag).ConfigureAwait(false);
        service.GetOrSet<string>(key, _ => "default").ShouldBe("default");
    }

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public async Task GetOrSetAsync_WithExtraTags_MergesTags(string implementation)
    {
        var service = CreateCacheService(implementation);
        var extraTag = $"both-extra-{implementation}";
        var key = $"both-extra-key-{implementation}";
        await service.GetOrSetAsync<string>(key, ct => Task.FromResult("value")!, [extraTag], TestContext.Current.CancellationToken).ConfigureAwait(false);
        service.GetOrSet<string>(key, _ => "default").ShouldBe("value");
        await service.InvalidateCacheItemByTag(extraTag).ConfigureAwait(false);
        service.GetOrSet<string>(key, _ => "default").ShouldBe("default");
    }

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public void GetOrSet_WithDuration_UsesCustomExpiration(string implementation)
    {
        var service = CreateCacheService(implementation);
        var key = $"both-duration-{implementation}";
        var shortDuration = TimeSpan.FromMilliseconds(50);
        service.GetOrSet(key, _ => "value", shortDuration);
        service.GetOrSet<string>(key, _ => "default").ShouldBe("value");
        Thread.Sleep(100);
        service.GetOrSet<string>(key, _ => "default").ShouldBe("default");
    }

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public void Items_ReturnsReadOnlyCollection(string implementation)
    {
        var service = CreateCacheService(implementation);
        var items = service.Items;
        items.ShouldNotBeNull();
        items.ShouldBeAssignableTo<IReadOnlyCollection<CacheItem>>();
    }

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public async Task GetOrSetAsync_KeyNormalizedToLowercase(string implementation)
    {
        var service = CreateCacheService(implementation);
        var key = $"Both-MixedCase-{implementation}";
        await service.GetOrSetAsync<string>(key, ct => Task.FromResult("value")!, token: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result = service.GetOrSet<string>(key.ToLowerInvariant(), _ => "default");
        result.ShouldBe("value");
    }
}