using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyo.Authentication.Web.Components.Abstractions;
using Lyo.Authentication.Web.Components.Models;
using Lyo.Exceptions;

namespace Lyo.Authentication.Web.Components.Wasm;

/// <summary>
/// WASM-side <see cref="IAuthTokenManagementClient" />. Wraps a typed <see cref="HttpClient" /> whose pipeline contains <see cref="WasmAuthDelegatingHandler" />, so requests
/// automatically carry the bearer and refresh on 401.
/// </summary>
public sealed class WasmAuthTokenManagementClient : IAuthTokenManagementClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    /// <summary>Creates a new client.</summary>
    public WasmAuthTokenManagementClient(HttpClient http)
    {
        ArgumentHelpers.ThrowIfNull(http);
        _http = http;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuthTokenKindDescriptor>?> ListKindsAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync("/tokens/kinds", ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<AuthTokenKindDescriptor[]>(JsonOptions, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuthTokenSummary>?> ListAsync(bool includeRevoked = false, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"/tokens?includeRevoked={(includeRevoked ? "true" : "false")}", ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<AuthTokenSummary[]>(JsonOptions, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AuthIssuedTokenResult?> CreateAsync(AuthIssueTokenRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        using var response = await _http.PostAsJsonAsync("/tokens", request, JsonOptions, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<AuthIssuedTokenResult>(JsonOptions, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> RevokeAsync(string tokenId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tokenId);
        using var response = await _http.DeleteAsync($"/tokens/{Uri.EscapeDataString(tokenId)}", ct).ConfigureAwait(false);
        return response.StatusCode == HttpStatusCode.NoContent;
    }
}