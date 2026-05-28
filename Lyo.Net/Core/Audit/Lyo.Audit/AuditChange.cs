using System.Diagnostics;
using Lyo.EntityReference.Models;

namespace Lyo.Audit;

/// <summary>Represents a recorded change to an entity (property-level before/after diff). Immutable once created.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record AuditChange(EntityRef Entity, IReadOnlyDictionary<string, object?> OldValues, IReadOnlyDictionary<string, object?> ChangedProperties)
{
    /// <summary>Gets the unique identifier for this audit change.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Gets the timestamp when the change was recorded.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>Gets the actor that performed the change, if known.</summary>
    public EntityRef? Actor { get; init; }

    /// <summary>Gets the tenant the change is scoped to. <see langword="null" /> means system / no tenant.</summary>
    public Guid? TenantId { get; init; }

    public override string ToString()
        => $"AuditChange: {Entity.EntityType}/{Entity.EntityId}, OldValues: {OldValues.Count}, Changed: {ChangedProperties.Count}, Tenant: {TenantId}";
}