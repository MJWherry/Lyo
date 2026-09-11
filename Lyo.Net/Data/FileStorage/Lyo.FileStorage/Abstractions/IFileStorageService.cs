using System.Text.Json;
using Lyo.Compression.Models;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Models;
using Lyo.Health;

namespace Lyo.FileStorage.Abstractions;

/// <summary>Stores, reads, and removes files, with optional compression and encryption on the write path.</summary>
public interface IFileStorageService : IHealth
{
    /// <summary>Raised after a successful save.</summary>
    event EventHandler<FileSavedResult>? FileSaved;

    /// <summary>Raised after a successful retrieve.</summary>
    event EventHandler<FileRetrievedResult>? FileRetrieved;

    /// <summary>Raised after a successful delete.</summary>
    event EventHandler<FileDeletedResult>? FileDeleted;

    /// <summary>Raised after a file moves to a new path prefix. The file id stays the same.</summary>
    event EventHandler<FileMovedResult>? FileMoved;

    /// <summary>Raised after the display name (<c>OriginalFileName</c>) changes. Metadata only; backing bytes are untouched.</summary>
    event EventHandler<FileRenamedResult>? FileRenamed;

    /// <summary>Raised when metadata is read without the payload. <c>File</c> is a redacted snapshot: wrapped DEK, KEK salt, and similar secrets are omitted.</summary>
    event EventHandler<FileMetadataRetrievedResult>? FileMetadataRetrieved;

    /// <summary>Raised when an audit fact is written (save, read, delete, presigned URL, multipart, DEK migration, and similar).</summary>
    event EventHandler<FileAuditEventArgs>? FileAuditOccurred;

    /// <summary>Writes <paramref name="data" /> into storage.</summary>
    /// <param name="data">Bytes to persist.</param>
    /// <param name="originalFileName">Optional original filename stored on metadata.</param>
    /// <param name="compress">If true, compress before write.</param>
    /// <param name="encrypt">If true, encrypt before write.</param>
    /// <param name="keyId">Encryption key id. Required when <paramref name="encrypt" /> is true.</param>
    /// <param name="pathPrefix">Optional prefix used to group stored files.</param>
    /// <param name="chunkSize">Chunk size for compress/encrypt. When null, chosen from the payload size.</param>
    /// <param name="contentType">Optional MIME type used for policy and metadata.</param>
    /// <param name="charset">Optional client-declared character encoding of the plaintext (IANA/web name). Stored as given after trim. Not converted.</param>
    /// <param name="tenantId">Optional tenant. When null, the ambient operation-context accessor is used if one is registered.</param>
    /// <param name="metadata">Optional opaque caller JSON bag. Lyo does not interpret keys. Null when omitted.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Metadata for the stored file, including id and storage path.</returns>
    Task<FileStoreResult> SaveFileAsync(
        byte[] data,
        string? originalFileName = null,
        bool compress = false,
        bool encrypt = false,
        string? keyId = null,
        string? pathPrefix = null,
        int? chunkSize = null,
        string? contentType = null,
        string? charset = null,
        string? tenantId = null,
        JsonElement? metadata = null,
        CancellationToken ct = default);

    /// <summary>Streams a file from disk into storage. Prefer this overload for large files.</summary>
    /// <param name="filePath">Path of the file to persist</param>
    /// <param name="originalFileName">Optional original filename stored on metadata</param>
    /// <param name="compress">If true, compress before write</param>
    /// <param name="encrypt">If true, encrypt before write</param>
    /// <param name="keyId">Encryption key id. Required when <paramref name="encrypt" /> is true</param>
    /// <param name="pathPrefix">Optional prefix used to group stored files</param>
    /// <param name="chunkSize">Chunk size for compress/encrypt. When null, chosen from the file size.</param>
    /// <param name="contentType">Optional MIME type used for policy and metadata.</param>
    /// <param name="charset">Optional client-declared character encoding of the plaintext (IANA/web name). Stored as given after trim. Not converted.</param>
    /// <param name="tenantId">Optional tenant. When null, the operation-context accessor is used if one is registered.</param>
    /// <param name="metadata">Optional opaque caller JSON bag. Lyo does not interpret keys. Null when omitted.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Metadata for the stored file</returns>
    Task<FileStoreResult> SaveFileAsync(
        string filePath,
        string? originalFileName = null,
        bool compress = false,
        bool encrypt = false,
        string? keyId = null,
        string? pathPrefix = null,
        int? chunkSize = null,
        string? contentType = null,
        string? charset = null,
        string? tenantId = null,
        JsonElement? metadata = null,
        CancellationToken ct = default);

