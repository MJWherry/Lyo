using System.Diagnostics;
using Lyo.EntityReference.Models;

namespace Lyo.ChangeTracker;

/// <summary>One change written against a generic entity reference.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ChangeRecord(EntityRef ForEntity, IReadOnlyDictionary<string, object?> OldValues, IReadOnlyDictionary<string, object?> ChangedProperties)
{
    /// <summary>Unique id of this change.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>When the change took place.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>Entity that initiated the change, when known.</summary>
    public EntityRef? FromEntity { get; init; }

    /// <summary>Caller-defined category such as Created, Updated, or Deleted.</summary>
    public string? ChangeType { get; init; }

    /// <summary>Optional description intended for people to read.</summary>
    public string? Message { get; init; }

    /// <summary>Tenant this change is bound to. <see langword="null" /> is system-wide / no tenant.</summary>
    public Guid? TenantId { get; init; }

    public override string ToString()
        => $"ChangeRecord: {ForEntity.EntityType}/{ForEntity.EntityId}, OldValues: {OldValues.Count}, Changed: {ChangedProperties.Count}, Tenant: {TenantId}";
}