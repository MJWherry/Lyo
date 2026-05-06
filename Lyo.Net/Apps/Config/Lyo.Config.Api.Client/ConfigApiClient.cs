using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Config.Api.Models;
using Lyo.Common.Extensions;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Config.Api.Client;

/// <summary>HTTP client surface for conditional app-config reads (see <see cref="AppConfigEntity" />).</summary>
public interface IConfigApiClient : IApiClient
{
    Task<ConfigResolveConditionalResult> ResolveForAppAsync(
        string appKind,
        string appId,
        string? ifNoneMatch = null,
        string? version = null,
        bool headOnly = false,
        CancellationToken ct = default);
}

/// <summary>Typed HTTP client for the central Config API (ETag / <c>?version</c> polling).</summary>
public sealed class ConfigApiClient : ApiClient, IConfigApiClient
{
    private static readonly JsonSerializerOptions ConfigDeserialize = new(ConfigJsonSerializerOptions.Default);

    private readonly ConfigApiClientOptions _options;

    public ConfigApiClient(HttpClient httpClient, IOptions<ConfigApiClientOptions> options, JsonSerializerOptions? serializerOptions = null)
        : base(
            httpClient: httpClient,
            options: options.Value,
            serializerOptions: serializerOptions ?? new JsonSerializerOptions(ConfigJsonSerializerOptions.Default))
    {
        _options = options.Value;
    }

    internal static void ApplyApiKey(HttpRequestHeaders headers, string? apiKey)
    {
        headers.Remove("X-Api-Key");
        if (!apiKey.IsNullOrEmpty())
            headers.Add("X-Api-Key", apiKey.Trim());
    }

    /// <inheritdoc />
    public async Task<ConfigResolveConditionalResult> ResolveForAppAsync(
        string appKind,
        string appId,
        string? ifNoneMatch = null,
        string? version = null,
        bool headOnly = false,
        CancellationToken ct = default)
    {
        if (HttpClient.BaseAddress == null)
            UriHelpers.ThrowIfInvalidAbsoluteUri($"api/config/{appKind}/{appId}");

        if (!AppConfigEntity.TryCreate(appKind, appId, out _, out var errMsg))
            throw new ArgumentException(errMsg ?? "Invalid app route segments.");

        var trimmedVersion = version.OrDefault().Trim();
        var qp = trimmedVersion.Length == 0 ? string.Empty : $"?version={Uri.EscapeDataString(trimmedVersion)}";
        var uri = $"api/config/{Uri.EscapeDataString(appKind.Trim())}/{Uri.EscapeDataString(appId.Trim())}{qp}";
        using var request = new HttpRequestMessage(headOnly ? HttpMethod.Head : HttpMethod.Get, uri);
        ApplyApiKey(request.Headers, _options.ApiKey);
        if (!string.IsNullOrEmpty(ifNoneMatch))
            request.Headers.TryAddWithoutValidation("If-None-Match", ifNoneMatch);

        using var response = await HttpClient.SendAsync(request, ct).ConfigureAwait(false);
        var etag = response.Headers.ETag?.ToString();

        if (response.StatusCode == HttpStatusCode.NotModified)
            return new(ConfigResolveOutcome.NotModified, etag ?? ifNoneMatch, null);

        if (!response.IsSuccessStatusCode)
        {
            if (_options.EnsureStatusCode)
                response.EnsureSuccessStatusCode();

            return new(ConfigResolveOutcome.Failed, etag, null, new((int)response.StatusCode, response.ReasonPhrase ?? string.Empty));
        }

        if (headOnly || response.StatusCode == HttpStatusCode.NoContent)
            return new(ConfigResolveOutcome.Ok, etag, null);

#if NETSTANDARD2_0
        ResolvedConfigRecord? resolvedDeserialized;
        using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false)) {
            resolvedDeserialized =
                await JsonSerializer.DeserializeAsync<ResolvedConfigRecord>(stream, ConfigDeserialize).ConfigureAwait(false);
        }
#else
        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var resolvedDeserialized =
            await JsonSerializer.DeserializeAsync<ResolvedConfigRecord>(stream, ConfigDeserialize, ct).ConfigureAwait(false);
#endif

        OperationHelpers.ThrowIfNull(resolvedDeserialized, "Resolved config deserialization returned null.");
        return new ConfigResolveConditionalResult(ConfigResolveOutcome.Ok, etag, resolvedDeserialized);
    }
}

/// <remarks>Prefer <see cref="AddConfigApiClientFromConfiguration" /> from consuming hosts.</remarks>
public static class ConfigApiHttpClientRegistration
{
    public static IHttpClientBuilder AddConfigApiClientFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = ConfigApiClientOptions.SectionName)
    {
        var options = new ConfigApiClientOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        services.TryAddSingleton(Options.Create(options));

        return services.AddHttpClient<IConfigApiClient, ConfigApiClient>(client => {
                if (!string.IsNullOrWhiteSpace(options.BaseUrl))
                    client.BaseAddress = new(options.BaseUrl!.TrimEnd('/') + "/");

                ConfigApiClient.ApplyApiKey(client.DefaultRequestHeaders, options.ApiKey);

                foreach (
                    var enc in (options.AcceptEncodings ?? []).Select(e => e.Trim().ToLowerInvariant()).Where(e => e is "gzip" or "deflate" or "br").Distinct()) {
                    if (client.DefaultRequestHeaders.AcceptEncoding.All(h => !string.Equals(h.Value, enc, StringComparison.OrdinalIgnoreCase)))
                        client.DefaultRequestHeaders.AcceptEncoding.Add(new(enc));
                }
            })
            .ConfigurePrimaryHttpMessageHandler(() => {
                var handler = new HttpClientHandler();
                if (!options.EnableAutoResponseDecompression)
                    return handler;

                var methods = DecompressionMethods.None;
                foreach (var enc in options.AcceptEncodings ?? []) {
                    if (string.Equals(enc, "gzip", StringComparison.OrdinalIgnoreCase))
                        methods |= DecompressionMethods.GZip;
                    else if (string.Equals(enc, "deflate", StringComparison.OrdinalIgnoreCase))
                        methods |= DecompressionMethods.Deflate;
#if !NETSTANDARD2_0
                    else if (string.Equals(enc, "br", StringComparison.OrdinalIgnoreCase))
                        methods |= DecompressionMethods.Brotli;
#endif
                }

                handler.AutomaticDecompression = methods;
                return handler;
            });
    }
}
