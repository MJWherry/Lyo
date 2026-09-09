namespace Lyo.Authentication.Models.Response;

/// <summary>Display-safe Lyo user for QueryConcrete / Get. QueryProject selects entity fields (including <c>ScopesJson</c>).</summary>
public sealed record AuthUserRes(
    Guid Id,
    string DisplayName,
    string Email,
    bool EmailVerified,
    string? AvatarUrl,
    string? PreferredLanguageBcp47,
    string ScopesJson,
    Guid? PersonId,
    Guid? TenantId,
    DateTime CreatedTimestamp,
    DateTime? UpdatedTimestamp,
    DateTime? LastLoginTimestamp,
    DateTime? DisabledTimestamp,
    string? DisabledReason);
