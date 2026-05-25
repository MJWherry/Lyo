using Lyo.FileStorage.Audit;

namespace Lyo.FileStorage.Tests.Support;

/// <summary>Test sink that captures every audit event for later inspection. Thread-safe enough for the single-test scope it is used in (xUnit serializes by default per class).</summary>
public sealed class CaptureAuditHandler : IFileAuditEventHandler
{
    public List<FileAuditEvent> Events { get; } = [];

    public Task HandleAsync(FileAuditEvent auditEvent, CancellationToken ct = default)
    {
        Events.Add(auditEvent);
        return Task.CompletedTask;
    }
}
