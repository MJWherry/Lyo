using Lyo.Authentication.Client;
using Lyo.Authentication.Web.Components.Abstractions;
using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.Web.Components.Server;

/// <summary>DI surface for the Blazor Server host adapter.</summary>
public static class Extensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the Server-side <see cref="IAuthSignInLauncher" /> / <see cref="IAuthUserClient" /> / <see cref="IAuthSessionAccessor" /> that wrap the
        /// <c>Lyo.Authentication.Client</c> BFF runtime. The caller is responsible for having already invoked <c>AddLyoAuthClient(...)</c> and <c>AddLyoAuthBlazorStateProvider()</c>, plus
        /// <c>AddLyoAuthWebComponents(...)</c> on the shared library.
        /// </summary>
        /// <remarks>
        /// The typed clients use <c>AddLyoAuthHandler()</c>, which by default chains <c>LyoCorrelationDelegatingHandler</c> as the outermost handler so every outbound call carries
        /// the same correlation id as the inbound Blazor circuit. Pass <c>AddLyoAuthHandler(propagateCorrelationId: false)</c> at a registration site that needs to opt out.
        /// </remarks>
        public IServiceCollection AddLyoAuthWebComponentsServer()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddHttpContextAccessor();
            services.TryAddScoped<IAuthSignInLauncher, ServerAuthSignInLauncher>();
            services.TryAddScoped<IAuthSessionAccessor, ServerAuthSessionAccessor>();
            services.AddHttpClient<IAuthUserClient, ServerAuthUserClient>((sp, http) => {
                    var opts = sp.GetRequiredService<IOptions<LyoAuthClientOptions>>().Value;
                    ArgumentHelpers.ThrowIfNullOrWhiteSpace(opts.AuthBaseUrl, "LyoAuthClientOptions.AuthBaseUrl");
                    http.BaseAddress = new(opts.AuthBaseUrl);
                })
                .AddLyoAuthHandler();

            services.AddHttpClient<IAuthTokenManagementClient, ServerAuthTokenManagementClient>((sp, http) => {
                    var opts = sp.GetRequiredService<IOptions<LyoAuthClientOptions>>().Value;
                    ArgumentHelpers.ThrowIfNullOrWhiteSpace(opts.AuthBaseUrl, "LyoAuthClientOptions.AuthBaseUrl");
                    http.BaseAddress = new(opts.AuthBaseUrl);
                })
                .AddLyoAuthHandler();

            return services;
        }
    }
}