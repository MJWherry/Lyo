namespace Lyo.Authentication.AspNetCore.Claims;

/// <summary>Re-exports the canonical claim names so consumers of this lib don't need to depend on the base lib's record namespace.</summary>
public static class LyoClaims
{
    /// <summary>Standard <c>sub</c>.</summary>
    public const string Subject = Models.Records.LyoJwtClaims.Subject;

    /// <summary>The fine-grained scope claim. May appear multiple times (one per scope) for opaque tokens; the JWT validator splits the space-delimited string into individual claims so policies see the same shape regardless of bearer format.</summary>
    public const string Scope = Models.Records.LyoJwtClaims.Scope;

    /// <summary>The Lyo user identifier (GUID, no prefix).</summary>
    public const string LyoUser = Models.Records.LyoJwtClaims.LyoUser;

    /// <summary>The originating identity provider (<c>local</c>, <c>google</c>, <c>keycloak:&lt;realm&gt;</c>).</summary>
    public const string LyoProvider = Models.Records.LyoJwtClaims.LyoProvider;

    /// <summary>The Format-B token id (only on opaque-token principals).</summary>
    public const string LyoTokenId = Models.Records.LyoJwtClaims.LyoTokenId;

    /// <summary>The Format-B token kind (only on opaque-token principals).</summary>
    public const string LyoKind = Models.Records.LyoJwtClaims.LyoKind;

    /// <summary>The Format-B token ring (only on opaque-token principals).</summary>
    public const string LyoRing = Models.Records.LyoJwtClaims.LyoRing;
}
