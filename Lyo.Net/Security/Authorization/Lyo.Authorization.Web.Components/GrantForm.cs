using Lyo.Exceptions;

namespace Lyo.Authorization.Web.Components;

/// <summary>Fields from the grant editor. Blank resource id and tenant are wildcards. A local expiry keeps the clock time and is stored as UTC.</summary>
public sealed class GrantForm
{
    /// <summary>Opaque subject. Trimmed by the store, never lowercased.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Action name.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Resource type.</summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>Blank is a wildcard. Whitespace throws. A guid is stored as lowercase <c>D</c>.</summary>
    public string? ResourceId { get; set; }

    /// <summary>Blank is a wildcard. A value must parse as a guid.</summary>
    public string? TenantId { get; set; }

    /// <summary>Optional actor stored on the grant.</summary>
    public string? GrantedBy { get; set; }

    /// <summary>Optional expiry. <see cref="DateTimeKind.Local" /> is marked UTC without shifting the clock.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Builds a write the store can save.</summary>
    public GrantWrite ToWrite() => GrantRules.Normalize(new GrantWrite {
        Subject = Subject,
        Action = Action,
        ResourceType = ResourceType,
        ResourceId = ResourceIdOrNull(),
        TenantId = TenantOrNull(),
        GrantedBy = string.IsNullOrWhiteSpace(GrantedBy) ? null : GrantedBy,
        ExpiresAt = AsUtc(ExpiresAt)
    });

    /// <summary>Builds a check from the same field rules as <see cref="ToWrite" />.</summary>
    public GrantCheck ToCheck() => GrantRules.Normalize(new GrantCheck {
        Subject = Subject,
        Action = Action,
        ResourceType = ResourceType,
        ResourceId = ResourceIdOrNull(),
        TenantId = TenantOrNull()
    });

    private string? ResourceIdOrNull()
    {
        if (ResourceId is null || ResourceId.Length == 0)
            return null;
        if (string.IsNullOrWhiteSpace(ResourceId))
            throw new ArgumentException("A whitespace resource id is not a wildcard. Pass null.", nameof(ResourceId));

        return ResourceId;
    }

    private Guid? TenantOrNull()
    {
        if (string.IsNullOrWhiteSpace(TenantId))
            return null;
        if (!Guid.TryParse(TenantId.Trim(), out var id))
            throw new ArgumentException("Tenant id must be a guid.", nameof(TenantId));

        return id;
    }

    private static DateTime? AsUtc(DateTime? value)
    {
        if (value is not { } expiry)
            return null;

        return expiry.Kind == DateTimeKind.Local ? DateTime.SpecifyKind(expiry, DateTimeKind.Utc) : expiry;
    }
}