    /// <summary>
    /// Writes <paramref name="input" /> through the same compress/encrypt pipeline as the other save methods. The caller keeps the stream open; this method does not dispose
    /// <paramref name="input" />. Optional <paramref name="charset"/> is stored as given after trim and is not converted. Optional <paramref name="metadata"/> is an opaque
    /// caller JSON bag.
    /// </summary>
    /// <param name="metadata">Optional opaque caller JSON bag. Lyo does not interpret keys. Null when omitted.</param>
    Task<FileStoreResult> SaveFromStreamAsync(
        Stream input,
        long declaredLength,
        string? originalFileName = null,
        bool compress = false,
        bool encrypt = false,
        string? keyId = null,
        string? pathPrefix = null,
        int? chunkSize = null,
        string? contentType = null,
        string? charset = null,
        string? tenantId = null,
        FileAvailability? availabilityOverride = null,
        Guid? fileId = null,
        JsonElement? metadata = null,
        CancellationToken ct = default);

    /// <summary>
    /// Builds a short-lived read URL when the backend can hand the browser a direct GET (S3 or Azure presigned GET, for example). The URL points at the stored object, which is
    /// ciphertext when the file is encrypted. Use <see cref="GetFileStreamAsync" /> for a decrypted download. Key material is never placed in the URL.
    /// </summary>
    /// <remarks>Overload with no response-header overrides. <paramref name="ct" /> may not cancel synchronous signing on some backends.</remarks>
    Task<string> GetPreSignedReadUrlAsync(Guid fileId, TimeSpan? expiration = null, string? pathPrefix = null, CancellationToken ct = default);

    /// <inheritdoc cref="GetPreSignedReadUrlAsync(Guid, TimeSpan?, string?, CancellationToken)" />
    /// <remarks><paramref name="ct" /> may not cancel synchronous presigned-URL construction in vendor SDKs (<c>AWS</c> / <c>Azure</c>).</remarks>
    Task<string> GetPreSignedReadUrlAsync(Guid fileId, TimeSpan? expiration, string? pathPrefix, PreSignedReadUrlOptions? urlResponseOptions, CancellationToken ct = default);

    /// <summary>Allocates metadata and returns a URL for one client PUT. Payload is plaintext: no compression or encryption. Behavior is backend-specific.</summary>
    /// <remarks>Persistence and URL construction honor cancellation where they can. Synchronous signing steps in vendor SDKs may ignore <paramref name="ct" />.</remarks>
    Task<DirectUploadBeginResult> BeginDirectUploadAsync(DirectUploadBeginRequest request, CancellationToken ct = default);

    /// <summary>Checks backing bytes and seals metadata after a direct PUT.</summary>
    Task<FileStoreResult> CompleteDirectUploadAsync(Guid fileId, DirectUploadCompleteRequest? completeRequest = null, CancellationToken ct = default);

    /// <summary>Copies to a new file id on the server (local or server-side). Backing bytes are reproduced; metadata is cloned with new identifiers.</summary>
    /// <remarks>Remote copies can sit in long SDK calls. Treat <paramref name="ct" /> as best-effort when the SDK does not forward tokens.</remarks>
    Task<FileStoreResult> CopyFileAsync(Guid sourceFileId, CopyFileRequest? request = null, CancellationToken ct = default);

    /// <summary>
    /// Moves backing bytes under a new path prefix and keeps the same file id. <see cref="FileStoreResult.SourceFileName" /> stays as-is; only
    /// <see cref="FileStoreResult.PathPrefix" /> changes.
    /// </summary>
    /// <remarks>Cloud backends usually copy then delete. Treat <paramref name="ct" /> as best-effort when the SDK does not forward tokens.</remarks>
    Task<FileStoreResult> MoveFileAsync(Guid fileId, MoveFileRequest request, CancellationToken ct = default);

    /// <summary>Changes <see cref="FileStoreResult.OriginalFileName" /> in metadata only. Object keys and <see cref="FileStoreResult.SourceFileName" /> stay as-is.</summary>
    Task<FileStoreResult> RenameFileAsync(Guid fileId, RenameFileRequest request, CancellationToken ct = default);

    /// <summary>Reads a file from storage as a byte array.</summary>
    /// <param name="fileId">Id of the file to load</param>
    /// <param name="compressionAlgorithmOverride">When set, decompress with this algorithm instead of the value on metadata.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>File bytes. Empty array when the file is missing and ThrowOnFileNotFound is false.</returns>
    /// <exception cref="FileNotFoundException">Raised when the file is missing and ThrowOnFileNotFound is true (the default)</exception>
    Task<byte[]> GetFileAsync(Guid fileId, CompressionAlgorithm? compressionAlgorithmOverride = null, CancellationToken ct = default);

