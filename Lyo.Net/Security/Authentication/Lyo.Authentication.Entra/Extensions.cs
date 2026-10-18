using Lyo.Authentication.Saml;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.Entra;

/// <summary>Container helpers for <c>Lyo.Authentication.Entra</c>.</summary>
public static class Extensions
{
    /// <param name="services">Collection that receives the Entra registration.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds the Entra SAML profile and registers <c>entra</c> on the SSO protocol registry. Does not add OpenID Connect.</summary>
        public IServiceCollection AddEntraProvider(Action<EntraOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddLyoSaml();
            services.Configure(configure);
            services.PostConfigure<EntraOptions>(options => options.Validate());
            services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<EntraOptions>>().Value);
            services.AddSingleton<ISamlIdentityProvider, EntraSamlProvider>();
            return services;
        }

        /// <summary>Adds the Entra SAML profile by binding <see cref="EntraOptions" /> (section defaults to <c>EntraAuth</c>).</summary>
        public IServiceCollection AddEntraProviderFromConfiguration(IConfiguration configuration, string sectionName = EntraOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            services.AddLyoSaml(configuration);
            services.Configure<EntraOptions>(options => configuration.GetSection(sectionName).Bind(options));
            services.PostConfigure<EntraOptions>(options => options.Validate());
            services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<EntraOptions>>().Value);
            services.AddSingleton<ISamlIdentityProvider, EntraSamlProvider>();
            return services;
        }
    }
}
