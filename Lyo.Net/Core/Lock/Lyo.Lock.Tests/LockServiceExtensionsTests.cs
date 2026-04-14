using Lyo.Lock.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Lyo.Lock.Tests;

public class LockServiceExtensionsTests
{
    [Fact]
    public void AddLocalLock_WithOptions_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalLock(options => {
            options.DefaultAcquireTimeout = TimeSpan.FromSeconds(10);
            options.DefaultLockDuration = TimeSpan.FromSeconds(90);
        });

        var serviceProvider = services.BuildServiceProvider();
        var lockService = serviceProvider.GetService<ILockService>();
        var lockOptions = serviceProvider.GetService<LockOptions>();
        Assert.NotNull(lockService);
        Assert.NotNull(lockOptions);
        Assert.Equal(TimeSpan.FromSeconds(10), lockOptions.DefaultAcquireTimeout);
        Assert.Equal(TimeSpan.FromSeconds(90), lockOptions.DefaultLockDuration);
    }

    [Fact]
    public void AddLocalLock_WithConfiguration_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> {
                    { "LockOptions:DefaultAcquireTimeout", "00:00:15" },
                    { "LockOptions:DefaultLockDuration", "00:02:00" },
                    { "LockOptions:KeyPrefix", "test:lock:" },
                    { "LockOptions:EnableMetrics", "true" }
                })
            .Build();

        services.AddLocalLockFromConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var lockOptions = serviceProvider.GetRequiredService<LockOptions>();
        Assert.Equal(TimeSpan.FromSeconds(15), lockOptions.DefaultAcquireTimeout);
        Assert.Equal(TimeSpan.FromMinutes(2), lockOptions.DefaultLockDuration);
        Assert.Equal("test:lock:", lockOptions.KeyPrefix);
        Assert.True(lockOptions.EnableMetrics);
    }

    [Fact]
    public void AddLocalKeyedSemaphore_WithOptions_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalKeyedSemaphore(options => {
            options.DefaultAcquireTimeout = TimeSpan.FromSeconds(12);
            options.SkipKeyNormalization = true;
            options.EnableMetrics = true;
        });

        var serviceProvider = services.BuildServiceProvider();
        var semaphoreService = serviceProvider.GetService<IKeyedSemaphoreService>();
        var semaphoreOptions = serviceProvider.GetService<KeyedSemaphoreOptions>();
        Assert.NotNull(semaphoreService);
        Assert.NotNull(semaphoreOptions);
        Assert.Equal(TimeSpan.FromSeconds(12), semaphoreOptions.DefaultAcquireTimeout);
        Assert.True(semaphoreOptions.SkipKeyNormalization);
        Assert.True(semaphoreOptions.EnableMetrics);
    }

    [Fact]
    public void AddLocalKeyedSemaphore_WithConfiguration_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> {
                    { "KeyedSemaphoreOptions:DefaultAcquireTimeout", "00:00:07" },
                    { "KeyedSemaphoreOptions:SkipKeyNormalization", "true" },
                    { "KeyedSemaphoreOptions:EnableMetrics", "true" }
                })
            .Build();

        services.AddLocalKeyedSemaphoreFromConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var semaphoreOptions = serviceProvider.GetRequiredService<KeyedSemaphoreOptions>();
        var semaphoreService = serviceProvider.GetRequiredService<IKeyedSemaphoreService>();
        Assert.Equal(TimeSpan.FromSeconds(7), semaphoreOptions.DefaultAcquireTimeout);
        Assert.True(semaphoreOptions.SkipKeyNormalization);
        Assert.True(semaphoreOptions.EnableMetrics);
        Assert.NotNull(semaphoreService);
    }

    [Fact]
    public void AddRedisLock_WithConnectionString_RegistersRedisAndLockService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRedisLock("localhost:6379", options => options.KeyPrefix = "lyo:lock:");
        var lockDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ILockService));
        var redisDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IConnectionMultiplexer));
        Assert.NotNull(lockDescriptor);
        Assert.NotNull(redisDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, redisDescriptor.Lifetime);
    }

    [Fact]
    public void AddRedisLock_WithConfiguration_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> {
                    { "Redis:ConnectionString", "localhost:6379" }, { "LockOptions:DefaultAcquireTimeout", "00:00:20" }, { "LockOptions:AcquirePollInterval", "00:00:00.100" }
                })
            .Build();

        services.AddRedisLockFromConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var lockOptions = serviceProvider.GetRequiredService<RedisLockOptions>();
        Assert.Equal(TimeSpan.FromSeconds(20), lockOptions.DefaultAcquireTimeout);
        Assert.Equal(TimeSpan.FromMilliseconds(100), lockOptions.AcquirePollInterval);
    }

    [Fact]
    public void AddRedisLock_WithConfiguration_ThrowsWhenRedisNotConfigured()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "LockOptions:DefaultAcquireTimeout", "00:00:10" } }).Build();
        Assert.Throws<InvalidOperationException>(() => services.AddRedisLockFromConfiguration(configuration));
    }

    [Fact]
    public void LockOptions_DefaultValues_AreReasonable()
    {
        var options = new LockOptions();
        Assert.Equal(TimeSpan.FromSeconds(30), options.DefaultAcquireTimeout);
        Assert.Equal(TimeSpan.FromSeconds(60), options.DefaultLockDuration);
        Assert.Equal("lyo:lock:", options.KeyPrefix);
        Assert.False(options.EnableMetrics);
    }

    [Fact]
    public void RedisLockOptions_ExtendsLockOptions_WithAcquirePollInterval()
    {
        var options = new RedisLockOptions();
        Assert.Equal(TimeSpan.FromSeconds(30), options.DefaultAcquireTimeout);
        Assert.Equal(TimeSpan.FromMilliseconds(10), options.AcquirePollInterval);
    }

    [Fact]
    public void KeyedSemaphoreOptions_DefaultValues_AreReasonable()
    {
        var options = new KeyedSemaphoreOptions();
        Assert.Equal(TimeSpan.FromSeconds(30), options.DefaultAcquireTimeout);
        Assert.False(options.SkipKeyNormalization);
        Assert.False(options.EnableMetrics);
    }
}