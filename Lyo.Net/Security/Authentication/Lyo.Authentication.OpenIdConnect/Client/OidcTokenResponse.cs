using System.Text.Json.Serialization;

namespace Lyo.Authentication.OpenIdConnect.Client;

/// <summary>Token response from a provider <c>/token</c> endpoint.</summary>
public sealed class OidcTokenResponse
{
    /// <summary>Access token (typically a JWT).</summary>
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    /// <summary>id_token (JWT) — the primary signal Lyo cares about.</summary>
    [JsonPropertyName("id_token")]
    public string? IdToken { get; set; }

    /// <summary>Provider refresh token (Lyo does not currently use this).</summary>
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    /// <summary>Granted scopes.</summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    /// <summary>Bearer / DPoP / etc.</summary>
    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    /// <summary>Access-token TTL in seconds.</summary>
    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; set; }
}