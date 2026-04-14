using System.Linq;
using Lyo.Compression;
using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Lyo.Cache;

/// <summary>Extension methods for configuring LocalCacheService with dependency injection.</summary>
public static class CacheServiceExtensions
{
    private static void RegisterCachePayloadCodec(IServiceCollection services)
    {
        services.AddSingleton<ICachePayloadCodec>(sp =>
#if NET10_0_OR_GREATER
            new CachePayloadCodec(
                sp.GetRequiredService<CacheOptions>(),
                sp.GetRequiredService<ICompressionService>(),
                sp.GetService<Lyo.Encryption.IEncryptionService>())
#else
            new CachePayloadCodec(
                sp.GetRequiredService<CacheOptions>(),
                sp.GetRequiredService<ICompressionService>())
#endif
        );
    }

    private static void RegisterCachePayloadSerializer(IServiceCollection services)
        => services.TryAddSingleton<ICachePayloadSerializer>(CachePayloadSerializerRegistration.Create);

    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds LocalCacheService using IMemoryCache (local-only, no FusionCache/Redis).</summary>
        /// <param name="configureOptions">Optional action to configure CacheOptions</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddLocalCache(Action<CacheOptions>? configureOptions = null)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            var cacheOptions = new CacheOptions();
            configureOptions?.Invoke(cacheOptions);
            services.AddSingleton(cacheOptions);
            services.AddMemoryCache();
            if (!services.Any(static d => d.ServiceType == typeof(ICompressionService)))
                services.AddCompressionService();

            RegisterCachePayloadCodec(services);
            RegisterCachePayloadSerializer(services);
            services.AddSingleton<ICacheService>(serviceProvider => {
                var logger = serviceProvider.GetService<ILogger<LocalCacheService>>();
                var options = serviceProvider.GetRequiredService<CacheOptions>();
                var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
                var metrics = options.EnableMetrics ? serviceProvider.GetService<IMetrics>() : null;
                var payloadCodec = serviceProvider.GetRequiredService<ICachePayloadCodec>();
                var payloadSerializer = serviceProvider.GetRequiredService<ICachePayloadSerializer>();
                return new LocalCacheService(memoryCache, logger, options, metrics, payloadCodec, payloadSerializer);
            });

            return services;
        }

        /// <summary>Adds LocalCacheService with configuration from IConfiguration.</summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="configureOptions">Optional action to further configure CacheOptions after binding from configuration</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddLocalCacheFromConfiguration(IConfiguration configuration, Action<CacheOptions>? configureOptions = null)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
            var cacheOptions = new CacheOptions();
            var section = configuration.GetSection(CacheOptions.SectionName);
            if (section.Exists())
                section.Bind(cacheOptions);

            configureOptions?.Invoke(cacheOptions);
            services.AddSingleton(cacheOptions);
            services.AddMemoryCache();
            if (!services.Any(static d => d.ServiceType == typeof(ICompressionService)))
                services.AddCompressionService();

            RegisterCachePayloadCodec(services);
            RegisterCachePayloadSerializer(services);
            services.AddSingleton<ICacheService>(serviceProvider => {
                var logger = serviceProvider.GetService<ILogger<LocalCacheService>>();
                var options = serviceProvider.GetRequiredService<CacheOptions>();
                var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
                var metrics = options.EnableMetrics ? serviceProvider.GetService<IMetrics>() : null;
                var payloadCodec = serviceProvider.GetRequiredService<ICachePayloadCodec>();
                var payloadSerializer = serviceProvider.GetRequiredService<ICachePayloadSerializer>();
                return new LocalCacheService(memoryCache, logger, options, metrics, payloadCodec, payloadSerializer);
            });

            return services;
        }
    }

    /// <param name="options">The CacheOptions instance</param>
    extension(CacheOptions options)
    {
        /// <summary>Configures type-specific cache expiration timeouts.</summary>
        /// <param name="typeExpirations">Dictionary where key is full type name and value is expiration in minutes</param>
        /// <returns>The CacheOptions instance for chaining</returns>
        public CacheOptions WithTypeExpirations(Dictionary<string, int> typeExpirations)
        {
            foreach (var kvp in typeExpirations)
                options.TypeExpirations[kvp.Key] = kvp.Value;

            return options;
        }

        /// <summary>Configures cache expiration for a specific type.</summary>
        /// <param name="fullTypeName">Full type name (e.g., "My.Lib.Class")</param>
        /// <param name="expirationMinutes">Expiration time in minutes</param>
        /// <returns>The CacheOptions instance for chaining</returns>
        public CacheOptions WithTypeExpiration(string fullTypeName, int expirationMinutes)
        {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(fullTypeName, nameof(fullTypeName));
            ArgumentHelpers.ThrowIfNegative(expirationMinutes, nameof(expirationMinutes));
            options.TypeExpirations[fullTypeName] = expirationMinutes;
            return options;
        }

        /// <summary>Configures cache expiration for a specific type.</summary>
        /// <param name="type">The type to configure expiration for</param>
        /// <param name="expirationMinutes">Expiration time in minutes</param>
        /// <returns>The CacheOptions instance for chaining</returns>
        public CacheOptions WithTypeExpiration(Type type, int expirationMinutes) => options.WithTypeExpiration(type.FullName ?? type.Name, expirationMinutes);
    }
}

/// <summary>Extension methods for ICacheEntryOptions.</summary>
public static class CacheEntryOptionsExtensions
{
    /// <summary>Sets the cache entry duration.</summary>
    /// <returns>The options instance for chaining</returns>
    public static ICacheEntryOptions SetDuration(this ICacheEntryOptions options, TimeSpan duration)
    {
        options.Duration = duration;
        return options;
    }
}