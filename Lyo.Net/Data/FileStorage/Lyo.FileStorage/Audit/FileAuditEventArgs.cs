namespace Lyo.FileStorage.Audit;

/// <summary>Event arguments for <see cref="IFileStorageService.FileAuditOccurred" />.</summary>
public sealed class FileAuditEventArgs : EventArgs
{
    public FileAuditEventArgs(FileAuditEvent audit, CancellationToken cancellationToken)
    {
        Audit = audit;
        CancellationToken = cancellationToken;
    }

    public FileAuditEvent Audit { get; }

    public CancellationToken CancellationToken { get; }
}