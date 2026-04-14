namespace Lyo.Audit;

/// <summary>No-op implementation of IAuditRecorder that discards all audit entries. Use as a default when auditing is not needed.</summary>
public class NullAuditRecorder : IAuditRecorder
{
    /// <summary>Gets the singleton instance of NullAuditRecorder.</summary>
    public static NullAuditRecorder Instance { get; } = new();

    private NullAuditRecorder() { }

    /// <inheritdoc />
    public void RecordChange(AuditChange change) { }

    /// <inheritdoc />
    public Task RecordChangeAsync(AuditChange change, CancellationToken ct = default) => Task.CompletedTask;

    /// <inheritdoc />
    public void RecordChanges(IEnumerable<AuditChange> changes) { }

    /// <inheritdoc />
    public Task RecordChangesAsync(IEnumerable<AuditChange> changes, CancellationToken ct = default) => Task.CompletedTask;

    /// <inheritdoc />
    public void RecordEvent(AuditEvent evt) { }

    /// <inheritdoc />
    public Task RecordEventAsync(AuditEvent evt, CancellationToken ct = default) => Task.CompletedTask;

    /// <inheritdoc />
    public void RecordEvents(IEnumerable<AuditEvent> events) { }

    /// <inheritdoc />
    public Task RecordEventsAsync(IEnumerable<AuditEvent> events, CancellationToken ct = default) => Task.CompletedTask;
}