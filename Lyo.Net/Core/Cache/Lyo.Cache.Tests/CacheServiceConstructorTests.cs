using Lyo.Cache.Fusion;
using Lyo.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;

namespace Lyo.Cache.Tests;

public class CacheServiceConstructorTests
{
    private readonly ILogger<LocalCacheService> _localLogger;
    private readonly ILogger<FusionCacheService> _logger;
    private readonly IMemoryCache _memoryCache;

    public CacheServiceConstructorTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _logger = loggerFactory.CreateLogger<FusionCacheService>();
        _localLogger = loggerFactory.CreateLogger<LocalCacheService>();
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var sp = services.BuildServiceProvider();
        _memoryCache = sp.GetRequiredService<IMemoryCache>();
    }

    [Fact]
    public void FusionCacheService_WithNullFusionCache_ThrowsArgumentNullException()
    {
        var options = new CacheOptions { Enabled = true };
        var exception = ExceptionAssertions.Throws<ArgumentNullException>(() => new FusionCacheService(null!, _logger, options));
        exception.ParamName.ShouldBe("fusionCache");
    }

    [Fact]
    public void LocalCacheService_WithDisabledOptions_CreatesService()
    {
        var options = new CacheOptions { Enabled = false };
        var service = new LocalCacheService(_memoryCache, _localLogger, options);
        service.ShouldNotBeNull();
    }

    [Fact]
    public void FusionCacheService_WithEnabledOptions_CreatesService()
    {
        var options = new CacheOptions { Enabled = true };
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddFusionCache().TryWithAutoSetup();
        var serviceProvider = services.BuildServiceProvider();
        var fusionCache = serviceProvider.GetRequiredService<IFusionCache>();
        var service = new FusionCacheService(fusionCache, _logger, options);
        service.ShouldNotBeNull();
    }

    [Fact]
    public void FusionCacheService_WithDisabledOptions_CreatesService()
    {
        var options = new CacheOptions { Enabled = false };
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddFusionCache().TryWithAutoSetup();
        var serviceProvider = services.BuildServiceProvider();
        var fusionCache = serviceProvider.GetRequiredService<IFusionCache>();
        var service = new FusionCacheService(fusionCache, _logger, options);
        service.ShouldNotBeNull();
    }

    [Fact]
    public void FusionCacheService_ConfiguresFailsafeOptions_WhenEnabled()
    {
        var options = new CacheOptions { Enabled = true };
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddFusionCache().TryWithAutoSetup();
        var serviceProvider = services.BuildServiceProvider();
        var fusionCache = serviceProvider.GetRequiredService<IFusionCache>();
        var service = new FusionCacheService(fusionCache, _logger, options);
        service.ShouldNotBeNull();
        service.Items.ShouldNotBeNull();
    }
}