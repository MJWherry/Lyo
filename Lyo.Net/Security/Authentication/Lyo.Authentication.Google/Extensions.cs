using System;
using Lyo.Authentication.OpenIdConnect.Provider;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Authentication.Google;

/// <summary>DI surface for <c>Lyo.Authentication.Google</c>.</summary>
public static class Extensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers a Google OIDC provider profile using inline options.</summary>
        public IServiceCollection AddGoogleProvider(Action<GoogleOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.Configure(configure);
            services.AddSingleton<IOpenIdConnectProvider, GoogleOpenIdConnectProvider>();
            return services;
        }

        /// <summary>Registers a Google OIDC provider profile by binding <see cref="GoogleOptions"/> from configuration (default section <c>GoogleAuth</c>).</summary>
        public IServiceCollection AddGoogleProviderFromConfiguration(IConfiguration configuration, string sectionName = GoogleOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            services.Configure<GoogleOptions>(o => configuration.GetSection(sectionName).Bind(o));
            services.AddSingleton<IOpenIdConnectProvider, GoogleOpenIdConnectProvider>();
            return services;
        }
    }
}
