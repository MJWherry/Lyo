using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Common.Extensions;
using Lyo.Exceptions;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.Web.Components.Wasm;

/// <summary>Typed <see cref="HttpClient" /> wrapper for the API's <c>/auth/handoff/exchange</c>, <c>/auth/refresh</c>, and <c>/auth/logout</c> endpoints used by the WASM runtime.</summary>
public sealed class WasmAuthApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private readonly HttpClient _http;

    /// <summary>Exposes the bound options so callers (handoff page, sign-in launcher) can read <see cref="WasmAuthClientOptions.AuthBaseUrl" /> without re-resolving.</summary>
    public WasmAuthClientOptions Options { get; }

    /// <summary>Creates a new client.</summary>
    public WasmAuthApiClient(HttpClient http, IOptions<WasmAuthClientOptions> options)
    {
        ArgumentHelpers.ThrowIfNull(http);
        ArgumentHelpers.ThrowIfNull(options);
        _http = http;
        Options = options.Value;
    }

    /// <summary>POSTs <c>{ code }</c> to <c>/auth/handoff/exchange</c>. Returns the issued tokens on success, <c>null</c> otherwise.</summary>
    public async Task<WasmTokenResponse?> ExchangeHandoffAsync(string handoffCode, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(handoffCode);
        using var response = await _http.PostAsJsonAsync("/auth/handoff/exchange", new { code = handoffCode }, JsonOptions, ct).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK)
            return null;

        return await response.Content.ReadFromJsonAsync<WasmTokenResponse>(JsonOptions, ct).ConfigureAwait(false);
    }

    /// <summary>POSTs <c>{ refresh_token }</c> to <c>/auth/refresh</c>. Returns the rotated tokens on success, <c>null</c> otherwise.</summary>
    public async Task<WasmTokenResponse?> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(refreshToken);
        using var response = await _http.PostAsJsonAsync("/auth/refresh", new { refresh_token = refreshToken }, JsonOptions, ct).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK)
            return null;

        return await response.Content.ReadFromJsonAsync<WasmTokenResponse>(JsonOptions, ct).ConfigureAwait(false);
    }

    /// <summary>POSTs <c>{ refresh_token }</c> to <c>/auth/logout</c>. Best-effort: returns <c>true</c> on 2xx.</summary>
    public async Task<bool> LogoutAsync(string? refreshToken, CancellationToken ct = default)
    {
        if (refreshToken.IsNullOrWhitespace())
            return true;

        using var response = await _http.PostAsJsonAsync("/auth/logout", new { refresh_token = refreshToken }, JsonOptions, ct).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }
}

/// <summary>Mirror of the API's <c>TokenResponse</c> record. Snake-case JSON.</summary>
public sealed record WasmTokenResponse(
    [property: JsonPropertyName("access_token")]
    string AccessToken,
    [property: JsonPropertyName("expires_in")]
    int ExpiresIn,
    [property: JsonPropertyName("refresh_token")]
    string? RefreshToken,
    [property: JsonPropertyName("token_type")]
    string TokenType);