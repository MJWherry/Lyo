using Lyo.Common.Extensions;
using Lyo.Encryption.Extensions;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.OperationContext;
using Lyo.Hashing;
using Lyo.Streams;
using Microsoft.Extensions.Logging;

namespace Lyo.FileStorage;

/// <summary>
/// Implements batch-oriented DEK migration (re-wrapping envelope keys) and full payload rotation (decrypt, re-encrypt ciphertext) using <see cref="IFileStoragePhysicalIo" />
/// .
/// </summary>
internal sealed class FileStorageDekOperations
{
    private const long LargePayloadSpillThresholdBytes = 32L * 1024 * 1024;
    private readonly IFileAuditPublisher _auditPublisher;
    private readonly int _copyToBufferSizeBytes;
    private readonly ILogger _logger;
    private readonly IFileMetadataStore _metadataService;
    private readonly IFileOperationContextAccessor _operationContextAccessor;
    private readonly FileStorageServiceBaseOptions _options;
    private readonly IFileStoragePhysicalIo _physicalIo;
    private readonly ITwoKeyEncryptionService? _twoKeyEncryptionService;

    /// <summary>Initializes helpers with metadata services, optional encryption, storage I/O, and auditing facade.</summary>
    internal FileStorageDekOperations(
        IFileMetadataStore metadataService,
        ITwoKeyEncryptionService? twoKeyEncryptionService,
        IFileOperationContextAccessor operationContextAccessor,
        ILogger logger,
        FileStorageServiceBaseOptions options,
        IFileStoragePhysicalIo physicalIo,
        IFileAuditPublisher auditPublisher,
        int copyToBufferSizeBytes)
    {
        _metadataService = metadataService;
        _twoKeyEncryptionService = twoKeyEncryptionService;
        _operationContextAccessor = operationContextAccessor;
        _logger = logger;
        _options = options;
        _physicalIo = physicalIo;
        _auditPublisher = auditPublisher;
        _copyToBufferSizeBytes = copyToBufferSizeBytes;
    }

    /// <summary>
    /// Finds files whose metadata references the supplied KEK/key version, re-wraps wrapped DEKs in storage headers to a target KEK/version, updates metadata inline, and emits
    /// an audit aggregate.
    /// </summary>
    /// <remarks>Does not re-encrypt ciphertext; only the enveloped DEK blob and header salt fields change.</remarks>
    /// <param name="sourceKeyId">Key identifier currently protecting DEKs prior to migration.</param>
    /// <param name="sourceKeyVersion">Optional specific source version filter; omit to include all recorded versions.</param>
    /// <param name="targetKeyId">Destination KEK id; defaults to <paramref name="sourceKeyId" /> when omitted.</param>
    /// <param name="targetKeyVersion">Explicit destination version; when omitted resolves the current library version for the target key.</param>
    /// <param name="batchSize">Fan-in size applied when iterating metadata rows for logging granularity.</param>
    /// <param name="ct">Cancellation token forwarded to downstream IO.</param>
    /// <returns>Counts of inspected, succeeded, and failed files alongside error payloads.</returns>
    internal async Task<DekMigrationResult> MigrateDeksAsync(
        string sourceKeyId,
        string? sourceKeyVersion,
        string? targetKeyId,
        string? targetKeyVersion,
        int batchSize,
        CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sourceKeyId);
        ArgumentHelpers.ThrowIfNotInRange(batchSize, 1, int.MaxValue);
        OperationHelpers.ThrowIfNull(_twoKeyEncryptionService, "ITwoKeyEncryptionService is not configured. Cannot migrate DEKs without encryption service.");
        _logger.LogInformation(
            "Starting DEK migration: sourceKeyId='{SourceKeyId}', sourceKeyVersion={SourceKeyVersion}, targetKeyId='{TargetKeyId}', targetKeyVersion={TargetKeyVersion}",
            sourceKeyId, sourceKeyVersion ?? "all", targetKeyId ?? sourceKeyId, targetKeyVersion ?? "current");

        var actualTargetKeyId = targetKeyId ?? sourceKeyId;
        string actualTargetVersion;
        if (!targetKeyVersion.IsNullOrWhitespace())
            actualTargetVersion = targetKeyVersion;
        else {
            var retrievedKeyVersion = _twoKeyEncryptionService!.GetKeyVersion(actualTargetKeyId);
            OperationHelpers.ThrowIfNullOrWhiteSpace(
                retrievedKeyVersion, $"No current key version available for key ID '{actualTargetKeyId}'. Ensure the keystore is properly initialized.");

            actualTargetVersion = retrievedKeyVersion;
        }

