namespace Lyo.Authentication.Postgres.Database;

/// <summary>Entity for the <c>[user].[linked_identity]</c> table (external OIDC identity links).</summary>
public sealed class LinkedIdentityEntity
{
    /// <summary>Stable id for the link. PK.</summary>
    public Guid Id { get; set; }

    /// <summary>FK to <c>[user].[user].id</c>.</summary>
    public Guid UserId { get; set; }

    /// <summary>Optional tenant scope. <see langword="null" /> permits cross-tenant linking; non-null binds the link to a specific tenant.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Provider name (e.g. <c>google</c>, <c>keycloak:my-realm</c>).</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Provider's <c>sub</c> claim.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Provider's email claim at link time. Not kept fresh.</summary>
    public string? EmailAtLink { get; set; }

    /// <summary>Provider-derived scopes (string array serialized to <c>jsonb</c>).</summary>
    public string ScopesJson { get; set; } = "[]";

    /// <summary>Snapshot of all claims from the most recent id_token (object serialized to <c>jsonb</c>).</summary>
    public string? RawClaimsJson { get; set; }

    /// <summary>When the link was first established.</summary>
    public DateTime LinkedTimestamp { get; set; }

    /// <summary>When the link was last updated (typically on each login).</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>When the link most recently produced a successful login.</summary>
    public DateTime? LastUsedTimestamp { get; set; }

    /// <summary>When the link was soft-deleted. While non-null the link is inactive and the (provider, subject) pair can be re-linked.</summary>
    public DateTime? UnlinkedTimestamp { get; set; }
}