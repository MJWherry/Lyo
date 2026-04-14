using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Lyo.Lock.Redis;

/// <summary>Extension methods for configuring RedisLockService (Redis distributed lock) with dependency injection.</summary>
public static class RedisLockServiceExtensions
{
    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds RedisLockService (Redis-based distributed locking). Requires IConnectionMultiplexer to be registered (e.g. via Lyo.Cache.Fusion.AddRedisConnection).</summary>
        /// <param name="configureOptions">Optional action to configure RedisLockOptions</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddRedisLock(Action<RedisLockOptions>? configureOptions = null)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            var options = new RedisLockOptions();
            configureOptions?.Invoke(options);
            services.AddSingleton(options);
            services.AddSingleton<ILockService>(sp => {
                var redis = sp.GetRequiredService<IConnectionMultiplexer>();
                var logger = sp.GetService<ILogger<RedisLockService>>();
                var opts = sp.GetRequiredService<RedisLockOptions>();
                var metrics = opts.EnableMetrics ? sp.GetService<IMetrics>() : null;
                return new RedisLockService(redis, logger, opts, metrics);
            });

            return services;
        }

        /// <summary>Adds RedisLockService with Redis connection string. Registers IConnectionMultiplexer (if not already present) and the lock service.</summary>
        public IServiceCollection AddRedisLock(string redisConnectionString, Action<RedisLockOptions>? configureOptions = null, Action<ConfigurationOptions>? configureRedis = null)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(redisConnectionString, nameof(redisConnectionString));
            services.TryAddSingleton<IConnectionMultiplexer>(_ => {
                var options = ConfigurationOptions.Parse(redisConnectionString);
                options.AbortOnConnectFail = false;
                configureRedis?.Invoke(options);
                return ConnectionMultiplexer.Connect(options);
            });

            return services.AddRedisLock(configureOptions);
        }

        /// <summary>Adds RedisLockService with configuration from IConfiguration. Registers Redis from the Redis section if not already present.</summary>
        public IServiceCollection AddRedisLockFromConfiguration(IConfiguration configuration, Action<RedisLockOptions>? configureOptions = null, string redisSectionName = "Redis")
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
            var options = new RedisLockOptions();
            var lockSection = configuration.GetSection(LockOptions.SectionName);
            if (lockSection.Exists())
                lockSection.Bind(options);

            configureOptions?.Invoke(options);
            var redisSection = configuration.GetSection(redisSectionName);
            var connectionString = redisSection["ConnectionString"] ?? redisSection.Value ?? "";
            if (!string.IsNullOrWhiteSpace(connectionString)) {
                return services.AddRedisLock(
                    connectionString, o => {
                        o.DefaultAcquireTimeout = options.DefaultAcquireTimeout;
                        o.DefaultLockDuration = options.DefaultLockDuration;
                        o.KeyPrefix = options.KeyPrefix;
                        o.AcquirePollInterval = options.AcquirePollInterval;
                        o.UsePubSubForAcquireWait = options.UsePubSubForAcquireWait;
                        o.EnableMetrics = options.EnableMetrics;
                        o.SkipKeyNormalization = options.SkipKeyNormalization;
                    }, opts => {
                        var password = redisSection["Password"];
                        if (!string.IsNullOrEmpty(password))
                            opts.Password = password;
                    });
            }

            throw new InvalidOperationException(
                $"Redis connection string not found in configuration section '{redisSectionName}'. Add RedisLock with a connection string or ensure Redis is configured.");
        }
    }
}