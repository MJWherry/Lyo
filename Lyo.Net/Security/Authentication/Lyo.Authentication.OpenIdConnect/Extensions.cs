using Lyo.Authentication.OpenIdConnect.Client;
using Lyo.Authentication.OpenIdConnect.Coordinator;
using Lyo.Authentication.OpenIdConnect.Discovery;
using Lyo.Authentication.OpenIdConnect.Handoff;
using Lyo.Authentication.OpenIdConnect.Pkce;
using Lyo.Authentication.OpenIdConnect.Provider;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Authentication.OpenIdConnect;

/// <summary>DI surface for <c>Lyo.Authentication.OpenIdConnect</c>.</summary>
public static class Extensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the OIDC base: discovery cache, JWKS resolver, PKCE state protector, authorize URL builder, token-exchange client, id_token validator, provider registry,
        /// <see cref="DefaultExternalLoginCoordinator" />, <see cref="OpenIdConnectBffOptions" />, and the default in-memory <see cref="IHandoffCodeStore" />. Per-provider profiles (Google,
        /// Keycloak) layer on top.
        /// </summary>
        public IServiceCollection AddLyoOpenIdConnect()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddDataProtection();
            services.AddOptions<ExternalLoginOptions>();
            services.AddOptions<OpenIdConnectBffOptions>();
            services.AddHttpClient<OidcDiscoveryCache>();
            services.AddHttpClient<OidcJwksResolver>();
            services.AddHttpClient<OidcTokenExchangeClient>();
            services.AddSingleton<StateNonceProtector>();
            services.AddSingleton<OidcAuthorizationUrlBuilder>();
            services.AddSingleton<OidcIdTokenValidator>();
            services.AddSingleton<OpenIdConnectProviderRegistry>();
            services.AddSingleton<IExternalLoginCoordinator, DefaultExternalLoginCoordinator>();
            services.TryAddSingleton<IHandoffCodeStore, InMemoryHandoffCodeStore>();
            return services;
        }

        /// <summary>Same as <see cref="AddLyoOpenIdConnect()" /> plus binding <see cref="ExternalLoginOptions" /> and <see cref="OpenIdConnectBffOptions" /> from configuration.</summary>
        public IServiceCollection AddLyoOpenIdConnect(
            IConfiguration configuration,
            string externalLoginSectionName = ExternalLoginOptions.SectionName,
            string bffSectionName = OpenIdConnectBffOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            services.AddLyoOpenIdConnect();
            services.Configure<ExternalLoginOptions>(o => configuration.GetSection(externalLoginSectionName).Bind(o));
            services.Configure<OpenIdConnectBffOptions>(o => configuration.GetSection(bffSectionName).Bind(o));
            return services;
        }
    }
}