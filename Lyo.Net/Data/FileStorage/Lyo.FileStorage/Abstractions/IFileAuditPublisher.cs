using Lyo.FileStorage.Audit;

namespace Lyo.FileStorage.Abstractions;

/// <summary>Forwards structured <see cref="FileAuditEvent"/> instances through the owning storage service audit pipeline.</summary>
internal interface IFileAuditPublisher
{
    Task PublishAuditAsync(FileAuditEvent auditEvent, CancellationToken ct);
}
