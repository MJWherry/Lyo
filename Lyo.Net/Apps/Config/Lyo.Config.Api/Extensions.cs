using System.Text;
using Lyo.Authentication;
using Lyo.Authentication.AspNetCore;
using Lyo.Authentication.AspNetCore.Authorization;
using Lyo.Authentication.Postgres;
using Lyo.Authentication.Scopes;
using Lyo.Config.Api.Endpoints;
using Lyo.Config.Api.Security;
using Lyo.Config.Postgres;
using Lyo.KeyStore;

namespace Lyo.Config.Api;

/// <summary>Registers Postgres-backed config services plus route mapping.</summary>
public static class Extensions
{
    /// <summary>
    /// Adds <see cref="IConfigStore" /> plus security + hosting options and the Lyo authentication pipeline (opaque-token + JWT, scope policies, Postgres-backed user/token/audit
    /// stores).
    /// </summary>
    public static IServiceCollection AddConfigApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPostgresConfigStoreFromConfiguration(configuration);
        services.Configure<ConfigApiSecurityOptions>(configuration.GetSection(ConfigApiSecurityOptions.SectionName));
        services.Configure<ConfigApiHostingOptions>(configuration.GetSection(ConfigApiHostingOptions.SectionName));
        services.AddLocalKeyStore(ks => {
            var seed = SHA256.HashData(Encoding.UTF8.GetBytes("lyo-config-api-dev-jwt-signing-key/v1"));
            ks.AddKey("lyo-sig", "v1", seed);
            ks.SetCurrentVersion("lyo-sig", "v1");
        });

        services.AddLyoAuthentication(configuration);
        services.AddPostgresAuthenticationStoresFromConfiguration(configuration);
        services.AddLyoApiTokenAuthentication();
        services.AddAuthorization();
        services.AddScope("config.read", "Read Lyo config bindings and definitions.");
        services.AddScope("config.write", "Mutate Lyo config bindings and definitions.", "config.read");
        services.AddScope("config.admin", "Admin Lyo config (delete, revert, bulk).", "config.write");
        services.AddHostedService<ConfigApiLegacyBootstrap>();
        return services;
    }

    /// <summary>
    /// Maps centralized config routes grouped under <paramref name="prefix" />. All endpoints require <c>config.read</c>; mutating endpoints require <c>config.write</c>;
    /// deletes/reverts require <c>config.admin</c>.
    /// </summary>
    public static RouteGroupBuilder MapConfigApiEndpoints(this WebApplication app, string prefix = "/api/config")
    {
        var group = app.MapGroup(prefix).RequireScope("config.read");
        group.MapLyoConfiguredEndpoints();
        return group;
    }
}