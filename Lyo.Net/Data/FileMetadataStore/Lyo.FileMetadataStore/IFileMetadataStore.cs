using Lyo.FileMetadataStore.Models;

namespace Lyo.FileMetadataStore;

/// <summary>File metadata store, independent of blob storage. Metadata can live in Postgres, SQLite, or files while blobs live on disk, S3, or another backend.</summary>
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
    /// Soft-deletes metadata: writes a deletion timestamp and <see cref="FileAvailability.Deleted" /> instead of removing the row. Later <see cref="GetMetadataAsync" /> and
    /// duplicate lookups skip tombstones; idempotent deletes return false when already logically deleted.
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
    Task<IReadOnlyList<FileStoreResult>> ListByPathPrefixAsync(string? pathPrefix, bool includeDescendants, int maxKeys, CancellationToken ct = default);

    /// <summary>Lists files encrypted with a given keyId and optional version (key rotation/migration).</summary>
    /// <param name="keyId">Key identifier to match.</param>
    /// <param name="keyVersion">Optional key version. Null matches every version for this keyId.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Matching file metadata.</returns>
    Task<IEnumerable<FileStoreResult>> FindByKeyIdAndVersionAsync(string keyId, string? keyVersion = null, CancellationToken ct = default);

    /// <summary>
    /// Lists non-deleted <see cref="FileAvailability.PendingDirectUpload" /> rows whose <see cref="FileStoreResult.Timestamp" /> (set when the upload began) is earlier than
    /// <paramref name="beganBefore" />, oldest first. Backs the FileStorage sweeper that aborts direct uploads nobody finalized.
    /// </summary>
    /// <param name="beganBefore">UTC cutoff. Rows that began at or after it are not returned.</param>
    /// <param name="maxKeys">Maximum rows to return. Must be at least 1.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<FileStoreResult>> ListPendingDirectUploadsAsync(DateTime beganBefore, int maxKeys, CancellationToken ct = default);

    /// <summary>Non-deleted files whose <see cref="FileStoreResult.OriginalFileName" /> contains <paramref name="text" />. This is a contains query, not full-text search.</summary>
    Task<IReadOnlyList<FileStoreResult>> SearchByNameAsync(string text, int maxKeys, CancellationToken ct = default);

    /// <summary>Records the current stored key as a prior revision, then stores <paramref name="storedKey" /> on the file. The previous key stays readable.</summary>
    Task<FileRevision> SaveNewVersionAsync(Guid fileId, string storedKey, CancellationToken ct = default);

    /// <summary>Loads a prior revision by id, or null when it was never recorded.</summary>
    Task<FileRevision?> GetVersionAsync(Guid revisionId, CancellationToken ct = default);

    /// <summary>Adds a tag on <paramref name="fileId" />. A second add of the same type and value returns the existing row.</summary>
    Task<FileTag> AddFileTagAsync(Guid fileId, string tagType, string value, string? slug, bool automatic, CancellationToken ct = default);

    /// <summary>Removes one tag. Returns false when that pair is not stored.</summary>
    Task<bool> RemoveFileTagAsync(Guid fileId, string tagType, string value, CancellationToken ct = default);

    /// <summary>Tags on one file. Empty when the file has none.</summary>
    Task<IReadOnlyList<FileTag>> ListFileTagsAsync(Guid fileId, CancellationToken ct = default);

    /// <summary>Copies every tag from <paramref name="sourceFileId" /> onto <paramref name="destinationFileId" />.</summary>
    Task CopyFileTagsAsync(Guid sourceFileId, Guid destinationFileId, CancellationToken ct = default);

    /// <summary>Deletes every tag for a file. Local purge calls this because the sidecar has no foreign key.</summary>
    Task DeleteFileTagsAsync(Guid fileId, CancellationToken ct = default);

    /// <summary>Distinct manual tag types on non-deleted files, unordered.</summary>
    Task<IReadOnlyList<string>> ListManualTagTypesAsync(CancellationToken ct = default);

    /// <summary>One level of the virtual folder tree for <paramref name="groupBy" />. Blob keys are not moved.</summary>
    Task<FileTagGroupPage> ListTagGroupAsync(IReadOnlyList<FileTagGroupKey> groupBy, string? virtualPath, int maxKeys, CancellationToken ct = default);
}