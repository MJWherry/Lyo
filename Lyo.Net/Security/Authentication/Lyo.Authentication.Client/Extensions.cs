using Lyo.Diagnostic.AspNetCore;
using Lyo.Exceptions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.Client;

/// <summary>Container helpers for <c>Lyo.Authentication.Client</c>.</summary>
public static class Extensions
{
    /// <param name="services">Collection that receives the consumer auth registrations.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the consumer-side Lyo auth runtime: options, <see cref="LyoAuthSessionStore" />, the typed <see cref="LyoAuthApiClient" /> aimed at
        /// <see cref="LyoAuthClientOptions.AuthBaseUrl" />, <see cref="LyoAuthDelegatingHandler" />, cookie scheme (<see cref="LyoAuthClientOptions.SchemeName" />),
        /// data protection, and <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor" />. Does not register <see cref="LyoAuthStateProvider" /> (Blazor-only); call
        /// <see cref="AddLyoAuthBlazorStateProvider" /> on Blazor hosts.
        /// </summary>
        public IServiceCollection AddLyoAuthClient(Action<LyoAuthClientOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddOptions<LyoAuthClientOptions>().Configure(configure);
            services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<LyoAuthClientOptions>>().Value);
            return services.RegisterLyoAuthClientCore();
        }

        /// <summary>Same as <see cref="AddLyoAuthClient(Action{LyoAuthClientOptions})" /> but binds <see cref="LyoAuthClientOptions" /> from configuration.</summary>
        public IServiceCollection AddLyoAuthClient(IConfiguration configuration, string sectionName = LyoAuthClientOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            services.AddOptions<LyoAuthClientOptions>().Bind(configuration.GetSection(sectionName));
            services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<LyoAuthClientOptions>>().Value);
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

    /// <param name="builder">HTTP client builder.</param>
    extension(IHttpClientBuilder builder)
    {
        /// <summary>
        /// Attaches <see cref="LyoAuthDelegatingHandler" /> so outbound calls get <c>Authorization: Bearer</c> and auto-refresh. When
        /// <paramref name="propagateCorrelationId" /> is <c>true</c> (the default), also chains <see cref="Lyo.Diagnostic.Correlation.LyoCorrelationDelegatingHandler" /> as the
        /// <strong>outermost</strong> handler so the correlation header is stamped before auth runs (covers the primary call and any nested refresh). Opt out by
        /// passing <c>false</c> when the host already stamps that header elsewhere.
        /// </summary>
        public IHttpClientBuilder AddLyoAuthHandler(bool propagateCorrelationId = true)
        {
            if (propagateCorrelationId)
                builder.AddLyoCorrelationHandler();

            return builder.AddHttpMessageHandler<LyoAuthDelegatingHandler>();
        }
    }
}