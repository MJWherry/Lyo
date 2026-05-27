namespace Lyo.Authentication.Records;

/// <summary>Canonical claim names used on Lyo-issued JWTs and the projected ASP.NET <see cref="System.Security.Claims.ClaimsPrincipal"/>.</summary>
public static class LyoJwtClaims
{
    /// <summary>Standard <c>iss</c>.</summary>
    public const string Issuer = "iss";

    /// <summary>Standard <c>sub</c>. <c>lyo_user:&lt;guid&gt;</c> on JWTs, <c>lyo_token:&lt;id&gt;</c> on opaque-token principals.</summary>
    public const string Subject = "sub";

    /// <summary>Standard <c>aud</c>.</summary>
    public const string Audience = "aud";

    /// <summary>Standard <c>exp</c>.</summary>
    public const string ExpiresAt = "exp";

    /// <summary>Standard <c>iat</c>.</summary>
    public const string IssuedAt = "iat";

    /// <summary>Standard <c>nbf</c>.</summary>
    public const string NotBefore = "nbf";

    /// <summary>Standard <c>jti</c>.</summary>
    public const string TokenId = "jti";

    /// <summary>RFC 8693 <c>scope</c>. On Lyo JWTs the value is space-delimited; on opaque-token principals every scope is emitted as a separate claim.</summary>
    public const string Scope = "scope";

    /// <summary>Lyo user id (<c>guid</c> form, without the <c>lyo_user:</c> prefix). Always present on JWTs; present on opaque-token principals when the token has an owner.</summary>
    public const string LyoUser = "lyo:user";

    /// <summary>Identity provider that minted the credential. <c>local</c> for opaque tokens, <c>google</c>/<c>keycloak:&lt;realm&gt;</c>/<c>local</c> for JWTs.</summary>
    public const string LyoProvider = "lyo:provider";

    /// <summary>Provider's <c>sub</c> claim at issuance. Audit-only.</summary>
    public const string LyoExternalSub = "lyo:external_sub";

    /// <summary>Opaque-token id (only present on opaque-token principals).</summary>
    public const string LyoTokenId = "lyo:token_id";

    /// <summary>Opaque-token kind (only present on opaque-token principals).</summary>
    public const string LyoKind = "lyo:kind";

    /// <summary>Opaque-token ring (only present on opaque-token principals).</summary>
    public const string LyoRing = "lyo:ring";
}
