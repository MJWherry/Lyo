using System.Diagnostics;
using System.Net;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Lyo.Common.Extensions;
using Lyo.Common.Records;
using Lyo.Compression;
using Lyo.Encryption;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.Hashing;
using Lyo.Health;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FileStorage.Blob;

/// <summary><see cref="IFileStorageService" /> backed by Azure Blob Storage (package-level abstraction uses <c>Blob</c> naming).</summary>
public sealed class BlobFileStorageService : FileStorageServiceBase, IFileStorageDiagnosticsService
{
    private readonly BlobContainerClient _containerClient;
    private readonly BlobFileStorageOptions _blobOptions;

    public BlobFileStorageService(
        BlobFileStorageOptions options,
        IFileMetadataStore metadataService,
        ILoggerFactory? loggerFactory = null,
        ICompressionService? compressionService = null,
        ITwoKeyEncryptionService? twoKeyEncryptionService = null,
        BlobContainerClient? containerClient = null,
        IMetrics? metrics = null,
        IFileOperationContextAccessor? operationContextAccessor = null,
        IEnumerable<IFileAuditEventHandler>? auditHandlers = null,
        IFileContentPolicy? contentPolicy = null,
        IFileMalwareScanner? malwareScanner = null)
        : base(
            ArgumentHelpers.ThrowIfNullReturn(options), ArgumentHelpers.ThrowIfNullReturn(metadataService),
            (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<BlobFileStorageService>(), compressionService, twoKeyEncryptionService, metrics, operationContextAccessor,
            auditHandlers, contentPolicy, malwareScanner)
    {
        _blobOptions = options;
        _containerClient = containerClient ?? new BlobContainerClient(options.ConnectionString, options.ContainerName);
        Logger.LogInformation("Initialized blob file storage (Azure Blob) for container {Container}", options.ContainerName);
        MetricNames[nameof(FileStorage.Constants.Metrics.FileStoragePreSignedUrlGenerated)] = Constants.Metrics.FileStoragePreSignedUrlGenerated;
        MetricNames[nameof(FileStorage.Constants.Metrics.FileStoragePreSignedUrlGenerationFailed)] = Constants.Metrics.FileStoragePreSignedUrlGenerationFailed;
    }

    protected override async Task<HealthResult> CheckHealthLightweightAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try {
            var exists = await _containerClient.ExistsAsync(ct).ConfigureAwait(false);
            if (!exists.Value) {
                sw.Stop();
                return HealthResult.Unhealthy(sw.Elapsed, $"Azure blob container {_blobOptions.ContainerName} does not exist or is inaccessible.");
            }

            await _containerClient.GetPropertiesAsync(cancellationToken: ct).ConfigureAwait(false);
            sw.Stop();
            return HealthResult.Healthy(sw.Elapsed, null, new Dictionary<string, object?> { ["container"] = _blobOptions.ContainerName });
        }
        catch (Exception ex) {
            sw.Stop();
            return HealthResult.Unhealthy(sw.Elapsed, ex.Message, null, ex);
        }
    }

