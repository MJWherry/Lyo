using Lyo.Authentication.Sso.Completion;
using Lyo.Authentication.Sso.Handoff;
using Lyo.Authentication.Sso.Protocol;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.Sso;

/// <summary>Container helpers for <c>Lyo.Authentication.Sso</c>.</summary>
public static class Extensions
{
    /// <param name="services">Collection that receives the SSO registrations.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers handoff storage, external-login completion, <see cref="SsoBffOptions" />, <see cref="ExternalLoginOptions" />, and <see cref="SsoProtocolRegistry" />.
        /// Protocols register themselves on top.
        /// </summary>
        public IServiceCollection AddLyoSso()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddOptions<ExternalLoginOptions>();
            services.AddOptions<SsoBffOptions>();
            services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<ExternalLoginOptions>>().Value);
            services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<SsoBffOptions>>().Value);
            services.TryAddSingleton<IExternalLoginCompletion, DefaultExternalLoginCompletion>();
            services.TryAddSingleton<IHandoffCodeStore, InMemoryHandoffCodeStore>();
            services.TryAddSingleton<SsoProtocolRegistry>();
            return services;
        }

        /// <summary>
        /// Like <see cref="AddLyoSso()" /> plus binding <see cref="ExternalLoginOptions" /> from <c>LyoExternalLogin</c> and <see cref="SsoBffOptions" /> from <c>LyoSso</c>.
        /// </summary>
        public IServiceCollection AddLyoSso(IConfiguration configuration, string externalLoginSectionName = ExternalLoginOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            services.AddLyoSso();
            services.Configure<ExternalLoginOptions>(o => configuration.GetSection(externalLoginSectionName).Bind(o));
            services.Configure<SsoBffOptions>(o => BindBff(configuration, o));
            return services;
        }
    }

    /// <summary>Binds <c>LyoSso</c> when that section exists.</summary>
    public static void BindBff(IConfiguration configuration, SsoBffOptions options)
    {
        ArgumentHelpers.ThrowIfNull(configuration);
        ArgumentHelpers.ThrowIfNull(options);
        configuration.GetSection(SsoBffOptions.SectionName).Bind(options);
    }
}
