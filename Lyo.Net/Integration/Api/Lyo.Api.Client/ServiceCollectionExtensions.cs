using System.Net;
using Lyo.Diagnostic.Correlation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Api.Client;

/// <summary>Dependency-injection extensions for configuring HttpClientFactory for <see cref="ApiClient" />.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a compression-aware HttpClientFactory configuration for ApiClient. Uses <see cref="ApiClientOptions" /> to set Accept-Encoding and automatic response decompression.
    /// When <paramref name="propagateCorrelationId"/> is <c>true</c> (the default), chains <see cref="LyoCorrelationDelegatingHandler"/> as the outermost handler so every outbound
    /// request carries the ambient correlation id; opt out by passing <c>false</c> for hosts that don't want stamping (e.g. when the caller already manages headers themselves).
    /// </summary>
    public static IHttpClientBuilder AddLyoApiClient(
        this IServiceCollection services,
        string? clientName = null,
        Action<ApiClientOptions>? optionsOverride = null,
        Action<IHttpClientBuilder>? httpClientBuilderOverride = null,
        bool propagateCorrelationId = true)
    {
        clientName ??= nameof(IApiClient);
        services.AddOptions<ApiClientOptions>();
        if (optionsOverride != null)
            services.Configure(optionsOverride);

        if (propagateCorrelationId)
            services.AddLyoCorrelationHandlerCore();

        var builder = services.AddHttpClient<IApiClient, ApiClient>(clientName)
            .ConfigureHttpClient((provider, client) => {
                var options = provider.GetRequiredService<IOptions<ApiClientOptions>>().Value;
                ApplyAcceptEncodingHeaders(client, options.AcceptEncodings);
            })
            .ConfigurePrimaryHttpMessageHandler(provider => {
                var options = provider.GetRequiredService<IOptions<ApiClientOptions>>().Value;
                var handler = new HttpClientHandler();
                if (options.EnableAutoResponseDecompression)
                    handler.AutomaticDecompression = ToDecompressionMethods(options.AcceptEncodings);

                return handler;
            });

        if (propagateCorrelationId)
            builder.AddHttpMessageHandler<LyoCorrelationDelegatingHandler>();

        httpClientBuilderOverride?.Invoke(builder);
        return builder;
    }

    /// <summary>
    /// Registers the <see cref="LyoCorrelationDelegatingHandler"/> transient + <see cref="CorrelationHandlerOptions"/> + an <see cref="AmbientCorrelationIdResolver"/> fallback if
    /// no <see cref="ICorrelationIdResolver"/> is already registered. Idempotent (uses <c>TryAdd</c>), so hosts that wire <c>AddLyoDiagnosticsWeb</c> (which registers the
    /// HTTP-aware resolver via the same <c>TryAdd</c>) keep their resolver.
    /// </summary>
    internal static IServiceCollection AddLyoCorrelationHandlerCore(this IServiceCollection services)
    {
        services.AddOptions<CorrelationHandlerOptions>();
        services.TryAddSingleton<ICorrelationIdResolver>(_ => AmbientCorrelationIdResolver.Instance);
        services.TryAddTransient<LyoCorrelationDelegatingHandler>(sp => new(
            sp.GetRequiredService<ICorrelationIdResolver>(),
            sp.GetService<IOptions<CorrelationHandlerOptions>>()?.Value));
        return services;
    }

    internal static void ApplyAcceptEncodingHeaders(HttpClient client, IEnumerable<string>? encodings)
    {
        if (encodings == null)
            return;

        foreach (var encoding in encodings.Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => i.Trim().ToLowerInvariant()).Distinct()) {
            if (!IsSupportedResponseEncoding(encoding))
                continue;

            if (client.DefaultRequestHeaders.AcceptEncoding.All(i => !string.Equals(i.Value, encoding, StringComparison.OrdinalIgnoreCase)))
                client.DefaultRequestHeaders.AcceptEncoding.Add(new(encoding));
        }
    }

    internal static DecompressionMethods ToDecompressionMethods(IEnumerable<string>? encodings)
    {
        var methods = DecompressionMethods.None;
        if (encodings == null)
            return methods;

        foreach (var raw in encodings) {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var encoding = raw.Trim().ToLowerInvariant();
            if (encoding == "gzip")
                methods |= DecompressionMethods.GZip;
            else if (encoding == "deflate")
                methods |= DecompressionMethods.Deflate;
#if !NETSTANDARD2_0
            else if (encoding == "br")
                methods |= DecompressionMethods.Brotli;
#endif
        }

        return methods;
    }

    internal static bool IsSupportedResponseEncoding(string encoding)
    {
        if (encoding is "gzip" or "deflate")
            return true;
#if !NETSTANDARD2_0
        if (encoding == "br")
            return true;
#endif
        return false;
    }
}