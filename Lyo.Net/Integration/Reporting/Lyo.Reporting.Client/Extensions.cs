using Lyo.Api.Client;
using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Reporting.Client;

/// <summary>DI extensions for <see cref="ReportingClient" />.</summary>
public static class Extensions
{
    public static IServiceCollection AddReportingClient<TApiClient>(this IServiceCollection services, ReportingClientOptions? options = null)
        where TApiClient : class, IApiClient
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.AddSingleton<IReportingClient>(sp => new ReportingClient(sp.GetRequiredService<TApiClient>(), options));
        return services;
    }

    public static IServiceCollection AddReportingClient(
        this IServiceCollection services,
        Func<IServiceProvider, IApiClient> apiClientFactory,
        ReportingClientOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(apiClientFactory);
        services.AddSingleton<IReportingClient>(sp => new ReportingClient(apiClientFactory(sp), options));
        return services;
    }
}