using Lyo.Authentication.Saml;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.Okta;

/// <summary>Container helpers for <c>Lyo.Authentication.Okta</c>.</summary>
public static class Extensions
{
    /// <param name="services">Collection that receives the Okta registration.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds the Okta SAML profile and registers <c>okta</c> on the SSO protocol registry. Does not add OpenID Connect.</summary>
        public IServiceCollection AddOktaProvider(Action<OktaOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddLyoSaml();
            services.Configure(configure);
            services.PostConfigure<OktaOptions>(options => options.Validate());
            services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<OktaOptions>>().Value);
            services.AddSingleton<ISamlIdentityProvider, OktaSamlProvider>();
            return services;
        }

        /// <summary>Adds the Okta SAML profile by binding <see cref="OktaOptions" /> (section defaults to <c>OktaAuth</c>).</summary>
        public IServiceCollection AddOktaProviderFromConfiguration(IConfiguration configuration, string sectionName = OktaOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            services.AddLyoSaml(configuration);
            services.Configure<OktaOptions>(options => configuration.GetSection(sectionName).Bind(options));
            services.PostConfigure<OktaOptions>(options => options.Validate());
            services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<OktaOptions>>().Value);
            services.AddSingleton<ISamlIdentityProvider, OktaSamlProvider>();
            return services;
        }
    }
}
