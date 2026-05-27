using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lyo.Authentication.OpenIdConnect.Discovery;

/// <summary>JWKS document returned by a provider's <c>jwks_uri</c>.</summary>
public sealed class OidcJwksDocument
{
    /// <summary>The set of keys.</summary>
    [JsonPropertyName("keys")]
    public List<OidcJsonWebKey> Keys { get; set; } = [];
}

/// <summary>A single JSON Web Key from a provider's JWKS.</summary>
public sealed class OidcJsonWebKey
{
    /// <summary>Key type (<c>RSA</c>, <c>OKP</c>, <c>EC</c>).</summary>
    [JsonPropertyName("kty")]
    public string Kty { get; set; } = string.Empty;

    /// <summary>Public key use (<c>sig</c> / <c>enc</c>).</summary>
    [JsonPropertyName("use")]
    public string? Use { get; set; }

    /// <summary>Algorithm (<c>RS256</c>, <c>EdDSA</c>, ...).</summary>
    [JsonPropertyName("alg")]
    public string? Alg { get; set; }

    /// <summary>Key id (matches the JWT header's <c>kid</c>).</summary>
    [JsonPropertyName("kid")]
    public string Kid { get; set; } = string.Empty;

    /// <summary>RSA modulus (base64url).</summary>
    [JsonPropertyName("n")]
    public string? N { get; set; }

    /// <summary>RSA exponent (base64url).</summary>
    [JsonPropertyName("e")]
    public string? E { get; set; }

    /// <summary>OKP/EC curve (<c>Ed25519</c>, <c>P-256</c>).</summary>
    [JsonPropertyName("crv")]
    public string? Crv { get; set; }

    /// <summary>OKP/EC public point x (base64url).</summary>
    [JsonPropertyName("x")]
    public string? X { get; set; }

    /// <summary>EC public point y (base64url).</summary>
    [JsonPropertyName("y")]
    public string? Y { get; set; }
}
