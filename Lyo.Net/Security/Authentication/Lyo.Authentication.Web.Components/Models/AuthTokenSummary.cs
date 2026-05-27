using System;
using System.Text.Json.Serialization;

namespace Lyo.Authentication.Web.Components.Models;

/// <summary>
/// Display-safe projection of an issued token, mirroring the shape returned by <c>GET /tokens</c> and the <c>Record</c> field of <c>POST /tokens</c>. No secret material — only what's
/// safe to render in a list.
/// </summary>
/// <param name="Id">The 11-character Crockford base32 id (also the second-to-last segment of the wire-form token).</param>
/// <param name="Kind">One of <c>pat</c> / <c>svc</c> / <c>cli</c> / <c>webhook</c>.</param>
/// <param name="Ring">The ring this token was minted into (e.g. <c>live</c>, <c>test</c>).</param>
/// <param name="DisplayName">User-facing label.</param>
/// <param name="Scopes">Snapshotted scopes at issuance.</param>
/// <param name="CreatedAt">When the token was issued.</param>
/// <param name="ExpiresAt">When the token expires, or <c>null</c> for "no expiry".</param>
/// <param name="LastUsedAt">Best-effort last-validated timestamp.</param>
/// <param name="RevokedAt">When the token was revoked, or <c>null</c> if still active.</param>
/// <param name="RevokedReason">Audit string captured at revocation.</param>
public sealed record AuthTokenSummary(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("ring")] string Ring,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("scopes")] string[] Scopes,
    [property: JsonPropertyName("createdAt")] DateTime CreatedAt,
    [property: JsonPropertyName("expiresAt")] DateTime? ExpiresAt,
    [property: JsonPropertyName("lastUsedAt")] DateTime? LastUsedAt,
    [property: JsonPropertyName("revokedAt")] DateTime? RevokedAt,
    [property: JsonPropertyName("revokedReason")] string? RevokedReason)
{
    /// <summary>Convenience for the UI: true when the token is still good (not revoked, not expired).</summary>
    public bool IsActive(DateTime now) =>
        !RevokedAt.HasValue && (!ExpiresAt.HasValue || ExpiresAt.Value > now);
}
