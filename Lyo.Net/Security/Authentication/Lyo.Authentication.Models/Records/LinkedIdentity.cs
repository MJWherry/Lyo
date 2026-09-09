using System.Diagnostics;

namespace Lyo.Authentication.Models.Records;

/// <summary>
/// Mapping between a Lyo user and an external OIDC identity (a single <c>(provider, subject)</c> tuple). Backed by <c>[user].[linked_identity]</c> when persisted
/// via <c>Lyo.Authentication.Postgres</c>.
/// </summary>
/// <param name="Id">Stable identifier for the link.</param>
/// <param name="UserId">Lyo user this identity belongs to.</param>
/// <param name="Provider">Provider name (e.g. <c>google</c>, <c>keycloak:my-realm</c>).</param>
/// <param name="Subject">Provider <c>sub</c> claim. Stable per provider.</param>
/// <param name="EmailAtLink">Provider email claim when the link was first established. Not refreshed automatically.</param>
/// <param name="Scopes">Provider-derived scopes (e.g. Keycloak <c>realm_access.roles</c> mapped via <c>RolesToScopes</c>). Refreshed on every successful login.</param>
/// <param name="RawClaims">Snapshot of all claims from the most recent id_token, kept for forensic review.</param>
/// <param name="LinkedAt">When the link was first established.</param>
/// <param name="UpdatedAt">When the link was last updated (typically on each login).</param>
/// <param name="LastUsedAt">When the link most recently produced a successful login.</param>
/// <param name="UnlinkedAt">When the link was soft-deleted. While non-null, the link is inactive and the (provider, subject) pair can be re-linked.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record LinkedIdentity(
    Guid Id,
    Guid UserId,
    string Provider,
    string Subject,
    string? EmailAtLink,
    IReadOnlyList<string> Scopes,
    IReadOnlyDictionary<string, object?>? RawClaims,
    DateTime LinkedAt,
    DateTime? UpdatedAt,
    DateTime? LastUsedAt,
    DateTime? UnlinkedAt)
{
    /// <summary>True if the link is still active (not soft-deleted).</summary>
    public bool IsActive => !UnlinkedAt.HasValue;

    public override string ToString() => $"LinkedIdentity: provider={Provider}, subject={Subject}, user={UserId}, active={IsActive}";
}