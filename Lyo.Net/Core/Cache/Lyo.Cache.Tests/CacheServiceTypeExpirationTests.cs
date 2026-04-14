using Lyo.Cache.Fusion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;
using static Lyo.Cache.Tests.TestModels;

namespace Lyo.Cache.Tests;

public class CacheServiceTypeExpirationTests : IDisposable
{
    private readonly IFusionCache _fusionCache;
    private readonly ILogger<FusionCacheService> _logger;
    private readonly CacheOptions _options;

    public CacheServiceTypeExpirationTests()
    {
        _logger = new LoggerFactory().CreateLogger<FusionCacheService>();
        _options = new() {
            Enabled = true,
            DefaultExpiration = TimeSpan.FromMinutes(10),
            TypeExpirations = new() {
                { typeof(TestEntity).FullName!, 100 }, // 100 minutes
                { typeof(TestProduct).FullName!, 30 }, // 30 minutes
                { typeof(TestOrder).FullName!, 60 } // 60 minutes
            }
        };

        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddFusionCache().TryWithAutoSetup();
        var serviceProvider = services.BuildServiceProvider();
        _fusionCache = serviceProvider.GetRequiredService<IFusionCache>();
    }

    public void Dispose() => _fusionCache.Dispose();

    [Fact]
    public void CacheOptions_GetExpirationForType_ReturnsConfiguredExpiration()
    {
        var expiration = _options.GetExpirationForType(typeof(TestEntity));
        Assert.Equal(TimeSpan.FromMinutes(100), expiration);
    }

    [Fact]
    public void CacheOptions_GetExpirationForType_ReturnsDefaultForUnconfiguredType()
    {
        var expiration = _options.GetExpirationForType(typeof(TestUnconfiguredType));
        Assert.Equal(TimeSpan.FromMinutes(10), expiration); // Default expiration
    }

    [Fact]
    public void CacheOptions_GetExpirationForType_WithFullTypeName_ReturnsConfiguredExpiration()
    {
        var expiration = _options.GetExpirationForType(typeof(TestProduct).FullName!);
        Assert.Equal(TimeSpan.FromMinutes(30), expiration);
    }

    [Fact]
    public async Task GetOrSetAsync_WithType_UsesTypeSpecificExpiration()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "test-user-key";
        var user = new TestEntity { Id = 1, Name = "Test User" };
        await service.GetOrSetAsync<TestEntity>(key, _ => Task.FromResult(user), typeof(TestEntity), token: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Verify it's cached
        var cached = service.GetOrSet<TestEntity>(key, _ => null, typeof(TestEntity));
        Assert.NotNull(cached);
        Assert.Equal(user.Id, cached.Id);
        Assert.Equal(user.Name, cached.Name);
    }

    [Fact]
    public void GetOrSet_WithType_UsesTypeSpecificExpiration()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "test-product-key";
        var product = new TestProduct { Id = 2, Name = "Test Product", Price = 99.99m };
        service.GetOrSet<TestProduct>(key, _ => product, typeof(TestProduct));

