using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;

namespace Lyo.Cache.Fusion;

/// <summary>Internal service locator to capture the service provider when FusionCacheService is resolved.</summary>
internal class ServiceLocator
{
    public IServiceProvider? ServiceProvider { get; set; }
}

/// <summary>Extension methods for configuring FusionCacheService with dependency injection.</summary>
public static class FusionCacheServiceExtensions
{
    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds FusionCacheService and FusionCache. Use overloads with Redis connection to enable distributed cache backplane.</summary>
        /// <param name="configureOptions">Optional action to configure CacheOptions</param>
        /// <param name="configureFusionCache">Optional action to configure FusionCache</param>
        /// <param name="configureRedisBackplane">Optional action to configure Redis backplane. Requires IConnectionMultiplexer (use AddRedisConnection or overload with connection string).</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddFusionCache(
            Action<CacheOptions>? configureOptions = null,
            Action<FusionCacheOptions>? configureFusionCache = null,
            Action<RedisBackplaneOptions>? configureRedisBackplane = null)
            => FusionCacheRegistration.AddFusionCacheInternal(services, configureOptions, configureFusionCache, configureRedisBackplane);

        /// <summary>Adds FusionCacheService with Redis backplane. Registers Redis connection from connection string.</summary>
        /// <param name="redisConnectionString">Redis connection string</param>
        /// <param name="configureOptions">Optional action to configure CacheOptions</param>
        /// <param name="configureFusionCache">Optional action to configure FusionCache</param>
        /// <param name="configureRedisBackplane">Optional action to configure Redis backplane</param>
        /// <param name="configureRedis">Optional action to configure Redis ConfigurationOptions</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddFusionCache(
            string redisConnectionString,
            Action<CacheOptions>? configureOptions = null,
            Action<FusionCacheOptions>? configureFusionCache = null,
            Action<RedisBackplaneOptions>? configureRedisBackplane = null,
            Action<ConfigurationOptions>? configureRedis = null)
        {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(redisConnectionString, nameof(redisConnectionString));
            services.AddRedisConnection(redisConnectionString, configureRedis);
            return FusionCacheRegistration.AddFusionCacheInternal(services, configureOptions, configureFusionCache, configureRedisBackplane ?? (_ => { }));
        }

        /// <summary>Adds FusionCacheService with configuration from IConfiguration. Binds CacheOptions and optionally Redis (when Redis section exists).</summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="configureOptions">Optional action to further configure CacheOptions after binding</param>
        /// <param name="configureFusionCache">Optional action to configure FusionCache</param>
        /// <param name="configureRedisBackplane">Optional action to configure Redis backplane when Redis is used</param>
        /// <param name="configureRedis">
        /// Optional action to further configure Redis ConfigurationOptions (e.g. password from secure source). Redis:Password from config is applied
        /// automatically when present.
        /// </param>
        /// <param name="redisSectionName">Configuration section for Redis (default "Redis"). When present, registers Redis and backplane.</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddFusionCacheFromConfiguration(
            IConfiguration configuration,
            Action<CacheOptions>? configureOptions = null,
            Action<FusionCacheOptions>? configureFusionCache = null,
            Action<RedisBackplaneOptions>? configureRedisBackplane = null,
            Action<ConfigurationOptions>? configureRedis = null,
            string redisSectionName = "Redis")
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
            var cacheOptions = new CacheOptions();
            var section = configuration.GetSection(CacheOptions.SectionName);
            if (section.Exists())
                section.Bind(cacheOptions);

            configureOptions?.Invoke(cacheOptions);
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

        /// <summary>Adds Redis connection (IConnectionMultiplexer) to the service collection.</summary>
        public IServiceCollection AddRedisConnection(string connectionString, Action<ConfigurationOptions>? configureOptions = null)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));
            services.AddSingleton<IConnectionMultiplexer>(_ => {
                var options = ConfigurationOptions.Parse(connectionString);
                options.AbortOnConnectFail = false;
                configureOptions?.Invoke(options);
                return ConnectionMultiplexer.Connect(options);
            });

            return services;
        }

        /// <summary>Adds Redis connection from configuration. Reads ConnectionString and optionally Password from the section. Use User Secrets or env vars for passwords.</summary>
        public IServiceCollection AddRedisConnectionFromConfiguration(
            IConfiguration configuration,
            string sectionName = "Redis",
            Action<ConfigurationOptions>? configureOptions = null)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(sectionName, nameof(sectionName));
            var redisSection = configuration.GetSection(sectionName);
            var connectionString = redisSection["ConnectionString"] ??
                redisSection.Value ?? throw new InvalidOperationException($"Redis connection string not found in configuration section '{sectionName}'");

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