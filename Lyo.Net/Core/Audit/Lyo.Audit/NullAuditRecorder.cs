namespace Lyo.Audit;

/// <summary>An <see cref="IAuditRecorder" /> that drops every entry. Suitable as the default when you do not need auditing.</summary>
public class NullAuditRecorder : IAuditRecorder
{
    /// <summary>Shared singleton <see cref="NullAuditRecorder" />.</summary>
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