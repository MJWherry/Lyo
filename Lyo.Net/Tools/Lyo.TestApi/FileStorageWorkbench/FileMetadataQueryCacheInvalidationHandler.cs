using Lyo.Cache;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Audit;

namespace Lyo.TestApi.FileStorageWorkbench;

/// <summary>
/// Drops the API QueryProject cache for <see cref="FileMetadataEntity" /> after file storage records a mutating operation. Hosted in the API so only the query-caching layer
/// takes a dependency on <see cref="ICacheService" />.
/// </summary>
public sealed class FileMetadataQueryCacheInvalidationHandler : IFileAuditEventHandler
{
    private readonly ICacheService _cache;

    public FileMetadataQueryCacheInvalidationHandler(ICacheService cache) => _cache = cache;

    public Task HandleAsync(FileAuditEvent auditEvent, CancellationToken ct = default)
    {
        if (!ShouldInvalidate(auditEvent))
            return Task.CompletedTask;

        return _cache.InvalidateQueryCacheAsync<FileMetadataEntity>();
    }

    /// <summary>Migrate and rotate still invalidate after a failed audit outcome, since some rows may already have changed.</summary>
    private static bool ShouldInvalidate(FileAuditEvent e)
        => e.EventType switch {
            FileAuditEventType.Save or FileAuditEventType.Delete or FileAuditEventType.MultipartComplete => e.Outcome == FileAuditOutcome.Success,
            FileAuditEventType.MigrateDeks or FileAuditEventType.RotateDeks => true,
            var _ => false
        };
}