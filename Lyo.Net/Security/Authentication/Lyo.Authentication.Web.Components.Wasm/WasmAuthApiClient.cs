using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Authentication.Models.Records;
using Lyo.Common.Core.Extensions;
using Lyo.Exceptions;
namespace Lyo.Authentication.Web.Components.Wasm;

/// <summary>Typed <see cref="HttpClient" /> wrapper for API <c>/auth/handoff/exchange</c>, <c>/auth/refresh</c>, and <c>/auth/logout</c> used by the WASM runtime.</summary>
public sealed class WasmAuthApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private readonly HttpClient _http;

    /// <summary>Exposes the bound options so callers (handoff page, sign-in launcher) can read <see cref="WasmAuthClientOptions.AuthBaseUrl" /> without resolving again.</summary>
    public WasmAuthClientOptions Options { get; }

    /// <summary>Builds a new client.</summary>
    public WasmAuthApiClient(HttpClient http, WasmAuthClientOptions options)
    {
        ArgumentHelpers.ThrowIfNull(http);
        ArgumentHelpers.ThrowIfNull(options);
        _http = http;
        Options = options;
    }

    /// <summary>POSTs <c>{ code }</c> to <c>/auth/handoff/exchange</c>. Returns issued tokens on success, <c>null</c> otherwise.</summary>
    public async Task<LyoOAuthTokenResponse?> ExchangeHandoffAsync(string handoffCode, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(handoffCode);
        using var response = await _http.PostAsJsonAsync("/auth/handoff/exchange", new { code = handoffCode }, JsonOptions, ct).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK)
            return null;

        return await response.Content.ReadFromJsonAsync<LyoOAuthTokenResponse>(JsonOptions, ct).ConfigureAwait(false);
    }

    /// <summary>POSTs <c>{ refresh_token }</c> to <c>/auth/refresh</c>. Returns rotated tokens on success, <c>null</c> otherwise.</summary>
    public async Task<LyoOAuthTokenResponse?> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(refreshToken);
        using var response = await _http.PostAsJsonAsync("/auth/refresh", new { refresh_token = refreshToken }, JsonOptions, ct).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK)
            return null;

        return await response.Content.ReadFromJsonAsync<LyoOAuthTokenResponse>(JsonOptions, ct).ConfigureAwait(false);
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