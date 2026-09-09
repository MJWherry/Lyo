using System.Diagnostics;
using Lyo.EntityReference.Models;

namespace Lyo.Audit;

/// <summary>Immutable record of an entity mutation (per-property before/after values).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record AuditChange(EntityRef Entity, IReadOnlyDictionary<string, object?> OldValues, IReadOnlyDictionary<string, object?> ChangedProperties)
{
    /// <summary>Unique id of this audit change.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>When the change was written.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>Who made the change, when that is known.</summary>
    public EntityRef? Actor { get; init; }

    /// <summary>Tenant this change is bound to. <see langword="null" /> is system-wide / no tenant.</summary>
    public Guid? TenantId { get; init; }

    public override string ToString()
        => $"AuditChange: {Entity.EntityType}/{Entity.EntityId}, OldValues: {OldValues.Count}, Changed: {ChangedProperties.Count}, Tenant: {TenantId}";
}