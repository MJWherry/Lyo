namespace Lyo.Authentication.Options;

/// <summary>Options controlling Lyo-signed JWT issuance and validation.</summary>
public sealed class LyoJwtOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "LyoJwt";

    /// <summary>JWT <c>iss</c> claim. Must match across all Lyo hosts that share the same signing keystore.</summary>
    public string Issuer { get; set; } = "https://auth.lyo";

    /// <summary>JWT <c>aud</c> claim. Validators accept this value (and only this value) by default.</summary>
    public string Audience { get; set; } = "lyo-api";

    /// <summary>How long a freshly-minted access JWT is valid. Default = 15 minutes — short enough that scope demotion via re-login takes effect quickly.</summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>How long a freshly-minted refresh token is valid (delivered as <c>lyo_refresh</c> cookie). Default = 30 days.</summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);

    /// <summary>The key id under which the signing key is stored in <see cref="Lyo.Keystore.IKeyStore" />. Default = <c>lyo-sig</c>.</summary>
    public string SigningKeyId { get; set; } = "lyo-sig";

    /// <summary>Signing algorithm. Currently only <c>EdDSA</c> (Ed25519) is supported.</summary>
    public string Algorithm { get; set; } = "EdDSA";

    /// <summary>Permitted clock skew when validating <c>exp</c> / <c>iat</c> / <c>nbf</c>. Default = 30 seconds.</summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// When <c>true</c>, <see cref="Services.Jwt.Ed25519KeyBootstrapper" /> auto-generates a fresh Ed25519 keypair into the keystore on startup if none exists. Default =
    /// <c>true</c>. Set to <c>false</c> for environments that provision the key out-of-band (e.g. via HSM or a sealed secret).
    /// </summary>
    public bool AutoGenerateSigningKey { get; set; } = true;
}