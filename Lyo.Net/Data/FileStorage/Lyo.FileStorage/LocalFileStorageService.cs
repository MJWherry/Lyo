using Lyo.Common.Records;
using Lyo.Compression;
using Lyo.Encryption;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using LocalFileStorageServiceOptions = Lyo.FileStorage.Models.LocalFileStorageServiceOptions;

namespace Lyo.FileStorage;

public class LocalFileStorageService : FileStorageServiceBase
{
    private readonly LocalFileStorageServiceOptions _options;
    private readonly bool _ownsMetadataService;

    public LocalFileStorageService(
        LocalFileStorageServiceOptions options,
        ILoggerFactory? loggerFactory = null,
        ICompressionService? compressionService = null,
        ITwoKeyEncryptionService? twoKeyEncryptionService = null,
        IFileMetadataStore? metadataService = null,
        IMetrics? metrics = null,
        IFileOperationContextAccessor? operationContextAccessor = null,
        IEnumerable<IFileAuditEventHandler>? auditHandlers = null,
        IFileContentPolicy? contentPolicy = null,
        IFileMalwareScanner? malwareScanner = null)
        : base(
            ArgumentHelpers.ThrowIfNullReturn(options, nameof(options)), metadataService ?? new LocalFileMetadataStore(options.RootDirectoryPath, loggerFactory),
            (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<LocalFileStorageService>(), compressionService, twoKeyEncryptionService,
            options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance, operationContextAccessor, auditHandlers, contentPolicy, malwareScanner)
    {
        _options = options;
        _ownsMetadataService = metadataService == null;
        if (_ownsMetadataService)
            Logger.LogInformation("Using JSON file-based metadata storage");

        if (Directory.Exists(_options.RootDirectoryPath))
            return;

        Directory.CreateDirectory(_options.RootDirectoryPath);
        Logger.LogInformation("Created root directory: {RootPath}", _options.RootDirectoryPath);
    }

    public event EventHandler<FileMetadataRetrievedResult>? FileMetadataRetrieved;

    /// <inheritdoc />
    public override async Task<FileStoreResult> GetMetadataAsync(Guid fileId, CancellationToken ct = default)
    {
        Logger.LogDebug("Retrieving metadata for file {FileId}", fileId);
        var metadata = await MetadataService.GetMetadataAsync(fileId, ct).ConfigureAwait(false);
        Logger.LogDebug("Retrieved metadata for file {FileId}", fileId);
        FileMetadataRetrieved?.Invoke(this, new(fileId, metadata));
        return metadata;
    }

    /// <inheritdoc />
    public override async Task<DekMigrationResult> MigrateDeksAsync(
        string sourceKeyId,
        string? sourceKeyVersion = null,
        string? targetKeyId = null,
        string? targetKeyVersion = null,
        int batchSize = 100,
        CancellationToken ct = default)
    {
        OperationHelpers.ThrowIfNull(TwoKeyEncryptionService, "ITwoKeyEncryptionService is not configured. Cannot migrate DEKs without encryption service.");
        return await base.MigrateDeksAsync(sourceKeyId, sourceKeyVersion, targetKeyId, targetKeyVersion, batchSize, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task<DekMigrationResult> RotateDeksAsync(
        IReadOnlyCollection<Guid> fileIds,
        string? targetKeyId = null,
        string? targetKeyVersion = null,
        int batchSize = 100,
        CancellationToken ct = default)
    {
        OperationHelpers.ThrowIfNull(TwoKeyEncryptionService, "ITwoKeyEncryptionService is not configured. Cannot rotate DEKs without encryption service.");
        return await base.RotateDeksAsync(fileIds, targetKeyId, targetKeyVersion, batchSize, ct).ConfigureAwait(false);
    }

    protected override Task<Stream> CreateOutputStreamAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
    {
        var filePath = GetFilePath(fileId, extension, pathPrefix);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        return Task.FromResult<Stream>(File.Create(filePath));
    }

    protected override Task<long> GetStorageSizeAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
    {
        var filePath = GetFilePath(fileId, extension, pathPrefix);
        return File.Exists(filePath) ? Task.FromResult(new FileInfo(filePath).Length) : Task.FromResult<long>(0);
    }

    protected override Task<Stream?> ReadFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
    {
        var filePath = FindFilePath(fileId, pathPrefix);
        if (filePath != null && File.Exists(filePath))
            return Task.FromResult<Stream?>(File.OpenRead(filePath));

        return Task.FromResult<Stream?>(null);
    }

    protected override Task<bool> DeleteFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
    {
        var filePath = FindFilePath(fileId, pathPrefix);
        if (filePath == null || !File.Exists(filePath))
            return Task.FromResult(false);

        File.Delete(filePath);
        Logger.LogDebug("Deleted file {FileId} at {FilePath}", fileId, filePath);
        return Task.FromResult(true);
    }

    protected override async Task<EncryptionHeaderInfo> ExtractEncryptionHeaderAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
    {
        var filePath = GetFilePath(fileId, extension, pathPrefix);
        ArgumentHelpers.ThrowIfFileNotFound(filePath, nameof(filePath));
        using var headerReader = File.OpenRead(filePath);
        var header = EncryptionHeader.Read(headerReader);
        return new(header.EncryptedDataEncryptionKey, header.KeyId, header.KeyVersion, header.DekKeyMaterialBytes);
    }

    protected override async Task UpdateFileHeaderAsync(Guid fileId, string? pathPrefix, string targetKeyId, string targetKeyVersion, byte[] newEncryptedDek, CancellationToken ct)
    {
        var filePath = FindFilePath(fileId, pathPrefix);
        if (filePath == null || !File.Exists(filePath))
            return;

#if NET5_0_OR_GREATER
        var fileBytes = await File.ReadAllBytesAsync(filePath, ct).ConfigureAwait(false);
#else
        ct.ThrowIfCancellationRequested();
        var fileBytes = File.ReadAllBytes(filePath);
#endif
        if (fileBytes.Length < 4)
            throw new InvalidDataException($"File {fileId} is too small to contain encrypted data header");

        // Read existing header with new format
        using var fileStream = new MemoryStream(fileBytes, false);
        var header = EncryptionHeader.Read(fileStream);

        // Position after reading header (where chunks start)
        var chunksStartPos = (int)fileStream.Position;

        // Create updated header with new keyId, keyVersion, and encrypted DEK
        var updatedHeader = header.With(targetKeyId, targetKeyVersion, newEncryptedDek);

        // Build new file with updated header
        var newFileBytes = new List<byte>();
        updatedHeader.Write(newFileBytes);

        // Add rest of file (encrypted data chunks) - chunks start after the old header
        newFileBytes.AddRange(fileBytes.Skip(chunksStartPos));

        // Write updated file
#if NET5_0_OR_GREATER
        await File.WriteAllBytesAsync(filePath, newFileBytes.ToArray(), ct).ConfigureAwait(false);
#else
        ct.ThrowIfCancellationRequested();
        File.WriteAllBytes(filePath, newFileBytes.ToArray());
#endif
        Logger.LogDebug("Updated file header for {FileId} with new keyId '{KeyId}', version {Version}, and encrypted DEK", fileId, targetKeyId, targetKeyVersion);
    }

    /// <inheritdoc />
    public override async Task<string> GetPreSignedReadUrlAsync(Guid fileId, TimeSpan? expiration = null, string? pathPrefix = null, CancellationToken ct = default)
    {
        if (!_options.AllowFileUriPresignedUrls)
            return await base.GetPreSignedReadUrlAsync(fileId, expiration, pathPrefix, ct).ConfigureAwait(false);

        var meta = await GetMetadataAsync(fileId, ct).ConfigureAwait(false);
        EnsureReadableAvailability(meta);
        var filePath = FindFilePath(fileId, pathPrefix);
        if (filePath == null || !File.Exists(filePath))
            throw new FileNotFoundException($"File with ID {fileId} was not found in storage.");

        var uri = new Uri(filePath);
        await RaiseFileAuditAsync(
                new(
                    FileAuditEventType.PresignedRead, DateTime.UtcNow, fileId, meta.TenantId, OperationContextAccessor.Current?.ActorId, meta.DataEncryptionKeyId,
                    meta.DataEncryptionKeyVersion, FileAuditOutcome.Success), ct)
            .ConfigureAwait(false);

        return uri.AbsoluteUri;
    }

    protected override Task CleanupPartialFileAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
    {
        // Try to find and delete any file with this ID (could have different extensions)
        var filePath = FindFilePath(fileId, pathPrefix);
        if (filePath == null || !File.Exists(filePath))
            return Task.CompletedTask;

        try {
            File.Delete(filePath);
        }
        catch {
            // Ignore errors during cleanup
        }

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        if (Disposed)
            return;

        if (_ownsMetadataService && MetadataService is IDisposable disposableMetadataService)
            disposableMetadataService.Dispose();

        base.Dispose();
    }

    private string GetFilePath(Guid fileId, string extension = "", string? pathPrefix = null)
    {
        var fileName = fileId.ToString("N") + extension;
        if (!string.IsNullOrWhiteSpace(pathPrefix))
            return Path.Combine(_options.RootDirectoryPath, pathPrefix, fileName);

        var idString = fileId.ToString("N");
        var subDir = Path.Combine(idString.Substring(0, 2), idString.Substring(2, 2));
        return Path.Combine(_options.RootDirectoryPath, subDir, fileName);
    }

    private string? FindFilePath(Guid fileId, string? pathPrefix = null)
    {
        var basePath = GetFilePath(fileId, "", pathPrefix);
        var directory = Path.GetDirectoryName(basePath);
        var fileNameWithoutExt = Path.GetFileName(basePath);
        if (directory == null || !Directory.Exists(directory))
            return null;

        var filePath = Path.Combine(directory, fileNameWithoutExt);
        if (File.Exists(filePath))
            return filePath;

        if (CompressionService != null) {
            filePath = Path.Combine(directory, fileNameWithoutExt + CompressionService.FileExtension);
            if (File.Exists(filePath))
                return filePath;
        }

        if (TwoKeyEncryptionService != null) {
            filePath = Path.Combine(directory, fileNameWithoutExt + TwoKeyEncryptionService.FileExtension);
            if (File.Exists(filePath))
                return filePath;

            // Check for compression + two-key encryption combination
            if (CompressionService != null) {
                filePath = Path.Combine(directory, fileNameWithoutExt + CompressionService.FileExtension + TwoKeyEncryptionService.FileExtension);
                if (File.Exists(filePath))
                    return filePath;
            }
        }

        // Try common extensions as fallback
        var commonExtensions = FileTypeInfo.CommonStorageResolutionSuffixes;
        foreach (var ext in commonExtensions) {
            filePath = Path.Combine(directory, fileNameWithoutExt + ext);
            if (File.Exists(filePath))
                return filePath;
        }

        return null;
    }
}