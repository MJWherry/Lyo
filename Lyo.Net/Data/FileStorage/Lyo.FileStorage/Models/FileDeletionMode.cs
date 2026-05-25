namespace Lyo.FileStorage.Models;

/// <summary>How <see cref="Lyo.FileStorage.Abstractions.IFileStorageService.DeleteFileAsync(System.Guid,Lyo.FileStorage.Models.FileDeletionMode,System.Threading.CancellationToken)" /> persists metadata removal after deleting the backing object.</summary>
public enum FileDeletionMode
{
    /// <summary>
    /// Remove the backing object from storage (hard delete), then tombstone metadata (soft delete with <see cref="FileMetadataStore.Models.FileStoreResult.DeletedAt" />). Default.
    /// </summary>
    RemoveObjectAndTombstoneMetadata = 0,

    /// <summary>
    /// Remove the backing object from storage, then permanently remove the metadata row or underlying metadata record. Operators use this only for retention/governance —
    /// do not choose this mode from inbound end-user/API request input (retention is a server-owner decision).
    /// </summary>
    /// <remarks>
    /// Same entry preconditions as the default flow: metadata must resolve via active (non–soft-deleted) metadata; already tombstoned files are not reachable for purge unless a
    /// separate internal path bypasses tombstone filtering.
    /// </remarks>
    RemoveObjectAndPurgeMetadata = 1
}
