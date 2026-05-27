using System.Text.Json.Serialization;

namespace Lyo.Authentication.OpenIdConnect.Discovery;

/// <summary>The minimal subset of the OIDC discovery document Lyo cares about.</summary>
public sealed class OidcDiscoveryDocument
{
    /// <summary>The issuer URL.</summary>
    [JsonPropertyName("issuer")]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>The <c>/authorize</c> endpoint URL.</summary>
    [JsonPropertyName("authorization_endpoint")]
    public string AuthorizationEndpoint { get; set; } = string.Empty;

    /// <summary>The <c>/token</c> endpoint URL.</summary>
    [JsonPropertyName("token_endpoint")]
    public string TokenEndpoint { get; set; } = string.Empty;

    /// <summary>The JWKS URI used to verify id_token signatures.</summary>
    [JsonPropertyName("jwks_uri")]
    public string JwksUri { get; set; } = string.Empty;

    /// <summary>Optional <c>/userinfo</c> endpoint.</summary>
    [JsonPropertyName("userinfo_endpoint")]
    public string? UserInfoEndpoint { get; set; }
}
