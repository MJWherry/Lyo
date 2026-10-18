using Lyo.Authentication.OpenIdConnect.Client;
using Lyo.Authentication.OpenIdConnect.Coordinator;
using Lyo.Authentication.OpenIdConnect.Discovery;
using Lyo.Authentication.OpenIdConnect.Pkce;
using Lyo.Authentication.OpenIdConnect.Protocol;
using Lyo.Authentication.OpenIdConnect.Provider;
using Lyo.Authentication.Sso;
using Lyo.Authentication.Sso.Protocol;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Authentication.OpenIdConnect;

/// <summary>Container helpers for <c>Lyo.Authentication.OpenIdConnect</c>.</summary>
public static class Extensions
{
    /// <param name="services">Collection that receives the OIDC registrations.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the OpenID Connect protocol on top of <c>AddLyoSso</c>: discovery, JWKS, PKCE, token exchange, id_token validation, and
        /// <see cref="OidcSsoProtocol" />. Does not bind BFF options; <c>AddLyoSso(IConfiguration)</c> does that.
        /// </summary>
        public IServiceCollection AddLyoOpenIdConnect()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddLyoSso();
            services.AddDataProtection();
            services.AddHttpClient<OidcDiscoveryCache>();
            services.AddHttpClient<OidcJwksResolver>();
            services.AddHttpClient<OidcTokenExchangeClient>();
            services.AddSingleton<StateNonceProtector>();
            services.AddSingleton<OidcAuthorizationUrlBuilder>();
            services.AddSingleton<OidcIdTokenValidator>();
            services.AddSingleton<OpenIdConnectProviderRegistry>();
            services.AddSingleton<OidcSsoProtocol>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ISsoProtocol, OidcSsoProtocol>(sp => sp.GetRequiredService<OidcSsoProtocol>()));
            services.AddSingleton<IExternalLoginCoordinator, DefaultExternalLoginCoordinator>();
            return services;
        }

        /// <summary>Like <see cref="AddLyoOpenIdConnect()" />. BFF and external-login options are bound by <see cref="Sso.Extensions" />.</summary>
        public IServiceCollection AddLyoOpenIdConnect(IConfiguration configuration)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            services.AddLyoSso(configuration);
            services.AddLyoOpenIdConnect();
            return services;
        }
    }
}
