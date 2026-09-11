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
        int copyToBufferSizeBytes)
    {
        _contentPolicy = contentPolicy;
        _metadataService = metadataService;
        _metadataLookup = metadataLookup;
        _operationContextAccessor = operationContextAccessor;
        _options = options;
        _logger = logger;
        _physicalIO = physicalIO;
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
    /// <returns>The newly persisted metadata snapshot.</returns>
    internal async Task<FileStoreResult> PersistPendingPlainDirectUploadMetadataAsync(
        Guid fileId,
        DirectUploadBeginRequest request,
        string normalizedPathPrefix,
        CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(request);
        OperationHelpers.ThrowIfLessThanOrEqual(request.DeclaredMaxSizeBytes, 0, message: "DeclaredMaxSizeBytes must be positive.");
        FileStorageServiceBase.ValidatePathPrefix(request.PathPrefix);
        var resolvedTenant = _metadataNormalization.ResolveTenantId(request.TenantId);
        var ctResolved = _metadataNormalization.ResolveStoredContentType(request.ContentType, request.OriginalFileName);
        var charset = FileStorageServiceBase.NormalizeCharset(request.Charset);
        if (_options.MaxUploadSizeBytes.HasValue && request.DeclaredMaxSizeBytes > _options.MaxUploadSizeBytes.Value)
            throw new FilePolicyRejectedException($"DeclaredMaxSizeBytes {request.DeclaredMaxSizeBytes} exceeds configured MaxUploadSizeBytes.");

        await _contentPolicy.ValidateAsync(
                new() {
                    ByteLength = request.DeclaredMaxSizeBytes,
                    ContentType = ctResolved,
                    OriginalFileName = request.OriginalFileName,
                    TenantId = resolvedTenant
                }, ct)
            .ConfigureAwait(false);

        var ts = DateTime.UtcNow;
        var meta = new FileStoreResult(
            fileId, request.OriginalFileName ?? fileId.ToString(), 0, [], fileId.ToString(), 0, [], false, null, null, null, false, null, null, null, null, null, null, null, null,
            ts, normalizedPathPrefix, _options.HashAlgorithm, ctResolved, charset, resolvedTenant, FileAvailability.PendingDirectUpload, Metadata: request.Metadata);

        await _metadataService.SaveMetadataAsync(fileId, meta, ct).ConfigureAwait(false);
        await _auditPublisher.PublishAuditAsync(
                new(
                    FileAuditEventType.DirectUploadBegin, DateTime.UtcNow, fileId, resolvedTenant, _operationContextAccessor.Current?.ActorId, null, null,
                    FileAuditOutcome.Success),
                ct)
            .ConfigureAwait(false);

        return meta;
    }

    /// <summary>
    /// Loads the provisional object bytes, checks optional expected lengths, hashes integrity, and sets availability.
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
            var raw = await _physicalIO.ReadFromStorageAsync(fileId, meta.PathPrefix, ct).ConfigureAwait(false);
            if (raw == null)
                throw new FileNotFoundException($"No backing object exists for pending direct upload {fileId}");

            // Spool to a temp file so we can stream-hash and stay in bounded RAM no matter the payload size.
            var spoolPath = TempSpool.CreatePath("fs-direct", fileId);
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
                OperationHelpers.ThrowIfLessThan(observedLength, 1, "Direct uploaded object was empty.");
                OperationHelpers.ThrowIf(
                    _options.MaxUploadSizeBytes.HasValue && observedLength > _options.MaxUploadSizeBytes.Value,
                    $"Uploaded payload length {observedLength} exceeds MaxUploadSizeBytes.");

                OperationHelpers.ThrowIf(
                    completeRequest?.ExpectedByteLength.HasValue == true && completeRequest.ExpectedByteLength!.Value != observedLength,
                    $"Expected byte length {completeRequest?.ExpectedByteLength} but read observed {observedLength}.");

                // Hash from disk in a stream.
                byte[] plainHash;
                using (var ha = _options.HashAlgorithm.Create())
#if NETSTANDARD2_0
                {
                    using (var hashStream = File.OpenRead(spoolPath))
                        plainHash = ha.ComputeHash(hashStream);
                }
#else
                {
                    await using (var hashStream = File.OpenRead(spoolPath))
                        plainHash = await ha.ComputeHashAsync(hashStream, ct).ConfigureAwait(false);
                }
#endif
                FileAvailability availability = _options.DefaultAvailability;

                var finalized = meta with {
                    OriginalFileSize = observedLength,
                    OriginalFileHash = plainHash,
                    SourceFileSize = observedLength,
                    SourceFileHash = plainHash,
                    Timestamp = DateTime.UtcNow,
                    Availability = availability,
                    OriginalFileName = completeRequest?.OriginalFileName ?? meta.OriginalFileName,
                    ContentType = meta.ContentType
                };

                await _metadataService.SaveMetadataAsync(fileId, finalized, ct).ConfigureAwait(false);
                await _auditPublisher.PublishAuditAsync(
                        new(
                            FileAuditEventType.DirectUploadComplete, DateTime.UtcNow, fileId, finalized.TenantId, _operationContextAccessor.Current?.ActorId, null, null,
                            FileAuditOutcome.Success), ct)
                    .ConfigureAwait(false);

                return finalized;
            }
            finally {
                if (!TempSpool.TryDelete(spoolPath))
                    _logger.LogDebug("Best-effort spool cleanup failed for {Path}", spoolPath);
            }
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