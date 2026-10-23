using System.Diagnostics;
using System.Security.Claims;

namespace Lyo.Authentication.Web.Components.Models;

/// <summary>Diagnostic snapshot of the active session via <c>IAuthSessionAccessor</c> for the debug page. Refresh token is never echoed — only its presence.</summary>
/// <param name="AccessToken">Current access JWT (visible in the UI).</param>
/// <param name="AccessTokenExpiresAt">When the access token expires.</param>
/// <param name="HasRefreshToken">Whether a refresh token is held server-side / in browser storage. The token itself is intentionally not exposed.</param>
/// <param name="RefreshTokenExpiresAt">When the refresh token expires, when known.</param>
/// <param name="Claims">Projected claims for the current principal.</param>
/// <param name="Scopes">Flat list of <c>scope</c> claim values for quick display.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record AuthSessionSnapshot(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    bool HasRefreshToken,
    DateTime? RefreshTokenExpiresAt,
    IReadOnlyList<Claim> Claims,
    IReadOnlyList<string> Scopes)
{
    /// <summary>Expiry and claim counts. The access token is omitted.</summary>
    public override string ToString()
        => $"AccessExp={AccessTokenExpiresAt:O} AccessSet={AccessToken.Length > 0} HasRefresh={HasRefreshToken} Claims={Claims.Count} Scopes={Scopes.Count}";
}