        // Verify it's cached
        var cached = service.GetOrSet<TestProduct>(key, _ => null, typeof(TestProduct));
        Assert.NotNull(cached);
        Assert.Equal(product.Id, cached.Id);
        Assert.Equal(product.Name, cached.Name);
        Assert.Equal(product.Price, cached.Price);
    }

    [Fact]
    public async Task InvalidateCacheByTypeAsync_WithTypeName_RemovesTypeCache()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key1 = "user-1";
        var key2 = "user-2";
        var key3 = "product-1";
        service.Set(key1, new TestEntity { Id = 1 }, [$"type:{typeof(TestEntity).FullName}"]);
        service.Set(key2, new TestEntity { Id = 2 }, [$"type:{typeof(TestEntity).FullName}"]);
        service.Set(key3, new TestProduct { Id = 1 }, [$"type:{typeof(TestProduct).FullName}"]);

        // Verify they're cached
        Assert.NotNull(service.GetOrSet<TestEntity>(key1, _ => null));
        Assert.NotNull(service.GetOrSet<TestEntity>(key2, _ => null));
        Assert.NotNull(service.GetOrSet<TestProduct>(key3, _ => null));

        // Invalidate User type
        await service.InvalidateCacheByTypeAsync(typeof(TestEntity).FullName!).ConfigureAwait(false);

        // Users should be gone, product should remain
        Assert.Null(service.GetOrSet<TestEntity>(key1, _ => null));
        Assert.Null(service.GetOrSet<TestEntity>(key2, _ => null));
        Assert.NotNull(service.GetOrSet<TestProduct>(key3, _ => null));
    }

    [Fact]
    public async Task InvalidateCacheByTypeAsync_WithType_RemovesTypeCache()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "order-1";
        service.Set(key, new TestOrder { Id = 1 }, [$"type:{typeof(TestOrder).FullName}"]);
        Assert.NotNull(service.GetOrSet<TestOrder>(key, _ => null));
        await service.InvalidateCacheByTypeAsync(typeof(TestOrder)).ConfigureAwait(false);
        Assert.Null(service.GetOrSet<TestOrder>(key, _ => null));
    }

    [Fact]
    public async Task InvalidateCacheByTypeAsync_WithGenericType_RemovesTypeCache()
    {
        var service = new FusionCacheService(_fusionCache, _logger, _options);
        var key = "user-generic-1";
        service.Set(key, new TestEntity { Id = 1 }, [$"type:{typeof(TestEntity).FullName}"]);
        Assert.NotNull(service.GetOrSet<TestEntity>(key, _ => null));
        await service.InvalidateCacheByTypeAsync<TestEntity>().ConfigureAwait(false);
        Assert.Null(service.GetOrSet<TestEntity>(key, _ => null));
    }

    [Fact]
    public void CacheOptions_WithTypeExpiration_AddsExpiration()
    {
        var options = new CacheOptions();
        options.WithTypeExpiration("My.Namespace.MyClass", 45);
        var expiration = options.GetExpirationForType("My.Namespace.MyClass");
        Assert.Equal(TimeSpan.FromMinutes(45), expiration);
    }

    [Fact]
    public void CacheOptions_WithTypeExpiration_UsingType_AddsExpiration()
    {
        var options = new CacheOptions();
        options.WithTypeExpiration(typeof(TestEntity), 120);
        var expiration = options.GetExpirationForType(typeof(TestEntity));
        Assert.Equal(TimeSpan.FromMinutes(120), expiration);
    }

    [Fact]
    public void CacheOptions_WithTypeExpirations_AddsMultipleExpirations()
    {
        var options = new CacheOptions();
        var expirations = new Dictionary<string, int> { { "Type1", 10 }, { "Type2", 20 }, { "Type3", 30 } };
        options.WithTypeExpirations(expirations);
        Assert.Equal(TimeSpan.FromMinutes(10), options.GetExpirationForType("Type1"));
        Assert.Equal(TimeSpan.FromMinutes(20), options.GetExpirationForType("Type2"));
        Assert.Equal(TimeSpan.FromMinutes(30), options.GetExpirationForType("Type3"));
    }

    [Fact]
    public void CacheOptions_GetExpirationForType_WithWildcardPattern_MatchesNamespace()
    {
        var options = new CacheOptions {
            DefaultExpiration = TimeSpan.FromMinutes(10),
            TypeExpirations = new() {
                { "My.Lib.*", 60 } // 60 minutes for all types in My.Lib namespace
            }
        };

        // Should match types in the namespace
        Assert.Equal(TimeSpan.FromMinutes(60), options.GetExpirationForType("My.Lib.Class1"));
        Assert.Equal(TimeSpan.FromMinutes(60), options.GetExpirationForType("My.Lib.Class2"));
        Assert.Equal(TimeSpan.FromMinutes(60), options.GetExpirationForType("My.Lib.SubNamespace.Class3"));

        // Should not match types outside the namespace
        Assert.Equal(TimeSpan.FromMinutes(10), options.GetExpirationForType("My.OtherLib.Class"));
        Assert.Equal(TimeSpan.FromMinutes(10), options.GetExpirationForType("Other.Lib.Class"));
    }

    [Fact]
    public void CacheOptions_GetExpirationForType_WithWildcardPattern_ExactMatchTakesPrecedence()
    {
        var options = new CacheOptions {
            DefaultExpiration = TimeSpan.FromMinutes(10),
            TypeExpirations = new() {
                { "My.Lib.*", 60 }, // Wildcard: 60 minutes
                { "My.Lib.SpecificClass", 120 } // Exact: 120 minutes
            }
        };

        // Exact match should take precedence over wildcard
        Assert.Equal(TimeSpan.FromMinutes(120), options.GetExpirationForType("My.Lib.SpecificClass"));

        // Other types in namespace should use wildcard
        Assert.Equal(TimeSpan.FromMinutes(60), options.GetExpirationForType("My.Lib.OtherClass"));
    }

    [Fact]
    public void CacheOptions_GetExpirationForType_WithWildcardPattern_MoreSpecificPatternWins()
    {
        var options = new CacheOptions {
            DefaultExpiration = TimeSpan.FromMinutes(10),
            TypeExpirations = new() {
                { "My.*", 30 }, // Less specific: 30 minutes
                { "My.Lib.*", 60 }, // More specific: 60 minutes
                { "My.Lib.Services.*", 90 } // Most specific: 90 minutes
            }
        };

        // Most specific pattern should win
        Assert.Equal(TimeSpan.FromMinutes(90), options.GetExpirationForType("My.Lib.Services.Service1"));
        Assert.Equal(TimeSpan.FromMinutes(90), options.GetExpirationForType("My.Lib.Services.Service2"));

        // Less specific pattern for other Lib types
        Assert.Equal(TimeSpan.FromMinutes(60), options.GetExpirationForType("My.Lib.Models.Model1"));

        // Least specific pattern for other My types
        Assert.Equal(TimeSpan.FromMinutes(30), options.GetExpirationForType("My.OtherLib.Class"));
    }

    [Fact]
    public void CacheOptions_GetExpirationForType_WithQuestionMarkWildcard_MatchesSingleCharacter()
    {
        var options = new CacheOptions {
            DefaultExpiration = TimeSpan.FromMinutes(10),
            TypeExpirations = new() {
                { "My.Lib.Class?", 45 } // Matches Class1, Class2, etc.
            }
        };

        Assert.Equal(TimeSpan.FromMinutes(45), options.GetExpirationForType("My.Lib.Class1"));
        Assert.Equal(TimeSpan.FromMinutes(45), options.GetExpirationForType("My.Lib.ClassA"));
        Assert.Equal(TimeSpan.FromMinutes(10), options.GetExpirationForType("My.Lib.Class12")); // Too many characters
    }

    [Fact]
    public void CacheOptions_GetExpirationForType_WithWildcardPattern_CaseInsensitive()
    {
        var options = new CacheOptions { DefaultExpiration = TimeSpan.FromMinutes(10), TypeExpirations = new() { { "My.Lib.*", 60 } } };

        // Should be case-insensitive
        Assert.Equal(TimeSpan.FromMinutes(60), options.GetExpirationForType("my.lib.class"));
        Assert.Equal(TimeSpan.FromMinutes(60), options.GetExpirationForType("MY.LIB.CLASS"));
        Assert.Equal(TimeSpan.FromMinutes(60), options.GetExpirationForType("My.Lib.Class"));
    }

    [Fact]
    public void CacheOptions_GetExpirationForType_WithEmptyOrNull_ReturnsDefault()
    {
        var options = new CacheOptions { DefaultExpiration = TimeSpan.FromMinutes(10), TypeExpirations = new() { { "My.Lib.*", 60 } } };
        Assert.Equal(TimeSpan.FromMinutes(10), options.GetExpirationForType(""));
        Assert.Equal(TimeSpan.FromMinutes(10), options.GetExpirationForType("   "));
    }

    [Fact]
    public async Task GetOrSetAsync_WithWildcardPattern_UsesPatternExpiration()
    {
        var options = new CacheOptions {
            Enabled = true,
            DefaultExpiration = TimeSpan.FromMinutes(10),
            TypeExpirations = new() {
                { "Lyo.Cache.Tests.*", 120 } // 120 minutes for all test types
            }
        };

        var service = new FusionCacheService(_fusionCache, _logger, options);
        var key = "wildcard-test";
        var entity = new TestEntity { Id = 1, Name = "Test" };
        await service.GetOrSetAsync<TestEntity>(key, _ => Task.FromResult(entity), typeof(TestEntity), token: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Verify it's cached (the expiration should be 120 minutes from the wildcard pattern)
        var cached = service.GetOrSet<TestEntity>(key, _ => null, typeof(TestEntity));
        Assert.NotNull(cached);
        Assert.Equal(entity.Id, cached.Id);
    }
}