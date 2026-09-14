using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Common.Core.Extensions;
using Lyo.Common.Core.Net;
using Lyo.Config.Api.Models;
using Lyo.Exceptions;
using Lyo.Http.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Config.Api.Client;

/// <summary>HTTP client for conditional app-config GETs (see <see cref="AppConfigEntity" />).</summary>
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

/// <summary>Typed client for the central Config API, including ETag and <c>?version</c> polling.</summary>
public sealed class ConfigApiClient : ApiClient, IConfigApiClient
{
    private static readonly JsonSerializerOptions ConfigDeserialize = new(ConfigJsonSerializerOptions.Default);

    private readonly ConfigApiClientOptions _options;

    public ConfigApiClient(HttpClient httpClient, ConfigApiClientOptions options, JsonSerializerOptions? serializerOptions = null)
        : base(null, httpClient, serializerOptions ?? new JsonSerializerOptions(ConfigJsonSerializerOptions.Default), options)
        => _options = options;

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

        if (!AppConfigEntity.TryCreate(appKind, appId, out var _, out var errMsg))
            throw new ArgumentException(errMsg);

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

        if (!response.IsSuccessStatusCode) {
            if (_options.EnsureStatusCode)
                response.EnsureSuccessStatusCode();

            return new(ConfigResolveOutcome.Failed, etag, null, new((int)response.StatusCode, response.ReasonPhrase ?? string.Empty));
        }

        if (headOnly || response.StatusCode == HttpStatusCode.NoContent)
            return new(ConfigResolveOutcome.Ok, etag, null);

#if NETSTANDARD2_0
        ResolvedConfigRecord? resolvedDeserialized;
        using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            resolvedDeserialized = await JsonSerializer.DeserializeAsync<ResolvedConfigRecord>(stream, ConfigDeserialize).ConfigureAwait(false);
#else
        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var resolvedDeserialized = await JsonSerializer.DeserializeAsync<ResolvedConfigRecord>(stream, ConfigDeserialize, ct).ConfigureAwait(false);
#endif
        OperationHelpers.ThrowIfNull(resolvedDeserialized, "Resolved config deserialization returned null.");
        return new(ConfigResolveOutcome.Ok, etag, resolvedDeserialized);
    }

    internal static void ApplyApiKey(HttpRequestHeaders headers, string? apiKey)
    {
        headers.Remove(LyoHttpHeaders.ApiKey);
        if (!apiKey.IsNullOrEmpty())
            headers.Add(LyoHttpHeaders.ApiKey, apiKey.Trim());
    }
}

/// <remarks>Hosts should call <see cref="AddConfigApiClientFromConfiguration" /> rather than wiring this by hand.</remarks>
public static class ConfigApiHttpClientRegistration
{
    extension(IServiceCollection services)
    {
        public IHttpClientBuilder AddConfigApiClientFromConfiguration(
            IConfiguration configuration,
            string configSectionName = ConfigApiClientOptions.SectionName)
        {
            var options = new ConfigApiClientOptions();
            configuration.GetSection(configSectionName).Bind(options);
            options.Validate();

            services.TryAddSingleton(Options.Create(options));
            services.TryAddSingleton(options);
            var builder = services.AddHttpClient<IConfigApiClient, ConfigApiClient>(client => {
                if (!string.IsNullOrWhiteSpace(options.BaseUrl))
                    client.BaseAddress = new(options.BaseUrl!.TrimEnd('/') + "/");

                ConfigApiClient.ApplyApiKey(client.DefaultRequestHeaders, options.ApiKey);
                LyoHttpClient.ApplyAcceptEncodingHeaders(client, options.AcceptEncodings);
            });
            return Extensions.UseLyoHttpClientHandler<ConfigApiClientOptions>(builder);
        }

        /// <summary>
        /// Registers scoped <see cref="IConfigStore" /> as <see cref="ConfigApiStore" /> on the host <see cref="IApiClient" />. Meant for TestGateway so writes reach TestApi
        /// (encryption lives there). Do not add encryption on the workbench host.
        /// </summary>
        public IServiceCollection AddConfigApiStore()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddScoped<IConfigStore, ConfigApiStore>();
            return services;
        }
    }
}