namespace Lyo.Authentication.OpenIdConnect.Handoff;

/// <summary>
/// A single-use, short-TTL code that lets the API hand off a freshly issued JWT (plus rotating refresh token) to a trusted consumer origin without putting tokens into a URL
/// fragment or a cross-origin cookie. Only the code id travels in the redirect URL; tokens live in <see cref="IHandoffCodeStore" /> server-side until the
/// consumer POSTs them back to <c>/auth/handoff/exchange</c>.
/// </summary>
/// <param name="Id">Wire identifier. Format <c>lyoh_&lt;base64url(16 random bytes)&gt;</c>. Single-use; consuming it removes it from the store.</param>
/// <param name="AccessToken">Freshly minted Lyo JWT (header.payload.signature).</param>
/// <param name="RefreshToken">Rotating refresh token (Format-B <c>internal</c> kind), or <c>null</c> if the issuer was not configured to mint one.</param>
/// <param name="AccessTokenExpiresAt">When <paramref name="AccessToken" /> expires.</param>
/// <param name="RefreshTokenExpiresAt">When <paramref name="RefreshToken" /> expires (or <c>null</c>).</param>
/// <param name="IssuedTo">
/// Exact origin (<c>scheme://host[:port]</c>) the API will accept on the consumer <c>Origin</c> request header when the code is exchanged. Locks the code
/// to one frontend.
/// </param>
/// <param name="CreatedAt">Server-side issuance timestamp.</param>
public sealed record LyoHandoffCode(
    string Id,
    string AccessToken,
    string? RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime? RefreshTokenExpiresAt,
    string IssuedTo,
    DateTime CreatedAt);