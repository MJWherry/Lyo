using Lyo.Common.Core.Extensions;
using Lyo.Common.Core.Pathing;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Microsoft.Extensions.Logging;

namespace Lyo.FileStorage;

/// <summary>
/// Owns the metadata side of plain-object direct uploads plus server-side copy, move, and rename bookkeeping, so storage drivers share one policy, audit, and hash
/// finalize path.
/// </summary>
internal sealed class PlainDirectUploadCoordinator
{
    private readonly IFileAuditPublisher _auditPublisher;
    private readonly IFileContentPolicy _contentPolicy;
    private readonly int _copyToBufferSizeBytes;
    private readonly ILogger _logger;
    private readonly IFileStorageMetadataLookup _metadataLookup;
    private readonly IFileStorageMetadataNormalization _metadataNormalization;
    private readonly IFileMetadataStore _metadataService;
    private readonly IFileOperationContextAccessor _operationContextAccessor;
    private readonly FileStorageServiceBaseOptions _options;
    private readonly IFileStoragePhysicalIO _physicalIO;
    private readonly IFileTypeSniffer _fileTypeSniffer;
    private readonly IDirectUploadVerification? _verification;

    /// <summary>URL lifetime every backend applies when <see cref="DirectUploadBeginRequest.UrlExpiration" /> is unset.</summary>
    private static readonly TimeSpan DefaultUrlExpiration = TimeSpan.FromHours(1);

    /// <summary>
    /// Builds a coordinator with shared policy, metadata store, blob I/O, auditing, and field normalization supplied by the concrete storage backend.
    /// </summary>
    internal PlainDirectUploadCoordinator(
        IFileContentPolicy contentPolicy,
        IFileMetadataStore metadataService,
        IFileOperationContextAccessor operationContextAccessor,
        FileStorageServiceBaseOptions options,
        ILogger logger,
        IFileStoragePhysicalIO physicalIO,
        IFileAuditPublisher auditPublisher,
        IFileStorageMetadataNormalization metadataNormalization,
        IFileStorageMetadataLookup metadataLookup,
        IFileTypeSniffer fileTypeSniffer,
        int copyToBufferSizeBytes,
        IDirectUploadVerification? verification)
    {
        _verification = verification;
        _contentPolicy = contentPolicy;
        _metadataService = metadataService;
        _metadataLookup = metadataLookup;
        _operationContextAccessor = operationContextAccessor;
        _options = options;
        _logger = logger;
        _physicalIO = physicalIO;
        _fileTypeSniffer = fileTypeSniffer;
        _auditPublisher = auditPublisher;
        _metadataNormalization = metadataNormalization;
        _copyToBufferSizeBytes = copyToBufferSizeBytes;
    }

    /// <summary>Takes the trailing characters of <paramref name="sourceFileName" /> after the GUID prefix so hashed layouts keep extensions and extra suffix segments.</summary>
    /// <param name="id"><see cref="FileStoreResult.Id" /> used when matching prefixed filenames.</param>
    /// <param name="sourceFileName">Upstream filename that may start with the file id in <c>N</c> (no hyphens) or default <c>D</c> (with hyphens) format.</param>
    /// <returns>Suffix after the GUID prefix, or an empty string when it cannot be determined.</returns>
    internal static string InferTrailingSuffixAfterFileId(Guid id, string? sourceFileName)
        => CloudObjectKeyBuilder.InferTrailingSuffixAfterFileId(id, sourceFileName);

