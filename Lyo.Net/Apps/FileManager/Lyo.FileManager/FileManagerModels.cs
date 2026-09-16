using Lyo.Api.FileStorage.Models;
using Lyo.FileMetadataStore.Models;

namespace Lyo.FileManager;

/// <summary>One child of a folder listing.</summary>
public sealed record FileManagerEntry(
    string Name,
    bool IsDirectory,
    string? PathPrefix,
    Guid? FileId,
    FileStoragePresence Presence,
    long Size,
    string? PhysicalKey,
    bool Pending = false,
    bool IsCompressed = false,
    string? Compression = null,
    bool IsEncrypted = false,
    string? Encryption = null);

/// <summary>A FileMetadata query row.</summary>
public sealed record FileManagerQueryRow(Guid Id, string? OriginalFileName, string? PathPrefix, long OriginalFileSize);

/// <summary>Immediate children of one prefix.</summary>
public sealed record FileManagerListing(IReadOnlyList<FileManagerEntry> Entries, bool Truncated);

/// <summary>Connect probe.</summary>
public sealed record FileManagerHealth(bool IsHealthy, string? Message);

/// <summary>How many steps of a bulk action succeeded.</summary>
public sealed record FileManagerActionResult(int Succeeded, int Failed, IReadOnlyList<string> Errors);

/// <summary>Options for one upload into the current prefix.</summary>
public sealed class FileManagerUploadOptions
{
    /// <summary>Compress before write.</summary>
    public bool Compress { get; init; }

    /// <summary>Encrypt before write. Requires a key id.</summary>
    public bool Encrypt { get; init; }

    /// <summary>Encryption key id. Required when <see cref="Encrypt" /> is true.</summary>
    public string? KeyId { get; set; }

    /// <summary>MIME type stored on metadata.</summary>
    public string? ContentType { get; init; }

    /// <summary>Character encoding stored on metadata.</summary>
    public string? Charset { get; init; }

    /// <summary>Files larger than this many bytes use multipart. Null uses <see cref="FileManagerLimits.MultipartThresholdBytes" />.</summary>
    public int? MultipartThresholdBytes { get; init; }

    /// <summary>Compression algorithm name, such as <c>GZip</c> or <c>Brotli</c>. Used when <see cref="Compress" /> is true. Null keeps Brotli.</summary>
    public string? CompressionAlgorithm { get; init; }

    /// <summary>Encryption algorithm name, such as <c>AesSiv</c> or <c>AesGcm</c>. Used when <see cref="Encrypt" /> is true. Null keeps AES-SIV.</summary>
    public string? EncryptionAlgorithm { get; init; }
}

/// <summary>Shared limits for uploads and the local key.</summary>
public static class FileManagerLimits
{
    /// <summary>Uploads larger than 8 MiB use multipart.</summary>
    public const int MultipartThresholdBytes = 8 * 1024 * 1024;

    /// <summary>Key id derived from a local server passphrase.</summary>
    public const string LocalKeyId = "file-manager";

    /// <summary>Version of <see cref="LocalKeyId" />.</summary>
    public const string LocalKeyVersion = "1";
}

/// <summary>Gateway the session uses for a remote API or a local store.</summary>
public interface IFileManagerApi : IDisposable
{
    /// <summary>Probes the server.</summary>
    Task<FileManagerHealth> CheckHealthAsync(CancellationToken ct);

    /// <summary>Loads feature flags so the client can enable or disable actions.</summary>
    Task<FileStorageCapabilitiesResponse> GetCapabilitiesAsync(CancellationToken ct);

    /// <summary>Lists one prefix.</summary>
    Task<FileManagerListing> ListAsync(string? pathPrefix, CancellationToken ct);

    /// <summary>Finds files by name and prefix.</summary>
    Task<IReadOnlyList<FileManagerQueryRow>> QueryAsync(string? nameContains, string? pathPrefix, CancellationToken ct);

    /// <summary>Stores a local file at <paramref name="pathPrefix" />.</summary>
    Task UploadAsync(string filePath, string? pathPrefix, FileManagerUploadOptions options, CancellationToken ct);

    /// <summary>Writes a decrypted catalog file to <paramref name="destinationPath" />.</summary>
    Task DownloadAsync(Guid fileId, string destinationPath, CancellationToken ct);

    /// <summary>Writes a zip of the ids to <paramref name="destinationPath" />.</summary>
    Task DownloadArchiveAsync(IReadOnlyList<Guid> fileIds, string destinationPath, CancellationToken ct);

    /// <summary>Loads metadata, including a tombstone when <paramref name="includeDeleted" /> is set.</summary>
    Task<FileStoreResult> GetMetadataAsync(Guid fileId, bool includeDeleted, CancellationToken ct);

    /// <summary>Changes the display name.</summary>
    Task RenameAsync(Guid fileId, string originalFileName, CancellationToken ct);

    /// <summary>Moves the file onto <paramref name="pathPrefix" />.</summary>
    Task MoveAsync(Guid fileId, string? pathPrefix, CancellationToken ct);

    /// <summary>Copies the file onto <paramref name="pathPrefix" />.</summary>
    Task CopyAsync(Guid fileId, string? pathPrefix, CancellationToken ct);

    /// <summary>Deletes a catalog file.</summary>
    Task DeleteAsync(Guid fileId, CancellationToken ct);

    /// <summary>Creates a download access link and returns a URL to copy.</summary>
    Task<string> CreateAccessLinkAsync(Guid fileId, DateTime? expiresAtUtc, int? maxDownloads, CancellationToken ct);

    /// <summary>Returns a presigned read URL.</summary>
    Task<string> PresignedReadAsync(Guid fileId, CancellationToken ct);

    /// <summary>Downloads a physical object that has no catalog id.</summary>
    Task DownloadPhysicalAsync(string physicalKey, string destinationPath, CancellationToken ct);

    /// <summary>Renames a physical object.</summary>
    Task RenamePhysicalAsync(string physicalKey, string newFileName, CancellationToken ct);

    /// <summary>Moves a physical object.</summary>
    Task MovePhysicalAsync(string physicalKey, string? destinationPrefix, CancellationToken ct);

    /// <summary>Copies a physical object.</summary>
    Task CopyPhysicalAsync(string physicalKey, string? destinationPrefix, CancellationToken ct);

    /// <summary>Deletes a physical object.</summary>
    Task DeletePhysicalAsync(string physicalKey, CancellationToken ct);

    /// <summary>Lists encryption key ids.</summary>
    Task<IReadOnlyList<string>> ListKeyIdsAsync(CancellationToken ct);

    /// <summary>Rotates DEKs for the ids.</summary>
    Task<DekMigrationResult> RotateAsync(IReadOnlyList<Guid> fileIds, string? targetKeyId, string? targetKeyVersion, CancellationToken ct);

    /// <summary>Migrates every file wrapped by <paramref name="sourceKeyId" />.</summary>
    Task<DekMigrationResult> MigrateAsync(
        string sourceKeyId,
        string? sourceKeyVersion,
        string? targetKeyId,
        string? targetKeyVersion,
        CancellationToken ct);
}

/// <summary>An in-process store that <see cref="Lyo.FileStorage.FileStorageTransfer" /> can copy or move.</summary>
public interface IFileManagerTransfer
{
    /// <summary>Storage, catalog, and keys for this connection.</summary>
    Lyo.FileStorage.FileStorageTransferEndpoint Endpoint { get; }
}
