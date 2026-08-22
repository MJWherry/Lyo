using Lyo.Common.Extensions;
using Lyo.Diagnostic.Correlation;
using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Api.Client;

/// <summary>Dependency-injection extensions for configuring HttpClientFactory for <see cref="ApiClient" />.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a compression-aware HttpClientFactory configuration for ApiClient. Uses <see cref="ApiClientOptions" /> to set Accept-Encoding and automatic response decompression.
    /// When <paramref name="propagateCorrelationId" /> is <c>true</c> (the default), chains <see cref="LyoCorrelationDelegatingHandler" /> as the outermost handler so every outbound
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
                if (!options.BaseUrl.IsNullOrWhitespace())
                    client.BaseAddress = new(options.BaseUrl!.TrimEnd('/') + "/");
            })
            .UseLyoHttpClientHandler();

        if (propagateCorrelationId)
            builder.AddHttpMessageHandler<LyoCorrelationDelegatingHandler>();

        httpClientBuilderOverride?.Invoke(builder);
        return builder;
    }

    /// <summary>
    /// Registers the <see cref="LyoCorrelationDelegatingHandler" /> transient + <see cref="CorrelationHandlerOptions" /> + an <see cref="AmbientCorrelationIdResolver" />
    /// fallback if no <see cref="ICorrelationIdResolver" /> is already registered. Idempotent (uses <c>TryAdd</c>), so hosts that wire <c>AddLyoDiagnosticsWeb</c> (which registers the
    /// HTTP-aware resolver via the same <c>TryAdd</c>) keep their resolver.
    /// </summary>
    internal static IServiceCollection AddLyoCorrelationHandlerCore(this IServiceCollection services)
    {
        services.AddOptions<CorrelationHandlerOptions>();
        services.TryAddSingleton<ICorrelationIdResolver>(_ => AmbientCorrelationIdResolver.Instance);
        services.TryAddTransient<LyoCorrelationDelegatingHandler>(sp => new(
            sp.GetRequiredService<ICorrelationIdResolver>(), sp.GetService<IOptions<CorrelationHandlerOptions>>()?.Value));

        return services;
    }

    /// <summary>
    /// Sets the primary handler to a new <see cref="LyoHttpClientHandler" /> built from <see cref="ApiClientOptions" />.
    /// A later <c>ConfigurePrimaryHttpMessageHandler</c> call replaces this handler and drops auto-decompress unless the replacement sets it.
    /// </summary>
    public static IHttpClientBuilder UseLyoHttpClientHandler(this IHttpClientBuilder builder) => builder.UseLyoHttpClientHandler<ApiClientOptions>();

    /// <summary>
    /// Sets the primary handler to a new <see cref="LyoHttpClientHandler" /> built from <typeparamref name="TOptions" /> in DI.
    /// Register as factory-created (the default here), never a singleton. <typeparamref name="TOptions" /> must be available as <see cref="IOptions{TOptions}" />.
    /// </summary>
    public static IHttpClientBuilder UseLyoHttpClientHandler<TOptions>(this IHttpClientBuilder builder)
        where TOptions : ApiClientOptions
    {
        ArgumentHelpers.ThrowIfNull(builder);
        return builder.ConfigurePrimaryHttpMessageHandler(provider => {
            var options = provider.GetRequiredService<IOptions<TOptions>>().Value;
            return new LyoHttpClientHandler(options);
        });
    }

    /// <summary>Adds supported values from <paramref name="encodings" /> to <see cref="HttpClient.DefaultRequestHeaders" /> <c>Accept-Encoding</c>.</summary>
    public static void ApplyAcceptEncodingHeaders(HttpClient client, IEnumerable<string>? encodings)
    {
        ArgumentHelpers.ThrowIfNull(client);
        if (encodings == null)
            return;

        foreach (var encoding in encodings.Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => i.Trim().ToLowerInvariant()).Distinct()) {
            if (!LyoHttpClientHandler.IsSupportedResponseEncoding(encoding))
                continue;

            if (client.DefaultRequestHeaders.AcceptEncoding.All(i => !string.Equals(i.Value, encoding, StringComparison.OrdinalIgnoreCase)))
                client.DefaultRequestHeaders.AcceptEncoding.Add(new(encoding));
        }
    }
}