    /// <summary>
    /// Checks declarative upload metadata, clamps tenant and content-type fields, writes a <see cref="FileAvailability.PendingDirectUpload" /> row, and emits a begin audit
    /// marker.
    /// </summary>
    /// <param name="fileId">Id clients will finalize against.</param>
    /// <param name="request">Client-supplied bounds and filenames.</param>
    /// <param name="normalizedPathPrefix">Canonical prefix that matches downstream object keys.</param>
    /// <param name="ct">Cancellation token passed into policy and persistence.</param>
    /// <param name="verifiedChecksumAlgorithm">
    /// Algorithm the backend verifies on write (SHA-256 for S3, MD5 for Azure). Picks which client-declared digest is recorded on the pending row for finalize to
    /// compare against. Null means the service's configured <see cref="FileStorageServiceBaseOptions.HashAlgorithm" />, which is what the download-and-hash path uses.
    /// </param>
    /// <returns>The newly persisted metadata snapshot.</returns>
    internal async Task<FileStoreResult> PersistPendingPlainDirectUploadMetadataAsync(
        Guid fileId,
        DirectUploadBeginRequest request,
        string normalizedPathPrefix,
        CancellationToken ct,
        HashAlgorithm? verifiedChecksumAlgorithm = null)
    {
        ArgumentHelpers.ThrowIfNull(request);
        OperationHelpers.ThrowIfLessThanOrEqual(request.DeclaredMaxSizeBytes, 0, message: "DeclaredMaxSizeBytes must be positive.");
        FileStorageServiceBase.ValidatePathPrefix(request.PathPrefix);
        var resolvedTenant = _metadataNormalization.ResolveTenantId(request.TenantId);
        var ctResolved = _metadataNormalization.ResolveStoredContentType(request.ContentType, request.OriginalFileName);
        var charset = FileStorageServiceBase.NormalizeCharset(request.Charset);
        if (_options.MaxUploadSizeBytes.HasValue && request.DeclaredMaxSizeBytes > _options.MaxUploadSizeBytes.Value)
            throw new FilePolicyRejectedException($"DeclaredMaxSizeBytes {request.DeclaredMaxSizeBytes} exceeds configured MaxUploadSizeBytes.");

        ArgumentHelpers.ThrowIf(
            (request.UrlExpiration ?? DefaultUrlExpiration) > _options.PendingDirectUploadTtl,
            "UrlExpiration must not exceed PendingDirectUploadTtl, or the sweeper could discard the row while the URL still accepts a PUT.", nameof(request.UrlExpiration));

        await _contentPolicy.ValidateAsync(
                new() {
                    ByteLength = request.DeclaredMaxSizeBytes,
                    ContentType = ctResolved,
                    OriginalFileName = request.OriginalFileName,
                    TenantId = resolvedTenant
                }, ct)
            .ConfigureAwait(false);

        var checksumAlgorithm = verifiedChecksumAlgorithm ?? _options.HashAlgorithm;
        var declaredChecksum = ResolveDeclaredChecksum(request, checksumAlgorithm);
        var ts = DateTime.UtcNow;
        var meta = new FileStoreResult(
            fileId, request.OriginalFileName ?? fileId.ToString(), 0, declaredChecksum, fileId.ToString(), 0, [], false, null, null, null, false, null, null, null, null, null,
            null, null, null, ts, normalizedPathPrefix, checksumAlgorithm, ctResolved, charset, resolvedTenant, FileAvailability.PendingDirectUpload, Metadata: request.Metadata);

        await _metadataService.SaveMetadataAsync(fileId, meta, ct).ConfigureAwait(false);
        await _auditPublisher.PublishAuditAsync(
                new(
                    FileAuditEventType.DirectUploadBegin, DateTime.UtcNow, fileId, resolvedTenant, _operationContextAccessor.Current?.ActorId, null, null,
                    FileAuditOutcome.Success),
                ct)
            .ConfigureAwait(false);

        return meta;
    }

    private static byte[] ResolveDeclaredChecksum(DirectUploadBeginRequest request, HashAlgorithm algorithm)
    {
        ArgumentHelpers.ThrowIf(request.ExpectedSha256 is { Length: not 32 }, "ExpectedSha256 must be exactly 32 bytes.", nameof(request.ExpectedSha256));
        ArgumentHelpers.ThrowIf(request.ExpectedContentMd5 is { Length: not 16 }, "ExpectedContentMd5 must be exactly 16 bytes.", nameof(request.ExpectedContentMd5));
        return algorithm switch {
            HashAlgorithm.Sha256 => request.ExpectedSha256 ?? [],
            HashAlgorithm.Md5 => request.ExpectedContentMd5 ?? [],
            _ => []
        };
    }

