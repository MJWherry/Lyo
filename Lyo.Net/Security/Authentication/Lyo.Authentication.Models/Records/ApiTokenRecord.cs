namespace Lyo.Authentication.Models.Records;

/// <summary>The persistent shape of a Format-B token. Backed by the <c>[user].[token]</c> table when persisted via <c>Lyo.Authentication.Postgres</c>.</summary>
/// <param name="Id">The 11-character Crockford base32 id (PK).</param>
/// <param name="SecretHash">SHA-256 of the secret segment.</param>
/// <param name="Kind">One of <see cref="Format.ApiTokenKind"/>.</param>
/// <param name="Ring">One of <see cref="Format.ApiTokenRing"/>.</param>
/// <param name="UserId">Owning user, or <c>null</c> for unowned <c>svc</c>/<c>internal</c> tokens.</param>
/// <param name="DisplayName">User-facing label (e.g. "GitHub Actions deploy").</param>
/// <param name="Scopes">Snapshot of scopes at issuance (Option C — never re-intersected at validation).</param>
/// <param name="Metadata">App-attached key/value bag.</param>
/// <param name="CreatedAt">When the token was issued.</param>
/// <param name="UpdatedAt">When the row was last touched.</param>
/// <param name="ExpiresAt">When the token expires (<c>null</c> = no expiry).</param>
/// <param name="LastUsedAt">Best-effort "last validated successfully" timestamp.</param>
/// <param name="RevokedAt">Hard stop. When non-null and in the past, validators reject the token.</param>
/// <param name="RevokedReason">Optional audit context.</param>
/// <param name="RotatedFromId">Audit trail when this token replaced another via rotate.</param>
public sealed record ApiTokenRecord(
    string Id,
    byte[] SecretHash,
    string Kind,
    string Ring,
    Guid? UserId,
    string DisplayName,
    IReadOnlyList<string> Scopes,
    IReadOnlyDictionary<string, object?>? Metadata,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? ExpiresAt,
    DateTime? LastUsedAt,
    DateTime? RevokedAt,
    string? RevokedReason,
    string? RotatedFromId)
{
    /// <summary>True if <see cref="RevokedAt"/> is set and in the past relative to <paramref name="now"/>.</summary>
    public bool IsRevoked(DateTime now) => RevokedAt.HasValue && RevokedAt.Value <= now;

    /// <summary>True if <see cref="ExpiresAt"/> is set and in the past relative to <paramref name="now"/>.</summary>
    public bool IsExpired(DateTime now) => ExpiresAt.HasValue && ExpiresAt.Value <= now;
}
