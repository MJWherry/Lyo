using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;

namespace Lyo.Cache.Fusion;

/// <summary>Holds the service provider once <see cref="FusionCacheService" /> is resolved.</summary>
internal class ServiceLocator
{
    public IServiceProvider? ServiceProvider { get; set; }
}

/// <summary>DI helpers that register <see cref="FusionCacheService" />.</summary>
public static class FusionCacheServiceExtensions
{
    /// <param name="services">Collection to add services to</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="FusionCacheService" /> and FusionCache. Use the Redis overloads to turn on the distributed backplane.</summary>
        /// <param name="configureOptions">Optional callback that fills <see cref="CacheOptions" /></param>
        /// <param name="configureFusionCache">Optional callback that fills FusionCache options</param>
        /// <param name="configureRedisBackplane">Optional Redis backplane callback. Needs <c>IConnectionMultiplexer</c> (use <c>AddRedisConnection</c> or the connection-string overload).</param>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddFusionCache(
            Action<CacheOptions>? configureOptions = null,
            Action<FusionCacheOptions>? configureFusionCache = null,
            Action<RedisBackplaneOptions>? configureRedisBackplane = null)
            => FusionCacheRegistration.AddFusionCacheInternal(services, configureOptions, configureFusionCache, configureRedisBackplane);

        /// <summary>Registers <see cref="FusionCacheService" /> with a Redis backplane. Registers the Redis connection from the connection string.</summary>
        /// <param name="redisConnectionString">Redis connection string</param>
        /// <param name="configureOptions">Optional callback that fills <see cref="CacheOptions" /></param>
        /// <param name="configureFusionCache">Optional callback that fills FusionCache options</param>
        /// <param name="configureRedisBackplane">Optional Redis backplane callback</param>
        /// <param name="configureRedis">Optional callback that fills Redis <c>ConfigurationOptions</c></param>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddFusionCache(
            string redisConnectionString,
            Action<CacheOptions>? configureOptions = null,
            Action<FusionCacheOptions>? configureFusionCache = null,
            Action<RedisBackplaneOptions>? configureRedisBackplane = null,
            Action<ConfigurationOptions>? configureRedis = null)
        {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(redisConnectionString);
            services.AddRedisConnection(redisConnectionString, configureRedis);
            return FusionCacheRegistration.AddFusionCacheInternal(services, configureOptions, configureFusionCache, configureRedisBackplane ?? (_ => { }));
        }

        /// <summary>Registers <see cref="FusionCacheService" /> from <see cref="IConfiguration" />. Binds <see cref="CacheOptions" /> and Redis when that section exists.</summary>
        /// <param name="configuration">Configuration root</param>
        /// <param name="configureOptions">Optional callback that further fills <see cref="CacheOptions" /> after binding</param>
        /// <param name="configureFusionCache">Optional callback that fills FusionCache options</param>
        /// <param name="configureRedisBackplane">Optional Redis backplane callback when Redis is in use</param>
        /// <param name="configureRedis">
        /// Optional callback that further fills Redis <c>ConfigurationOptions</c> (password from a secret store, for example). <c>Redis:Password</c> from config is
        /// applied automatically when present.
        /// </param>
        /// <param name="redisSectionName">Redis configuration section (default "Redis"). When present, Redis and the backplane are registered.</param>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddFusionCacheFromConfiguration(
            IConfiguration configuration,
            Action<CacheOptions>? configureOptions = null,
            Action<FusionCacheOptions>? configureFusionCache = null,
            Action<RedisBackplaneOptions>? configureRedisBackplane = null,
            Action<ConfigurationOptions>? configureRedis = null,
            string redisSectionName = "Redis")
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            var cacheOptions = new CacheOptions();
            configuration.GetSection(CacheOptions.SectionName).Bind(cacheOptions);
            configureOptions?.Invoke(cacheOptions);
            cacheOptions.Validate();
            var redisSection = configuration.GetSection(redisSectionName);
            var connectionString = redisSection["ConnectionString"] ?? redisSection.Value;
            if (!string.IsNullOrWhiteSpace(connectionString)) {
                services.AddRedisConnectionFromConfiguration(configuration, redisSectionName, configureRedis);
                return FusionCacheRegistration.AddFusionCacheInternal(
                    services, options => {
                        options.Enabled = cacheOptions.Enabled;
                        options.DefaultExpiration = cacheOptions.DefaultExpiration;
                        options.PropertyInfoExpiration = cacheOptions.PropertyInfoExpiration;
                        options.TypeMetadataExpiration = cacheOptions.TypeMetadataExpiration;
                        options.PropertyGetterExpiration = cacheOptions.PropertyGetterExpiration;
                        options.ComparisonInfoExpiration = cacheOptions.ComparisonInfoExpiration;
                        options.TypeExpirations = cacheOptions.TypeExpirations;
                        options.Payload = cacheOptions.Payload;
                    }, configureFusionCache, configureRedisBackplane ?? (_ => { }));
            }

            return FusionCacheRegistration.AddFusionCacheInternal(
                services, options => {
                    options.Enabled = cacheOptions.Enabled;
                    options.DefaultExpiration = cacheOptions.DefaultExpiration;
                    options.PropertyInfoExpiration = cacheOptions.PropertyInfoExpiration;
                    options.TypeMetadataExpiration = cacheOptions.TypeMetadataExpiration;
                    options.PropertyGetterExpiration = cacheOptions.PropertyGetterExpiration;
                    options.ComparisonInfoExpiration = cacheOptions.ComparisonInfoExpiration;
                    options.TypeExpirations = cacheOptions.TypeExpirations;
                    options.Payload = cacheOptions.Payload;
                }, configureFusionCache, null);
        }

        /// <summary>Registers an <c>IConnectionMultiplexer</c> on the collection.</summary>
        public IServiceCollection AddRedisConnection(string connectionString, Action<ConfigurationOptions>? configureOptions = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
            services.AddSingleton<IConnectionMultiplexer>(_ => {
                var options = ConfigurationOptions.Parse(connectionString);
                options.AbortOnConnectFail = false;
                configureOptions?.Invoke(options);
                return ConnectionMultiplexer.Connect(options);
            });

            return services;
        }

        /// <summary>Registers Redis from configuration. Reads ConnectionString and optional Password from the section. Prefer User Secrets or env vars for passwords.</summary>
        public IServiceCollection AddRedisConnectionFromConfiguration(
            IConfiguration configuration,
            string sectionName = "Redis",
            Action<ConfigurationOptions>? configureOptions = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(sectionName);
            var redisSection = configuration.GetSection(sectionName);
            var connectionString = redisSection.GetValue<string>("ConnectionString")
                .Or(redisSection.Value)
                .OrThrowInvalidOperation($"Redis connection string not found in configuration section '{sectionName}'");

            return services.AddRedisConnection(
                connectionString, opts => {
                    var password = redisSection["Password"];
                    if (!string.IsNullOrEmpty(password))
                        opts.Password = password;

                    configureOptions?.Invoke(opts);
                });
        }
    }
}