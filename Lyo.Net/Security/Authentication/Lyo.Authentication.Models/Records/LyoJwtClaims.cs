namespace Lyo.Authentication.Models.Records;

/// <summary>Canonical claim names on Lyo-issued JWTs and the projected ASP.NET <see cref="System.Security.Claims.ClaimsPrincipal" />.</summary>
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

    /// <summary>RFC 8693 <c>scope</c>. On Lyo JWTs the value is space-delimited; on opaque-token principals each scope is a separate claim.</summary>
    public const string Scope = "scope";

    /// <summary>Lyo user id (<c>guid</c> form, without the <c>lyo_user:</c> prefix). Always on JWTs; on opaque-token principals when the token has an owner.</summary>
    public const string LyoUser = "lyo:user";

    /// <summary>Identity provider that minted the credential. <c>local</c> for opaque tokens, <c>google</c>/<c>keycloak:&lt;realm&gt;</c>/<c>local</c> for JWTs.</summary>
    public const string LyoProvider = "lyo:provider";

    /// <summary>Provider <c>sub</c> claim at issuance. Audit-only.</summary>
    public const string LyoExternalSub = "lyo:external_sub";

    /// <summary>Opaque-token id (opaque-token principals only).</summary>
    public const string LyoTokenId = "lyo:token_id";

    /// <summary>Opaque-token kind (opaque-token principals only).</summary>
    public const string LyoKind = "lyo:kind";

    /// <summary>Opaque-token ring (opaque-token principals only).</summary>
    public const string LyoRing = "lyo:ring";

    /// <summary>
    /// True when <paramref name="type" /> is a reserved JWT or Lyo claim name that user-claim CRUD must not overwrite (<c>iss</c>, <c>sub</c>, <c>aud</c>,
    /// <c>exp</c>, <c>nbf</c>, <c>iat</c>, <c>jti</c>, <c>scope</c>, or any name starting with <c>lyo:</c>).
    /// </summary>
    public static bool IsReserved(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return true;

        if (type!.StartsWith("lyo:", StringComparison.Ordinal))
            return true;

        return type.Equals(Issuer, StringComparison.Ordinal)
            || type.Equals(Subject, StringComparison.Ordinal)
            || type.Equals(Audience, StringComparison.Ordinal)
            || type.Equals(ExpiresAt, StringComparison.Ordinal)
            || type.Equals(IssuedAt, StringComparison.Ordinal)
            || type.Equals(NotBefore, StringComparison.Ordinal)
            || type.Equals(TokenId, StringComparison.Ordinal)
            || type.Equals(Scope, StringComparison.Ordinal);
    }
}