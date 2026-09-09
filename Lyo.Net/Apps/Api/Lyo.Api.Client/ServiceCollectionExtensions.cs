using Lyo.Diagnostic.Correlation;
using Lyo.Exceptions;
using Lyo.Http.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Api.Client;

/// <summary>DI helpers that wire HttpClientFactory for <see cref="ApiClient" /> (problem-details + correlation).</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds an HttpClientFactory setup for ApiClient. Reads <see cref="ApiClientOptions" /> for Accept-Encoding and automatic response decompression.
    /// When <paramref name="optionsOverride" /> is omitted, a host <c>Configure&lt;ApiClientOptions&gt;</c> bind (for example <c>ApiClient:BaseUrl</c>) is used.
    /// When <paramref name="propagateCorrelationId" /> is <c>true</c> (the default), <see cref="LyoCorrelationDelegatingHandler" /> sits outermost so every outbound
    /// request carries the ambient correlation id. Pass <c>false</c> if the host already stamps headers itself.
    /// </summary>
    public static IHttpClientBuilder AddLyoApiClient(
        this IServiceCollection services,
        string? clientName = null,
        Action<ApiClientOptions>? optionsOverride = null,
        Action<IHttpClientBuilder>? httpClientBuilderOverride = null,
        bool propagateCorrelationId = true)
    {
        clientName ??= nameof(IApiClient);
        var builder = services.AddLyoHttpClient<IApiClient, ApiClientOptions>(
            optionsOverride,
            (sp, http, resolved) => new ApiClient(sp.GetService<Microsoft.Extensions.Logging.ILogger<ApiClient>>(), http, null, resolved),
            clientName);
        builder.AddTypedClient<ApiClient>((http, sp) => new ApiClient(
            sp.GetService<Microsoft.Extensions.Logging.ILogger<ApiClient>>(),
            http,
            null,
            sp.GetRequiredService<IOptions<ApiClientOptions>>().Value));

        if (propagateCorrelationId) {
            services.AddLyoCorrelationHandlerCore();
            builder.AddHttpMessageHandler<LyoCorrelationDelegatingHandler>();
        }

        httpClientBuilderOverride?.Invoke(builder);
        return builder;
    }

    /// <summary>
    /// Adds transient <see cref="LyoCorrelationDelegatingHandler" />, <see cref="CorrelationHandlerOptions" />, and an <see cref="AmbientCorrelationIdResolver" />
    /// fallback when no <see cref="ICorrelationIdResolver" /> is registered. Uses <c>TryAdd</c>, so a host that already called <c>AddLyoDiagnosticsWeb</c> (same
    /// <c>TryAdd</c> for the HTTP-aware resolver) keeps that resolver.
    /// </summary>
    internal static IServiceCollection AddLyoCorrelationHandlerCore(this IServiceCollection services)
    {
        services.AddOptions<CorrelationHandlerOptions>();
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<CorrelationHandlerOptions>>().Value);
        services.TryAddSingleton<ICorrelationIdResolver>(_ => AmbientCorrelationIdResolver.Instance);
        services.TryAddTransient<LyoCorrelationDelegatingHandler>(sp => new(
            sp.GetRequiredService<ICorrelationIdResolver>(), sp.GetService<IOptions<CorrelationHandlerOptions>>()?.Value));

        return services;
    }

    /// <summary>Sets the primary handler to a new <see cref="LyoHttpClientHandler" /> from <see cref="ApiClientOptions" />.</summary>
    [Obsolete("Use Lyo.Http.Client.Extensions.UseLyoHttpClientHandler.")]
    public static IHttpClientBuilder UseLyoHttpClientHandler(this IHttpClientBuilder builder) => builder.UseLyoHttpClientHandler<ApiClientOptions>();

    /// <summary>Sets the primary handler from <typeparamref name="TOptions" />.</summary>
    [Obsolete("Use Lyo.Http.Client.Extensions.UseLyoHttpClientHandler.")]
    public static IHttpClientBuilder UseLyoHttpClientHandler<TOptions>(this IHttpClientBuilder builder)
        where TOptions : LyoHttpClientOptions
        => Lyo.Http.Client.Extensions.UseLyoHttpClientHandler<TOptions>(builder);

    /// <summary>Copies supported values from <paramref name="encodings" /> onto Accept-Encoding.</summary>
    [Obsolete("Use Lyo.Http.Client.LyoHttpClient.ApplyAcceptEncodingHeaders.")]
    public static void ApplyAcceptEncodingHeaders(HttpClient client, IEnumerable<string>? encodings)
        => LyoHttpClient.ApplyAcceptEncodingHeaders(client, encodings);
}
