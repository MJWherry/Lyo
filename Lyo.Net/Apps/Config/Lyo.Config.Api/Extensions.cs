using System.Text;
using Lyo.Api;
using Lyo.Authentication;
using Lyo.Authentication.AspNetCore;
using Lyo.Authentication.AspNetCore.Authorization;
using Lyo.Authentication.Postgres;
using Lyo.Authentication.Scopes;
using Lyo.Cache;
using Lyo.Config;
using Lyo.Config.Api.Endpoints;
using Lyo.Config.Api.Security;
using Lyo.Config.Postgres;
using Lyo.Encryption.Extensions;
using Lyo.KeyStore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Config.Api;

/// <summary>Adds the Postgres config store and maps its HTTP routes.</summary>
public static class Extensions
{
    /// <summary>
    /// Registers <see cref="IConfigStore" />, security and hosting options, and Lyo auth (opaque tokens, JWT, scope policies, Postgres user/token/audit
    /// stores). Also hooks query services and encrypts config values on the API.
    /// </summary>
    public static IServiceCollection AddConfigApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPostgresConfigStoreFromConfiguration(configuration);
        services.AddLocalCache();
        services.AddLyoQueryServices();
        services.AddConfigQueryServices();
        services.Configure<ConfigApiSecurityOptions>(configuration.GetSection(ConfigApiSecurityOptions.SectionName));
        services.Configure<ConfigApiHostingOptions>(configuration.GetSection(ConfigApiHostingOptions.SectionName));
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<ConfigApiSecurityOptions>>().Value);
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<ConfigApiHostingOptions>>().Value);
        services.AddLocalKeyStore(ks => {
            var seed = SHA256.HashData(Encoding.UTF8.GetBytes("lyo-config-api-dev-jwt-signing-key/v1"));
            ks.AddKey("lyo-sig", "v1", seed);
            ks.SetCurrentVersion("lyo-sig", "v1");
        });
        services.AddKeyedLocalKeyStore(
            ConfigQueryRoutes.EncryptionKeyName, ks => {
                var seed = SHA256.HashData(Encoding.UTF8.GetBytes("lyo-config-values-dev-key/v1"));
                ks.AddKey(ConfigQueryRoutes.EncryptionKeyName, "v1", seed);
                ks.SetCurrentVersion(ConfigQueryRoutes.EncryptionKeyName, "v1");
            });
        services.AddEncryptionServiceKeyed(ConfigQueryRoutes.EncryptionKeyName, ConfigQueryRoutes.EncryptionKeyName);
        services.AddConfigValueEncryption(ConfigQueryRoutes.EncryptionKeyName);

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
    /// Maps the central config group at <paramref name="prefix" />. With <paramref name="requireAuthentication" /> on, reads need
    /// <c>config.read</c>, writes need <c>config.write</c>, and delete/revert need <c>config.admin</c>. Query, get, and export for definitions and bindings are mapped too.
    /// TestApi sets <c>false</c> so the Gateway workbench can hit manage routes without scopes. Encryption still runs in-process.
    /// </summary>
    public static RouteGroupBuilder MapConfigApiEndpoints(this WebApplication app, string prefix = "/api/config", bool requireAuthentication = true)
    {
        app.MapConfigQueryEndpoints();
        var group = app.MapGroup(prefix);
        if (requireAuthentication)
            group.RequireScope("config.read");
        else
            group.AllowAnonymous();

        group.MapLyoConfiguredEndpoints(requireAuthentication);
        return group;
    }
}
