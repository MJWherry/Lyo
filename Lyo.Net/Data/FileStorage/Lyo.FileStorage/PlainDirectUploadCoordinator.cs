using Lyo.Common.Extensions;
using Lyo.Exceptions;
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
/// Encapsulates the metadata side of plain-object direct uploads plus server-side copy bookkeeping so storage drivers delegate policy, auditing, and hash finalization
/// uniformly.
/// </summary>
internal sealed class PlainDirectUploadCoordinator
{
    private readonly IFileAuditPublisher _auditPublisher;
    private readonly IFileContentPolicy _contentPolicy;
    private readonly int _copyToBufferSizeBytes;
    private readonly ILogger _logger;
    private readonly IFileMalwareScanner _malwareScanner;
    private readonly IFileStorageMetadataLookup _metadataLookup;
    private readonly IFileStorageMetadataNormalization _metadataNormalization;
    private readonly IFileMetadataStore _metadataService;
    private readonly IFileOperationContextAccessor _operationContextAccessor;
    private readonly FileStorageServiceBaseOptions _options;
    private readonly IFileStoragePhysicalIo _physicalIo;

    /// <summary>Creates a coordinator with shared policy services, metadata store, polymorphic blob I/O, auditing, and field normalization supplied by concrete storage backends.</summary>
    internal PlainDirectUploadCoordinator(
        IFileContentPolicy contentPolicy,
        IFileMalwareScanner malwareScanner,
        IFileMetadataStore metadataService,
        IFileOperationContextAccessor operationContextAccessor,
        FileStorageServiceBaseOptions options,
        ILogger logger,
        IFileStoragePhysicalIo physicalIo,
        IFileAuditPublisher auditPublisher,
        IFileStorageMetadataNormalization metadataNormalization,
        IFileStorageMetadataLookup metadataLookup,
        int copyToBufferSizeBytes)
    {
        _contentPolicy = contentPolicy;
        _malwareScanner = malwareScanner;
        _metadataService = metadataService;
        _metadataLookup = metadataLookup;
        _operationContextAccessor = operationContextAccessor;
        _options = options;
        _logger = logger;
        _physicalIo = physicalIo;
        _auditPublisher = auditPublisher;
        _metadataNormalization = metadataNormalization;
        _copyToBufferSizeBytes = copyToBufferSizeBytes;
    }

    /// <summary>Derives trailing characters from <paramref name="sourceFileName" /> after stripping the GUID prefix so hashed storage layouts preserve extensions/extra suffix segments.</summary>
    /// <param name="id"><see cref="FileStoreResult.Id" /> used when matching prefixed filenames.</param>
    /// <param name="sourceFileName">Upstream filename optionally beginning with the file id in <c>N</c> (no hyphens) or default <c>D</c> (with hyphens) format.</param>
    /// <returns>Suffix following the GUID prefix, or an empty string when indeterminate.</returns>
    internal static string InferTrailingSuffixAfterFileId(Guid id, string? sourceFileName)
    {
        if (sourceFileName.IsNullOrEmpty())
            return "";

        var s = sourceFileName;
        var n = id.ToString("N");
        if (s.StartsWith(n, StringComparison.Ordinal))
            return s[n.Length..];

        var dash = id.ToString();
        return s.StartsWith(dash, StringComparison.OrdinalIgnoreCase) ? s[dash.Length..] : "";
    }

