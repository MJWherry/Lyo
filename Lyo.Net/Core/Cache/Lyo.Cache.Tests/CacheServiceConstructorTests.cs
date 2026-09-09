using Lyo.Cache.Fusion;
using Lyo.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;

namespace Lyo.Cache.Tests;

public class CacheServiceConstructorTests
{
    private readonly ILogger<FusionCacheService> _logger;

    public CacheServiceConstructorTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _logger = loggerFactory.CreateLogger<FusionCacheService>();
    }

    [Fact]
    public void FusionCacheService_WithNullFusionCache_ThrowsArgumentNullException()
    {
        var options = new CacheOptions { Enabled = true };
        // ReSharper disable once ObjectCreationAsStatement
        var exception = ExceptionAssertions.Throws<ArgumentNullException>(() => new FusionCacheService(null!, _logger, options));
        exception.ParamName.ShouldBe("fusionCache");
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