    /// <summary>Reads a file from storage as a stream. The caller disposes the returned stream.</summary>
    /// <param name="fileId">Id of the file to load</param>
    /// <param name="compressionAlgorithmOverride">When set, decompress with this algorithm instead of the value on metadata.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Stream of file data (decrypted and decompressed when those transforms apply), or null when missing and ThrowOnFileNotFound is false.</returns>
    /// <exception cref="FileNotFoundException">Raised when the file is missing and ThrowOnFileNotFound is true (the default)</exception>
    Task<Stream?> GetFileStreamAsync(Guid fileId, CompressionAlgorithm? compressionAlgorithmOverride = null, CancellationToken ct = default);

    /// <summary>Removes a file from storage.</summary>
    /// <param name="fileId">Id of the file to remove</param>
    /// <param name="mode">Keep metadata as a tombstone, or purge it together with the backing object.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>
    /// True when the backing object was removed. False only when the file was already missing and <c>ThrowOnDeleteNotFound</c> is off. False never means the delete failed —
    /// every other failure throws.
    /// </returns>
    /// <exception cref="FileNotFoundException">Raised when the file is missing and ThrowOnDeleteNotFound is true (the default)</exception>
    /// <remarks>
    /// <para>
    /// <paramref name="mode" /> must come from operator or retention policy. Do not take <see cref="FileDeletionMode.RemoveObjectAndPurgeMetadata" /> from inbound HTTP/API
    /// client input (queries, bodies, headers, and similar).
    /// </para>
    /// <para>
    /// Metadata is tombstoned or purged before the backing object is removed. If this method throws, the delete is only half done: the metadata row is already gone and the
    /// object may still sit in storage. Treat a throw as "retry or reconcile", not as "nothing happened".
    /// </para>
    /// </remarks>
    Task<bool> DeleteFileAsync(Guid fileId, FileDeletionMode mode = FileDeletionMode.RemoveObjectAndTombstoneMetadata, CancellationToken ct = default);

    /// <summary>Loads metadata for a file by id.</summary>
    /// <param name="fileId">Id of the file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Metadata including original filename, size, hash, and encryption/compression flags.</returns>
    /// <exception cref="FileNotFoundException">Raised when the file is missing.</exception>
    Task<FileStoreResult> GetMetadataAsync(Guid fileId, CancellationToken ct = default);

    /// <summary>
    /// Re-wraps DEKs that were encrypted under a given KEK version onto the current or latest KEK. Use this when rotating keys so older files pick up the newest KEK.
    /// </summary>
    /// <param name="sourceKeyId">Key id that originally wrapped the files</param>
    /// <param name="sourceKeyVersion">KEK version to migrate from. When null, every version for this keyId is migrated.</param>
    /// <param name="targetKeyId">Key id to migrate onto. When null, <paramref name="sourceKeyId" /> is reused (same key, new version).</param>
    /// <param name="targetKeyVersion">KEK version to migrate onto. When null, the current version of targetKeyId is used.</param>
    /// <param name="batchSize">Files processed per batch. Starts as 100.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Migration statistics</returns>
    Task<DekMigrationResult> MigrateDeksAsync(
        string sourceKeyId,
        string? sourceKeyVersion = null,
        string? targetKeyId = null,
        string? targetKeyVersion = null,
        int batchSize = 100,
        CancellationToken ct = default);

    /// <summary>
    /// Builds a new DEK for each listed file by decrypting and re-encrypting the payload. Can also move the files onto a different key id or version in the same pass.
    /// </summary>
    /// <param name="fileIds">File ids whose DEKs should be rebuilt.</param>
    /// <param name="targetKeyId">Optional target key id. When null, each file keeps its current key id.</param>
    /// <param name="targetKeyVersion">
    /// Optional target key version. When null and <paramref name="targetKeyId" /> is set, that key's current version is used. When both are null,
    /// each file keeps its current key version.
    /// </param>
    /// <param name="batchSize">Files processed per batch. Starts as 100.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Rotation statistics.</returns>
    Task<DekMigrationResult> RotateDeksAsync(
        IReadOnlyCollection<Guid> fileIds,
        string? targetKeyId = null,
        string? targetKeyVersion = null,
        int batchSize = 100,
        CancellationToken ct = default);
}