using Lyo.Authentication.Web.Components.Models;

namespace Lyo.Authentication.Web.Components.Abstractions;

/// <summary>
/// Host-agnostic HTTP wrapper for the <c>/tokens</c> management endpoints in <c>Lyo.Authentication.AspNetCore</c>. The host adapter is responsible for attaching the bearer
/// token (Server-side via <c>LyoAuthDelegatingHandler</c>, WASM-side via <see cref="Wasm.WasmAuthDelegatingHandler" />) so that callers only need to know about the typed methods
/// below.
/// </summary>
public interface IAuthTokenManagementClient
{
    /// <summary>Returns the kinds of tokens this API understands, along with whether the current caller is permitted to mint each kind. Returns <c>null</c> on any non-2xx.</summary>
    Task<IReadOnlyList<AuthTokenKindDescriptor>?> ListKindsAsync(CancellationToken ct = default);

    /// <summary>Returns the caller's tokens. When <paramref name="includeRevoked" /> is <c>true</c>, includes tokens whose <c>RevokedAt</c> is set. Returns <c>null</c> on any non-2xx.</summary>
    Task<IReadOnlyList<AuthTokenSummary>?> ListAsync(bool includeRevoked = false, CancellationToken ct = default);

    /// <summary>
    /// Mints a new token. Returns <c>null</c> when the API responds non-2xx (missing scope, bad kind, no grantable scopes, etc.); the caller should treat that as failure and
    /// reload the token list to recover. The returned <see cref="AuthIssuedTokenResult.Plaintext" /> is shown to the user exactly once — never persist it client-side.
    /// </summary>
    Task<AuthIssuedTokenResult?> CreateAsync(AuthIssueTokenRequest request, CancellationToken ct = default);

    /// <summary>Revokes one of the caller's own tokens. Returns <c>true</c> on success (HTTP 204), <c>false</c> on 404 / 403 / 5xx.</summary>
    Task<bool> RevokeAsync(string tokenId, CancellationToken ct = default);
}