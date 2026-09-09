using Lyo.Api.Client;
using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Drift.Client;

/// <summary>DI helpers for <see cref="IDriftClient" />.</summary>
public static class Extensions
{
    /// <summary>Adds a singleton <see cref="IDriftClient" /> built from the registered <see cref="IApiClient" />.</summary>
    public static IServiceCollection AddDriftClient(this IServiceCollection services, DriftClientOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.TryAddSingleton<IDriftClient>(sp => new DriftClient(sp.GetRequiredService<IApiClient>(), options));
        return services;
    }

    /// <summary>Adds a singleton <see cref="IDriftClient" /> whose inner <see cref="IApiClient" /> comes from a factory.</summary>
    public static IServiceCollection AddDriftClient(this IServiceCollection services, Func<IServiceProvider, IApiClient> apiClientFactory, DriftClientOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(apiClientFactory);
        services.TryAddSingleton<IDriftClient>(sp => new DriftClient(apiClientFactory(sp), options));
        return services;
    }
}
