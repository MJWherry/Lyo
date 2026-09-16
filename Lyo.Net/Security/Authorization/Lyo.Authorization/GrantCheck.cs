namespace Lyo.Authorization;

/// <summary>One authorization question. Null resource id matches only a wildcard grant.</summary>
public sealed class GrantCheck
{
    /// <summary>Opaque subject.</summary>
    public required string Subject { get; init; }

    /// <summary>Action.</summary>
    public required string Action { get; init; }

    /// <summary>Resource type.</summary>
    public required string ResourceType { get; init; }

    /// <summary>Null matches only grants whose resource id is null.</summary>
    public string? ResourceId { get; init; }

    /// <summary>Null matches only grants whose tenant is null.</summary>
    public Guid? TenantId { get; init; }
}
