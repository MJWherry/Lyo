using Lyo.Api.Client;
using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Job.Client;

/// <summary>DI extensions for <see cref="JobClient" />.</summary>
public static class Extensions
{
    /// <summary>Registers a singleton <see cref="IJobClient" /> built from the registered <typeparamref name="TApiClient" />.</summary>
    public static IServiceCollection AddJobClient<TApiClient>(this IServiceCollection services, JobClientOptions? options = null)
        where TApiClient : class, IApiClient
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.AddSingleton<IJobClient>(sp => new JobClient(sp.GetRequiredService<TApiClient>(), options));
        return services;
    }

    /// <summary>Registers a singleton <see cref="IJobClient" /> using a factory to resolve the underlying <see cref="IApiClient" />.</summary>
    public static IServiceCollection AddJobClient(this IServiceCollection services, Func<IServiceProvider, IApiClient> apiClientFactory, JobClientOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(apiClientFactory);
        services.AddSingleton<IJobClient>(sp => new JobClient(apiClientFactory(sp), options));
        return services;
    }
}
