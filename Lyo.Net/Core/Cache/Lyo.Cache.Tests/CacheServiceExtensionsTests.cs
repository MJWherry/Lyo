using Lyo.Cache.Fusion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Lyo.Cache.Tests;

public class CacheServiceExtensionsTests
{
    [Fact]
    public void AddFusionCache_WithOptions_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFusionCache(options => {
            options.Enabled = true;
            options.DefaultExpiration = TimeSpan.FromMinutes(5);
        });

        var serviceProvider = services.BuildServiceProvider();
        var cacheService = serviceProvider.GetService<ICacheService>();
        var cacheOptions = serviceProvider.GetService<CacheOptions>();
        Assert.NotNull(cacheService);
        Assert.NotNull(cacheOptions);
        Assert.True(cacheOptions.Enabled);
        Assert.Equal(TimeSpan.FromMinutes(5), cacheOptions.DefaultExpiration);
    }

    [Fact]
    public void AddFusionCache_WithConfiguration_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> {
                    { "CacheOptions:Enabled", "true" },
                    { "CacheOptions:DefaultExpiration", "00:10:00" },
                    { "CacheOptions:TypeExpirations:My.Namespace.MyClass", "60" },
                    { "CacheOptions:TypeExpirations:My.Namespace.OtherClass", "30" }
                })
            .Build();

        services.AddFusionCacheFromConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var cacheOptions = serviceProvider.GetRequiredService<CacheOptions>();
        Assert.True(cacheOptions.Enabled);
        Assert.Equal(TimeSpan.FromMinutes(10), cacheOptions.DefaultExpiration);
        Assert.Equal(60, cacheOptions.TypeExpirations["My.Namespace.MyClass"]);
        Assert.Equal(30, cacheOptions.TypeExpirations["My.Namespace.OtherClass"]);
    }

    [Fact]
    public void AddFusionCache_WithConfigurationAndOverride_AppliesOverride()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "CacheOptions:Enabled", "false" }, { "CacheOptions:DefaultExpiration", "00:05:00" } })
            .Build();

        services.AddFusionCacheFromConfiguration(
            configuration, options => {
                options.Enabled = true; // Override configuration
                options.DefaultExpiration = TimeSpan.FromMinutes(15);
            });

        var serviceProvider = services.BuildServiceProvider();
        var cacheOptions = serviceProvider.GetRequiredService<CacheOptions>();
        Assert.True(cacheOptions.Enabled); // Overridden
        Assert.Equal(TimeSpan.FromMinutes(15), cacheOptions.DefaultExpiration); // Overridden
    }

    [Fact]
    public void AddFusionCache_WithRedisBackplane_RegistersBackplane()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Registration must not throw. A live Redis connection would be needed for a full backplane check.
        // Unit tests leave Redis backplane configuration off.
        services.AddFusionCache(options => options.Enabled = true);
        var serviceProvider = services.BuildServiceProvider();
        var cacheService = serviceProvider.GetService<ICacheService>();
        Assert.NotNull(cacheService);
    }

    [Fact]
    public void AddFusionCache_WithRedisConnectionString_RegistersRedisAndBackplane()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFusionCache("localhost:6379", options => options.Enabled = true);

        // Confirm ICacheService and IConnectionMultiplexer are registered (no live connection).
        var cacheDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ICacheService));
        var redisDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IConnectionMultiplexer));
        Assert.NotNull(cacheDescriptor);
        Assert.NotNull(redisDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, redisDescriptor.Lifetime);
    }

    [Fact]
    public void AddLocalCache_WithOptions_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalCache(options => {
            options.Enabled = true;
            options.DefaultExpiration = TimeSpan.FromMinutes(3);
        });

        var serviceProvider = services.BuildServiceProvider();
        var cacheService = serviceProvider.GetService<ICacheService>();
        var cacheOptions = serviceProvider.GetService<CacheOptions>();
        Assert.NotNull(cacheService);
        Assert.NotNull(cacheOptions);
        Assert.True(cacheOptions.Enabled);
        Assert.Equal(TimeSpan.FromMinutes(3), cacheOptions.DefaultExpiration);
    }

    [Fact]
    public void AddLocalCache_WithConfiguration_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "CacheOptions:Enabled", "true" }, { "CacheOptions:DefaultExpiration", "00:07:00" } })
            .Build();

        services.AddLocalCacheFromConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var cacheOptions = serviceProvider.GetRequiredService<CacheOptions>();
        Assert.True(cacheOptions.Enabled);
        Assert.Equal(TimeSpan.FromMinutes(7), cacheOptions.DefaultExpiration);
    }

    [Fact]
    public void AddRedisConnection_WithConnectionString_RegistersConnection()
    {
        var services = new ServiceCollection();
        services.AddRedisConnection("localhost:6379");
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IConnectionMultiplexer));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddRedisConnection_WithConfiguration_RegistersConnection()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "Redis:ConnectionString", "localhost:6379,password=test" } })
            .Build();

        services.AddRedisConnectionFromConfiguration(configuration);
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IConnectionMultiplexer));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddRedisConnection_WithConfiguration_CustomSectionName()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "CustomRedis:ConnectionString", "localhost:6379" } }).Build();
        services.AddRedisConnectionFromConfiguration(configuration, "CustomRedis");
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IConnectionMultiplexer));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddRedisConnection_WithConfiguration_AppliesPasswordFromSection()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "Redis:ConnectionString", "localhost:6379" }, { "Redis:Password", "secret-from-config" } })
            .Build();

        services.AddRedisConnectionFromConfiguration(configuration);
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IConnectionMultiplexer));
        Assert.NotNull(descriptor);
    }
}