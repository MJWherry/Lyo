using Lyo.Exceptions;
using Lyo.Lock;
using Lyo.Lock.Abstractions;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Lyo.Lock.Redis;

/// <summary>Registers <see cref="RedisLockService"/> as <see cref="ILockService"/> with Redis connectivity.</summary>
public static class RedisLockServiceExtensions
{
    /// <param name="services">Application service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="RedisLockOptions"/> and <see cref="RedisLockService"/> as singletons.
        /// Requires <see cref="IConnectionMultiplexer"/> unless you use the overload that accepts a Redis connection string.
        /// </summary>
        /// <param name="configureOptions">Optional lock/redis behavior tuning.</param>
        public IServiceCollection AddRedisLock(Action<RedisLockOptions>? configureOptions = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
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

        /// <summary>
        /// Ensures a singleton <see cref="IConnectionMultiplexer"/> from <paramref name="redisConnectionString"/> (uses <c>TryAddSingleton</c>),
        /// then registers <see cref="RedisLockService"/>.
        /// </summary>
        /// <param name="redisConnectionString">StackExchange.Redis connection string.</param>
        /// <param name="configureOptions">Lock options (TTL, prefix, pub/sub, metrics).</param>
        /// <param name="configureRedis">Optional low-level <see cref="ConfigurationOptions"/> tweaks (e.g. password not in the connection string).</param>
        public IServiceCollection AddRedisLock(string redisConnectionString, Action<RedisLockOptions>? configureOptions = null, Action<ConfigurationOptions>? configureRedis = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(redisConnectionString);
            services.TryAddSingleton<IConnectionMultiplexer>(_ => {
                var options = ConfigurationOptions.Parse(redisConnectionString);
                options.AbortOnConnectFail = false;
                configureRedis?.Invoke(options);
                return ConnectionMultiplexer.Connect(options);
            });

            return services.AddRedisLock(configureOptions);
        }

        /// <summary>
        /// Reads <see cref="LockOptions.SectionName"/> into <see cref="RedisLockOptions"/> and connects Redis from <paramref name="redisSectionName"/> (<c>ConnectionString</c>, optional <c>Password</c>).
        /// </summary>
        /// <param name="configuration">Typically application configuration root.</param>
        /// <param name="configureOptions">Applied after binding lock options from configuration.</param>
        /// <param name="redisSectionName">Section containing Redis connection settings.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="InvalidOperationException">No Redis connection string resolved from configuration.</exception>
        public IServiceCollection AddRedisLockFromConfiguration(IConfiguration configuration, Action<RedisLockOptions>? configureOptions = null, string redisSectionName = "Redis")
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
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