    /// <summary>
    /// Takes size and checksum from the backend when it reports them (reading at most the header prefix), otherwise downloads and hashes the object. Checks the
    /// client-declared digest and optional expected length, then sets availability.
    /// </summary>
    /// <param name="fileId">Pending upload id.</param>
    /// <param name="completeRequest">Optional client overrides for filenames and asserted byte counts.</param>
    /// <param name="ct">Cancellation token passed through hashing.</param>
    /// <returns>Updated metadata for the persisted object plus the availability outcome.</returns>
    /// <exception cref="InvalidOperationException">
    /// Raised when metadata is not awaiting finalize, encryption is unexpectedly on, the payload is empty, or the length mismatches.
    /// </exception>
    /// <exception cref="FileNotFoundException">Raised when the backing blob is missing.</exception>
    /// <exception cref="FilePolicyRejectedException">Raised when the payload is over the size policy.</exception>
    internal async Task<FileStoreResult> FinalizePendingPlainDirectUploadCoreAsync(Guid fileId, DirectUploadCompleteRequest? completeRequest, CancellationToken ct)
    {
        FileStoreResult? metaForAudit = null;
        try {
            var meta = await _metadataLookup.GetMetadataForStorageAsync(fileId, ct).ConfigureAwait(false);
            metaForAudit = meta;
            OperationHelpers.ThrowIf(
                meta.Availability != FileAvailability.PendingDirectUpload, $"File {fileId} is not pending direct upload finalize (availability={meta.Availability}).");

            OperationHelpers.ThrowIf(meta.IsEncrypted || meta.IsCompressed, "Direct finalize only supports uncompressed, unencrypted placeholder metadata.");
            var finalName = completeRequest?.OriginalFileName ?? meta.OriginalFileName;
            var reported = _verification == null ? null : await _verification.GetDirectUploadAttributesAsync(fileId, meta.PathPrefix, ct).ConfigureAwait(false);
            DirectUploadObjectAttributes observed;
            if (reported != null) {
                await ValidateUploadedObjectAsync(
                        meta, finalName, reported.Length, completeRequest,
                        () => _verification!.ReadDirectUploadPrefixAsync(fileId, meta.PathPrefix, FileHeaderInspection.PrefixLength, ct), ct)
                    .ConfigureAwait(false);

                observed = reported;
            }
            else
                observed = await SpoolAndHashAsync(meta, finalName, completeRequest, ct).ConfigureAwait(false);

            if (meta.OriginalFileHash is { Length: > 0 } declared && meta.HashAlgorithm == observed.Algorithm && !declared.AsSpan().SequenceEqual(observed.Hash))
                throw new InvalidDataException($"Uploaded object {fileId} does not match the {observed.Algorithm} checksum declared at begin.");

            var finalized = meta with {
                OriginalFileSize = observed.Length,
                OriginalFileHash = observed.Hash,
                SourceFileSize = observed.Length,
                SourceFileHash = observed.Hash,
                HashAlgorithm = observed.Algorithm,
                Timestamp = DateTime.UtcNow,
                Availability = _options.DefaultAvailability,
                OriginalFileName = finalName
            };

            await _metadataService.SaveMetadataAsync(fileId, finalized, ct).ConfigureAwait(false);
            await _auditPublisher.PublishAuditAsync(
                    new(
                        FileAuditEventType.DirectUploadComplete, DateTime.UtcNow, fileId, finalized.TenantId, _operationContextAccessor.Current?.ActorId, null, null,
                        FileAuditOutcome.Success), ct)
                .ConfigureAwait(false);

            return finalized;
        }
        catch (Exception ex) {
            await _auditPublisher.PublishAuditAsync(
                    new(
                        FileAuditEventType.DirectUploadFailed, DateTime.UtcNow, fileId, metaForAudit?.TenantId, _operationContextAccessor.Current?.ActorId, null, null,
                        FileAuditOutcome.Failure, SanitizeAuditError(ex.Message)), ct)
                .ConfigureAwait(false);

            throw;
        }
    }

