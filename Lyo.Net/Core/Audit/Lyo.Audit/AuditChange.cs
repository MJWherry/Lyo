using System.Diagnostics;

namespace Lyo.Audit;

/// <summary>Represents a recorded change to an entity (property-level before/after diff). Immutable once created.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public record AuditChange(string TypeAssemblyFullName, IReadOnlyDictionary<string, object?> OldValues, IReadOnlyDictionary<string, object?> ChangedProperties)
{
    /// <summary>Gets the unique identifier for this audit change.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Gets the timestamp when the change was recorded.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public override string ToString() => $"AuditChange: {TypeAssemblyFullName}, OldValues: {OldValues.Count}, Changed: {ChangedProperties.Count}";
}