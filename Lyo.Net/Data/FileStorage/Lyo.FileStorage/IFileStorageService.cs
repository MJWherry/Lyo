using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Models;
using Lyo.Health;

namespace Lyo.FileStorage;

/// <summary>Service for saving, retrieving, and deleting files with optional compression and encryption.</summary>
public interface IFileStorageService : IHealth
{
    /// <summary>Occurs when a file has been saved successfully.</summary>
    event EventHandler<FileSavedResult>? FileSaved;

    /// <summary>Occurs when a file has been retrieved successfully.</summary>
    event EventHandler<FileRetrievedResult>? FileRetrieved;

    /// <summary>Occurs when a file has been deleted successfully.</summary>
    event EventHandler<FileDeletedResult>? FileDeleted;

    /// <summary>Occurs when a file audit fact is recorded (save, read, delete, presigned URL, multipart, DEK migration, etc.).</summary>
    event EventHandler<FileAuditEventArgs>? FileAuditOccurred;

    /// <summary>Saves file data to storage.</summary>
    /// <param name="data">The file data to save.</param>
    /// <param name="originalFileName">Optional original filename for metadata.</param>
    /// <param name="compress">Whether to compress the file.</param>
    /// <param name="encrypt">Whether to encrypt the file.</param>
    /// <param name="keyId">Key ID for encryption (required if encrypt is true).</param>
    /// <param name="pathPrefix">Optional path prefix for organizing files.</param>
    /// <param name="chunkSize">Chunk size for compression/encryption. If null, automatically determined based on data size.</param>
    /// <param name="contentType">Optional MIME type for policy checks and metadata.</param>
    /// <param name="tenantId">Optional tenant; when null, uses the ambient operation context accessor when configured.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>File metadata result including file ID and storage path.</returns>
    Task<FileStoreResult> SaveFileAsync(
        byte[] data,
        string? originalFileName = null,
        bool compress = false,
        bool encrypt = false,
        string? keyId = null,
        string? pathPrefix = null,
        int? chunkSize = null,
        string? contentType = null,
        string? tenantId = null,
        CancellationToken ct = default);

    /// <summary>Saves a file from disk to storage using streaming for optimal performance with large files.</summary>
    /// <param name="filePath">Path to the file to save</param>
    /// <param name="originalFileName">Optional original filename for metadata</param>
    /// <param name="compress">Whether to compress the file</param>
    /// <param name="encrypt">Whether to encrypt the file</param>
    /// <param name="keyId">Key ID for encryption (required if encrypt is true)</param>
    /// <param name="pathPrefix">Optional path prefix for organizing files</param>
    /// <param name="chunkSize">Chunk size for compression/encryption operations. If null, automatically determined based on file size.</param>
    /// <param name="contentType">Optional MIME type for policy checks and metadata.</param>
    /// <param name="tenantId">Optional tenant; when null, uses operation context accessor when configured.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>File metadata result</returns>
    Task<FileStoreResult> SaveFileAsync(
        string filePath,
        string? originalFileName = null,
        bool compress = false,
        bool encrypt = false,
        string? keyId = null,
        string? pathPrefix = null,
        int? chunkSize = null,
        string? contentType = null,
        string? tenantId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Saves a stream through the same compression/encryption pipeline as other save APIs. The caller must leave the stream open; the implementation does not dispose
    /// <paramref name="input" />.
    /// </summary>
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
        string? tenantId = null,
        FileAvailability? availabilityOverride = null,
        Guid? fileId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a time-limited read URL when the backend supports direct browser access (e.g. S3/Azure presigned GET). The URL refers to the stored object (ciphertext if encrypted). For decrypted downloads use
    /// <see cref="GetFileStreamAsync"/>. Key material is never embedded in the URL.
    /// </summary>
    Task<string> GetPreSignedReadUrlAsync(Guid fileId, TimeSpan? expiration = null, string? pathPrefix = null, CancellationToken ct = default);

    /// <summary>Gets a file from storage.</summary>
    /// <param name="fileId">The unique identifier of the file to retrieve</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The file data as a byte array. Returns empty array if file not found and ThrowOnFileNotFound is false.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file is not found and ThrowOnFileNotFound is true (default)</exception>
    Task<byte[]> GetFileAsync(Guid fileId, CancellationToken ct = default);

    /// <summary>Gets a file from storage as a stream. Caller must dispose the returned stream.</summary>
    /// <param name="fileId">The unique identifier of the file to retrieve</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>A stream containing the file data (decrypted and decompressed if applicable), or null if file not found and ThrowOnFileNotFound is false.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file is not found and ThrowOnFileNotFound is true (default)</exception>
    Task<Stream?> GetFileStreamAsync(Guid fileId, CancellationToken ct = default);

    /// <summary>Deletes a file from storage.</summary>
    /// <param name="fileId">The unique identifier of the file to delete</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if the file was successfully deleted. Returns false if file not found and ThrowOnDeleteNotFound is false.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file is not found and ThrowOnDeleteNotFound is true (default)</exception>
    Task<bool> DeleteFileAsync(Guid fileId, CancellationToken ct = default);

    /// <summary>Gets metadata for a file by its ID.</summary>
    /// <param name="fileId">The unique identifier of the file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>File metadata including original filename, size, hash, and encryption/compression info.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file is not found.</exception>
    Task<FileStoreResult> GetMetadataAsync(Guid fileId, CancellationToken ct = default);

    /// <summary>
    /// Re-encrypts DEKs for files encrypted with a specific KEK version using the current/latest KEK version. This is useful for key rotation scenarios where you want to migrate
    /// older files to use the latest KEK.
    /// </summary>
    /// <param name="sourceKeyId">The key identifier that was used to encrypt the files</param>
    /// <param name="sourceKeyVersion">The KEK version to migrate from. If null, migrates all versions for this keyId.</param>
    /// <param name="targetKeyId">The key identifier to migrate to. If null, uses sourceKeyId (same key, different version).</param>
    /// <param name="targetKeyVersion">The KEK version to migrate to. If null, uses the current version of targetKeyId.</param>
    /// <param name="batchSize">Number of files to process per batch. Default is 100.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result containing migration statistics</returns>
    Task<DekMigrationResult> MigrateDeksAsync(
        string sourceKeyId,
        string? sourceKeyVersion = null,
        string? targetKeyId = null,
        string? targetKeyVersion = null,
        int batchSize = 100,
        CancellationToken ct = default);

    /// <summary>
    /// Re-creates the DEK for the specified files by decrypting and re-encrypting each file with a fresh DEK. This can optionally migrate the files to a different key ID or
    /// version at the same time.
    /// </summary>
    /// <param name="fileIds">File IDs whose DEKs should be re-created.</param>
    /// <param name="targetKeyId">Optional target key identifier. If null, each file keeps its current key ID.</param>
    /// <param name="targetKeyVersion">
    /// Optional target key version. If null and <paramref name="targetKeyId" /> is provided, the current version of that key is used. If both are null,
    /// each file keeps its current key version.
    /// </param>
    /// <param name="batchSize">Number of files to process per batch. Default is 100.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing rotation statistics.</returns>
    Task<DekMigrationResult> RotateDeksAsync(
        IReadOnlyCollection<Guid> fileIds,
        string? targetKeyId = null,
        string? targetKeyVersion = null,
        int batchSize = 100,
        CancellationToken ct = default);
}