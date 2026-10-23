using Lyo.Exceptions;

namespace Lyo.Authorization;

/// <summary>Shared trim, length, and expiry rules for grants.</summary>
public static class GrantRules
{
    /// <summary>Maximum subject and GrantedBy length.</summary>
    public const int MaxSubjectLength = 512;

    /// <summary>Maximum action and resource type length.</summary>
    public const int MaxNameLength = 128;

    /// <summary>Maximum resource id length.</summary>
    public const int MaxResourceIdLength = 256;

    /// <summary>Normalizes a write. Throws when a field is illegal.</summary>
    public static GrantWrite Normalize(GrantWrite write)
    {
        ArgumentHelpers.ThrowIfNull(write);
        var subject = Required(write.Subject, nameof(write.Subject), MaxSubjectLength);
        var action = Name(write.Action, nameof(write.Action));
        var type = Name(write.ResourceType, nameof(write.ResourceType));
        var resourceId = ResourceId(write.ResourceId);
        var grantedBy = write.GrantedBy is null ? null : Required(write.GrantedBy, nameof(write.GrantedBy), MaxSubjectLength);
        return new() {
            Subject = subject,
            Action = action,
            ResourceType = type,
            ResourceId = resourceId,
            TenantId = write.TenantId,
            GrantedBy = grantedBy,
            ExpiresAt = write.ExpiresAt is null ? null : AsUtc(write.ExpiresAt.Value, nameof(write.ExpiresAt))
        };
    }

    /// <summary>Normalizes a check the same way a write is stored, so a guid id matches lowercase <c>D</c>.</summary>
    public static GrantCheck Normalize(GrantCheck check)
    {
        ArgumentHelpers.ThrowIfNull(check);
        return new() {
            Subject = Required(check.Subject, nameof(check.Subject), MaxSubjectLength),
            Action = Name(check.Action, nameof(check.Action)),
            ResourceType = Name(check.ResourceType, nameof(check.ResourceType)),
            ResourceId = ResourceId(check.ResourceId),
            TenantId = check.TenantId
        };
    }

    /// <summary>True when the grant is revoked or its expiry is at or before <paramref name="utcNow" />.</summary>
    public static bool IsInactive(Grant grant, DateTime utcNow) => grant.RevokedAt is not null || (grant.ExpiresAt is DateTime exp && exp <= utcNow);

    /// <summary>True when resource id and tenant match in both directions.</summary>
    public static bool Matches(Grant grant, GrantCheck check, DateTime utcNow)
    {
        if (!string.Equals(grant.Subject, check.Subject, StringComparison.Ordinal))
            return false;

        if (!string.Equals(grant.Action, check.Action, StringComparison.Ordinal))
            return false;

        if (!string.Equals(grant.ResourceType, check.ResourceType, StringComparison.Ordinal))
            return false;

        if (IsInactive(grant, utcNow))
            return false;

        if (!IdMatches(grant.ResourceId, check.ResourceId))
            return false;

        return TenantMatches(grant.TenantId, check.TenantId);
    }

    /// <summary>A null grant id covers every check id. A set id matches only that id. A null check id matches only a null grant id.</summary>
    public static bool IdMatches(string? grantId, string? checkId)
        => checkId is null ? grantId is null : grantId is null || string.Equals(grantId, checkId, StringComparison.Ordinal);

    /// <summary>Same two directions as <see cref="IdMatches" />.</summary>
    public static bool TenantMatches(Guid? grantTenant, Guid? checkTenant) => checkTenant is null ? grantTenant is null : grantTenant is null || grantTenant == checkTenant;

    private static string Required(string value, string name, int max)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value);
        var trimmed = value.Trim();
        if (trimmed.Length > max)
            throw new ArgumentException($"{name} length {trimmed.Length} exceeds {max}.", name);

        return trimmed;
    }

    private static string Name(string value, string name)
    {
        var trimmed = Required(value, name, MaxNameLength);
        if (trimmed.Contains('|'))
            throw new ArgumentException($"{name} cannot contain '|'.", name);

        return trimmed;
    }

    private static string? ResourceId(string? value)
    {
        if (value is null)
            return null;

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A whitespace resource id is not a wildcard. Pass null.", nameof(value));

        var trimmed = value.Trim();
        if (trimmed.Contains('|'))
            throw new ArgumentException("Resource id cannot contain '|'.", nameof(value));

        if (trimmed.Length > MaxResourceIdLength)
            throw new ArgumentException($"Resource id length {trimmed.Length} exceeds {MaxResourceIdLength}.", nameof(value));

        if (Guid.TryParse(trimmed, out var id))
            return id.ToString("D");

        return trimmed;
    }

    private static DateTime AsUtc(DateTime value, string name)
    {
        if (value.Kind == DateTimeKind.Local)
            throw new ArgumentException("Local DateTime is not accepted. Pass UTC or Unspecified.", name);

        return value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}