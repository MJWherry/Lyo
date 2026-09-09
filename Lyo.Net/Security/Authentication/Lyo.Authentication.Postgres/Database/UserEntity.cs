namespace Lyo.Authentication.Postgres.Database;

/// <summary>Row mapped to <c>[user].[user]</c>.</summary>
public sealed class UserEntity
{
    /// <summary>Stable Lyo user id (primary key).</summary>
    public Guid Id { get; set; }

    /// <summary>Free-form display name shown in the UI.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Primary email; unique without regard to case.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary><c>true</c> after any linked identity has proven ownership through a verified provider claim.</summary>
    public bool EmailVerified { get; set; }

    /// <summary>Optional picture URL from a provider <c>picture</c> claim.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>BCP-47 language tag from a provider <c>locale</c> claim.</summary>
    public string? PreferredLanguageBcp47 { get; set; }

    /// <summary>Admin-assigned baseline scopes (string array stored as <c>jsonb</c>).</summary>
    public string ScopesJson { get; set; } = "[]";

    /// <summary>App-attached metadata (object stored as <c>jsonb</c>; null when absent).</summary>
    public string? MetadataJson { get; set; }

    /// <summary>Soft link to a <c>Lyo.People.Person</c> row. No foreign key across schemas.</summary>
    public Guid? PersonId { get; set; }

    /// <summary>Optional tenant binding. <see langword="null" /> allows cross-tenant SSO-style users; a value pins the user to one tenant.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Timestamp of the first insert.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Timestamp of the last update.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Most recent successful login or JWT refresh.</summary>
    public DateTime? LastLoginTimestamp { get; set; }

    /// <summary>Option-C kill switch. When set, validators reject this user's tokens and JWTs.</summary>
    public DateTime? DisabledTimestamp { get; set; }

    /// <summary>Optional human-readable explanation for the disable.</summary>
    public string? DisabledReason { get; set; }
}