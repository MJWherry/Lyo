using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Lyo.Authentication.Web.Components.Models;

/// <summary>
/// Diagnostic snapshot of the active session, surfaced through <c>IAuthSessionAccessor</c> for the debug page. The refresh token is never echoed — only its presence is.
/// </summary>
/// <param name="AccessToken">Current access JWT (visible in the UI).</param>
/// <param name="AccessTokenExpiresAt">When the access token expires.</param>
/// <param name="HasRefreshToken">Whether a refresh token is held server-side / in browser storage. The token itself is intentionally not exposed.</param>
/// <param name="RefreshTokenExpiresAt">When the refresh token expires, when known.</param>
/// <param name="Claims">Projected claims for the current principal.</param>
/// <param name="Scopes">Flat list of <c>scope</c> claim values for quick display.</param>
public sealed record AuthSessionSnapshot(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    bool HasRefreshToken,
    DateTime? RefreshTokenExpiresAt,
    IReadOnlyList<Claim> Claims,
    IReadOnlyList<string> Scopes);
