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
                await Task.Delay(5, ct);
                return expectedValue;
            }, token: TestContext.Current.CancellationToken);

        result.ShouldBe(expectedValue);
        var callCount = 0;
        var cachedResult = await service.GetOrSetAsync<string>(
            key, _ => {
                try {
                    callCount++;
                    return Task.FromResult("different")!;
                }
                catch (Exception exception) {
                    return Task.FromException<string?>(exception);
                }
            }, token: TestContext.Current.CancellationToken);

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
        await service.InvalidateCacheItem(key);
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
        await service.GetOrSetAsync<string>(key1, _ => Task.FromResult("v1")!, [tag], TestContext.Current.CancellationToken);
        await service.GetOrSetAsync<string>(key2, _ => Task.FromResult("v2")!, [tag], TestContext.Current.CancellationToken);
        await service.GetOrSetAsync<string>(key3, _ => Task.FromResult("v3")!, ["other-tag"], TestContext.Current.CancellationToken);
        service.GetOrSet<string>(key1, _ => "default").ShouldBe("v1");
        service.GetOrSet<string>(key2, _ => "default").ShouldBe("v2");
        service.GetOrSet<string>(key3, _ => "default").ShouldBe("v3");
        await service.InvalidateCacheItemByTag(tag);
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
        await service.GetOrSetAsync<string>(key, _ => Task.FromResult<(string?, string[]?)>(("value", [tag])), token: TestContext.Current.CancellationToken);
        service.GetOrSet<string>(key, _ => "default").ShouldBe("value");
        await service.InvalidateCacheItemByTag(tag);
        service.GetOrSet<string>(key, _ => "default").ShouldBe("default");
    }

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public async Task GetOrSetAsync_WithExtraTags_MergesTags(string implementation)
    {
        var service = CreateCacheService(implementation);
        var extraTag = $"both-extra-{implementation}";
        var key = $"both-extra-key-{implementation}";
        await service.GetOrSetAsync<string>(key, _ => Task.FromResult("value")!, [extraTag], TestContext.Current.CancellationToken);
        service.GetOrSet<string>(key, _ => "default").ShouldBe("value");
        await service.InvalidateCacheItemByTag(extraTag);
        service.GetOrSet<string>(key, _ => "default").ShouldBe("default");
    }

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public void Set_WithAbsoluteDuration_ExpiresWithoutRefresh(string implementation)
    {
        var service = CreateCacheService(implementation);
        var key = $"both-absolute-set-{implementation}-{Guid.NewGuid():N}";
        var ttl = TimeSpan.FromMilliseconds(400);
        service.Set(key, "value", ttl);
        service.TryGetValue<string>(key, out var first).ShouldBeTrue();
        first.ShouldBe("value");
        Thread.Sleep(550);
        service.TryGetValue<string>(key, out _).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public void Set_WithSlidingExpiration_TryGetValueExtendsLifetime(string implementation)
    {
        var service = CreateCacheService(implementation);
        var key = $"both-sliding-set-{implementation}-{Guid.NewGuid():N}";
        var ttl = TimeSpan.FromMilliseconds(500);
        service.Set(key, "value", o => o.SetSlidingExpiration(ttl));
        Thread.Sleep(150);
        service.TryGetValue<string>(key, out var hit).ShouldBeTrue();
        hit.ShouldBe("value");
        Thread.Sleep(400);
        service.TryGetValue<string>(key, out var still).ShouldBeTrue();
        still.ShouldBe("value");
    }

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public void GetOrSet_WithSlidingSetup_HitDoesNotCallFactoryAndExtendsLifetime(string implementation)
    {
        var service = CreateCacheService(implementation);
        var key = $"both-sliding-getorset-{implementation}-{Guid.NewGuid():N}";
        var ttl = TimeSpan.FromMilliseconds(500);
        var factoryCalls = 0;
        service.GetOrSet(key, _ => {
            factoryCalls++;
            return "value";
        }, o => o.SetSlidingExpiration(ttl)).ShouldBe("value");
        factoryCalls.ShouldBe(1);
        Thread.Sleep(150);
        service.GetOrSet(key, _ => {
            factoryCalls++;
            return "other";
        }, o => o.SetSlidingExpiration(ttl)).ShouldBe("value");
        factoryCalls.ShouldBe(1);
        Thread.Sleep(400);
        service.TryGetValue<string>(key, out var still).ShouldBeTrue();
        still.ShouldBe("value");
    }

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public void Set_WithAbsoluteDuration_TryGetValueDoesNotExtendLifetime(string implementation)
    {
        var service = CreateCacheService(implementation);
        var key = $"both-absolute-norefresh-{implementation}-{Guid.NewGuid():N}";
        var ttl = TimeSpan.FromMilliseconds(400);
        service.Set(key, "value", ttl);
        Thread.Sleep(150);
        service.TryGetValue<string>(key, out _).ShouldBeTrue();
        Thread.Sleep(400);
        service.TryGetValue<string>(key, out _).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public async Task Set_WithSlidingExpiration_TagsSurviveRefresh(string implementation)
    {
        var service = CreateCacheService(implementation);
        var key = $"both-sliding-tag-{implementation}-{Guid.NewGuid():N}";
        var tag = $"both-sliding-tag-name-{implementation}-{Guid.NewGuid():N}";
        service.Set(key, "value", o => o.SetSlidingExpiration(TimeSpan.FromMinutes(5)), [tag]);
        service.TryGetValue<string>(key, out var hit).ShouldBeTrue();
        hit.ShouldBe("value");
        await service.InvalidateCacheItemByTag(tag);
        service.TryGetValue<string>(key, out _).ShouldBeFalse();
    }

    [Fact]
    public void Set_WithSlidingExpiration_DisabledCache_TryGetReturnsFalse()
    {
        var disabled = new CacheOptions { Enabled = false };
        var service = new LocalCacheService(_memoryCache, _localLogger, disabled);
        var key = $"both-sliding-disabled-{Guid.NewGuid():N}";
        service.Set(key, "value", o => o.SetSlidingExpiration(TimeSpan.FromHours(1)));
        service.TryGetValue<string>(key, out _).ShouldBeFalse();
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
    public async Task ClearAsync_RemovesAllKeysAndTags(string implementation)
    {
        var service = CreateCacheService(implementation);
        var key1 = $"both-clear-key1-{implementation}-{Guid.NewGuid():N}";
        var key2 = $"both-clear-key2-{implementation}-{Guid.NewGuid():N}";
        var tag = $"both-clear-tag-{implementation}-{Guid.NewGuid():N}";
        service.Set(key1, "v1", [tag]);
        service.Set(key2, "v2", [tag]);
        service.GetOrSet<string>(key1, _ => "default").ShouldBe("v1");
        service.GetOrSet<string>(key2, _ => "default").ShouldBe("v2");
        service.Items.Count.ShouldBeGreaterThan(0);
        await service.ClearAsync();
        service.TryGetValue<string>(key1, out _).ShouldBeFalse();
        service.TryGetValue<string>(key2, out _).ShouldBeFalse();
        service.Items.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ClearAsync_WithDisabledCache_DoesNothing()
    {
        var disabled = new CacheOptions { Enabled = false };
        var service = new LocalCacheService(_memoryCache, _localLogger, disabled);
        service.Set("both-clear-disabled", "value");
        await service.ClearAsync();
        service.Items.Count.ShouldBe(0);
    }

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public async Task GetOrSetAsync_KeyNormalizedToLowercase(string implementation)
    {
        var service = CreateCacheService(implementation);
        var key = $"Both-MixedCase-{implementation}";
        await service.GetOrSetAsync<string>(key, _ => Task.FromResult("value")!, token: TestContext.Current.CancellationToken);
        var result = service.GetOrSet<string>(key.ToLowerInvariant(), _ => "default");
        result.ShouldBe("value");
    }
}