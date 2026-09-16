namespace Lyo.Authorization;

/// <summary>Fields for a new grant. The store assigns the id.</summary>
public sealed class GrantWrite
{
    /// <summary>Opaque subject.</summary>
    public required string Subject { get; init; }

    /// <summary>Action name. <c>|</c> is rejected.</summary>
    public required string Action { get; init; }

    /// <summary>Resource type. <c>|</c> is rejected.</summary>
    public required string ResourceType { get; init; }

    /// <summary>Null is a wildcard. Whitespace throws. A guid is stored as lowercase <c>D</c>.</summary>
    public string? ResourceId { get; init; }

    /// <summary>Null covers every tenant.</summary>
    public Guid? TenantId { get; init; }

    /// <summary>Optional actor recorded on the audit event.</summary>
    public string? GrantedBy { get; init; }

    /// <summary>Optional expiry. <see cref="DateTimeKind.Local" /> throws. Unspecified is treated as UTC.</summary>
    public DateTime? ExpiresAt { get; init; }
}