    /// <inheritdoc />
    public override async Task<FileStoreResult> CopyFileAsync(Guid sourceFileId, CopyFileRequest? request = null, CancellationToken ct = default)
    {
        var meta = await GetMetadataAsync(sourceFileId, ct).ConfigureAwait(false);
        EnsureReadableAvailability(meta);
        OperationHelpers.ThrowIf(meta.Availability == FileAvailability.PendingDirectUpload, $"Cannot copy file {sourceFileId}; it is awaiting direct-upload finalize.");

        var srcBlobName = await FindBlobNameAsync(sourceFileId, meta.PathPrefix, ct).ConfigureAwait(false);
        if (srcBlobName == null)
            throw new FileNotFoundException($"Source blob missing for id {sourceFileId}.");

        var destId = Guid.NewGuid();
        var destPrefixArg = NormalizePathPrefix(request?.PathPrefix ?? meta.PathPrefix);
        var suffix = InferTrailingSuffixAfterFileId(meta.Id, meta.SourceFileName);
        var destBlobName = GetBlobName(destId, suffix, destPrefixArg);

        var copyCompleted = false;
        try {
            var src = _containerClient.GetBlobClient(srcBlobName);
            var dst = _containerClient.GetBlobClient(destBlobName);
            await dst.SyncCopyFromUriAsync(src.Uri, cancellationToken: ct).ConfigureAwait(false);
            copyCompleted = true;
            return await RecordCopyMetadataAsync(sourceFileId, meta, destId, request, ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "Blob copy failed {Source}->{Dest}", srcBlobName, destBlobName);
            // If we created the destination blob but failed to persist metadata, remove the orphan destination.
            if (copyCompleted) {
                try {
                    await _containerClient.GetBlobClient(destBlobName).DeleteIfExistsAsync(cancellationToken: CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception delEx) {
                    Logger.LogWarning(delEx, "Failed to clean up orphan destination {Dest} after copy metadata failure", destBlobName);
                }
            }

            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.Copy, DateTime.UtcNow, destId, meta.TenantId, OperationContextAccessor.Current?.ActorId, meta.DataEncryptionKeyId,
                        meta.DataEncryptionKeyVersion, FileAuditOutcome.Failure, FileStorageServiceBase.SanitizeAuditError(ex.Message), CorrelationId: sourceFileId),
                    ct)
                .ConfigureAwait(false);

            throw;
        }
    }

    /// <inheritdoc />
    async Task<IReadOnlyList<string>> IFileStorageDiagnosticsService.ListStorageKeysAsync(string? prefix = null, int maxKeys = 1000, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfLessThan(maxKeys, 1);
        var cap = Math.Min(maxKeys, 10_000);
        var combined = DiagnosticsPrefix(FileHelpers.NormalizeAndValidatePathPrefix(prefix));
        var list = new List<string>();

        await foreach (var blob in _containerClient.GetBlobsAsync(BlobTraits.None, BlobStates.None, combined, ct).ConfigureAwait(false)) {
            if (list.Count >= cap)
                break;

            list.Add(blob.Name);
        }

        return list;
    }

    /// <inheritdoc />
    public override async Task<DirectUploadBeginResult> BeginDirectUploadAsync(DirectUploadBeginRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        OperationHelpers.ThrowIf(_blobOptions.UsesCustomerProvidedKey, "Direct upload presigned PUT is not compatible with SSE-C CustomerProvidedKey; remove it or disable direct uploads.");

        var normalized = NormalizePathPrefix(request.PathPrefix) ?? "";
        var fileId = Guid.NewGuid();

        await PersistPendingPlainDirectUploadMetadataAsync(fileId, request, normalized, ct).ConfigureAwait(false);

        var blobName = GetBlobName(fileId, "", normalized);
        var expiry = request.UrlExpiration ?? TimeSpan.FromHours(1);
        ArgumentHelpers.ThrowIfNotInRange(expiry, TimeSpan.Zero, TimeSpan.FromDays(7));

        try {
            var blockBlob = _containerClient.GetBlockBlobClient(blobName);
            var sas = new BlobSasBuilder {
                BlobContainerName = _containerClient.Name,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
            };

            sas.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);
            if (!string.IsNullOrWhiteSpace(_blobOptions.EncryptionScope))
                sas.EncryptionScope = _blobOptions.EncryptionScope;

            var url = blockBlob.GenerateSasUri(sas);

            // Parity with S3: surface required PUT headers so the client can set Content-Type / x-ms-blob-type at upload time.
            // Azure block blob PUT requires x-ms-blob-type: BlockBlob; the SAS does not sign Content-Type, but we still echo
            // the caller-requested content type so clients can apply x-ms-blob-content-type and have it persisted on the blob.
            var requiredHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                ["x-ms-blob-type"] = "BlockBlob"
            };
            if (!string.IsNullOrWhiteSpace(request.ContentType)) {
                var trimmed = request.ContentType.Trim();
                requiredHeaders["x-ms-blob-content-type"] = trimmed;
                requiredHeaders["Content-Type"] = trimmed;
            }

            if (!string.IsNullOrWhiteSpace(_blobOptions.EncryptionScope))
                requiredHeaders["x-ms-encryption-scope"] = _blobOptions.EncryptionScope!;

            return new() {
                FileId = fileId,
                PresignedPutUrl = url.ToString(),
                UrlExpiresUtc = sas.ExpiresOn,
                StorageLocation = blobName,
                RequiredPutHeaders = requiredHeaders
            };
        }
        catch (Exception ex) {
            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.DirectUploadFailed, DateTime.UtcNow, fileId, ResolveTenantId(request.TenantId), OperationContextAccessor.Current?.ActorId, null, null,
                        FileAuditOutcome.Failure, ex.Message),
                    ct)
                .ConfigureAwait(false);

            throw;
        }
    }

    /// <inheritdoc />
    public override Task<FileStoreResult> CompleteDirectUploadAsync(Guid fileId, DirectUploadCompleteRequest? completeRequest = null, CancellationToken ct = default)
        => FinalizePendingPlainDirectUploadCoreAsync(fileId, completeRequest, ct);

    protected override Task<Stream> CreateOutputStreamAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
    {
        var blobName = GetBlobName(fileId, extension, pathPrefix);
        var blockBlob = _containerClient.GetBlockBlobClient(blobName);
        if (!string.IsNullOrWhiteSpace(_blobOptions.EncryptionScope))
            blockBlob = blockBlob.WithEncryptionScope(_blobOptions.EncryptionScope);

        var cpk = _blobOptions.ResolveCustomerProvidedKey();
        if (cpk.HasValue)
            blockBlob = blockBlob.WithCustomerProvidedKey(cpk.Value);

        return blockBlob.OpenWriteAsync(true, cancellationToken: ct);
    }

    /// <summary>
    /// Finalizes a multipart staging blob using Azure server-side <c>StartCopyFromUriAsync</c> instead of round-tripping the bytes back through the client. The local download is
    /// still required for hash (and optional scan), but the upload step that <see cref="FileStorageServiceBase.SaveFromStreamAsync" /> would have performed is replaced by an
    /// in-account copy. Only valid for no-compress/no-encrypt finalize paths.
    /// </summary>
    internal async Task<FileStoreResult> FinalizeMultipartFromStagingAsync(
        string stagingBlobName,
        Guid targetFileId,
        string? originalFileName,
        string? contentType,
        string? pathPrefix,
        string? tenantId,
        FileAvailability? availabilityOverride,
        CancellationToken ct)
    {
        var normalizedPathPrefix = NormalizePathPrefix(pathPrefix);
        var finalBlobName = GetBlobName(targetFileId, "", normalizedPathPrefix);
        var hashAlg = Options.HashAlgorithm;
        var tempPath = Path.Combine(Path.GetTempPath(), $"lyo-fs-blob-mpufin-{targetFileId:N}.tmp");
        long observedLength;
        byte[] computedHash;
        try {
            var stagingClient = GetBlobClientWithEncryption(stagingBlobName);
            await using (var spool = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous)) {
                using (var hasher = hashAlg.Create())
                using (var hashing = new HashingStream(spool, hasher)) {
                    await stagingClient.DownloadToAsync(hashing, ct).ConfigureAwait(false);
                    computedHash = hashing.GetHash();
                }

                await spool.FlushAsync(ct).ConfigureAwait(false);
                observedLength = spool.Length;
            }

            // Server-side copy from staging blob URI → final blob.
            var finalClient = GetBlobClientWithEncryption(finalBlobName);
            var copyOp = await finalClient.StartCopyFromUriAsync(stagingClient.Uri, cancellationToken: ct).ConfigureAwait(false);
            await copyOp.WaitForCompletionAsync(ct).ConfigureAwait(false);

            try {
                var sourceFileName = originalFileName ?? targetFileId.ToString();
                var resolvedContentType = string.IsNullOrWhiteSpace(contentType) ? FileTypeInfo.Unknown.MimeType : contentType;
                var metadata = new FileStoreResult(
                    targetFileId, originalFileName ?? sourceFileName, observedLength, computedHash, sourceFileName, observedLength, computedHash,
                    false, null, null, null,
                    false, null, null, null, null, null, null, null, null, DateTime.UtcNow, normalizedPathPrefix, hashAlg, resolvedContentType, tenantId,
                    availabilityOverride ?? Options.DefaultAvailability, null);

                await MetadataService.SaveMetadataAsync(targetFileId, metadata, ct).ConfigureAwait(false);
                RaiseFileSaved(targetFileId, FileStoreSnapshot.From(metadata), observedLength, observedLength, false, false);
                return metadata;
            }
            catch {
                try {
                    await finalClient.DeleteIfExistsAsync(cancellationToken: CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception cleanupEx) {
                    Logger.LogWarning(cleanupEx, "Failed to clean up destination blob {Blob} after metadata save failure", finalBlobName);
                }

                throw;
            }
        }
        finally {
            try {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch (Exception ex) {
                Logger.LogDebug(ex, "Best-effort delete of multipart-finalize temp file failed: {Path}", tempPath);
            }
        }
    }

    protected override async Task<long> GetStorageSizeAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
    {
        // Try the suffix-based blob name first (cheap single GetProperties), and fall back to FindBlobNameAsync if the suffix is stale (e.g. legacy or alternate extension).
        var blobClient = GetBlobClient(fileId, extension, pathPrefix);
        try {
            var props = await blobClient.GetPropertiesAsync(cancellationToken: ct).ConfigureAwait(false);
            return props.Value.ContentLength;
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound) {
            var resolved = await FindBlobNameAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
            if (resolved == null)
                return 0;

            try {
                var fallbackClient = GetBlobClientWithEncryption(resolved);
                var fallbackProps = await fallbackClient.GetPropertiesAsync(cancellationToken: ct).ConfigureAwait(false);
                return fallbackProps.Value.ContentLength;
            }
            catch (RequestFailedException innerEx) when (innerEx.Status == (int)HttpStatusCode.NotFound) {
                return 0;
            }
        }
    }

    protected override async Task<Stream?> ReadFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
    {
        var blobName = await FindBlobNameAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
        if (blobName == null)
            return null;

        var blobClient = GetBlobClientWithEncryption(blobName);
        var response = await blobClient.DownloadStreamingAsync(cancellationToken: ct).ConfigureAwait(false);
        return response.Value.Content;
    }

    protected override async Task<bool> DeleteFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
    {
        var blobName = await FindBlobNameAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
        if (blobName == null)
            return false;

        await _containerClient.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: ct).ConfigureAwait(false);
        Logger.LogDebug("Deleted file {FileId} from blob storage at {BlobName}", fileId, blobName);
        return true;
    }

    protected override async Task<EncryptionHeaderInfo> ExtractEncryptionHeaderAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
    {
        // Header is bounded (a few KiB even for max key-id/version + wrapped DEK), so a small range GET is enough — no need to download the entire blob.
        const int rangeBytes = 8 * 1024;
        var blobClient = GetBlobClient(fileId, extension, pathPrefix);
        var response = await blobClient.DownloadStreamingAsync(new() { Range = new(0, rangeBytes) }, ct).ConfigureAwait(false);
        await using var stream = response.Value.Content;
        using var bufferStream = new MemoryStream();
        await stream.CopyToAsync(bufferStream, ct).ConfigureAwait(false);
        bufferStream.Position = 0;
        var header = EncryptionHeader.Read(bufferStream);
        return new(header.EncryptedDataEncryptionKey, header.KeyId, header.KeyVersion, header.DekKeyMaterialBytes);
    }

    protected override async Task UpdateFileHeaderAsync(Guid fileId, string? pathPrefix, string targetKeyId, string targetKeyVersion, byte[] newEncryptedDek, CancellationToken ct)
    {
        var blobName = await FindBlobNameAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
        if (blobName == null)
            throw new FileNotFoundException($"File {fileId} not found in blob storage; cannot rotate header.", fileId.ToString());

        // Use the encryption-scope/SSE-C-aware client so DEK rotation works on blobs that require those options for both reads and writes.
        var blobClient = GetBlobClientWithEncryption(blobName);

        // Spool the blob to a temp file so we can rewrite just the header without buffering the entire blob in RAM.
        var spoolPath = Path.Combine(Path.GetTempPath(), $"lyo-fs-blob-hdr-{fileId:N}.tmp");
        await using var spool = new FileStream(spoolPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.DeleteOnClose);
        await blobClient.DownloadToAsync(spool, ct).ConfigureAwait(false);
        await spool.FlushAsync(ct).ConfigureAwait(false);
        if (spool.Length < 13)
            throw new InvalidDataException($"File {fileId} has a truncated or invalid encryption header ({spool.Length} bytes); cannot rotate DEK.");

        spool.Position = 0;
        var oldHeader = EncryptionHeader.Read(spool);
        var oldHeaderSize = (int)spool.Position;
        var updatedHeader = oldHeader.With(targetKeyId, targetKeyVersion, newEncryptedDek);
        var newHeaderBytes = SerializeHeaderToArray(updatedHeader);

        if (newHeaderBytes.Length == oldHeaderSize) {
            spool.Position = 0;
            await spool.WriteAsync(newHeaderBytes, 0, newHeaderBytes.Length, ct).ConfigureAwait(false);
            await spool.FlushAsync(ct).ConfigureAwait(false);
            spool.Position = 0;
            await blobClient.UploadAsync(spool, true, ct).ConfigureAwait(false);
        }
        else {
            var stagedPath = Path.Combine(Path.GetTempPath(), $"lyo-fs-blob-hdr-{fileId:N}-staged.tmp");
            await using var staged = new FileStream(stagedPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.DeleteOnClose);
            await staged.WriteAsync(newHeaderBytes, 0, newHeaderBytes.Length, ct).ConfigureAwait(false);
            spool.Position = oldHeaderSize;
            await spool.CopyToAsync(staged, 81920, ct).ConfigureAwait(false);
            await staged.FlushAsync(ct).ConfigureAwait(false);
            staged.Position = 0;
            await blobClient.UploadAsync(staged, true, ct).ConfigureAwait(false);
        }

        Logger.LogDebug("Updated blob header for {FileId}; keyVersion {KeyVersion}", fileId, targetKeyVersion);
    }

    private static byte[] SerializeHeaderToArray(EncryptionHeader header)
    {
        using var ms = new MemoryStream(header.GetHeaderSize());
        header.Write(ms);
        return ms.ToArray();
    }

    protected override async Task CleanupPartialFileAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
    {
        var blobName = await FindBlobNameAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
        if (blobName == null)
            return;

        try {
            await _containerClient.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: ct).ConfigureAwait(false);
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound) {
            // race with concurrent delete — ok
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "Unexpected error cleaning partial blob for {FileId}", fileId);
        }
    }

    /// <inheritdoc />
    public override Task<string> GetPreSignedReadUrlAsync(
        Guid fileId,
        TimeSpan? expiration,
        string? pathPrefix,
        PreSignedReadUrlOptions? urlResponseOptions,
        CancellationToken ct)
        => GeneratePresignedReadUrlCoreAsync(fileId, expiration ?? TimeSpan.FromHours(1), pathPrefix, urlResponseOptions, ct);

    private async Task<string> GeneratePresignedReadUrlCoreAsync(
        Guid fileId,
        TimeSpan expirationTime,
        string? pathPrefix,
        PreSignedReadUrlOptions? urlResponseOptions,
        CancellationToken ct)
    {
        var meta = await GetMetadataAsync(fileId, ct).ConfigureAwait(false);
        EnsureReadableAvailability(meta);
        ArgumentHelpers.ThrowIfNotInRange(expirationTime, TimeSpan.Zero, TimeSpan.FromDays(7));
        var resolvedPrefix = pathPrefix ?? meta.PathPrefix;
        var blobName = await FindBlobNameAsync(fileId, resolvedPrefix, ct).ConfigureAwait(false);
        if (blobName == null) {
            Logger.LogWarning("File {FileId} not found in blob storage, cannot generate SAS URL", fileId);
            throw new FileNotFoundException($"File with ID {fileId} was not found in storage.");
        }

        try {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var sasBuilder = new BlobSasBuilder {
                BlobContainerName = _containerClient.Name,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.Add(expirationTime)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);
            if (!string.IsNullOrWhiteSpace(urlResponseOptions?.ContentDisposition))
                sasBuilder.ContentDisposition = urlResponseOptions!.ContentDisposition;

            if (!string.IsNullOrWhiteSpace(urlResponseOptions?.ContentType))
                sasBuilder.ContentType = urlResponseOptions.ContentType;

            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            Logger.LogDebug("Generated SAS URL for file {FileId}", fileId);
            Metrics.IncrementCounter(MetricNames[nameof(FileStorage.Constants.Metrics.FileStoragePreSignedUrlGenerated)], tags: [("container", _blobOptions.ContainerName)]);
            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.PresignedRead, DateTime.UtcNow, fileId, meta.TenantId, OperationContextAccessor.Current?.ActorId, meta.DataEncryptionKeyId,
                        meta.DataEncryptionKeyVersion, FileAuditOutcome.Success),
                    ct)
                .ConfigureAwait(false);

            return sasUri.ToString();
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Failed to generate SAS URL for file {FileId}", fileId);
            Metrics.IncrementCounter(
                MetricNames[nameof(FileStorage.Constants.Metrics.FileStoragePreSignedUrlGenerationFailed)],
                tags: [("container", _blobOptions.ContainerName), ("error", ex.GetType().Name)]);

            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.PresignedRead, DateTime.UtcNow, fileId, meta.TenantId, OperationContextAccessor.Current?.ActorId, meta.DataEncryptionKeyId,
                        meta.DataEncryptionKeyVersion, FileAuditOutcome.Failure, ex.Message),
                    ct)
                .ConfigureAwait(false);

            throw;
        }
    }

    private BlobClient GetBlobClient(Guid fileId, string extension, string? pathPrefix)
    {
        var blobName = GetBlobName(fileId, extension, pathPrefix);
        return GetBlobClientWithEncryption(blobName);
    }

    /// <summary>Returns a <see cref="BlobClient" /> with the configured encryption scope or customer-provided key applied so reads/writes hit blobs encrypted with those options.</summary>
    private BlobClient GetBlobClientWithEncryption(string blobName)
    {
        var client = _containerClient.GetBlobClient(blobName);
        if (!string.IsNullOrWhiteSpace(_blobOptions.EncryptionScope))
            client = client.WithEncryptionScope(_blobOptions.EncryptionScope);

        var cpk = _blobOptions.ResolveCustomerProvidedKey();
        if (cpk.HasValue)
            client = client.WithCustomerProvidedKey(cpk.Value);

        return client;
    }

    private string GetBlobName(Guid fileId, string extension = "", string? pathPrefix = null)
        => CloudObjectKeyBuilder.Build(fileId, extension, pathPrefix, _blobOptions.BlobPrefix);

    private async Task<string?> FindBlobNameAsync(Guid fileId, string? pathPrefix = null, CancellationToken ct = default)
    {
        // Hot-path optimization: if metadata is available for this fileId, try the suffix derived from SourceFileName before falling through to N+1 probes.
        var hintedSuffix = await TryGetSuffixHintFromMetadataAsync(fileId, ct).ConfigureAwait(false);
        if (hintedSuffix != null) {
            var hintedName = GetBlobName(fileId, hintedSuffix, pathPrefix);
            if (await BlobExistsAsync(hintedName, ct).ConfigureAwait(false))
                return hintedName;
        }

        var baseName = GetBlobName(fileId, "", pathPrefix);
        if (await BlobExistsAsync(baseName, ct).ConfigureAwait(false))
            return baseName;

        if (CompressionService != null) {
            var name = GetBlobName(fileId, CompressionService.FileExtension, pathPrefix);
            if (await BlobExistsAsync(name, ct).ConfigureAwait(false))
                return name;
        }

        if (TwoKeyEncryptionService != null) {
            var name = GetBlobName(fileId, TwoKeyEncryptionService.FileExtension, pathPrefix);
            if (await BlobExistsAsync(name, ct).ConfigureAwait(false))
                return name;

            if (CompressionService != null) {
                name = GetBlobName(fileId, CompressionService.FileExtension + TwoKeyEncryptionService.FileExtension, pathPrefix);
                if (await BlobExistsAsync(name, ct).ConfigureAwait(false))
                    return name;
            }
        }

        foreach (var ext in FileTypeInfo.CommonStorageResolutionSuffixes) {
            ct.ThrowIfCancellationRequested();
            var name = GetBlobName(fileId, ext, pathPrefix);
            if (await BlobExistsAsync(name, ct).ConfigureAwait(false))
                return name;
        }

        return null;
    }

    /// <summary>
    /// Returns the storage suffix recorded in metadata's <c>SourceFileName</c> for the given <paramref name="fileId" />, or <see langword="null" /> when the metadata is not
    /// reachable, the file id prefix is missing, or the metadata is itself missing. Used as a hint to skip the N+1 HEAD probes inside <see cref="FindBlobNameAsync" />.
    /// </summary>
    private async Task<string?> TryGetSuffixHintFromMetadataAsync(Guid fileId, CancellationToken ct)
    {
        try {
            var metadata = await MetadataService.GetMetadataAsync(fileId, ct).ConfigureAwait(false);
            return InferTrailingSuffixAfterFileId(fileId, metadata.SourceFileName);
        }
        catch (FileNotFoundException) {
            return null;
        }
        catch (Exception ex) {
            Logger.LogDebug(ex, "Metadata lookup for suffix hint failed for {FileId}; falling back to probe", fileId);
            return null;
        }
    }

    private async Task<bool> BlobExistsAsync(string blobName, CancellationToken ct)
    {
        try {
            return await _containerClient.GetBlobClient(blobName).ExistsAsync(ct).ConfigureAwait(false);
        }
        catch (RequestFailedException ex) {
            Logger.LogWarning(ex, "Unexpected failure probing blob existence {BlobName}; treating as inaccessible.", blobName);
            throw;
        }
    }

    private string DiagnosticsPrefix(string? suffix)
    {
        var basePrefix = FileHelpers.NormalizePathPrefix(_blobOptions.BlobPrefix);
        if (basePrefix.Length == 0)
            return suffix ?? "";

        return string.IsNullOrWhiteSpace(suffix) ? basePrefix : $"{basePrefix}/{suffix}".Trim('/');
    }
}
