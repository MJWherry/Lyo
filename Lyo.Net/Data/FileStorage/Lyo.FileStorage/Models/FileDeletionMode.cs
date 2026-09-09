namespace Lyo.FileStorage.Models;

/// <summary>
/// How
/// <see cref="Lyo.FileStorage.Abstractions.IFileStorageService.DeleteFileAsync(System.Guid,Lyo.FileStorage.Models.FileDeletionMode,System.Threading.CancellationToken)" />
/// records metadata removal after the backing object is deleted.
/// </summary>
public enum FileDeletionMode
{
    /// <summary>
    /// Hard-delete the backing object, then tombstone metadata (soft delete with <see cref="FileMetadataStore.Models.FileStoreResult.DeletedAt" />). This is the
    /// default.
    /// </summary>
    RemoveObjectAndTombstoneMetadata = 0,

    /// <summary>
    /// Hard-delete the backing object, then permanently drop the metadata row. Operators use this for retention and governance only. Do not pick this mode from inbound
    /// end-user or API request input. Retention is a server-owner decision.
    /// </summary>
    /// <remarks>
    /// Same entry rules as the default path: metadata must resolve through active (non-soft-deleted) metadata. Already tombstoned files cannot be purged unless a separate
    /// internal path skips tombstone filtering.
    /// </remarks>
    RemoveObjectAndPurgeMetadata = 1
}