using Lyo.FileStorage.Audit;

namespace Lyo.FileStorage.Abstractions;

/// <summary>Sends structured <see cref="FileAuditEvent" /> rows through the owning storage service audit pipeline.</summary>
internal interface IFileAuditPublisher
{
    Task PublishAuditAsync(FileAuditEvent auditEvent, CancellationToken ct);
}