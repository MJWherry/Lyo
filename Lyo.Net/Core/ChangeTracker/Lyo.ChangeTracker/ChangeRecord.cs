using System.Diagnostics;
using Lyo.Common;

namespace Lyo.ChangeTracker;

/// <summary>Represents a change recorded against a generic entity reference.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ChangeRecord(EntityRef ForEntity, IReadOnlyDictionary<string, object?> OldValues, IReadOnlyDictionary<string, object?> ChangedProperties)
{
    /// <summary>Gets the unique identifier for this change.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Gets when the change occurred.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>Gets the entity that initiated the change, if known.</summary>
    public EntityRef? FromEntity { get; init; }

    /// <summary>Gets a caller-defined change category such as Created, Updated, or Deleted.</summary>
    public string? ChangeType { get; init; }

    /// <summary>Gets an optional human-readable description of the change.</summary>
    public string? Message { get; init; }

    public override string ToString() => $"ChangeRecord: {ForEntity.EntityType}/{ForEntity.EntityId}, OldValues: {OldValues.Count}, Changed: {ChangedProperties.Count}";
}