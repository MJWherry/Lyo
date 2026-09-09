using System.Net;
using System.Reflection;
using System.Text.Json;
using Lyo.Api.Models.Error;
using Lyo.Common.Metadata.Records;
using Lyo.Http.Client;
using Lyo.Query.Models.Common.Request;
using Microsoft.Extensions.Logging;

namespace Lyo.Api.Client;

/// <summary>HTTP client for Lyo minimal APIs. Non-success responses throw <see cref="ApiException" /> with problem-details when the body parses.</summary>
public class ApiClient : LyoHttpClient, IApiClient
{
    /// <summary>Creates an <see cref="ApiClient" />. Prefer resolving it from <c>IHttpClientFactory</c> so the handler lifetime stays correct.</summary>
    public ApiClient(ILogger? logger = null, HttpClient? httpClient = null, JsonSerializerOptions? serializerOptions = null, LyoHttpClientOptions? options = null)
        : base(logger, httpClient, serializerOptions, options) { }

    /// <inheritdoc />
    protected override void ApplyDefaultHeaders()
    {
        var version = typeof(ApiClient).Assembly.GetName().Version?.ToString(3) ?? "1.0";
        HttpClient.DefaultRequestHeaders.Remove(HttpHeaderInfo.UserAgent);
        HttpClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpHeaderInfo.UserAgent, $"Lyo/{version}");
    }

    /// <inheritdoc />
    protected override async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        if (!BaseOptions.EnsureStatusCode)
            return;

        LyoProblemDetails? problemDetails = null;
        var message = $"Request failed: {(int)response.StatusCode} {response.ReasonPhrase}";
        if (response.Content.Headers.ContentType?.MediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) != true)
            throw new ApiException((int)response.StatusCode, message, problemDetails);

        try {
            var json = await ReadDecodedResponseStringAsync(response, ct).ConfigureAwait(false);
            problemDetails = JsonSerializer.Deserialize<LyoProblemDetails>(json, SerializerOptions);
            if (problemDetails != null) {
                var full = problemDetails.GetFullMessage();
                if (!string.IsNullOrEmpty(full))
                    message = full;
            }
        }
        catch {
            // Keep the generic message when the body cannot be parsed
        }

        throw new ApiException((int)response.StatusCode, message, problemDetails) { ErrorCode = problemDetails?.Errors.FirstOrDefault()?.Code };
    }

    /// <inheritdoc />
    public Task<TResult?> QueryProjectAsync<TResult>(string route, ProjectionQueryReq request, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => PostAsAsync<ProjectionQueryReq, TResult?>(QueryPath(route, "QueryProject"), request, before, ct)!;

    /// <inheritdoc />
    public Task<TResult?> QueryConcreteAsync<TResult>(string route, QueryConcreteReq request, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => PostAsAsync<QueryConcreteReq, TResult?>(QueryPath(route, "QueryConcrete"), request, before, ct)!;

    /// <summary>Joins <paramref name="route" /> and a Query/QueryProject suffix.</summary>
    public static string QueryPath(string route, string suffix)
    {
        var trimmed = (route ?? string.Empty).Trim().Trim('/');
        return trimmed.Length == 0 ? suffix : $"{trimmed}/{suffix}";
    }
}
