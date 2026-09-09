using System.Text.Json.Serialization;
using Lyo.Exceptions;

namespace Lyo.Authentication.Models.Records;

/// <summary>
/// JSON shape returned by <c>POST /auth/handoff/exchange</c>, <c>POST /auth/token</c>, and <c>POST /auth/refresh</c>. Snake-case per RFC 6749 (OAuth 2.0). Shared by the
/// API host and consumer clients so access and refresh expiries stay in one type.
/// </summary>
public sealed record LyoOAuthTokenResponse(
    [property: JsonPropertyName("access_token")]
    string AccessToken,
    [property: JsonPropertyName("expires_in")]
    int ExpiresIn,
    [property: JsonPropertyName("refresh_token")]
    string? RefreshToken,
    [property: JsonPropertyName("token_type")]
    string TokenType,
    [property: JsonPropertyName("refresh_expires_in")]
    int? RefreshExpiresIn = null)
{
    /// <summary>Builds the wire response from an issuer result, computing remaining lifetimes against <paramref name="utcNow" />.</summary>
    public static LyoOAuthTokenResponse FromIssued(IssuedLyoJwt issued, DateTime utcNow)
    {
        ArgumentHelpers.ThrowIfNull(issued);
        return FromTokens(issued.AccessToken, issued.AccessTokenExpiresAt, issued.RefreshToken, issued.RefreshTokenExpiresAt, utcNow);
    }

    /// <summary>Builds the wire response from raw token strings and expiry instants.</summary>
    public static LyoOAuthTokenResponse FromTokens(string accessToken, DateTime accessTokenExpiresAt, string? refreshToken, DateTime? refreshTokenExpiresAt, DateTime utcNow)
        => new(
            accessToken, SecondsRemaining(accessTokenExpiresAt, utcNow), refreshToken, "Bearer",
            refreshTokenExpiresAt is { } exp ? SecondsRemaining(exp, utcNow) : null);

    /// <summary>UTC instant when the refresh token expires, or <c>null</c> when none was issued / no lifetime was returned.</summary>
    public DateTime? RefreshExpiresAtUtc(DateTime utcNow) => RefreshExpiresIn is { } seconds ? utcNow.AddSeconds(seconds) : null;

    private static int SecondsRemaining(DateTime expiresAt, DateTime utcNow)
    {
        var seconds = (expiresAt - utcNow).TotalSeconds;
        return seconds <= 0 ? 0 : (int)seconds;
    }
}
