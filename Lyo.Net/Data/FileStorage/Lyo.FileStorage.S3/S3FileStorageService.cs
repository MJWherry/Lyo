using System.Diagnostics;
using System.Net;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
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

namespace Lyo.FileStorage.S3;

/// <summary>S3-backed <see cref="IFileStorageService" /> supporting presigned URLs, multipart, server-side copy, and optional customer / KMS SSE.</summary>
public class S3FileStorageService : FileStorageServiceBase, IFileStorageDiagnosticsService, IAsyncDisposable
{
    private readonly S3FileStorageOptions _options;
    private readonly bool _ownsS3Client;
    private readonly IAmazonS3 _s3Client;

    public S3FileStorageService(
        S3FileStorageOptions options,
        IFileMetadataStore metadataService,
        ILoggerFactory? loggerFactory = null,
        ICompressionService? compressionService = null,
        ITwoKeyEncryptionService? twoKeyEncryptionService = null,
        IAmazonS3? s3Client = null,
        IMetrics? metrics = null,
        IFileOperationContextAccessor? operationContextAccessor = null,
        IEnumerable<IFileAuditEventHandler>? auditHandlers = null,
        IFileContentPolicy? contentPolicy = null,
        IFileMalwareScanner? malwareScanner = null)
        : base(
            ArgumentHelpers.ThrowIfNullReturn(options), ArgumentHelpers.ThrowIfNullReturn(metadataService),
            (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<S3FileStorageService>(), compressionService, twoKeyEncryptionService, metrics, operationContextAccessor,
            auditHandlers, contentPolicy, malwareScanner)
    {
        _options = options;
        if (s3Client != null) {
            _s3Client = s3Client;
            _ownsS3Client = false;
        }
        else {
            _s3Client = CreateS3Client();
            _ownsS3Client = true;
        }

        Logger.LogInformation("Initialized S3 file storage for bucket: {BucketName}", _options.BucketName);

        // Override base metric names with S3-specific ones
        MetricNames[nameof(FileStorage.Constants.Metrics.FileStoragePreSignedUrlGenerated)] = Constants.Metrics.FileStoragePreSignedUrlGenerated;
        MetricNames[nameof(FileStorage.Constants.Metrics.FileStoragePreSignedUrlGenerationFailed)] = Constants.Metrics.FileStoragePreSignedUrlGenerationFailed;
    }

    /// <summary>Releases the S3 client when this service constructed it; otherwise same as <see cref="Dispose()" />.</summary>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    async Task<IReadOnlyList<string>> IFileStorageDiagnosticsService.ListStorageKeysAsync(string? prefix, int maxKeys, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfLessThan(maxKeys, 1);
        var cap = Math.Min(maxKeys, 10_000);
        ct.ThrowIfCancellationRequested();
        var list = new List<string>();
        var s3Prefix = BuildDiagnosticsCombinedPrefix(prefix);
        string? token = null;
        ListObjectsV2Response resp;
        do {
            var remaining = cap - list.Count;
            var pageSize = remaining > 1000 ? 1000 : remaining;
            if (pageSize <= 0)
                break;

            resp = await _s3Client.ListObjectsV2Async(
                    new() {
                        BucketName = _options.BucketName,
                        Prefix = s3Prefix,
                        MaxKeys = pageSize,
                        ContinuationToken = token
                    }, ct)
                .ConfigureAwait(false);

            foreach (var o in resp.S3Objects) {
                list.Add(o.Key);
                if (list.Count >= cap)
                    return list;
            }

            token = resp.NextContinuationToken;
        } while (resp.IsTruncated == true);

        return list;
    }

