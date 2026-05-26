using Lyo.FileMetadataStore.Models;

namespace Lyo.FileMetadataStore;

/// <summary>
/// Store for managing file metadata independently from file storage. This allows metadata to be stored in different backends (Postgres, SQLite, files, etc.) while files can
/// be stored in different locations (local filesystem, AWS S3, etc.).
/// </summary>
public interface IFileMetadataStore
{
    /// <summary>Retrieves metadata for a file by its ID.</summary>
    /// <param name="fileId">The unique identifier of the file</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The file metadata</returns>
    /// <exception cref="FileNotFoundException">Thrown when metadata for the file is not found</exception>
    Task<FileStoreResult> GetMetadataAsync(Guid fileId, CancellationToken ct = default);

    /// <summary>Saves metadata for a file.</summary>
    /// <param name="fileId">The unique identifier of the file</param>
    /// <param name="metadata">The metadata to save</param>
    /// <param name="ct">Cancellation token</param>
    Task SaveMetadataAsync(Guid fileId, FileStoreResult metadata, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes metadata: sets deletion timestamp instead of removing the row. Subsequent <see cref="GetMetadataAsync" /> and duplicate lookups omit tombstones; idempotent
    /// deletes return false if already logically deleted.
    /// </summary>
    /// <param name="fileId">The unique identifier of the file</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True when the row transitioned to deleted now; false if missing or already soft-deleted</returns>
    Task<bool> DeleteMetadataAsync(Guid fileId, CancellationToken ct = default);

    /// <summary>
    /// Permanently removes persisted metadata (database row or <c>.meta</c> JSON file). Use with operator-backed file deletion when metadata must not remain as a tombstone.
    /// Idempotent: returns <see langword="false" /> when no record existed.
    /// </summary>
    Task<bool> PurgeMetadataAsync(Guid fileId, CancellationToken ct = default);

    /// <summary>Finds metadata by file hash among non-deleted files. Used for duplicate detection.</summary>
    /// <param name="hash">The hash of the original file data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The metadata if found, null otherwise</returns>
    Task<FileStoreResult?> FindByHashAsync(byte[] hash, CancellationToken ct = default);

    /// <summary>Finds all files encrypted with a specific keyId and optionally a specific version. Used for key rotation/migration scenarios.</summary>
    /// <param name="keyId">The key identifier to search for</param>
    /// <param name="keyVersion">Optional key version. If null, finds all versions for this keyId.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Collection of file metadata matching the criteria</returns>
    Task<IEnumerable<FileStoreResult>> FindByKeyIdAndVersionAsync(string keyId, string? keyVersion = null, CancellationToken ct = default);
}