namespace Lyo.Audit;

/// <summary>Writes audit entries. Implement this to hook a store (database, log sink, and similar).</summary>
public interface IAuditRecorder
{
    /// <summary>Persists one entity change (per-property before/after diff).</summary>
    /// <param name="change">Change record to persist</param>
    void RecordChange(AuditChange change);

    /// <summary>Persists one entity change without blocking the caller.</summary>
    /// <param name="change">Change record to persist</param>
    /// <param name="ct">Token used to cancel the write</param>
    Task RecordChangeAsync(AuditChange change, CancellationToken ct = default);

    /// <summary>Persists several entity changes as one write.</summary>
    /// <param name="changes">Change records to persist</param>
    void RecordChanges(IEnumerable<AuditChange> changes);

    /// <summary>Persists several entity changes as one write without blocking the caller.</summary>
    /// <param name="changes">Change records to persist</param>
    /// <param name="ct">Token used to cancel the write</param>
    Task RecordChangesAsync(IEnumerable<AuditChange> changes, CancellationToken ct = default);

    /// <summary>Persists an audit event (something that happened and belongs in the log).</summary>
    /// <param name="evt">Event record to persist</param>
    void RecordEvent(AuditEvent evt);

    /// <summary>Persists an audit event without blocking the caller.</summary>
    /// <param name="evt">Event record to persist</param>
    /// <param name="ct">Token used to cancel the write</param>
    Task RecordEventAsync(AuditEvent evt, CancellationToken ct = default);

    /// <summary>Persists several audit events as one write.</summary>
    /// <param name="events">Event records to persist</param>
    void RecordEvents(IEnumerable<AuditEvent> events);

    /// <summary>Persists several audit events as one write without blocking the caller.</summary>
    /// <param name="events">Event records to persist</param>
    /// <param name="ct">Token used to cancel the write</param>
    Task RecordEventsAsync(IEnumerable<AuditEvent> events, CancellationToken ct = default);
}