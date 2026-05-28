using Lyo.Authentication.OpenIdConnect.Provider;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Authentication.Keycloak;

/// <summary>DI surface for <c>Lyo.Authentication.Keycloak</c>.</summary>
public static class Extensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers a Keycloak OIDC provider using inline options.</summary>
        public IServiceCollection AddKeycloakProvider(Action<KeycloakOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.Configure(configure);
            services.AddSingleton<IOpenIdConnectProvider, KeycloakOpenIdConnectProvider>();
            return services;
        }

        /// <summary>Registers a Keycloak OIDC provider by binding <see cref="KeycloakOptions" /> from configuration (default section <c>KeycloakAuth</c>).</summary>
        public IServiceCollection AddKeycloakProviderFromConfiguration(IConfiguration configuration, string sectionName = KeycloakOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            services.Configure<KeycloakOptions>(o => configuration.GetSection(sectionName).Bind(o));
            services.AddSingleton<IOpenIdConnectProvider, KeycloakOpenIdConnectProvider>();
            return services;
        }
    }
}