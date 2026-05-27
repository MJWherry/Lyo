using System;

namespace Lyo.Authentication.Postgres.Database;

/// <summary>Entity for the <c>[user].[user]</c> table.</summary>
public sealed class UserEntity
{
    /// <summary>Stable Lyo user id. PK.</summary>
    public Guid Id { get; set; }

    /// <summary>Display name (free-form, shown in UI).</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Primary email. Case-insensitive unique.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary><c>true</c> once any linked identity has proven ownership via a verified provider claim.</summary>
    public bool EmailVerified { get; set; }

    /// <summary>Optional URL from a provider <c>picture</c> claim.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>BCP-47 language tag from a provider <c>locale</c> claim.</summary>
    public string? PreferredLanguageBcp47 { get; set; }

    /// <summary>Admin-managed baseline scopes (string array serialized to <c>jsonb</c>).</summary>
    public string ScopesJson { get; set; } = "[]";

    /// <summary>App-attached metadata (object serialized to <c>jsonb</c>; null when none).</summary>
    public string? MetadataJson { get; set; }

    /// <summary>Soft pointer to a <c>Lyo.People.Person</c> record. No cross-schema FK.</summary>
    public Guid? PersonId { get; set; }

    /// <summary>Optional tenant scope. <see langword="null" /> permits cross-tenant SSO-style users; non-null binds the user to a specific tenant.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>When the row was first written.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>When the row was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>When the user most recently completed a login or refreshed a JWT.</summary>
    public DateTime? LastLoginTimestamp { get; set; }

    /// <summary>Option-C kill switch. When set, every validator rejects this user's tokens and JWTs.</summary>
    public DateTime? DisabledTimestamp { get; set; }

    /// <summary>Optional human-readable reason for the disable.</summary>
    public string? DisabledReason { get; set; }
}
