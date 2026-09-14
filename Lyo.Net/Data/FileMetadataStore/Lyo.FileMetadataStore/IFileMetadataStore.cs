using Lyo.FileMetadataStore.Models;

namespace Lyo.FileMetadataStore;

/// <summary>
/// File metadata store, independent of blob storage. Metadata can live in Postgres, SQLite, or files while blobs live on disk, S3, or another backend.
/// </summary>
public interface IFileMetadataStore
{
    /// <summary>Loads metadata for a file by ID.</summary>
    /// <param name="fileId">File identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>File metadata.</returns>
    /// <exception cref="FileNotFoundException">Raised when metadata for the file is missing.</exception>
    Task<FileStoreResult> GetMetadataAsync(Guid fileId, CancellationToken ct = default);

    /// <summary>Persists metadata for a file.</summary>
    /// <param name="fileId">File identifier.</param>
    /// <param name="metadata">Metadata to write.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SaveMetadataAsync(Guid fileId, FileStoreResult metadata, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes metadata: writes a deletion timestamp and <see cref="FileAvailability.Deleted" /> instead of removing the row. Later
    /// <see cref="GetMetadataAsync" /> and duplicate lookups skip tombstones; idempotent deletes return false when already logically deleted.
    /// </summary>
    /// <param name="fileId">File identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True when the row became deleted now; false if missing or already soft-deleted.</returns>
    Task<bool> DeleteMetadataAsync(Guid fileId, CancellationToken ct = default);

    /// <summary>
    /// Permanently removes persisted metadata (database row or <c>.meta</c> JSON file). Pair with operator-backed blob deletion when metadata must not remain as a tombstone.
    /// Idempotent: returns <see langword="false" /> when no record existed.
    /// </summary>
    Task<bool> PurgeMetadataAsync(Guid fileId, CancellationToken ct = default);

    /// <summary>Looks up metadata by file hash among non-deleted files (duplicate detection).</summary>
    /// <param name="hash">Hash of the original file bytes.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Matching metadata, or null when none.</returns>
    Task<FileStoreResult?> FindByHashAsync(byte[] hash, CancellationToken ct = default);

    /// <summary>
    /// Lists non-deleted files whose <see cref="FileStoreResult.PathPrefix" /> matches <paramref name="pathPrefix" />. Immediate files only when
    /// <paramref name="includeDescendants" /> is false; nested prefixes when true. No schema change; used by the FileStorage virtual file system.
    /// </summary>
    /// <param name="pathPrefix">Logical prefix. Null or empty is the tree root.</param>
    /// <param name="includeDescendants">When true, also return files under nested prefixes.</param>
    /// <param name="maxKeys">Maximum rows to return. Must be at least 1.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<FileStoreResult>> ListByPathPrefixAsync(
        string? pathPrefix,
        bool includeDescendants,
        int maxKeys,
        CancellationToken ct = default);

    /// <summary>Lists files encrypted with a given keyId and optional version (key rotation/migration).</summary>
    /// <param name="keyId">Key identifier to match.</param>
    /// <param name="keyVersion">Optional key version. Null matches every version for this keyId.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Matching file metadata.</returns>
    Task<IEnumerable<FileStoreResult>> FindByKeyIdAndVersionAsync(string keyId, string? keyVersion = null, CancellationToken ct = default);
}