        var filesToMigrate = await _metadataService.FindByKeyIdAndVersionAsync(sourceKeyId, sourceKeyVersion, ct).ConfigureAwait(false);
        var filesList = filesToMigrate.ToList();
        _logger.LogInformation("Found {Count} files to migrate", filesList.Count);
        if (filesList.Count == 0)
            return new(0, 0, 0, [], []);

        var successfullyMigrated = 0;
        var skipped = 0;
        var failedFileIds = new List<Guid>();
        var errors = new List<string>();
        for (var i = 0; i < filesList.Count; i += batchSize) {
            ct.ThrowIfCancellationRequested();
            var batch = filesList.Skip(i).Take(batchSize).ToList();
            _logger.LogDebug(
                "Processing batch {BatchNumber} ({StartIndex}-{EndIndex} of {Total})", i / batchSize + 1, i + 1, Math.Min(i + batchSize, filesList.Count), filesList.Count);

            foreach (var fileMetadata in batch) {
                ct.ThrowIfCancellationRequested();
                try {
                    if (fileMetadata.DataEncryptionKeyId == actualTargetKeyId && fileMetadata.DataEncryptionKeyVersion == actualTargetVersion) {
                        _logger.LogDebug(
                            "File {FileId} already migrated to target keyId '{TargetKeyId}' version {TargetVersion}, skipping", fileMetadata.Id, actualTargetKeyId,
                            actualTargetVersion);

                        skipped++;
                        continue;
                    }

                    if (!fileMetadata.IsEncrypted || fileMetadata.EncryptedDataEncryptionKey == null || fileMetadata.DataEncryptionKeyVersion.IsNullOrWhitespace()) {
                        _logger.LogWarning("File {FileId} is not encrypted or missing encryption metadata, skipping", fileMetadata.Id);
                        skipped++;
                        continue;
                    }

                    var newEncryptedDek = await _twoKeyEncryptionService!.ReEncryptDekAsync(
                            fileMetadata.EncryptedDataEncryptionKey, fileMetadata.DataEncryptionKeyId ?? sourceKeyId, fileMetadata.DataEncryptionKeyVersion, actualTargetKeyId,
                            actualTargetVersion, ct)
                        .ConfigureAwait(false);

                    var newSalt = _twoKeyEncryptionService!.GetSaltForVersion(actualTargetKeyId, actualTargetVersion);

                    // If the blob is missing (or has a truncated header) the underlying UpdateFileHeaderAsync now throws; surface it as a failure rather than a silent skip.
                    await _physicalIo.UpdateFileHeaderAsync(fileMetadata.Id, fileMetadata.PathPrefix, actualTargetKeyId, actualTargetVersion, newEncryptedDek, ct)
                        .ConfigureAwait(false);

                    var updatedMetadata = fileMetadata with {
                        EncryptedDataEncryptionKey = newEncryptedDek,
                        DataEncryptionKeyId = actualTargetKeyId,
                        DataEncryptionKeyVersion = actualTargetVersion,
                        KeyEncryptionKeySalt = newSalt
                    };

                    await _metadataService.SaveMetadataAsync(fileMetadata.Id, updatedMetadata, ct).ConfigureAwait(false);
                    successfullyMigrated++;
                    _logger.LogDebug("Successfully migrated DEK for file {FileId}", fileMetadata.Id);
                }
                catch (Exception ex) {
                    failedFileIds.Add(fileMetadata.Id);
                    var errorMessage = $"Failed to migrate file {fileMetadata.Id}: {ex.Message}";
                    errors.Add(errorMessage);
                    _logger.LogError(ex, "Failed to migrate DEK for file {FileId}", fileMetadata.Id);
                }
            }
        }

        _logger.LogInformation(
            "DEK migration completed: {SuccessfullyMigrated} succeeded, {Skipped} skipped, {Failed} failed out of {Total} files", successfullyMigrated, skipped,
            failedFileIds.Count, filesList.Count);

