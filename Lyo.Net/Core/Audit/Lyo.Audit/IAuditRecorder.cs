namespace Lyo.Audit;

/// <summary>Interface for recording audit entries. Implement this to integrate with your audit storage (e.g. database, log sink).</summary>
public interface IAuditRecorder
{
    /// <summary>Records an entity change (property-level before/after diff).</summary>
    /// <param name="change">The audit change to record</param>
    void RecordChange(AuditChange change);

    /// <summary>Records an entity change asynchronously.</summary>
    /// <param name="change">The audit change to record</param>
    /// <param name="ct">Cancellation token</param>
    Task RecordChangeAsync(AuditChange change, CancellationToken ct = default);

    /// <summary>Records multiple entity changes in a single operation.</summary>
    /// <param name="changes">The audit changes to record</param>
    void RecordChanges(IEnumerable<AuditChange> changes);

    /// <summary>Records multiple entity changes in a single operation asynchronously.</summary>
    /// <param name="changes">The audit changes to record</param>
    /// <param name="ct">Cancellation token</param>
    Task RecordChangesAsync(IEnumerable<AuditChange> changes, CancellationToken ct = default);

    /// <summary>Records an audit event (something that occurred and should be logged).</summary>
    /// <param name="evt">The audit event to record</param>
    void RecordEvent(AuditEvent evt);

    /// <summary>Records an audit event asynchronously.</summary>
    /// <param name="evt">The audit event to record</param>
    /// <param name="ct">Cancellation token</param>
    Task RecordEventAsync(AuditEvent evt, CancellationToken ct = default);

    /// <summary>Records multiple audit events in a single operation.</summary>
    /// <param name="events">The audit events to record</param>
    void RecordEvents(IEnumerable<AuditEvent> events);

    /// <summary>Records multiple audit events in a single operation asynchronously.</summary>
    /// <param name="events">The audit events to record</param>
    /// <param name="ct">Cancellation token</param>
    Task RecordEventsAsync(IEnumerable<AuditEvent> events, CancellationToken ct = default);
}