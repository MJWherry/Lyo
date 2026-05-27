using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Lyo.Authentication.Client;

/// <summary>
/// Mutable, server-side per-session token bundle. Lives in <see cref="LyoAuthSessionStore"/> keyed by <see cref="SessionId"/>; the browser cookie carries only the data-protected
/// session id, never the tokens themselves.
/// </summary>
public sealed class LyoAuthSession
{
    /// <summary>Stable session id (random GUID). Carried (data-protected) in the consumer's HttpOnly cookie.</summary>
    public Guid SessionId { get; }

    /// <summary>The current Lyo JWT used for outbound API calls.</summary>
    public string AccessToken { get; private set; }

    /// <summary>The matching rotating refresh token, or <c>null</c> if the issuer did not mint one.</summary>
    public string? RefreshToken { get; private set; }

    /// <summary>When <see cref="AccessToken"/> expires.</summary>
    public DateTime AccessTokenExpiresAt { get; private set; }

    /// <summary>When <see cref="RefreshToken"/> expires, or <c>null</c> when no refresh token is held.</summary>
    public DateTime? RefreshTokenExpiresAt { get; private set; }

    /// <summary>Claims projected from the access JWT at session-creation / refresh time. Used by <see cref="LyoAuthCookieAuthenticationHandler"/> to populate <see cref="ClaimsPrincipal"/>.</summary>
    public IReadOnlyList<Claim> Claims { get; private set; }

    /// <summary>When the session was first written.</summary>
    public DateTime CreatedAt { get; }

    /// <summary>Last write/refresh timestamp.</summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>Creates a new session.</summary>
    public LyoAuthSession(
        Guid sessionId,
        string accessToken,
        string? refreshToken,
        DateTime accessTokenExpiresAt,
        DateTime? refreshTokenExpiresAt,
        IReadOnlyList<Claim> claims,
        DateTime createdAt)
    {
        SessionId = sessionId;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        AccessTokenExpiresAt = accessTokenExpiresAt;
        RefreshTokenExpiresAt = refreshTokenExpiresAt;
        Claims = claims;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    /// <summary>Replaces the access/refresh tokens after a successful refresh, recomputing the claims and the updated-at timestamp.</summary>
    public void Update(string accessToken, string? refreshToken, DateTime accessTokenExpiresAt, DateTime? refreshTokenExpiresAt, IReadOnlyList<Claim> claims)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        AccessTokenExpiresAt = accessTokenExpiresAt;
        RefreshTokenExpiresAt = refreshTokenExpiresAt;
        Claims = claims;
        UpdatedAt = DateTime.UtcNow;
    }
}
