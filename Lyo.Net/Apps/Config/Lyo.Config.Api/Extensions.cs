using Lyo.Config.Api.Endpoints;
using Lyo.Config.Api.Security;
using Lyo.Config.Postgres;

namespace Lyo.Config.Api;

/// <summary>Registers Postgres-backed config services plus route mapping.</summary>
public static class Extensions
{
    /// <summary>Adds <see cref="IConfigStore" /> plus security + hosting options.</summary>
    public static IServiceCollection AddConfigApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPostgresConfigStoreFromConfiguration(configuration);
        services.Configure<ConfigApiSecurityOptions>(configuration.GetSection(ConfigApiSecurityOptions.SectionName));
        services.Configure<ConfigApiHostingOptions>(configuration.GetSection(ConfigApiHostingOptions.SectionName));
        return services;
    }

    /// <summary>Maps centralized config routes grouped under <paramref name="prefix" />.</summary>
    public static RouteGroupBuilder MapConfigApiEndpoints(this WebApplication app, string prefix = "/api/config")
    {
        var group = app.MapGroup(prefix);
        group.MapLyoConfiguredEndpoints();
        return group;
    }
}