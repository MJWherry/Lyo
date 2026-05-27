using System;
using Blazored.LocalStorage;
using Lyo.Authentication.Web.Components.Abstractions;
using Lyo.Diagnostic.Correlation;
using Lyo.Exceptions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.Web.Components.Wasm;

/// <summary>DI surface for the Blazor WebAssembly host adapter.</summary>
public static class Extensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the WASM-side <see cref="IAuthSignInLauncher"/> / <see cref="IAuthUserClient"/> / <see cref="IAuthSessionAccessor"/>, the
        /// <see cref="WasmAuthSessionStore"/>, the <see cref="WasmAuthDelegatingHandler"/>, and the <see cref="WasmAuthStateProvider"/> so <c>AuthorizeView</c> works out of the box.
        /// Bind <see cref="WasmAuthClientOptions"/> from configuration.
        /// </summary>
        public IServiceCollection AddLyoAuthWebComponentsWasm(IConfiguration configuration, string sectionName = WasmAuthClientOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            services.AddOptions<WasmAuthClientOptions>().Bind(configuration.GetSection(sectionName));
            return services.RegisterLyoAuthWebComponentsWasmCore();
        }

        /// <summary>Same as <see cref="AddLyoAuthWebComponentsWasm(IConfiguration, string)"/> but configures the options inline.</summary>
        public IServiceCollection AddLyoAuthWebComponentsWasm(Action<WasmAuthClientOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddOptions<WasmAuthClientOptions>().Configure(configure);
            return services.RegisterLyoAuthWebComponentsWasmCore();
        }

        internal IServiceCollection RegisterLyoAuthWebComponentsWasmCore()
        {
            services.AddBlazoredLocalStorageIfMissing();
            services.AddAuthorizationCore();
            services.TryAddSingleton<WasmAuthSessionStore>();
            services.TryAddScoped<WasmAuthStateProvider>();
            services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<WasmAuthStateProvider>());
            services.AddTransient<WasmAuthDelegatingHandler>();

            services.AddOptions<CorrelationHandlerOptions>();
            services.TryAddSingleton<ICorrelationIdResolver>(_ => AmbientCorrelationIdResolver.Instance);
            services.TryAddTransient<LyoCorrelationDelegatingHandler>(sp => new(
                sp.GetRequiredService<ICorrelationIdResolver>(),
                sp.GetService<IOptions<CorrelationHandlerOptions>>()?.Value));

            services.AddHttpClient<WasmAuthApiClient>((sp, http) => {
                    var opts = sp.GetRequiredService<IOptions<WasmAuthClientOptions>>().Value;
                    ArgumentHelpers.ThrowIfNullOrWhiteSpace(opts.AuthBaseUrl, "WasmAuthClientOptions.AuthBaseUrl");
                    http.BaseAddress = new(opts.AuthBaseUrl);
                })
                .AddHttpMessageHandler<LyoCorrelationDelegatingHandler>();

            services.AddHttpClient<IAuthUserClient, WasmAuthUserClient>((sp, http) => {
                    var opts = sp.GetRequiredService<IOptions<WasmAuthClientOptions>>().Value;
                    ArgumentHelpers.ThrowIfNullOrWhiteSpace(opts.AuthBaseUrl, "WasmAuthClientOptions.AuthBaseUrl");
                    http.BaseAddress = new(opts.AuthBaseUrl);
                })
                .AddHttpMessageHandler<LyoCorrelationDelegatingHandler>()
                .AddHttpMessageHandler<WasmAuthDelegatingHandler>();

            services.AddHttpClient<IAuthTokenManagementClient, WasmAuthTokenManagementClient>((sp, http) => {
                    var opts = sp.GetRequiredService<IOptions<WasmAuthClientOptions>>().Value;
                    ArgumentHelpers.ThrowIfNullOrWhiteSpace(opts.AuthBaseUrl, "WasmAuthClientOptions.AuthBaseUrl");
                    http.BaseAddress = new(opts.AuthBaseUrl);
                })
                .AddHttpMessageHandler<LyoCorrelationDelegatingHandler>()
                .AddHttpMessageHandler<WasmAuthDelegatingHandler>();

            services.TryAddScoped<IAuthSignInLauncher, WasmAuthSignInLauncher>();
            services.TryAddScoped<IAuthSessionAccessor, WasmAuthSessionAccessor>();
            return services;
        }

        private void AddBlazoredLocalStorageIfMissing()
        {
            foreach (var descriptor in services) {
                if (descriptor.ServiceType == typeof(ILocalStorageService))
                    return;
            }

            services.AddBlazoredLocalStorage();
        }
    }
}
