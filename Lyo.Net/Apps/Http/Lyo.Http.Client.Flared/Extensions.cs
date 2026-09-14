using FlareSolverrSharp;
using Lyo.Exceptions;
using Lyo.Http.Client;
using Lyo.Http.Client.Session;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Http.Client.Flared;

/// <summary>DI for the named Flared client (<see cref="FlaredHttpOptions.HttpClientName" />).</summary>
public static class Extensions
{
    /// <param name="services">DI collection being extended.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers the named Flared HttpClient (ThroughSolver by default). Returns the builder so hosts can chain handlers.</summary>
        public IHttpClientBuilder AddFlaredHttpClient(Action<FlaredHttpOptions>? configure = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            var options = new FlaredHttpOptions();
            configure?.Invoke(options);
            return services.AddFlaredHttpClient(options);
        }

        /// <summary>Registers Flared from a ready options instance.</summary>
        public IHttpClientBuilder AddFlaredHttpClient(FlaredHttpOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.TryAddSingleton(Options.Create(options));
            services.TryAddSingleton(options);
            services.TryAddScoped<IFlareSession, FlareSession>();
            services.TryAddScoped<ILyoHttpSession>(sp => sp.GetRequiredService<IFlareSession>());

            var builder = services.AddLyoHttpClient<LyoHttpClient, FlaredHttpOptions>(
                options,
                (sp, http, resolved) => new LyoHttpClient(
                    http, resolved, sp.GetService<ILogger<LyoHttpClient>>(), session: sp.GetService<IFlareSession>() ?? sp.GetService<ILyoHttpSession>()),
                FlaredHttpOptions.HttpClientName);

            if (options.FetchMode == FlaredFetchMode.ReplayWithClearanceHandler) {
                builder.ConfigurePrimaryHttpMessageHandler(() => {
                    var clearance = new ClearanceHandler(options.FlareSolverrUrl) { MaxTimeout = options.MaxTimeoutMs };
                    if (!string.IsNullOrWhiteSpace(options.ProxyUrl))
                        clearance.ProxyUrl = options.ProxyUrl;
                    clearance.ProxyUsername = options.ProxyUsername;
                    clearance.ProxyPassword = options.ProxyPassword;
                    clearance.InnerHandler = new LyoHttpClientHandler(options);
                    return clearance;
                });
            }
            else {
                // ThroughSolver: decompress primary, then Flared maps to /v1. New handler instance each factory call.
                builder.AddHttpMessageHandler(sp => new FlaredHttpMessageHandler(sp.GetRequiredService<IOptions<FlaredHttpOptions>>().Value));
            }

            return builder;
        }

        /// <summary>Binds <see cref="FlaredHttpOptions" /> from configuration.</summary>
        public IHttpClientBuilder AddFlaredHttpClientFromConfiguration(IConfiguration configuration, string sectionName = FlaredHttpOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new FlaredHttpOptions();
            configuration.GetSection(sectionName).Bind(options);
            return services.AddFlaredHttpClient(options);
        }
    }
}
