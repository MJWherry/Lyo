using Lyo.Compression;
using Lyo.Encryption;
using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Lyo.Cache;

/// <summary>DI helpers that register <see cref="LocalCacheService" />.</summary>
public static class CacheServiceExtensions
{
    private static void EnsureCompressionRegistered(IServiceCollection services)
    {
        if (!services.Any(static d => d.ServiceType == typeof(CompressionService)))
            services.AddCompressionService();

        if (!services.Any(static d => d.ServiceType == typeof(ICompressionService)))
            services.AddDefaultCompressionService<CompressionService>();
    }

    private static void RegisterCachePayloadCodec(IServiceCollection services)
        => services.AddSingleton<ICachePayloadCodec>(sp => new CachePayloadCodec(
            sp.GetRequiredService<CacheOptions>(), sp.GetRequiredService<ICompressionService>(), sp.GetService<IEncryptionService>()));

    private static void RegisterCachePayloadSerializer(IServiceCollection services) => services.TryAddSingleton(CachePayloadSerializerRegistration.Create);

    /// <summary>
    /// Registers the backing memory cache with <see cref="MemoryCacheOptions.SizeLimit" /> taken from <see cref="CacheOptions.MaxSizeBytes" />. Without a limit the cache only
    /// evicts on expiry, so a busy query cache grows until the host runs out of memory.
    /// </summary>
    private static void RegisterMemoryCache(IServiceCollection services, CacheOptions cacheOptions)
        => services.AddMemoryCache(memoryOptions => {
            if (cacheOptions.MaxSizeBytes is { } limit)
                memoryOptions.SizeLimit = limit;
        });

    /// <param name="services">Collection to register into.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="LocalCacheService" /> on <c>IMemoryCache</c> (local-only; no FusionCache/Redis).</summary>
        /// <param name="configureOptions">Optional callback that fills <see cref="CacheOptions" />.</param>
        /// <returns>The same collection, for chaining.</returns>
        public IServiceCollection AddLocalCache(Action<CacheOptions>? configureOptions = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            var cacheOptions = new CacheOptions();
            configureOptions?.Invoke(cacheOptions);
            cacheOptions.Validate();
            services.AddSingleton(cacheOptions);
            RegisterMemoryCache(services, cacheOptions);
            EnsureCompressionRegistered(services);
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

        /// <summary>Registers <see cref="LocalCacheService" /> after binding <see cref="CacheOptions" /> from configuration.</summary>
        /// <param name="configuration">Host configuration.</param>
        /// <param name="configureOptions">Optional callback applied after binding.</param>
        /// <returns>The same collection, for chaining.</returns>
        public IServiceCollection AddLocalCacheFromConfiguration(IConfiguration configuration, Action<CacheOptions>? configureOptions = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            var cacheOptions = new CacheOptions();
            configuration.GetSection(CacheOptions.SectionName).Bind(cacheOptions);
            configureOptions?.Invoke(cacheOptions);
            cacheOptions.Validate();
            services.AddSingleton(cacheOptions);
            RegisterMemoryCache(services, cacheOptions);
            EnsureCompressionRegistered(services);
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

    /// <param name="options">Options being mutated.</param>
    extension(CacheOptions options)
    {
        /// <summary>Merges per-type TTL minutes into <see cref="CacheOptions.TypeExpirations" />.</summary>
        /// <param name="typeExpirations">Map of full type name to minutes.</param>
        /// <returns>The same options instance, for chaining.</returns>
        public CacheOptions WithTypeExpirations(Dictionary<string, int> typeExpirations)
        {
            foreach (var kvp in typeExpirations)
                options.TypeExpirations[kvp.Key] = kvp.Value;

            return options;
        }

        /// <summary>Sets the TTL minutes for one type name.</summary>
        /// <param name="fullTypeName">Full type name (for example "My.Lib.Class").</param>
        /// <param name="expirationMinutes">TTL in minutes.</param>
        /// <returns>The same options instance, for chaining.</returns>
        public CacheOptions WithTypeExpiration(string fullTypeName, int expirationMinutes)
        {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(fullTypeName);
            ArgumentHelpers.ThrowIfNegative(expirationMinutes);
            options.TypeExpirations[fullTypeName] = expirationMinutes;
            return options;
        }

        /// <summary>Sets the TTL minutes for one CLR type.</summary>
        /// <param name="type">Type whose full name is stored.</param>
        /// <param name="expirationMinutes">TTL in minutes.</param>
        /// <returns>The same options instance, for chaining.</returns>
        public CacheOptions WithTypeExpiration(Type type, int expirationMinutes) => options.WithTypeExpiration(type.FullName ?? type.Name, expirationMinutes);
    }
}

/// <summary>Fluent helpers on <see cref="ICacheEntryOptions" />.</summary>
public static class CacheEntryOptionsExtensions
{
    extension(ICacheEntryOptions options)
    {
        /// <summary>Sets the entry duration without changing <see cref="ICacheEntryOptions.ExpirationMode" />.</summary>
        /// <returns>The same options instance, for chaining.</returns>
        public ICacheEntryOptions SetDuration(TimeSpan duration)
        {
            options.Duration = duration;
            return options;
        }

        /// <summary>Applies <paramref name="duration" /> with <see cref="CacheExpirationMode.Absolute" />.</summary>
        public ICacheEntryOptions SetAbsoluteExpiration(TimeSpan duration)
        {
            options.Duration = duration;
            options.ExpirationMode = CacheExpirationMode.Absolute;
            return options;
        }

        /// <summary>Applies <paramref name="duration" /> with <see cref="CacheExpirationMode.Sliding" />.</summary>
        public ICacheEntryOptions SetSlidingExpiration(TimeSpan duration)
        {
            options.Duration = duration;
            options.ExpirationMode = CacheExpirationMode.Sliding;
            return options;
        }
    }
}