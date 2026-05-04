using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Hashing.Registration;

/// <summary>Registers <see cref="IHashingService" /> / <see cref="HashingService" /> for dependency injection.</summary>
public static class HashingServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="IHashingService" />: uses <see cref="HashingService.Shared" /> when <paramref name="configure" /> is null; otherwise registers singleton
    /// <see cref="HashingOptions" /> from <paramref name="configure" /> and <see cref="HashingService" /> bound to those options.
    /// </summary>
    public static IServiceCollection AddLyoHashing(this IServiceCollection services, Action<HashingOptions>? configure = null)
    {
        if (configure is null) {
            services.AddSingleton<IHashingService>(_ => HashingService.Shared);
            return services;
        }

        services.AddSingleton(_ => {
            var o = new HashingOptions();
            configure(o);
            return o;
        });

        services.AddSingleton<IHashingService, HashingService>();
        return services;
    }

    /// <summary>Adds <see cref="IHashingService" /> with an explicit options instance (registered as singleton alongside the service).</summary>
    public static IServiceCollection AddLyoHashing(this IServiceCollection services, HashingOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        services.AddSingleton(options);
        services.AddSingleton<IHashingService, HashingService>();
        return services;
    }
}