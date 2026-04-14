using Lyo.Cache;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Audit;

namespace Lyo.TestApi.FileStorageWorkbench;

/// <summary>
/// Invalidates the API QueryProject cache for <see cref="FileMetadataEntity" /> when file storage reports mutating operations. Lives in the API host so only the layer that caches
/// queries depends on <see cref="ICacheService" />.
/// </summary>
public sealed class FileMetadataQueryCacheInvalidationHandler : IFileAuditEventHandler
{
    private readonly ICacheService _cache;

    public FileMetadataQueryCacheInvalidationHandler(ICacheService cache) =>
        _cache = cache;

    public Task HandleAsync(FileAuditEvent auditEvent, CancellationToken ct = default)
    {
        if (!ShouldInvalidate(auditEvent))
            return Task.CompletedTask;

        return _cache.InvalidateQueryCacheAsync<FileMetadataEntity>();
    }

    /// <summary>Migrate/rotate invalidate even when audit outcome is failure because some rows may have been updated.</summary>
    private static bool ShouldInvalidate(FileAuditEvent e) => e.EventType switch {
        FileAuditEventType.Save or FileAuditEventType.Delete or FileAuditEventType.MultipartComplete => e.Outcome == FileAuditOutcome.Success,
        FileAuditEventType.MigrateDeks or FileAuditEventType.RotateDeks => true,
        _ => false
    };
}
