using System;
using System.Collections.Generic;

namespace Lyo.Authentication.Records;

/// <summary>
/// A mapping between a Lyo user and an external OIDC identity (a single <c>(provider, subject)</c> tuple). Backed by the <c>[user].[linked_identity]</c> table when persisted via
/// <c>Lyo.Authentication.Postgres</c>.
/// </summary>
/// <param name="Id">Stable identifier for the link.</param>
/// <param name="UserId">The Lyo user this identity belongs to.</param>
/// <param name="Provider">Provider name (e.g. <c>google</c>, <c>keycloak:my-realm</c>).</param>
/// <param name="Subject">The provider's <c>sub</c> claim. Stable per provider.</param>
/// <param name="EmailAtLink">The provider's email claim at the time the link was first established. Not refreshed automatically.</param>
/// <param name="Scopes">Provider-derived scopes (e.g. Keycloak <c>realm_access.roles</c> mapped via <c>RolesToScopes</c>). Refreshed on every successful login.</param>
/// <param name="RawClaims">Snapshot of all claims from the most recent id_token, retained for forensic review.</param>
/// <param name="LinkedAt">When the link was first established.</param>
/// <param name="UpdatedAt">When the link was last updated (typically on each login).</param>
/// <param name="LastUsedAt">When the link most recently produced a successful login.</param>
/// <param name="UnlinkedAt">When the link was soft-deleted. While non-null, the link is inactive and the (provider, subject) pair can be re-linked.</param>
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
}
