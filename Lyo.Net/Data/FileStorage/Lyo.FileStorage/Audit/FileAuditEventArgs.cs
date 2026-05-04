namespace Lyo.FileStorage.Audit;

/// <summary>Event arguments for <see cref="IFileStorageService.FileAuditOccurred" />.</summary>
public sealed class FileAuditEventArgs(FileAuditEvent audit, CancellationToken ct) : EventArgs
{
    public FileAuditEvent Audit { get; } = audit;

    public CancellationToken CancellationToken { get; } = ct;
}