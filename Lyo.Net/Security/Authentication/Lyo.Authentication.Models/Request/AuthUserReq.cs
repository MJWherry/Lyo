namespace Lyo.Authentication.Models.Request;

/// <summary>Update payload for a Lyo user. Create is not mapped; users are provisioned through OIDC.</summary>
public sealed class AuthUserReq
{
    /// <summary>Human-readable name shown in UI.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Primary email.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Whether any linked identity has proven ownership of the email.</summary>
    public bool EmailVerified { get; set; }

    /// <summary>Optional picture URL.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Optional BCP-47 language tag.</summary>
    public string? PreferredLanguageBcp47 { get; set; }

    /// <summary>Denormalized JSON array of scopes (mirrored from <c>[user].[scope]</c>). Patch is blocked; use the Scope surface.</summary>
    public string ScopesJson { get; set; } = "[]";

    /// <summary>Optional People person id.</summary>
    public Guid? PersonId { get; set; }

    /// <summary>When set, validators reject this user's tokens and JWTs.</summary>
    public DateTime? DisabledTimestamp { get; set; }

    /// <summary>Optional reason for disable.</summary>
    public string? DisabledReason { get; set; }
}
