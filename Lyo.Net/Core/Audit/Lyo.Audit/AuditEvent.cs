using System.Diagnostics;

namespace Lyo.Audit;

/// <summary>Represents an audit event—something that occurred and should be logged (e.g. user action, system event). Immutable once created.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public record AuditEvent(string EventType, string? Message = null, string? Actor = null, IReadOnlyDictionary<string, object?>? Metadata = null)
{
    /// <summary>Gets the unique identifier for this audit event.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Gets the timestamp when the event occurred.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public override string ToString() => $"AuditEvent: {EventType} @ {Timestamp:O}, Actor: {Actor ?? "(none)"}";
}