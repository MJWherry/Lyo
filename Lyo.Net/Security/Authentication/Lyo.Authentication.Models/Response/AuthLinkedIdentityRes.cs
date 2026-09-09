namespace Lyo.Authentication.Models.Response;

/// <summary>Linked OIDC identity for admin QueryConcrete / Get.</summary>
public sealed record AuthLinkedIdentityRes(
    Guid Id,
    Guid UserId,
    Guid? TenantId,
    string Provider,
    string Subject,
    string? EmailAtLink,
    string ScopesJson,
    string? RawClaimsJson,
    DateTime LinkedTimestamp,
    DateTime? UpdatedTimestamp,
    DateTime? LastUsedTimestamp,
    DateTime? UnlinkedTimestamp);
