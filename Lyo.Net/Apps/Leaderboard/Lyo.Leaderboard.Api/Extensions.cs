using System.Security.Cryptography;
using System.Text;
using Lyo.Algorithms;
using Lyo.Algorithms.Postgres;
using Lyo.Api;
using Lyo.Authentication;
using Lyo.Authentication.AspNetCore;
using Lyo.Authentication.Google;
using Lyo.Authentication.Keycloak;
using Lyo.Authentication.OpenIdConnect;
using Lyo.Authentication.Postgres;
using Lyo.Cache;
using Lyo.Email;
using Lyo.Exceptions;
using Lyo.KeyStore;
using Lyo.Leaderboard.Api.Endpoints;
using Lyo.Leaderboard.Postgres;
using Lyo.QRCode;
using Lyo.ShortUrl.Postgres;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Leaderboard.Api;

/// <summary>Registers Postgres-backed leaderboard services plus route mapping.</summary>
public static class Extensions
{
    /// <summary>
    /// Adds the leaderboard store, scoring engine, authentication, query services, QR generation, and a persisted ShortUrl service. Registers Google/Keycloak OIDC providers when
    /// their ClientId is configured.
    /// </summary>
    public static IServiceCollection AddLeaderboardApi(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configuration);
        services.AddPostgresLeaderboardStoreFromConfiguration(configuration);
        var leaderboard = new PostgresLeaderboardOptions();
        configuration.GetSection(PostgresLeaderboardOptions.SectionName).Bind(leaderboard);
        services.AddPostgresAlgorithmStore(
            new PostgresAlgorithmOptions { ConnectionString = leaderboard.ConnectionString, EnableAutoMigrations = leaderboard.EnableAutoMigrations });

        services.AddAlgorithms();
        services.AddLocalCache();
        services.AddLyoQueryServices();
        services.AddLeaderboardQueryServices();
        services.Configure<LeaderboardApiOptions>(configuration.GetSection(LeaderboardApiOptions.SectionName));
        services.AddLocalKeyStore(ks => {
            var seed = SHA256.HashData(Encoding.UTF8.GetBytes("lyo-leaderboard-api-dev-jwt-signing-key/v1"));
            ks.AddKey("lyo-sig", "v1", seed);
            ks.SetCurrentVersion("lyo-sig", "v1");
        });

        services.AddLyoAuthentication(configuration);
        services.AddPostgresAuthenticationStoresFromConfiguration(configuration);
        services.AddLyoApiTokenAuthentication();
        services.AddAuthorization();
        services.AddLeaderboardScopes();
        services.AddLyoOpenIdConnect(configuration);
        if (!string.IsNullOrWhiteSpace(configuration.GetSection("GoogleAuth")["ClientId"]))
            services.AddGoogleProviderFromConfiguration(configuration);

        if (!string.IsNullOrWhiteSpace(configuration.GetSection("KeycloakAuth")["ClientId"]))
            services.AddKeycloakProviderFromConfiguration(configuration);

        services.AddQRCodeService();
        if (!string.IsNullOrWhiteSpace(configuration.GetSection("PostgresShortUrl")["ConnectionString"]))
            services.AddPostgresShortUrlServiceFromConfiguration(configuration);

        if (!string.IsNullOrWhiteSpace(configuration.GetSection("EmailServiceOptions")["Host"]))
            services.AddEmailServiceFromConfiguration(configuration);

        return services;
    }

    /// <summary>
    /// Registers the store, query services, QR generator, and persisted ShortUrl for an already-authenticated host (TestApi). Does not re-register JWT, OIDC, or Google
    /// providers.
    /// </summary>
    public static IServiceCollection AddLeaderboardApiForWorkbench(this IServiceCollection services, string connectionString, string publicBaseUrl)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(publicBaseUrl);
        if (!services.Any(d => d.ServiceType == typeof(IAlgorithmDefinitionStore))) {
            services.AddPostgresAlgorithmStore(new PostgresAlgorithmOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
            services.AddAlgorithms();
        }

        services.AddPostgresLeaderboardStore(o => {
            o.ConnectionString = connectionString;
            o.EnableAutoMigrations = true;
        });

        services.AddLeaderboardQueryServices();
        services.Configure<LeaderboardApiOptions>(o => {
            o.PublicBaseUrl = publicBaseUrl.TrimEnd('/');
            o.SendInviteEmail = false;
        });

        services.AddQRCodeService();
        services.AddPostgresShortUrlService(
            db => {
                db.ConnectionString = connectionString;
                db.EnableAutoMigrations = true;
            }, s => s.BaseUrl = publicBaseUrl.TrimEnd('/') + "/s");

        return services;
    }

    /// <summary>Maps leaderboard routes under <paramref name="prefix" /> and optional short-link redirects at <c>/s/{id}</c>.</summary>
    public static RouteGroupBuilder MapLeaderboardApiEndpoints(this WebApplication app, string prefix = "/api/leaderboard", bool requireAuthentication = true)
    {
        ArgumentHelpers.ThrowIfNull(app);
        app.MapLeaderboardQueryEndpoints();
        var group = app.MapGroup(prefix);
        if (requireAuthentication)
            group.RequireAuthorization();
        else
            group.AllowAnonymous();

        group.MapLeaderboardRoutes(requireAuthentication);
        app.MapShortUrlRedirect();
        return group;
    }
}