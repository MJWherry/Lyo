using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Api.Client;

/// <summary>Dependency-injection extensions for configuring HttpClientFactory for <see cref="ApiClient" />.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Adds a compression-aware HttpClientFactory configuration for ApiClient. Uses <see cref="ApiClientOptions" /> to set Accept-Encoding and automatic response decompression.</summary>
    public static IHttpClientBuilder AddLyoApiClient(
        this IServiceCollection services,
        string? clientName = null,
        Action<ApiClientOptions>? optionsOverride = null,
        Action<IHttpClientBuilder>? httpClientBuilderOverride = null)
    {
        clientName ??= nameof(IApiClient);
        services.AddOptions<ApiClientOptions>();
        if (optionsOverride != null)
            services.Configure(optionsOverride);

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

        httpClientBuilderOverride?.Invoke(builder);
        return builder;
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