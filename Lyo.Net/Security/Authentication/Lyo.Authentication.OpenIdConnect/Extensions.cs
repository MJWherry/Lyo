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
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.OpenIdConnect;

/// <summary>Container helpers for <c>Lyo.Authentication.OpenIdConnect</c>.</summary>
public static class Extensions
{
    /// <param name="services">Collection that receives the OIDC registrations.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the OIDC base: discovery cache, JWKS resolver, PKCE state protector, authorize URL builder, token-exchange client, id_token validator, provider registry,
        /// <see cref="DefaultExternalLoginCoordinator" />, <see cref="OpenIdConnectBffOptions" />, and in-memory <see cref="IHandoffCodeStore" />. Provider profiles (Google,
        /// Keycloak) layer on top.
        /// </summary>
        public IServiceCollection AddLyoOpenIdConnect()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddDataProtection();
            services.AddOptions<ExternalLoginOptions>();
            services.AddOptions<OpenIdConnectBffOptions>();
            services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<ExternalLoginOptions>>().Value);
            services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<OpenIdConnectBffOptions>>().Value);
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

        /// <summary>Like <see cref="AddLyoOpenIdConnect()" /> plus binding <see cref="ExternalLoginOptions" /> and <see cref="OpenIdConnectBffOptions" /> from config.</summary>
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