    /// <inheritdoc cref="Abstractions.IFileStorageService.SweepExpiredDirectUploadsAsync" />
    internal async Task<int> SweepExpiredDirectUploadsAsync(int maxUploads, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfLessThan(maxUploads, 1);
        var expired = await _metadataService.ListPendingDirectUploadsAsync(DateTime.UtcNow - _options.PendingDirectUploadTtl, maxUploads, ct).ConfigureAwait(false);
        var swept = 0;
        foreach (var meta in expired) {
            try {
                await _physicalIO.DeleteFromStorageAsync(meta.Id, meta.PathPrefix, ct).ConfigureAwait(false);
                if (!await _metadataService.DeleteMetadataAsync(meta.Id, ct).ConfigureAwait(false))
                    continue;

                swept++;
                await _auditPublisher.PublishAuditAsync(
                        new(
                            FileAuditEventType.DirectUploadFailed, DateTime.UtcNow, meta.Id, meta.TenantId, _operationContextAccessor.Current?.ActorId, null, null,
                            FileAuditOutcome.Failure, "Direct upload expired before finalize."), ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException) {
                _logger.LogWarning(ex, "Failed to sweep expired direct upload {FileId}", meta.Id);
            }
        }

        return swept;
    }

    /// <summary>
    /// Fallback for backends that cannot report a checksum: spools the object to a temp file so hashing stays in bounded RAM, then hashes with the declared-checksum
    /// algorithm when one was recorded at begin, otherwise the configured one.
    /// </summary>
    private async Task<DirectUploadObjectAttributes> SpoolAndHashAsync(FileStoreResult meta, string finalName, DirectUploadCompleteRequest? completeRequest, CancellationToken ct)
    {
        var raw = await _physicalIO.ReadFromStorageAsync(meta.Id, meta.PathPrefix, ct).ConfigureAwait(false);
        if (raw == null)
            throw new FileNotFoundException($"No backing object exists for pending direct upload {meta.Id}");

        var spoolPath = TempSpool.CreatePath("fs-direct", meta.Id);
        try {
            using (raw)
#if NETSTANDARD2_0
            {
                using (var spoolWrite = new FileStream(spoolPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
                    await raw.CopyToAsync(spoolWrite, _copyToBufferSizeBytes, ct).ConfigureAwait(false);
            }
#else
            {
                await using (var spoolWrite = new FileStream(spoolPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
                    await raw.CopyToAsync(spoolWrite, _copyToBufferSizeBytes, ct).ConfigureAwait(false);
            }
#endif
            var observedLength = new FileInfo(spoolPath).Length;
            await ValidateUploadedObjectAsync(
                    meta, finalName, observedLength, completeRequest, async () => {
                        using var head = File.OpenRead(spoolPath);
                        return await FileHeaderInspection.ReadPrefixAsync(head, ct).ConfigureAwait(false);
                    }, ct)
                .ConfigureAwait(false);

            var algorithm = meta.OriginalFileHash.Length > 0 ? meta.HashAlgorithm ?? _options.HashAlgorithm : _options.HashAlgorithm;
            using var ha = algorithm.Create();
#if NETSTANDARD2_0
            using (var hashStream = File.OpenRead(spoolPath))
                return new(observedLength, ha.ComputeHash(hashStream), algorithm);
#else
            await using (var hashStream = File.OpenRead(spoolPath))
                return new(observedLength, await ha.ComputeHashAsync(hashStream, ct).ConfigureAwait(false), algorithm);
#endif
        }
        finally {
            if (!TempSpool.TryDelete(spoolPath))
                _logger.LogDebug("Best-effort spool cleanup failed for {Path}", spoolPath);
        }
    }

    /// <summary>
    /// Applies the name and header guards (deleting the object when policy rejects it), then the size limits. <paramref name="readPrefix" /> runs only when header
    /// inspection is configured.
    /// </summary>
    private async Task ValidateUploadedObjectAsync(
        FileStoreResult meta,
        string finalName,
        long observedLength,
        DirectUploadCompleteRequest? completeRequest,
        Func<Task<byte[]>> readPrefix,
        CancellationToken ct)
    {
        OperationHelpers.ThrowIfLessThan(observedLength, 1, "Direct uploaded object was empty.");
        try {
            FileNamePatternFilter.ThrowIfRejected(_options, finalName);
            if (FileUploadGuards.RequiresHeaderInspection(_options)) {
                var header = await readPrefix().ConfigureAwait(false);
                FileUploadGuards.EnforceHeader(_options, _fileTypeSniffer, header, finalName, meta.ContentType);
            }
        }
        catch (FilePolicyRejectedException) {
            try {
                await _physicalIO.DeleteFromStorageAsync(meta.Id, meta.PathPrefix, ct).ConfigureAwait(false);
            }
            catch (Exception deleteEx) {
                _logger.LogDebug(deleteEx, "Failed to delete rejected direct upload {FileId}", meta.Id);
            }

            throw;
        }

        OperationHelpers.ThrowIf(
            _options.MaxUploadSizeBytes.HasValue && observedLength > _options.MaxUploadSizeBytes.Value, $"Uploaded payload length {observedLength} exceeds MaxUploadSizeBytes.");

        OperationHelpers.ThrowIf(
            completeRequest?.ExpectedByteLength.HasValue == true && completeRequest.ExpectedByteLength!.Value != observedLength,
            $"Expected byte length {completeRequest?.ExpectedByteLength} but read observed {observedLength}.");
    }

    private static string SanitizeAuditError(string? message)
    {
        if (message.IsNullOrEmpty())
            return string.Empty;

        const int max = 512;
        var s = message.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return s.Length > max ? s[..max] : s;
    }

    /// <summary>
    /// Clones <paramref name="sourceMeta" /> onto a destination id, applies optional path-prefix overrides from <paramref name="request" />, keeps cryptographic
    /// hashes, and writes a correlated copy audit anchored on <paramref name="sourceFileId" />.
    /// </summary>
    /// <param name="sourceFileId">Original object id used as the audit correlation payload.</param>
    /// <param name="sourceMeta">Baseline metadata mirrored onto the cloned record.</param>
    /// <param name="destId">New surrogate key assigned to the copied storage.</param>
    /// <param name="request">Optional copy parameters that change logical path prefixes.</param>
    /// <param name="ct">Cancellation token passed into persistence and auditing.</param>
    /// <returns>Persisted destination metadata after the duplication.</returns>
    internal async Task<FileStoreResult> RecordCopyMetadataAsync(Guid sourceFileId, FileStoreResult sourceMeta, Guid destId, CopyFileRequest? request, CancellationToken ct)
    {
        var suffix = InferTrailingSuffixAfterFileId(sourceMeta.Id, sourceMeta.SourceFileName);
        var destPathPrefix = _metadataNormalization.NormalizePathPrefix(request?.PathPrefix ?? sourceMeta.PathPrefix);
        var destSourceName = $"{destId}{suffix}";
        var now = DateTime.UtcNow;
        var copyMeta = sourceMeta with {
            Id = destId,
            Timestamp = now,
            PathPrefix = destPathPrefix,
            SourceFileName = destSourceName,
            OriginalFileHash = sourceMeta.OriginalFileHash,
            CompressedFileHash = sourceMeta.CompressedFileHash,
            EncryptedFileHash = sourceMeta.EncryptedFileHash,
            DeletedAt = null
        };

        await _metadataService.SaveMetadataAsync(destId, copyMeta, ct).ConfigureAwait(false);
        await _auditPublisher.PublishAuditAsync(
                new(
                    FileAuditEventType.Copy, DateTime.UtcNow, destId, copyMeta.TenantId, _operationContextAccessor.Current?.ActorId, sourceMeta.DataEncryptionKeyId,
                    sourceMeta.DataEncryptionKeyVersion, FileAuditOutcome.Success, CorrelationId: sourceFileId), ct)
            .ConfigureAwait(false);

        return copyMeta;
    }

    /// <summary>
    /// Updates <see cref="FileStoreResult.PathPrefix" /> for an existing file id after backing bytes have been relocated, refreshes <see cref="FileStoreResult.Timestamp" />, and
    /// writes a successful move audit.
    /// </summary>
    internal async Task<FileStoreResult> RecordMoveMetadataAsync(FileStoreResult sourceMeta, string? destPathPrefix, CancellationToken ct)
    {
        var normalizedDest = _metadataNormalization.NormalizePathPrefix(destPathPrefix);
        var now = DateTime.UtcNow;
        var moveMeta = sourceMeta with {
            Timestamp = now,
            PathPrefix = normalizedDest,
            OriginalFileHash = sourceMeta.OriginalFileHash,
            CompressedFileHash = sourceMeta.CompressedFileHash,
            EncryptedFileHash = sourceMeta.EncryptedFileHash
        };

        await _metadataService.SaveMetadataAsync(sourceMeta.Id, moveMeta, ct).ConfigureAwait(false);
        await _auditPublisher.PublishAuditAsync(
                new(
                    FileAuditEventType.Move, DateTime.UtcNow, sourceMeta.Id, moveMeta.TenantId, _operationContextAccessor.Current?.ActorId, sourceMeta.DataEncryptionKeyId,
                    sourceMeta.DataEncryptionKeyVersion, FileAuditOutcome.Success), ct)
            .ConfigureAwait(false);

        return moveMeta;
    }

    /// <summary>
    /// Updates <see cref="FileStoreResult.OriginalFileName" /> for an existing file id (metadata only), refreshes <see cref="FileStoreResult.Timestamp" />, and writes a
    /// successful rename audit.
    /// </summary>
    internal async Task<FileStoreResult> RecordRenameMetadataAsync(FileStoreResult sourceMeta, string originalFileName, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var renameMeta = sourceMeta with {
            Timestamp = now,
            OriginalFileName = originalFileName,
            OriginalFileHash = sourceMeta.OriginalFileHash,
            CompressedFileHash = sourceMeta.CompressedFileHash,
            EncryptedFileHash = sourceMeta.EncryptedFileHash
        };

        await _metadataService.SaveMetadataAsync(sourceMeta.Id, renameMeta, ct).ConfigureAwait(false);
        await _auditPublisher.PublishAuditAsync(
                new(
                    FileAuditEventType.Rename, DateTime.UtcNow, sourceMeta.Id, renameMeta.TenantId, _operationContextAccessor.Current?.ActorId, sourceMeta.DataEncryptionKeyId,
                    sourceMeta.DataEncryptionKeyVersion, FileAuditOutcome.Success), ct)
            .ConfigureAwait(false);

        return renameMeta;
    }
}