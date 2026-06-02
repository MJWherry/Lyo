using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Common.Extensions;
using Lyo.Exceptions;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.Client;

/// <summary>Typed <see cref="HttpClient" /> wrapper for the API's <c>/auth/handoff/exchange</c>, <c>/auth/refresh</c>, and <c>/auth/logout</c> endpoints.</summary>
public sealed class LyoAuthApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private readonly HttpClient _http;

    /// <summary>
    /// The configured consumer-side options. Exposed so callers (e.g. the handoff middleware) can read <see cref="LyoAuthClientOptions.AuthBaseUrl" /> without re-resolving the
    /// options.
    /// </summary>
    public LyoAuthClientOptions Options { get; }

    /// <summary>Creates a new client.</summary>
    public LyoAuthApiClient(HttpClient http, IOptions<LyoAuthClientOptions> options)
    {
        ArgumentHelpers.ThrowIfNull(http);
        ArgumentHelpers.ThrowIfNull(options);
        _http = http;
        Options = options.Value;
    }

    /// <summary>
    /// POSTs a handoff code to <c>/auth/handoff/exchange</c> with <paramref name="consumerOrigin" /> on the <c>Origin</c> header. Returns the issued tokens on success or
    /// <c>null</c> on any 4xx/5xx.
    /// </summary>
    /// <param name="handoffCode">The single-use code the API redirected the browser to (the <c>lyo_handoff</c> query value).</param>
    /// <param name="consumerOrigin">
    /// The consumer's own absolute origin (<c>scheme://host[:port]</c>) — i.e. the public origin of the host running this handoff middleware. Must match the
    /// origin the API stamped onto the code when it was issued (the API derives that from the <c>returnUrl</c> passed to <c>/auth/login/{provider}</c>). Using the API's base URL here is
    /// wrong and produces <c>400 invalid_or_consumed_code</c>; pass the value of <c>$"{ctx.Request.Scheme}://{ctx.Request.Host.Value}"</c> from the inbound request.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<LyoTokenResponse?> ExchangeHandoffAsync(string handoffCode, string consumerOrigin, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(handoffCode);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(consumerOrigin);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/handoff/exchange");
        request.Content = JsonContent.Create(new { code = handoffCode }, options: JsonOptions);
        request.Headers.TryAddWithoutValidation("Origin", consumerOrigin);
        request.Headers.TryAddWithoutValidation("X-Lyo-Caller-Origin", consumerOrigin);
        using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK)
            return null;

        return await response.Content.ReadFromJsonAsync<LyoTokenResponse>(JsonOptions, ct).ConfigureAwait(false);
    }

    /// <summary>POSTs <c>{"refresh_token": ...}</c> to <c>/auth/refresh</c> and returns the rotated tokens, or <c>null</c> if the refresh failed.</summary>
    public async Task<LyoTokenResponse?> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(refreshToken);
        using var response = await _http.PostAsJsonAsync("/auth/refresh", new { refresh_token = refreshToken }, JsonOptions, ct).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK)
            return null;

        return await response.Content.ReadFromJsonAsync<LyoTokenResponse>(JsonOptions, ct).ConfigureAwait(false);
    }

    /// <summary>POSTs <c>{"refresh_token": ...}</c> to <c>/auth/logout</c>. Best-effort — returns <c>true</c> on 2xx, <c>false</c> otherwise.</summary>
    public async Task<bool> LogoutAsync(string? refreshToken, CancellationToken ct = default)
    {
        if (refreshToken.IsNullOrWhitespace())
            return true;

        using var response = await _http.PostAsJsonAsync("/auth/logout", new { refresh_token = refreshToken }, JsonOptions, ct).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }
}

/// <summary>Mirror of the API's <c>TokenResponse</c> record. Snake-case JSON.</summary>
public sealed record LyoTokenResponse(
    [property: JsonPropertyName("access_token")]
    string AccessToken,
    [property: JsonPropertyName("expires_in")]
    int ExpiresIn,
    [property: JsonPropertyName("refresh_token")]
    string? RefreshToken,
    [property: JsonPropertyName("token_type")]
    string TokenType);