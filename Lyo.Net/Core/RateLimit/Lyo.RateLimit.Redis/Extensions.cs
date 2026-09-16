using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.RateLimit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace Lyo.RateLimit.Redis;

/// <summary>Registers <see cref="RedisRateLimitService" /> and replaces any in-memory <see cref="IRateLimitService" />.</summary>
public static class RedisRateLimitServiceExtensions
{
    /// <param name="services">Application service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers the Redis store. Requires <see cref="IConnectionMultiplexer" />. Replaces a local registration.</summary>
        /// <param name="configureOptions">Optional callback after defaults.</param>
        public IServiceCollection AddRedisRateLimit(Action<RedisRateLimitOptions>? configureOptions = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            var options = new RedisRateLimitOptions();
            configureOptions?.Invoke(options);
            return services.AddRedisRateLimit(options);
        }

        /// <summary>Registers <paramref name="options" /> and the Redis store, replacing a local registration.</summary>
        /// <param name="options">Validated options.</param>
        public IServiceCollection AddRedisRateLimit(RedisRateLimitOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            ReplaceRateLimitService(services, options);
            return services;
        }

        /// <summary>Registers a multiplexer from <paramref name="redisConnectionString" /> when one is missing, then the Redis store.</summary>
        /// <param name="redisConnectionString">StackExchange.Redis connection string. Empty fails.</param>
        /// <param name="configureOptions">Quota options.</param>
        /// <param name="configureRedis">Optional connection tweaks, such as a password kept out of the string.</param>
        public IServiceCollection AddRedisRateLimit(
            string redisConnectionString,
            Action<RedisRateLimitOptions>? configureOptions = null,
            Action<ConfigurationOptions>? configureRedis = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(redisConnectionString);
            services.TryAddSingleton<IConnectionMultiplexer>(_ => {
                var redis = ConfigurationOptions.Parse(redisConnectionString);
                redis.AbortOnConnectFail = false;
                configureRedis?.Invoke(redis);
                return ConnectionMultiplexer.Connect(redis);
            });
            return services.AddRedisRateLimit(configureOptions);
        }

        /// <summary>
        /// Binds <see cref="RateLimitOptions.SectionName" /> and connects Redis from <paramref name="redisSectionName" />. A missing connection string throws
        /// <see cref="ConfigurationException" />.
        /// </summary>
        /// <param name="configuration">Configuration root.</param>
        /// <param name="configureOptions">Runs after bind.</param>
        /// <param name="redisSectionName">Section with <c>ConnectionString</c> and optional <c>Password</c>.</param>
        public IServiceCollection AddRedisRateLimitFromConfiguration(
            IConfiguration configuration,
            Action<RedisRateLimitOptions>? configureOptions = null,
            string redisSectionName = "Redis")
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new RedisRateLimitOptions();
            configuration.GetSection(RateLimitOptions.SectionName).Bind(options);
            configureOptions?.Invoke(options);
            options.Validate();
            var redisSection = configuration.GetSection(redisSectionName);
            var connectionString = redisSection["ConnectionString"] ?? redisSection.Value ?? "";
            if (string.IsNullOrWhiteSpace(connectionString)) {
                throw new ConfigurationException(
                    $"Redis connection string not found in configuration section '{redisSectionName}'.",
                    redisSectionName);
            }

            return services.AddRedisRateLimit(connectionString, bound => {
                bound.FailOpen = options.FailOpen;
                bound.MaxEntries = options.MaxEntries;
                bound.Clock = options.Clock;
            }, redis => {
                var password = redisSection["Password"];
                if (!string.IsNullOrEmpty(password))
                    redis.Password = password;
            });
        }
    }

    private static void ReplaceRateLimitService(IServiceCollection services, RedisRateLimitOptions options)
    {
        for (var i = services.Count - 1; i >= 0; i--) {
            if (services[i].ServiceType == typeof(IRateLimitService))
                services.RemoveAt(i);
        }

        services.AddSingleton(options);
        services.AddSingleton<IRateLimitService>(sp => new RedisRateLimitService(sp.GetRequiredService<IConnectionMultiplexer>(), options));
    }
}
