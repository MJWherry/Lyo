using Lyo.Authentication.Web.Components.Abstractions;
using Lyo.Authentication.Web.Components.Options;
using Lyo.Authentication.Web.Components.Providers;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.Web.Components;

/// <summary>Container helpers for the shared auth web components library.</summary>
public static class Extensions
{
    /// <param name="services">Collection that receives the shared web-component registrations.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="LyoAuthWebComponentsOptions" /> and the default <see cref="IAuthProviderCatalog" />. Does not register the host-specific
        /// <see cref="IAuthSignInLauncher" /> / <see cref="IAuthUserClient" /> / <see cref="IAuthSessionAccessor" /> — call <c>AddLyoAuthWebComponentsServer</c> or
        /// <c>AddLyoAuthWebComponentsWasm</c> for those. <see cref="IAuthPasswordSignIn" /> stays opt-in: register your own impl if you want the password card on the
        /// login page.
        /// </summary>
        public IServiceCollection AddLyoAuthWebComponents(IConfiguration configuration, string sectionName = LyoAuthWebComponentsOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            services.AddOptions<LyoAuthWebComponentsOptions>().Bind(configuration.GetSection(sectionName));
            services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<LyoAuthWebComponentsOptions>>().Value);
            services.TryAddSingleton<IAuthProviderCatalog, DefaultAuthProviderCatalog>();
            return services;
        }

        /// <summary>Same as <see cref="AddLyoAuthWebComponents(IConfiguration, string)" /> but configures the options inline.</summary>
        public IServiceCollection AddLyoAuthWebComponents(Action<LyoAuthWebComponentsOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddOptions<LyoAuthWebComponentsOptions>().Configure(configure);
            services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<LyoAuthWebComponentsOptions>>().Value);
            services.TryAddSingleton<IAuthProviderCatalog, DefaultAuthProviderCatalog>();
            return services;
        }
    }
}