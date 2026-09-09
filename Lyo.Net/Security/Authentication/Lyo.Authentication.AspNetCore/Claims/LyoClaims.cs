using Lyo.Authentication.Models.Records;

namespace Lyo.Authentication.AspNetCore.Claims;

/// <summary>Re-exports the canonical claim names so this library's callers need not take a dependency on the base lib's record namespace.</summary>
public static class LyoClaims
{
    /// <summary>Standard <c>sub</c>.</summary>
    public const string Subject = LyoJwtClaims.Subject;

    /// <summary>
    /// Fine-grained scope claim. May appear once per scope (opaque tokens); the JWT validator splits the space-delimited string so
    /// policies see the same shape for either bearer format.
    /// </summary>
    public const string Scope = LyoJwtClaims.Scope;

    /// <summary>Lyo user identifier (GUID, no prefix).</summary>
    public const string LyoUser = LyoJwtClaims.LyoUser;

    /// <summary>Originating identity provider (<c>local</c>, <c>google</c>, <c>keycloak:&lt;realm&gt;</c>).</summary>
    public const string LyoProvider = LyoJwtClaims.LyoProvider;

    /// <summary>Format-B token id (opaque-token principals only).</summary>
    public const string LyoTokenId = LyoJwtClaims.LyoTokenId;

    /// <summary>Format-B token kind (opaque-token principals only).</summary>
    public const string LyoKind = LyoJwtClaims.LyoKind;

    /// <summary>Format-B token ring (opaque-token principals only).</summary>
    public const string LyoRing = LyoJwtClaims.LyoRing;
}