        var migResult = new DekMigrationResult(filesList.Count, successfullyMigrated, failedFileIds.Count, failedFileIds, errors, skipped);
        await _auditPublisher.PublishAuditAsync(
                new(
                    FileAuditEventType.MigrateDeks, DateTime.UtcNow, null, _operationContextAccessor.Current?.TenantId, _operationContextAccessor.Current?.ActorId, sourceKeyId,
                    sourceKeyVersion, failedFileIds.Count == 0 ? FileAuditOutcome.Success : FileAuditOutcome.Failure,
                    failedFileIds.Count == 0 ? null : $"{failedFileIds.Count} files failed"), ct)
            .ConfigureAwait(false);

        return migResult;
    }

    /// <summary>Decrypts each requested file entirely, re-seals ciphertext with refreshed KEK/version material from policy or explicit overrides, and persists updated metadata hashes.</summary>
    /// <remarks>Rotates payloads end-to-end; failures are accumulated per-file while others continue processing.</remarks>
    /// <param name="fileIds">Concrete file identifiers slated for cryptographic rotation.</param>
    /// <param name="targetKeyId">Optional override KEK identifier; omit to derive per-record defaults.</param>
    /// <param name="targetKeyVersion">Optional override KEK version; omit alongside <paramref name="targetKeyId" /> to retain recorded versions unless a new KEK mandates fresh versions.</param>
    /// <param name="batchSize">Chunk size controlling batch logging boundaries.</param>
    /// <param name="ct">Cancellation token propagated through storage pipelines.</param>
    /// <returns>Aggregate success/failure statistics mirroring migrate semantics.</returns>
    internal async Task<DekMigrationResult> RotateDeksAsync(IReadOnlyCollection<Guid> fileIds, string? targetKeyId, string? targetKeyVersion, int batchSize, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(fileIds);
        ArgumentHelpers.ThrowIfNotInRange(batchSize, 1, int.MaxValue);
        OperationHelpers.ThrowIfNull(_twoKeyEncryptionService, "ITwoKeyEncryptionService is not configured. Cannot rotate DEKs without encryption service.");
        var requestedFileIds = fileIds.Where(fileId => fileId != Guid.Empty).Distinct().ToList();
        if (requestedFileIds.Count == 0)
            return new(0, 0, 0, [], []);

        _logger.LogInformation(
            "Starting DEK rotation for {Count} files. targetKeyId='{TargetKeyId}', targetKeyVersion={TargetKeyVersion}", requestedFileIds.Count, targetKeyId ?? "per-file",
            targetKeyVersion ?? "per-file/current");

        var successfullyRotated = 0;
        var failedFileIds = new List<Guid>();
        var errors = new List<string>();
        for (var i = 0; i < requestedFileIds.Count; i += batchSize) {
            ct.ThrowIfCancellationRequested();
            var batch = requestedFileIds.Skip(i).Take(batchSize).ToList();
            _logger.LogDebug(
                "Processing DEK rotation batch {BatchNumber} ({StartIndex}-{EndIndex} of {Total})", i / batchSize + 1, i + 1, Math.Min(i + batchSize, requestedFileIds.Count),
                requestedFileIds.Count);

            foreach (var fileId in batch) {
                ct.ThrowIfCancellationRequested();
                try {
                    var fileMetadata = await _metadataService.GetMetadataAsync(fileId, ct).ConfigureAwait(false);
                    ValidateDekRotationMetadata(fileMetadata);
                    var (resolvedTargetKeyId, resolvedTargetKeyVersion) = ResolveDekRotationTarget(fileMetadata, targetKeyId, targetKeyVersion);
                    using var decryptedPayloadStream = await ReadEncryptedPayloadAsync(fileMetadata, ct).ConfigureAwait(false);
                    await RewriteEncryptedFileAsync(fileMetadata, decryptedPayloadStream, resolvedTargetKeyId, resolvedTargetKeyVersion, ct).ConfigureAwait(false);
                    successfullyRotated++;
                    _logger.LogDebug(
                        "Successfully rotated DEK for file {FileId} using keyId '{TargetKeyId}' version {TargetKeyVersion}", fileId, resolvedTargetKeyId, resolvedTargetKeyVersion);
                }
                catch (Exception ex) {
                    failedFileIds.Add(fileId);
                    var errorMessage = $"Failed to rotate DEK for file {fileId}: {ex.Message}";
                    errors.Add(errorMessage);
                    _logger.LogError(ex, "Failed to rotate DEK for file {FileId}", fileId);
                }
            }
        }

        _logger.LogInformation(
            "DEK rotation completed: {SuccessfullyRotated} succeeded, {Failed} failed out of {Total} requested files", successfullyRotated, failedFileIds.Count,
            requestedFileIds.Count);

        var rotResult = new DekMigrationResult(requestedFileIds.Count, successfullyRotated, failedFileIds.Count, failedFileIds, errors);
        await _auditPublisher.PublishAuditAsync(
                new(
                    FileAuditEventType.RotateDeks, DateTime.UtcNow, null, _operationContextAccessor.Current?.TenantId, _operationContextAccessor.Current?.ActorId, targetKeyId,
                    targetKeyVersion, failedFileIds.Count == 0 ? FileAuditOutcome.Success : FileAuditOutcome.Failure,
                    failedFileIds.Count == 0 ? null : $"{failedFileIds.Count} files failed"), ct)
            .ConfigureAwait(false);

        return rotResult;
    }

    private void ValidateDekRotationMetadata(FileStoreResult metadata)
    {
        OperationHelpers.ThrowIf(!metadata.IsEncrypted, $"File {metadata.Id} is not encrypted. DEK rotation is only supported for encrypted files.");
        OperationHelpers.ThrowIf(
            metadata.EncryptedDataEncryptionKey == null || metadata.EncryptedDataEncryptionKey.Length == 0, $"File {metadata.Id} is missing its encrypted DEK.");

        OperationHelpers.ThrowIfNullOrWhiteSpace(metadata.DataEncryptionKeyId, $"File {metadata.Id} is missing its key ID. Cannot rotate its DEK.");
        OperationHelpers.ThrowIfNullOrWhiteSpace(metadata.DataEncryptionKeyVersion, $"File {metadata.Id} is missing its key version. Cannot rotate its DEK.");
    }

    private (string TargetKeyId, string TargetKeyVersion) ResolveDekRotationTarget(FileStoreResult metadata, string? targetKeyId, string? targetKeyVersion)
    {
        var resolvedTargetKeyId = targetKeyId.IsNullOrWhitespace() ? metadata.DataEncryptionKeyId! : targetKeyId;
        string resolvedTargetKeyVersion;
        if (!targetKeyVersion.IsNullOrWhitespace())
            resolvedTargetKeyVersion = targetKeyVersion;
        else if (!targetKeyId.IsNullOrWhitespace()) {
            var rawVersion = _twoKeyEncryptionService!.GetKeyVersion(resolvedTargetKeyId);
            OperationHelpers.ThrowIfNull(rawVersion, $"No current key version available for key ID '{resolvedTargetKeyId}'. Ensure the keystore is properly initialized.");
            resolvedTargetKeyVersion = rawVersion;
        }
        else
            resolvedTargetKeyVersion = metadata.DataEncryptionKeyVersion!;

        return (resolvedTargetKeyId, resolvedTargetKeyVersion);
    }

    /// <summary>
    /// Returns a seekable stream positioned at 0 containing the decrypted payload of <paramref name="metadata" />. Small payloads stay in a pre-sized <see cref="MemoryStream" />
    /// ; payloads exceeding <see cref="LargePayloadSpillThresholdBytes" /> spill to a temp file (DeleteOnClose) to bound RAM.
    /// </summary>
    private async Task<Stream> ReadEncryptedPayloadAsync(FileStoreResult metadata, CancellationToken ct)
    {
        using var storageStream = await _physicalIo.ReadFromStorageAsync(metadata.Id, metadata.PathPrefix, ct).ConfigureAwait(false);
        if (storageStream == null)
            throw new FileNotFoundException($"File with ID {metadata.Id} was not found in storage.", metadata.Id.ToString());

        var processingStream = storageStream;
        MemoryStream? bufferedStream = null;
        try {
            if (!processingStream.CanSeek) {
                bufferedStream = new();
                await processingStream.CopyToAsync(bufferedStream, _copyToBufferSizeBytes, ct).ConfigureAwait(false);
                bufferedStream.Position = 0;
                processingStream = bufferedStream;
            }
            else
                processingStream.Position = 0;

            var decryptedStream = CreateDecryptedSpillStream(metadata.OriginalFileSize);
            try {
                await _twoKeyEncryptionService!.DecryptToStreamAsync(processingStream, decryptedStream, null, null, ct).ConfigureAwait(false);
                decryptedStream.Position = 0;
                return decryptedStream;
            }
            catch {
                decryptedStream.Dispose();
                throw;
            }
        }
        finally {
            bufferedStream?.Dispose();
        }
    }

    /// <summary>Creates the seekable destination buffer for the DEK rotate decrypt pass. Spills to a temp file (DeleteOnClose) past <see cref="LargePayloadSpillThresholdBytes" />.</summary>
    private static Stream CreateDecryptedSpillStream(long originalSize)
    {
        if (originalSize <= LargePayloadSpillThresholdBytes) {
            if (originalSize <= 0)
                return new MemoryStream();

            return new MemoryStream((int)Math.Min(originalSize, int.MaxValue));
        }

        var path = Path.Combine(Path.GetTempPath(), $"lyo-fs-dek-{Guid.NewGuid():N}.tmp");
        return new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.DeleteOnClose);
    }

    private async Task RewriteEncryptedFileAsync(FileStoreResult metadata, Stream decryptedPayloadStream, string targetKeyId, string targetKeyVersion, CancellationToken ct)
    {
        var fileExtension = _twoKeyEncryptionService!.FileExtension;
        var chunkSize = StreamChunkSizeHelper.DetermineChunkSize(metadata.CompressedFileSize ?? metadata.OriginalFileSize);
        byte[] encryptedHash;
        using (var outputStream = await _physicalIo.CreateOutputStreamAsync(metadata.Id, fileExtension, metadata.PathPrefix, ct).ConfigureAwait(false)) {
            using (var encryptedHashAlgo = _options.HashAlgorithm.Create()) {
                using (var encryptedHashStream = new HashingStream(outputStream, encryptedHashAlgo)) {
                    decryptedPayloadStream.Position = 0;
                    await _twoKeyEncryptionService!.EncryptToStreamAsync(decryptedPayloadStream, encryptedHashStream, targetKeyId, null, chunkSize, ct).ConfigureAwait(false);
                    await encryptedHashStream.FlushAsync(ct).ConfigureAwait(false);
                    await outputStream.FlushAsync(ct).ConfigureAwait(false);
                    encryptedHash = encryptedHashStream.GetHash();
                }
            }
        }

        var headerInfo = await _physicalIo.ExtractEncryptionHeaderAsync(metadata.Id, fileExtension, metadata.PathPrefix, ct).ConfigureAwait(false);
        var resolvedKeyId = headerInfo.DataEncryptionKeyId ?? targetKeyId;
        var resolvedKeyVersion = headerInfo.DataEncryptionKeyVersion ?? targetKeyVersion;
        OperationHelpers.ThrowIfNullOrWhiteSpace(resolvedKeyVersion, $"Unable to determine the target key version for file {metadata.Id} after rewriting its encrypted payload.");
        var encryptedSize = await _physicalIo.GetStorageSizeAsync(metadata.Id, fileExtension, metadata.PathPrefix, ct).ConfigureAwait(false);
        var keyEncryptionKeySalt = _twoKeyEncryptionService!.GetSaltForVersion(resolvedKeyId, resolvedKeyVersion);
        var updatedMetadata = metadata with {
            SourceFileName = metadata.Id + fileExtension,
            SourceFileSize = encryptedSize,
            SourceFileHash = encryptedHash,
            DataEncryptionKeyAlgorithm = EncryptionServiceExtensions.DetermineDekAlgorithm(_twoKeyEncryptionService),
            KeyEncryptionKeyAlgorithm = EncryptionServiceExtensions.DetermineKekAlgorithm(_twoKeyEncryptionService),
            EncryptedFileSize = encryptedSize,
            EncryptedFileHash = encryptedHash,
            EncryptedDataEncryptionKey = headerInfo.EncryptedDataEncryptionKey,
            DataEncryptionKeyId = resolvedKeyId,
            DataEncryptionKeyVersion = resolvedKeyVersion,
            KeyEncryptionKeySalt = keyEncryptionKeySalt,
            DekKeyMaterialBytes = headerInfo.DekKeyMaterialBytes
        };

        await _metadataService.SaveMetadataAsync(metadata.Id, updatedMetadata, ct).ConfigureAwait(false);
    }
}