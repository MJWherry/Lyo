using Lyo.FileStorage.Abstractions;

namespace Lyo.FileStorage.Audit;

/// <summary>Receives file audit events from <see cref="IFileStorageService" /> and multipart upload services.</summary>
public interface IFileAuditEventHandler
{
    Task HandleAsync(FileAuditEvent auditEvent, CancellationToken ct = default);
}