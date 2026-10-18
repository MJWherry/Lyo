using Lyo.Authentication.Sso;
using Lyo.Authentication.Sso.Protocol;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Authentication.Saml;

/// <summary>Container helpers for <c>Lyo.Authentication.Saml</c>.</summary>
public static class Extensions
{
    /// <param name="services">Collection that receives the SAML registrations.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the SAML service provider on top of <c>AddLyoSso</c>: metadata cache, login tickets, and <see cref="SamlSsoProtocol" />.
        /// Does not bind BFF options; <c>AddLyoSso(IConfiguration)</c> does that.
        /// </summary>
        public IServiceCollection AddLyoSaml()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddLyoSso();
            services.AddHttpClient<SamlMetadataCache>()
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
            services.TryAddSingleton<ISamlMetadataSource>(sp => sp.GetRequiredService<SamlMetadataCache>());
            services.TryAddSingleton<InMemorySamlLoginTicketStore>();
            services.TryAddSingleton<ISamlLoginTicketStore>(sp => sp.GetRequiredService<InMemorySamlLoginTicketStore>());
            services.TryAddSingleton<ISamlAssertionReplayCache, InMemorySamlAssertionReplayCache>();
            services.TryAddSingleton<SamlResponseValidator>();
            services.AddSingleton<SamlSsoProtocol>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ISsoProtocol, SamlSsoProtocol>(sp => sp.GetRequiredService<SamlSsoProtocol>()));
            return services;
        }

        /// <summary>Like <see cref="AddLyoSaml()" />. Also binds <c>LyoSso</c> and <c>LyoExternalLogin</c> through <c>AddLyoSso(IConfiguration)</c>.</summary>
        public IServiceCollection AddLyoSaml(IConfiguration configuration)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            services.AddLyoSso(configuration);
            services.AddLyoSaml();
            return services;
        }
    }
}
