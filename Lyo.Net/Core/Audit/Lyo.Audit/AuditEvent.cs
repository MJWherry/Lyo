using System.Diagnostics;
using Lyo.EntityReference.Models;

namespace Lyo.Audit;

/// <summary>Represents an audit event—something that occurred and should be logged (e.g. user action, system event). Immutable once created.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public record AuditEvent(
    EntityRef Subject,
    string EventType,
    string? Message = null,
    EntityRef? Actor = null,
    IReadOnlyDictionary<string, object?>? Metadata = null)
{
    /// <summary>Gets the unique identifier for this audit event.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Gets the timestamp when the event occurred.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>Gets the tenant the event is scoped to. <see langword="null" /> means system / no tenant.</summary>
    public Guid? TenantId { get; init; }

    public override string ToString()
        => $"AuditEvent: {EventType} @ {Timestamp:O}, Subject: {Subject.EntityType}/{Subject.EntityId}, Actor: {(Actor is { } a ? $"{a.EntityType}/{a.EntityId}" : "(none)")}, Tenant: {TenantId}";
}
