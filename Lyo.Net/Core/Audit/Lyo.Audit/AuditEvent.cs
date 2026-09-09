using System.Diagnostics;
using Lyo.EntityReference.Models;

namespace Lyo.Audit;

/// <summary>An immutable audit entry for something that happened and belongs in the log (a user action, a system event, and similar).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public record AuditEvent(EntityRef Subject, string EventType, string? Message = null, EntityRef? Actor = null, IReadOnlyDictionary<string, object?>? Metadata = null)
{
    /// <summary>Unique id of this audit event.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>When the event took place.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>Tenant this event is bound to. <see langword="null" /> is system-wide / no tenant.</summary>
    public Guid? TenantId { get; init; }

    public override string ToString()
        => $"AuditEvent: {EventType} @ {Timestamp:O}, Subject: {Subject.EntityType}/{Subject.EntityId}, Actor: {(Actor is { } a ? $"{a.EntityType}/{a.EntityId}" : "(none)")}, Tenant: {TenantId}";
}