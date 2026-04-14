using Lyo.Compression;
using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;

// Do not add 'using Lyo.Cache.Fusion' - we need ZiggyCreatures.AddFusionCache(), not ours

namespace Lyo.Cache.Fusion;

/// <summary>Internal helper to register FusionCache builder. Isolated to avoid name conflict with our AddFusionCache.</summary>
internal static class FusionCacheRegistration
{
    public static IServiceCollection AddFusionCacheInternal(
        IServiceCollection svc,
        Action<CacheOptions>? configureOptions,
        Action<FusionCacheOptions>? configureFusionCache,
        Action<RedisBackplaneOptions>? configureRedisBackplane)
    {
        ArgumentHelpers.ThrowIfNull(svc, nameof(svc));
        var cacheOptions = new CacheOptions();
        configureOptions?.Invoke(cacheOptions);
        svc.AddSingleton(cacheOptions);
        svc.AddMemoryCache();
        if (!svc.Any(static d => d.ServiceType == typeof(ICompressionService)))
            svc.AddCompressionService();

        svc.AddSingleton<ICachePayloadCodec>(sp =>
#if NET10_0_OR_GREATER
            new CachePayloadCodec(
                sp.GetRequiredService<CacheOptions>(),
                sp.GetRequiredService<ICompressionService>(),
                sp.GetService<Encryption.IEncryptionService>())
#else
            new CachePayloadCodec(
                sp.GetRequiredService<CacheOptions>(),
                sp.GetRequiredService<ICompressionService>())
#endif
        );

        svc.TryAddSingleton(CachePayloadSerializerRegistration.Create);

        var fusionCacheBuilder = FusionCacheServiceCollectionExtensions.AddFusionCache(svc);
        if (configureFusionCache != null)
            fusionCacheBuilder.WithOptions(configureFusionCache);

        fusionCacheBuilder.TryWithAutoSetup();
        if (configureRedisBackplane != null) {
            var serviceLocator = new ServiceLocator();
            svc.AddSingleton(serviceLocator);
            svc.AddFusionCacheStackExchangeRedisBackplane(options => {
                options.ConnectionMultiplexerFactory = () => {
                    var sp = serviceLocator.ServiceProvider;
                    OperationHelpers.ThrowIfNull(sp, "Service provider not available. Ensure FusionCacheService is resolved before using Redis backplane.");
                    var connectionMultiplexer = sp.GetService<IConnectionMultiplexer>();
                    OperationHelpers.ThrowIfNull(
                        connectionMultiplexer,
                        "IConnectionMultiplexer must be registered before configuring Redis backplane. Use AddRedisConnection() or AddFusionCache(redisConnectionString) or AddFusionCacheFromConfiguration(IConfiguration).");

                    return Task.FromResult(connectionMultiplexer);
                };

                configureRedisBackplane(options);
            });
        }

        svc.AddSingleton<ICacheService>(serviceProvider => {
            if (configureRedisBackplane != null) {
                var serviceLocator = serviceProvider.GetService<ServiceLocator>();
                serviceLocator?.ServiceProvider = serviceProvider;
            }

            var logger = serviceProvider.GetService<ILogger<FusionCacheService>>();
            var options = serviceProvider.GetRequiredService<CacheOptions>();
            var fusionCache = serviceProvider.GetRequiredService<IFusionCache>();
            var metrics = options.EnableMetrics ? serviceProvider.GetService<IMetrics>() : null;
            var payloadCodec = serviceProvider.GetRequiredService<ICachePayloadCodec>();
            var payloadSerializer = serviceProvider.GetRequiredService<ICachePayloadSerializer>();
            return new FusionCacheService(fusionCache, logger, options, metrics, payloadCodec, payloadSerializer);
        });

        return svc;
    }
}