    /// <summary>
    /// Validates declarative upload metadata, clamps tenant/content-type fields, persists a <see cref="FileAvailability.PendingDirectUpload" /> row, and emits a begin audit
    /// marker.
    /// </summary>
    /// <param name="fileId">Identifier clients will finalize against.</param>
    /// <param name="request">Client supplied bounds and filenames.</param>
    /// <param name="normalizedPathPrefix">Canonical prefix aligning with downstream object keys.</param>
    /// <param name="ct">Cancellation token threaded into policy + persistence.</param>
    /// <returns>The freshly persisted metadata snapshot.</returns>
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
        if (_options.MaxUploadSizeBytes.HasValue && request.DeclaredMaxSizeBytes > _options.MaxUploadSizeBytes.Value)
            throw new InvalidOperationException($"DeclaredMaxSizeBytes {request.DeclaredMaxSizeBytes} exceeds configured MaxUploadSizeBytes.");

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
            fileId, request.OriginalFileName ?? fileId.ToString(), 0, [], fileId.ToString(), 0, [], false, null, null, null, false, null, null,
            null, null, null, null, null, null, ts, normalizedPathPrefix, _options.HashAlgorithm, ctResolved, resolvedTenant, FileAvailability.PendingDirectUpload);

        await _metadataService.SaveMetadataAsync(fileId, meta, ct).ConfigureAwait(false);
        await _auditPublisher.PublishAuditAsync(
                new(
                    FileAuditEventType.DirectUploadBegin, DateTime.UtcNow, fileId, resolvedTenant, _operationContextAccessor.Current?.ActorId, null, null,
                    FileAuditOutcome.Success),
                ct)
            .ConfigureAwait(false);

        return meta;
    }

    /// <summary>Loads the provisional object bytes, verifies optional expected lengths, computes integrity hashes, runs malware gates when configured, and marks availability accordingly.</summary>
    /// <param name="fileId">Pending upload identifier.</param>
    /// <param name="completeRequest">Optional client overrides for filenames and asserted byte counts.</param>
    /// <param name="ct">Cancellation token propagated through hashing and scanning routines.</param>
    /// <returns>Updated metadata describing the persisted object plus availability outcome.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when metadata is not awaiting finalize, encryption unexpectedly enabled, payload empty, policy rejects size, or length mismatch
    /// occurs.
    /// </exception>
    /// <exception cref="FileNotFoundException">Thrown when the backing blob is absent.</exception>
    /// <exception cref="FilePolicyRejectedException">Thrown when scanning flags a definite threat.</exception>
    internal async Task<FileStoreResult> FinalizePendingPlainDirectUploadCoreAsync(Guid fileId, DirectUploadCompleteRequest? completeRequest, CancellationToken ct)
    {
        FileStoreResult? metaForAudit = null;
        try {
            var meta = await _metadataLookup.GetMetadataForStorageAsync(fileId, ct).ConfigureAwait(false);
            metaForAudit = meta;
            OperationHelpers.ThrowIf(
                meta.Availability != FileAvailability.PendingDirectUpload, $"File {fileId} is not pending direct upload finalize (availability={meta.Availability}).");

            OperationHelpers.ThrowIf(meta.IsEncrypted || meta.IsCompressed, "Direct finalize only supports uncompressed, unencrypted placeholder metadata.");
            EnsureScanRequirementSatisfied();
            var raw = await _physicalIo.ReadFromStorageAsync(fileId, meta.PathPrefix, ct).ConfigureAwait(false);
            if (raw == null)
                throw new FileNotFoundException($"No backing object exists for pending direct upload {fileId}");

            // Spool to a temp file so we can stream-hash, stream-scan, and stay within bounded RAM regardless of payload size.
            var spoolPath = Path.Combine(Path.GetTempPath(), $"lyo-fs-direct-{fileId:N}.tmp");
            try {
                using (raw)
#if NETSTANDARD2_0
                {
                    using (var spoolWrite = new FileStream(spoolPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
                        await raw.CopyToAsync(spoolWrite, _copyToBufferSizeBytes, ct).ConfigureAwait(false);
                }
#else
                await using (var spoolWrite = new FileStream(spoolPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
                    await raw.CopyToAsync(spoolWrite, _copyToBufferSizeBytes, ct).ConfigureAwait(false);
#endif
                var observedLength = new FileInfo(spoolPath).Length;
                OperationHelpers.ThrowIfLessThan(observedLength, 1, "Direct uploaded object was empty.");
                OperationHelpers.ThrowIf(
                    _options.MaxUploadSizeBytes.HasValue && observedLength > _options.MaxUploadSizeBytes.Value,
                    $"Uploaded payload length {observedLength} exceeds MaxUploadSizeBytes.");

                OperationHelpers.ThrowIf(
                    completeRequest?.ExpectedByteLength.HasValue == true && completeRequest.ExpectedByteLength!.Value != observedLength,
                    $"Expected byte length {completeRequest?.ExpectedByteLength} but read observed {observedLength}.");

                // Stream-hash from disk.
                byte[] plainHash;
                using (var ha = _options.HashAlgorithm.Create())
#if NETSTANDARD2_0
                {
                    using (var hashStream = File.OpenRead(spoolPath))
                        plainHash = ha.ComputeHash(hashStream);
                }
#else
                await using (var hashStream = File.OpenRead(spoolPath))
                    plainHash = await ha.ComputeHashAsync(hashStream, ct).ConfigureAwait(false);
#endif
                FileAvailability availability;
                if (_options.RequireScanBeforeAvailable) {
#if NETSTANDARD2_0
                    using var scanStream = File.OpenRead(spoolPath);
#else
                    await using var scanStream = File.OpenRead(spoolPath);
#endif
                    availability = await ScanAndMapAsync(scanStream, meta.ContentType, meta.OriginalFileName, ct).ConfigureAwait(false);
                }
                else
                    availability = _options.DefaultAvailability;

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
                try {
                    if (File.Exists(spoolPath))
                        File.Delete(spoolPath);
                }
                catch (Exception ex) {
                    _logger.LogDebug(ex, "Best-effort spool cleanup failed for {Path}", spoolPath);
                }
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

    private async Task<FileAvailability> ScanAndMapAsync(Stream s, string? contentType, string? originalFileName, CancellationToken ct)
    {
        var scan = await _malwareScanner.ScanAsync(s, contentType, originalFileName, ct).ConfigureAwait(false);
        return scan.ThreatLevel switch {
            FileScanThreatLevel.Clean => FileAvailability.Available,
            FileScanThreatLevel.Suspect => FileAvailability.Quarantined,
            FileScanThreatLevel.Threat => throw new FilePolicyRejectedException(scan.Detail ?? "Malware scan rejected direct upload."),
            // Unknown / future enum values: fail closed to quarantine.
            var _ => FileAvailability.Quarantined
        };
    }

    private void EnsureScanRequirementSatisfied()
    {
        if (_options.RequireScanBeforeAvailable && _malwareScanner is NullFileMalwareScanner) {
            throw new InvalidOperationException(
                "RequireScanBeforeAvailable is set but no IFileMalwareScanner is configured. " +
                "Register a real malware scanner (e.g. via DI) or disable RequireScanBeforeAvailable.");
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
    /// Clones <paramref name="sourceMeta" /> into a destination identifier, applies optional path-prefix overrides via <paramref name="request" />, preserves cryptographic
    /// hashes, and logs a correlated copy audit anchored on <paramref name="sourceFileId" />.
    /// </summary>
    /// <param name="sourceFileId">Original object identifier used as audit correlation payload.</param>
    /// <param name="sourceMeta">Baseline metadata mirrored for the cloned record.</param>
    /// <param name="destId">New surrogate key assigned to copied storage.</param>
    /// <param name="request">Optional copy parameters affecting logical path prefixes.</param>
    /// <param name="ct">Cancellation token threaded into persistence and auditing.</param>
    /// <returns>Persisted destination metadata reflecting the duplication.</returns>
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
}