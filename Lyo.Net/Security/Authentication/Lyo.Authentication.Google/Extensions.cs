using Lyo.Authentication.OpenIdConnect.Provider;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.Google;

/// <summary>Container helpers for <c>Lyo.Authentication.Google</c>.</summary>
public static class Extensions
{
    /// <param name="services">Collection that receives the Google provider registration.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds a Google OpenID Connect profile from the options instance you pass in.</summary>
        public IServiceCollection AddGoogleProvider(Action<GoogleOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.Configure(configure);
            services.PostConfigure<GoogleOptions>(o => o.Validate());
            services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<GoogleOptions>>().Value);
            services.AddSingleton<IOpenIdConnectProvider, GoogleOpenIdConnectProvider>();
            return services;
        }

        /// <summary>Adds a Google OpenID Connect profile by binding <see cref="GoogleOptions" /> (section defaults to <c>GoogleAuth</c>).</summary>
        public IServiceCollection AddGoogleProviderFromConfiguration(IConfiguration configuration, string sectionName = GoogleOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            services.Configure<GoogleOptions>(o => configuration.GetSection(sectionName).Bind(o));
            services.PostConfigure<GoogleOptions>(o => o.Validate());
            services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<GoogleOptions>>().Value);
            services.AddSingleton<IOpenIdConnectProvider, GoogleOpenIdConnectProvider>();
            return services;
        }
    }
}