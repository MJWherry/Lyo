using Lyo.Diagnostic.AspNetCore;
using Lyo.Exceptions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.Client;

/// <summary>DI surface for <c>Lyo.Authentication.Client</c>.</summary>
public static class Extensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the consumer-side Lyo auth runtime: options, <see cref="LyoAuthSessionStore" />, the typed <see cref="LyoAuthApiClient" /> targeting
        /// <see cref="LyoAuthClientOptions.AuthBaseUrl" />, the <see cref="LyoAuthDelegatingHandler" />, the cookie scheme (<see cref="LyoAuthClientOptions.SchemeName" />), data protection,
        /// and <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor" />. Does NOT register <see cref="LyoAuthStateProvider" /> (Blazor-specific); call
        /// <see cref="AddLyoAuthBlazorStateProvider" /> on Blazor hosts.
        /// </summary>
        public IServiceCollection AddLyoAuthClient(Action<LyoAuthClientOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddOptions<LyoAuthClientOptions>().Configure(configure);
            return services.RegisterLyoAuthClientCore();
        }

        /// <summary>Same as <see cref="AddLyoAuthClient(Action{LyoAuthClientOptions})" /> but binds <see cref="LyoAuthClientOptions" /> from configuration.</summary>
        public IServiceCollection AddLyoAuthClient(IConfiguration configuration, string sectionName = LyoAuthClientOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            services.AddOptions<LyoAuthClientOptions>().Bind(configuration.GetSection(sectionName));
            return services.RegisterLyoAuthClientCore();
        }

        /// <summary>
        /// Registers <see cref="LyoAuthStateProvider" /> as the Blazor <see cref="Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider" />. Call after
        /// <see cref="AddLyoAuthClient(Action{LyoAuthClientOptions})" />.
        /// </summary>
        public IServiceCollection AddLyoAuthBlazorStateProvider()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddScoped<AuthenticationStateProvider, LyoAuthStateProvider>();
            return services;
        }

        internal IServiceCollection RegisterLyoAuthClientCore()
        {
            services.AddDataProtection();
            services.AddHttpContextAccessor();
            services.AddLyoCorrelation();
            services.TryAddSingleton<LyoAuthSessionStore>();
            services.AddHttpClient<LyoAuthApiClient>((sp, http) => {
                    var opts = sp.GetRequiredService<IOptions<LyoAuthClientOptions>>().Value;
                    ArgumentHelpers.ThrowIfNullOrWhiteSpace(opts.AuthBaseUrl, "LyoAuthClientOptions.AuthBaseUrl");
                    http.BaseAddress = new(opts.AuthBaseUrl);
                })
                .AddLyoCorrelationHandler();

            services.AddTransient<LyoAuthDelegatingHandler>();
            services.AddAuthentication(LyoAuthClientOptions.SchemeName)
                .AddScheme<LyoAuthCookieOptions, LyoAuthCookieAuthenticationHandler>(LyoAuthClientOptions.SchemeName, _ => { });

            return services;
        }
    }

    /// <param name="builder">The HTTP client builder.</param>
    extension(IHttpClientBuilder builder)
    {
        /// <summary>
        /// Attaches <see cref="LyoAuthDelegatingHandler" /> so outbound calls automatically get <c>Authorization: Bearer</c> and auto-refresh. When
        /// <paramref name="propagateCorrelationId" /> is <c>true</c> (the default), also chains <see cref="Lyo.Diagnostic.Correlation.LyoCorrelationDelegatingHandler" /> as the
        /// <strong>outermost</strong> handler so the correlation header is stamped before the auth handler runs (covering both the primary call and any nested refresh roundtrip). Opt out by
        /// passing <c>false</c> for hosts that already stamp the header elsewhere.
        /// </summary>
        public IHttpClientBuilder AddLyoAuthHandler(bool propagateCorrelationId = true)
        {
            if (propagateCorrelationId)
                builder.AddLyoCorrelationHandler();

            return builder.AddHttpMessageHandler<LyoAuthDelegatingHandler>();
        }
    }
}