using Lyo.Authentication.OpenIdConnect.Provider;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.Keycloak;

/// <summary>Container helpers for <c>Lyo.Authentication.Keycloak</c>.</summary>
public static class Extensions
{
    /// <param name="services">Collection that receives the Keycloak provider registration.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds a Keycloak OpenID Connect provider from the options you pass in.</summary>
        public IServiceCollection AddKeycloakProvider(Action<KeycloakOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.Configure(configure);
            services.PostConfigure<KeycloakOptions>(o => o.Validate());
            services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<KeycloakOptions>>().Value);
            services.AddSingleton<IOpenIdConnectProvider, KeycloakOpenIdConnectProvider>();
            return services;
        }

        /// <summary>Adds a Keycloak OpenID Connect provider by binding <see cref="KeycloakOptions" /> (section defaults to <c>KeycloakAuth</c>).</summary>
        public IServiceCollection AddKeycloakProviderFromConfiguration(IConfiguration configuration, string sectionName = KeycloakOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            services.Configure<KeycloakOptions>(o => configuration.GetSection(sectionName).Bind(o));
            services.PostConfigure<KeycloakOptions>(o => o.Validate());
            services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<KeycloakOptions>>().Value);
            services.AddSingleton<IOpenIdConnectProvider, KeycloakOpenIdConnectProvider>();
            return services;
        }
    }
}