    protected override async Task<HealthResult> CheckHealthLightweightAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try {
            await _s3Client.GetBucketLocationAsync(new GetBucketLocationRequest { BucketName = _options.BucketName }, ct).ConfigureAwait(false);
            sw.Stop();
            return HealthResult.Healthy(sw.Elapsed, null, new Dictionary<string, object?> { ["bucket"] = _options.BucketName });
        }
        catch (Exception ex) {
            sw.Stop();
            return HealthResult.Unhealthy(sw.Elapsed, ex.Message, null, ex);
        }
    }

    /// <inheritdoc />
    public override Task<FileStoreResult> CompleteDirectUploadAsync(Guid fileId, DirectUploadCompleteRequest? completeRequest = null, CancellationToken ct = default)
        => FinalizePendingPlainDirectUploadCoreAsync(fileId, completeRequest, ct);

    /// <inheritdoc />
    public override async Task<DirectUploadBeginResult> BeginDirectUploadAsync(DirectUploadBeginRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalized = NormalizePathPrefix(request.PathPrefix) ?? "";
        var fileId = Guid.NewGuid();
        await PersistPendingPlainDirectUploadMetadataAsync(fileId, request, normalized, ct).ConfigureAwait(false);
        var storageKey = GetObjectKey(fileId, "", normalized);
        var expiry = request.UrlExpiration ?? TimeSpan.FromHours(1);
        ArgumentHelpers.ThrowIfNotInRange(expiry, TimeSpan.Zero, TimeSpan.FromDays(7));
        try {
            var presign = new GetPreSignedUrlRequest {
                BucketName = _options.BucketName,
                Key = storageKey,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.Add(expiry)
            };

            string? signedContentType = null;
            if (!string.IsNullOrWhiteSpace(request.ContentType)) {
                signedContentType = request.ContentType.Trim();
                presign.ContentType = signedContentType;
            }

            S3UploadServerSideEncryption.ApplyToPresignedPut(presign, _options);
            var url = await _s3Client.GetPreSignedURLAsync(presign).ConfigureAwait(false);
            return new() {
                FileId = fileId,
                PresignedPutUrl = url,
                UrlExpiresUtc = DateTimeOffset.UtcNow.Add(expiry),
                StorageLocation = storageKey,
                RequiredPutHeaders = S3UploadServerSideEncryption.BuildRequiredPutHeaders(_options, signedContentType)
            };
        }
        catch (Exception ex) {
            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.DirectUploadFailed, DateTime.UtcNow, fileId, ResolveTenantId(request.TenantId), OperationContextAccessor.Current?.ActorId, null, null,
                        FileAuditOutcome.Failure, ex.Message), ct)
                .ConfigureAwait(false);

            throw;
        }
    }

    /// <inheritdoc />
    public override async Task<FileStoreResult> CopyFileAsync(Guid sourceFileId, CopyFileRequest? request = null, CancellationToken ct = default)
    {
        var meta = await GetMetadataAsync(sourceFileId, ct).ConfigureAwait(false);
        EnsureReadableAvailability(meta);
        OperationHelpers.ThrowIf(meta.Availability == FileAvailability.PendingDirectUpload, $"Cannot copy file {sourceFileId}; it is awaiting direct-upload finalize.");
        var srcKey = await FindObjectKeyAsync(sourceFileId, meta.PathPrefix, ct).ConfigureAwait(false);
        if (srcKey == null)
            throw new FileNotFoundException($"Source object not found for id {sourceFileId}.");

        var destId = Guid.NewGuid();
        var destPrefix = NormalizePathPrefix(request?.PathPrefix ?? meta.PathPrefix);
        var suffix = InferTrailingSuffixAfterFileId(meta.Id, meta.SourceFileName);
        var destKey = GetObjectKey(destId, suffix, destPrefix);
        var copyCompleted = false;
        try {
            var copy = new CopyObjectRequest {
                SourceBucket = _options.BucketName,
                SourceKey = srcKey,
                DestinationBucket = _options.BucketName,
                DestinationKey = destKey
            };

            S3UploadServerSideEncryption.ApplyToCopyDestination(copy, _options);
            await _s3Client.CopyObjectAsync(copy, ct).ConfigureAwait(false);
            copyCompleted = true;
            return await RecordCopyMetadataAsync(sourceFileId, meta, destId, request, ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "S3 copy failed {Source}->{Dest}", srcKey, destKey);
            // If we created the destination object but failed to persist metadata, remove the orphan destination.
            if (copyCompleted) {
                try {
                    await _s3Client.DeleteObjectAsync(new() { BucketName = _options.BucketName, Key = destKey }, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception delEx) {
                    Logger.LogWarning(delEx, "Failed to clean up orphan destination {Dest} after copy metadata failure", destKey);
                }
            }

            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.Copy, DateTime.UtcNow, destId, meta.TenantId, OperationContextAccessor.Current?.ActorId, meta.DataEncryptionKeyId,
                        meta.DataEncryptionKeyVersion, FileAuditOutcome.Failure, SanitizeAuditError(ex.Message), sourceFileId), ct)
                .ConfigureAwait(false);

            throw;
        }
    }

    /// <inheritdoc />
    public override async Task<FileStoreResult> MoveFileAsync(Guid fileId, MoveFileRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ValidatePathPrefix(request.PathPrefix);
        var destPrefix = NormalizePathPrefix(request.PathPrefix);
        var meta = await GetMetadataAsync(fileId, ct).ConfigureAwait(false);
        EnsureReadableAvailability(meta);
        OperationHelpers.ThrowIf(meta.Availability == FileAvailability.PendingDirectUpload, $"Cannot move file {fileId}; it is awaiting direct-upload finalize.");
        var previousPrefix = meta.PathPrefix;
        if (string.Equals(previousPrefix, destPrefix, StringComparison.Ordinal))
            return meta;

        var srcKey = await FindObjectKeyAsync(fileId, previousPrefix, ct).ConfigureAwait(false);
        if (srcKey == null)
            throw new FileNotFoundException($"Source object not found for id {fileId}.");

        var suffix = InferTrailingSuffixAfterFileId(meta.Id, meta.SourceFileName);
        var destKey = GetObjectKey(fileId, suffix, destPrefix);
        var copyCompleted = false;
        var metadataSaved = false;
        try {
            var copy = new CopyObjectRequest {
                SourceBucket = _options.BucketName,
                SourceKey = srcKey,
                DestinationBucket = _options.BucketName,
                DestinationKey = destKey
            };

            S3UploadServerSideEncryption.ApplyToCopyDestination(copy, _options);
            await _s3Client.CopyObjectAsync(copy, ct).ConfigureAwait(false);
            copyCompleted = true;
            var movedMeta = await RecordMoveMetadataAsync(meta, destPrefix, ct).ConfigureAwait(false);
            metadataSaved = true;
            try {
                await _s3Client.DeleteObjectAsync(new() { BucketName = _options.BucketName, Key = srcKey }, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception delEx) {
                Logger.LogWarning(delEx, "Failed to delete source object {Src} after move to {Dest}; metadata already points at destination", srcKey, destKey);
            }

            RaiseFileMoved(fileId, FileStoreSnapshot.From(movedMeta), previousPrefix);
            return movedMeta;
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "S3 move failed {Source}->{Dest}", srcKey, destKey);
            if (copyCompleted && !metadataSaved) {
                try {
                    await _s3Client.DeleteObjectAsync(new() { BucketName = _options.BucketName, Key = destKey }, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception delEx) {
                    Logger.LogWarning(delEx, "Failed to clean up orphan destination {Dest} after move metadata failure", destKey);
                }
            }

            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.Move, DateTime.UtcNow, fileId, meta.TenantId, OperationContextAccessor.Current?.ActorId, meta.DataEncryptionKeyId,
                        meta.DataEncryptionKeyVersion, FileAuditOutcome.Failure, SanitizeAuditError(ex.Message)), ct)
                .ConfigureAwait(false);

            throw;
        }
    }

    protected override async Task<long> GetStorageSizeAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
    {
        // Prefer the suffix-based GetObjectKey first (cheapest: a single HEAD), and only fall back to FindObjectKeyAsync if the head misses,
        // which keeps the common path one round-trip but still tolerates legacy or alternate suffixes.
        var objectKey = GetObjectKey(fileId, extension, pathPrefix);
        try {
            var request = new GetObjectMetadataRequest { BucketName = _options.BucketName, Key = objectKey };
            var response = await _s3Client.GetObjectMetadataAsync(request, ct).ConfigureAwait(false);
            return response.ContentLength;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
            var resolved = await FindObjectKeyAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
            if (resolved == null || string.Equals(resolved, objectKey, StringComparison.Ordinal))
                return 0;

            try {
                var request = new GetObjectMetadataRequest { BucketName = _options.BucketName, Key = resolved };
                var response = await _s3Client.GetObjectMetadataAsync(request, ct).ConfigureAwait(false);
                return response.ContentLength;
            }
            catch (AmazonS3Exception innerEx) when (innerEx.StatusCode == HttpStatusCode.NotFound) {
                return 0;
            }
        }
    }

    protected override async Task<Stream?> ReadFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
    {
        var objectKey = await FindObjectKeyAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
        if (objectKey == null)
            return null;

        var getRequest = new GetObjectRequest { BucketName = _options.BucketName, Key = objectKey };
        var response = await _s3Client.GetObjectAsync(getRequest, ct).ConfigureAwait(false);
        // Wrap so the caller disposing the stream also releases the response's underlying HTTP handle.
        return new S3GetObjectResponseStream(response);
    }

    protected override async Task<bool> DeleteFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
    {
        var objectKey = await FindObjectKeyAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
        if (objectKey != null) {
            var deleteRequest = new DeleteObjectRequest { BucketName = _options.BucketName, Key = objectKey };
            await _s3Client.DeleteObjectAsync(deleteRequest, ct).ConfigureAwait(false);
            Logger.LogDebug("Deleted file {FileId} from S3 at key {ObjectKey}", fileId, objectKey);
            return true;
        }

        return false;
    }

    protected override async Task<EncryptionHeaderInfo> ExtractEncryptionHeaderAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
    {
        // Header is bounded (a few KiB even for max key-id/version + wrapped DEK), so a small range GET is enough — no need to download the entire ciphertext.
        const int rangeBytes = 8 * 1024;
        var objectKey = GetObjectKey(fileId, extension, pathPrefix);
        var getRequest = new GetObjectRequest { BucketName = _options.BucketName, Key = objectKey, ByteRange = new(0, rangeBytes - 1) };
        using var response = await _s3Client.GetObjectAsync(getRequest, ct).ConfigureAwait(false);
        using var bufferStream = new MemoryStream();
        await response.ResponseStream.CopyToAsync(bufferStream, ct).ConfigureAwait(false);
        bufferStream.Position = 0;
        var header = EncryptionHeader.Read(bufferStream);
        return new(header.EncryptedDataEncryptionKey, header.KeyId, header.KeyVersion, header.DekKeyMaterialBytes);
    }

    protected override async Task UpdateFileHeaderAsync(Guid fileId, string? pathPrefix, string targetKeyId, string targetKeyVersion, byte[] newEncryptedDek, CancellationToken ct)
    {
        var objectKey = await FindObjectKeyAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
        if (objectKey == null)
            throw new FileNotFoundException($"File {fileId} not found in S3; cannot rotate header.", fileId.ToString());

        // Spool the existing object to a temp file so we can read the header, rewrite it in-place, and stream the new payload back to S3 without buffering the whole blob in RAM.
        var spoolPath = Path.Combine(Path.GetTempPath(), $"lyo-fs-s3-hdr-{fileId:N}.tmp");
        try {
            var getRequest = new GetObjectRequest { BucketName = _options.BucketName, Key = objectKey };
            long objectLength;
            await using (var spool = new FileStream(
                spoolPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.DeleteOnClose)) {
                using (var getResponse = await _s3Client.GetObjectAsync(getRequest, ct).ConfigureAwait(false)) {
                    await using (var responseStream = getResponse.ResponseStream)
                        await responseStream.CopyToAsync(spool, 81920, ct).ConfigureAwait(false);
                }

                await spool.FlushAsync(ct).ConfigureAwait(false);
                objectLength = spool.Length;
                if (objectLength < 13)
                    throw new InvalidDataException($"File {fileId} has a truncated or invalid encryption header ({objectLength} bytes); cannot rotate DEK.");

                spool.Position = 0;
                var oldHeader = EncryptionHeader.Read(spool);
                var oldHeaderSize = (int)spool.Position;
                var updatedHeader = oldHeader.With(targetKeyId, targetKeyVersion, newEncryptedDek);
                var newHeaderBytes = SerializeHeaderToArray(updatedHeader);
                if (newHeaderBytes.Length == oldHeaderSize) {
                    // Same-size header rewrite — patch in place.
                    spool.Position = 0;
                    await spool.WriteAsync(newHeaderBytes, 0, newHeaderBytes.Length, ct).ConfigureAwait(false);
                    await spool.FlushAsync(ct).ConfigureAwait(false);
                    spool.Position = 0;
                }
                else {
                    // Header size changed — restage to a sibling temp file so we can swap atomically.
                    var staged = Path.Combine(Path.GetTempPath(), $"lyo-fs-s3-hdr-{fileId:N}-staged.tmp");
                    await using (var stagedStream = new FileStream(
                        staged, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.DeleteOnClose)) {
                        await stagedStream.WriteAsync(newHeaderBytes, 0, newHeaderBytes.Length, ct).ConfigureAwait(false);
                        spool.Position = oldHeaderSize;
                        await spool.CopyToAsync(stagedStream, 81920, ct).ConfigureAwait(false);
                        await stagedStream.FlushAsync(ct).ConfigureAwait(false);
                        stagedStream.Position = 0;
                        var putRequest = new PutObjectRequest {
                            BucketName = _options.BucketName,
                            Key = objectKey,
                            InputStream = stagedStream,
                            ContentType = FileTypeInfo.Unknown.MimeType,
                            AutoCloseStream = false
                        };

                        S3UploadServerSideEncryption.ApplyToPutObject(putRequest, _options);
                        await _s3Client.PutObjectAsync(putRequest, ct).ConfigureAwait(false);
                    }

                    Logger.LogDebug("Updated S3 object header for {FileId} with new keyId '{KeyId}', version {Version}, and encrypted DEK", fileId, targetKeyId, targetKeyVersion);
                    return;
                }

                var putRequestInPlace = new PutObjectRequest {
                    BucketName = _options.BucketName,
                    Key = objectKey,
                    InputStream = spool,
                    ContentType = FileTypeInfo.Unknown.MimeType,
                    AutoCloseStream = false
                };

                S3UploadServerSideEncryption.ApplyToPutObject(putRequestInPlace, _options);
                await _s3Client.PutObjectAsync(putRequestInPlace, ct).ConfigureAwait(false);
            }

            Logger.LogDebug("Updated S3 object header for {FileId} with new keyId '{KeyId}', version {Version}, and encrypted DEK", fileId, targetKeyId, targetKeyVersion);
        }
        catch (Exception ex) {
            Logger.LogDebug(ex, "S3 DEK header rotation failed for {FileId} key {ObjectKey}", fileId, objectKey);
            throw;
        }
    }

    private static byte[] SerializeHeaderToArray(EncryptionHeader header)
    {
        using var ms = new MemoryStream(header.GetHeaderSize());
        header.Write(ms);
        return ms.ToArray();
    }

    protected override async Task CleanupPartialFileAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
    {
        // Try to find and delete any object with this ID (could have different extensions)
        var objectKey = await FindObjectKeyAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
        if (objectKey != null) {
            try {
                var deleteRequest = new DeleteObjectRequest { BucketName = _options.BucketName, Key = objectKey };
                await _s3Client.DeleteObjectAsync(deleteRequest, ct).ConfigureAwait(false);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
                // race with concurrent delete
            }
            catch (Exception ex) {
                Logger.LogWarning(ex, "Failed partial-upload cleanup delete for key {Key}", objectKey);
            }
        }
    }

    public override void Dispose()
    {
        if (Disposed)
            return;

        if (_ownsS3Client)
            _s3Client.Dispose();

        base.Dispose();
    }

    protected override Task<Stream> CreateOutputStreamAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
    {
        var objectKey = GetObjectKey(fileId, extension, pathPrefix);
        return Task.FromResult<Stream>(new S3UploadStream(_s3Client, _options.BucketName, objectKey, _options, ct));
    }

    /// <summary>
    /// Finalizes a multipart staging object in-place using S3 server-side <c>CopyObject</c> instead of round-tripping the bytes back through the client. Hash and (optional) scan
    /// still require one download of the staging object into a local temp file; the upload that <see cref="FileStorageServiceBase.SaveFromStreamAsync" /> would have done is replaced by
    /// an in-bucket copy. Only supports the no-compress/no-encrypt finalize path; callers must use the streaming pipeline when transforms are required.
    /// </summary>
    internal async Task<FileStoreResult> FinalizeMultipartFromStagingAsync(
        string stagingKey,
        Guid targetFileId,
        string? originalFileName,
        string? contentType,
        string? pathPrefix,
        string? tenantId,
        FileAvailability? availabilityOverride,
        CancellationToken ct)
    {
        var normalizedPathPrefix = NormalizePathPrefix(pathPrefix);
        var finalKey = GetObjectKey(targetFileId, "", normalizedPathPrefix);
        var hashAlg = Options.HashAlgorithm;
        var tempPath = Path.Combine(Path.GetTempPath(), $"lyo-fs-s3-mpufin-{targetFileId:N}.tmp");
        long observedLength;
        byte[] computedHash;
        try {
            await using (var spool = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous)) {
                using (var getResponse = await _s3Client.GetObjectAsync(new() { BucketName = _options.BucketName, Key = stagingKey }, ct).ConfigureAwait(false)) {
                    await using (var responseStream = getResponse.ResponseStream) {
                        using (var hasher = hashAlg.Create()) {
                            await using (var hashing = new HashingStream(spool, hasher)) {
                                await responseStream.CopyToAsync(hashing, 81920, ct).ConfigureAwait(false);
                                computedHash = hashing.GetHash();
                            }
                        }
                    }
                }

                await spool.FlushAsync(ct).ConfigureAwait(false);
                observedLength = spool.Length;
            }

            var copyRequest = new CopyObjectRequest {
                SourceBucket = _options.BucketName,
                SourceKey = stagingKey,
                DestinationBucket = _options.BucketName,
                DestinationKey = finalKey,
                ContentType = contentType ?? FileTypeInfo.Unknown.MimeType
            };

            S3UploadServerSideEncryption.ApplyToCopyDestination(copyRequest, _options);
            await _s3Client.CopyObjectAsync(copyRequest, ct).ConfigureAwait(false);
            try {
                var sourceFileName = originalFileName ?? targetFileId.ToString();
                var resolvedContentType = string.IsNullOrWhiteSpace(contentType) ? FileTypeInfo.Unknown.MimeType : contentType;
                var metadata = new FileStoreResult(
                    targetFileId, originalFileName ?? sourceFileName, observedLength, computedHash, sourceFileName, observedLength, computedHash, false, null, null, null, false,
                    null, null, null, null, null, null, null, null, DateTime.UtcNow, normalizedPathPrefix, hashAlg, resolvedContentType, tenantId,
                    availabilityOverride ?? Options.DefaultAvailability);

                await MetadataService.SaveMetadataAsync(targetFileId, metadata, ct).ConfigureAwait(false);
                RaiseFileSaved(targetFileId, FileStoreSnapshot.From(metadata), observedLength, observedLength, false, false);
                return metadata;
            }
            catch {
                // Metadata save failed — roll back the destination object so the staging delete by the caller is the only side-effect.
                try {
                    await _s3Client.DeleteObjectAsync(new() { BucketName = _options.BucketName, Key = finalKey }, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception cleanupEx) {
                    Logger.LogWarning(cleanupEx, "Failed to clean up destination key {Key} after metadata save failure", finalKey);
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

    /// <inheritdoc />
    public override Task<string> GetPreSignedReadUrlAsync(Guid fileId, TimeSpan? expiration = null, string? pathPrefix = null, CancellationToken ct = default)
        => GetPreSignedReadUrlAsync(fileId, expiration, pathPrefix, null, ct);

    /// <inheritdoc />
    public override async Task<string> GetPreSignedReadUrlAsync(
        Guid fileId,
        TimeSpan? expiration,
        string? pathPrefix,
        PreSignedReadUrlOptions? urlResponseOptions,
        CancellationToken ct)
    {
        var meta = await GetMetadataAsync(fileId, ct).ConfigureAwait(false);
        EnsureReadableAvailability(meta);
        var expirationTime = expiration ?? TimeSpan.FromHours(1);
        ArgumentHelpers.ThrowIfNotInRange(expirationTime, TimeSpan.Zero, TimeSpan.FromDays(7));
        var resolvedPrefix = pathPrefix ?? meta.PathPrefix;
        var objectKey = await FindObjectKeyAsync(fileId, resolvedPrefix, ct).ConfigureAwait(false);
        if (objectKey == null) {
            Logger.LogWarning("File {FileId} not found in S3, cannot generate pre-signed URL", fileId);
            throw new FileNotFoundException($"File with ID {fileId} was not found in storage.");
        }

        try {
            var request = new GetPreSignedUrlRequest {
                BucketName = _options.BucketName,
                Key = objectKey,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.Add(expirationTime)
            };

            var cd = urlResponseOptions?.ContentDisposition;
            var ctOverride = urlResponseOptions?.ContentType;
            if (!string.IsNullOrWhiteSpace(cd) || !string.IsNullOrWhiteSpace(ctOverride)) {
                request.ResponseHeaderOverrides = new() {
                    ContentDisposition = string.IsNullOrWhiteSpace(cd) ? null : cd, ContentType = string.IsNullOrWhiteSpace(ctOverride) ? null : ctOverride
                };
            }

            // GetPreSignedURL is synchronous, but the method is async to support cancellation token
            var url = await _s3Client.GetPreSignedURLAsync(request).ConfigureAwait(false);
            Logger.LogDebug("Generated pre-signed URL for file {FileId} at key {ObjectKey}, expires in {Expiration}", fileId, objectKey, expirationTime);
            Metrics.IncrementCounter(MetricNames[nameof(FileStorage.Constants.Metrics.FileStoragePreSignedUrlGenerated)], tags: [("bucket", _options.BucketName)]);
            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.PresignedRead, DateTime.UtcNow, fileId, meta.TenantId, OperationContextAccessor.Current?.ActorId, meta.DataEncryptionKeyId,
                        meta.DataEncryptionKeyVersion, FileAuditOutcome.Success), ct)
                .ConfigureAwait(false);

            return url;
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Failed to generate pre-signed URL for file {FileId} at key {ObjectKey}", fileId, objectKey);
            Metrics.IncrementCounter(
                MetricNames[nameof(FileStorage.Constants.Metrics.FileStoragePreSignedUrlGenerationFailed)], tags: [("bucket", _options.BucketName), ("error", ex.GetType().Name)]);

            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.PresignedRead, DateTime.UtcNow, fileId, meta.TenantId, OperationContextAccessor.Current?.ActorId, meta.DataEncryptionKeyId,
                        meta.DataEncryptionKeyVersion, FileAuditOutcome.Failure, ex.Message), ct)
                .ConfigureAwait(false);

            throw;
        }
    }

    private string GetObjectKey(Guid fileId, string extension = "", string? pathPrefix = null) => CloudObjectKeyBuilder.Build(fileId, extension, pathPrefix, _options.KeyPrefix);

    private async Task<string?> FindObjectKeyAsync(Guid fileId, string? pathPrefix = null, CancellationToken ct = default)
    {
        // Hot-path optimization: if metadata is available for this fileId, try the suffix derived from its SourceFileName first to avoid the N+1 HEAD probes.
        var hintedSuffix = await TryGetSuffixHintFromMetadataAsync(fileId, ct).ConfigureAwait(false);
        if (hintedSuffix != null) {
            var hintedKey = GetObjectKey(fileId, hintedSuffix, pathPrefix);
            if (await ObjectExistsAsync(hintedKey, ct).ConfigureAwait(false))
                return hintedKey;
        }

        var baseKey = GetObjectKey(fileId, "", pathPrefix);
        if (await ObjectExistsAsync(baseKey, ct).ConfigureAwait(false))
            return baseKey;

        if (CompressionService != null) {
            var key = GetObjectKey(fileId, CompressionService.FileExtension, pathPrefix);
            if (await ObjectExistsAsync(key, ct).ConfigureAwait(false))
                return key;
        }

        if (TwoKeyEncryptionService != null) {
            var key = GetObjectKey(fileId, TwoKeyEncryptionService.FileExtension, pathPrefix);
            if (await ObjectExistsAsync(key, ct).ConfigureAwait(false))
                return key;

            if (CompressionService != null) {
                key = GetObjectKey(fileId, CompressionService.FileExtension + TwoKeyEncryptionService.FileExtension, pathPrefix);
                if (await ObjectExistsAsync(key, ct).ConfigureAwait(false))
                    return key;
            }
        }

        var commonExtensions = FileTypeInfo.CommonStorageResolutionSuffixes;
        foreach (var ext in commonExtensions) {
            ct.ThrowIfCancellationRequested();
            var key = GetObjectKey(fileId, ext, pathPrefix);
            if (await ObjectExistsAsync(key, ct).ConfigureAwait(false))
                return key;
        }

        return null;
    }

    /// <summary>
    /// Returns the storage suffix recorded in metadata's <c>SourceFileName</c> for the given <paramref name="fileId" />, or <see langword="null" /> when the metadata is not
    /// reachable, the file id prefix is missing, or the metadata is itself missing. Used as a hint to skip the N+1 HEAD probes inside <see cref="FindObjectKeyAsync" />.
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

    private async Task<bool> ObjectExistsAsync(string objectKey, CancellationToken ct = default)
    {
        try {
            var request = new GetObjectMetadataRequest { BucketName = _options.BucketName, Key = objectKey };
            await _s3Client.GetObjectMetadataAsync(request, ct).ConfigureAwait(false);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound || string.Equals(ex.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase)) {
            return false;
        }
        catch (AmazonS3Exception ex) {
            Logger.LogWarning(ex, "Unexpected S3 error probing key {Key}", objectKey);
            throw;
        }
    }

    private string BuildDiagnosticsCombinedPrefix(string? userPrefixExtended)
    {
        var trimmed = FileHelpers.NormalizeAndValidatePathPrefix(userPrefixExtended);
        var kp = FileHelpers.NormalizePathPrefix(_options.KeyPrefix);
        return kp.Length == 0 ? trimmed : trimmed.Length == 0 ? kp : kp + "/" + trimmed;
    }

    private IAmazonS3 CreateS3Client()
    {
        var config = new AmazonS3Config();
        if (!string.IsNullOrWhiteSpace(_options.Region)) {
            var region = RegionEndpoint.GetBySystemName(_options.Region);
            config.RegionEndpoint = region;
        }

        if (!string.IsNullOrWhiteSpace(_options.ServiceUrl)) {
            config.ServiceURL = _options.ServiceUrl;
            config.ForcePathStyle = true; // Required for S3-compatible services
        }

        if (S3AwsCredentialHelpers.TryGetExplicitCredentials(_options.AccessKeyId, _options.SecretAccessKey, out var credentials))
            return new AmazonS3Client(credentials, config);

        return new AmazonS3Client(config);
    }
}