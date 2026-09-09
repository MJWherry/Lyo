namespace Lyo.Authentication.Models.Response;

/// <summary>Display-safe opaque token. Never includes the secret hash.</summary>
public sealed record AuthTokenRes(
    string Id,
    string Kind,
    string Ring,
    Guid? UserId,
    Guid? TenantId,
    string DisplayName,
    string ScopesJson,
    string? MetadataJson,
    DateTime CreatedTimestamp,
    DateTime? UpdatedTimestamp,
    DateTime? ExpiresTimestamp,
    DateTime? LastUsedTimestamp,
    DateTime? RevokedTimestamp,
    string? RevokedReason,
    string? RotatedFromId);
