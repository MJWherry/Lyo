using System.Net;
using Lyo.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Comic.Api.Client;

/// <summary>Registers <see cref="IComicApiClient" /> with HTTP client defaults.</summary>
public static class ComicApiClientExtensions
{
    public static IServiceCollection AddComicApiClientFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = ComicApiClientOptions.SectionName)
    {
        var options = new ComicApiClientOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        services.AddSingleton(Options.Create(options));
        services.AddSingleton(p => p.GetRequiredService<IOptions<ComicApiClientOptions>>().Value);
        services.TryAddSingleton(_ => LyoJsonSerializerOptions.Create());
        services.AddHttpClient<IComicApiClient, ComicApiClient>()
            .ConfigureHttpClient(client => {
                var baseUrl = options.BaseUrl ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(baseUrl))
                    client.BaseAddress = new($"{baseUrl.TrimEnd('/')}/", UriKind.Absolute);

                foreach (var enc in options.AcceptEncodings.Select(e => e.Trim().ToLowerInvariant()).Distinct()) {
#if NETSTANDARD2_0
                    if (enc is not ("gzip" or "deflate"))
                        continue;
#else
                    if (enc is not ("gzip" or "deflate" or "br"))
                        continue;
#endif
                    if (client.DefaultRequestHeaders.AcceptEncoding.All(h => !string.Equals(h.Value, enc, StringComparison.OrdinalIgnoreCase)))
                        client.DefaultRequestHeaders.AcceptEncoding.Add(new(enc));
                }
            })
            .ConfigurePrimaryHttpMessageHandler(() => {
                var handler = new HttpClientHandler();
                if (!options.EnableAutoResponseDecompression)
                    return handler;

                var methods = DecompressionMethods.None;
                foreach (var enc in options.AcceptEncodings) {
                    if (enc.Equals("gzip", StringComparison.OrdinalIgnoreCase))
                        methods |= DecompressionMethods.GZip;

                    if (enc.Equals("deflate", StringComparison.OrdinalIgnoreCase))
                        methods |= DecompressionMethods.Deflate;

#if !NETSTANDARD2_0
                    if (enc.Equals("br", StringComparison.OrdinalIgnoreCase))
                        methods |= DecompressionMethods.Brotli;
#endif
                }

                handler.AutomaticDecompression = methods;
                return handler;
            });

        return services;
    }
}