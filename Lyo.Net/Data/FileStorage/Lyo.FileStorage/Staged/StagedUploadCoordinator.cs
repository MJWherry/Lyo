using Lyo.Common.Extensions;
using Lyo.Common.Records;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FileStorage.Staged;

/// <summary>
/// Shared orchestration for staged uploads; backend packages supply <see cref="IStagedFilePhysicalIo" /> and delegate public <see cref="IStagedFileUploadService" /> methods
/// here. Not intended for direct use by application code.
/// </summary>
public sealed class StagedUploadCoordinator
{
    private readonly IReadOnlyList<IFileAuditEventHandler> _auditHandlers;
    private readonly IFileContentPolicy _contentPolicy;
    private readonly int _copyToBufferSizeBytes;
    private readonly IReadOnlyList<IStagedFileUploadEventHandler> _eventHandlers;
    private readonly ILogger _logger;
    private readonly IFileMalwareScanner _malwareScanner;
    private readonly IMetrics _metrics;
    private readonly IFileOperationContextAccessor _operationContextAccessor;
    private readonly FileStorageServiceBaseOptions _options;
    private readonly IStagedFilePhysicalIo _physicalIo;
    private readonly IFileStorageService _storage;
    private readonly IStagedFileUploadStore _store;

    public StagedUploadCoordinator(
        IStagedFileUploadStore store,
        IStagedFilePhysicalIo physicalIo,
        IFileStorageService storage,
        FileStorageServiceBaseOptions options,
        IFileContentPolicy? contentPolicy = null,
        IFileMalwareScanner? malwareScanner = null,
        IFileOperationContextAccessor? operationContextAccessor = null,
        ILogger? logger = null,
        IMetrics? metrics = null,
        IEnumerable<IFileAuditEventHandler>? auditHandlers = null,
        IEnumerable<IStagedFileUploadEventHandler>? eventHandlers = null,
        int copyToBufferSizeBytes = 81920)
    {
        _store = ArgumentHelpers.ThrowIfNullReturn(store);
        _physicalIo = ArgumentHelpers.ThrowIfNullReturn(physicalIo);
        _storage = ArgumentHelpers.ThrowIfNullReturn(storage);
        _options = ArgumentHelpers.ThrowIfNullReturn(options);
        _logger = logger ?? NullLogger.Instance;
        _contentPolicy = contentPolicy ?? new AllowAllFileContentPolicy();
        _malwareScanner = malwareScanner ?? NullFileMalwareScanner.Instance;
        _operationContextAccessor = operationContextAccessor ?? NullFileOperationContextAccessor.Instance;
        _metrics = metrics ?? NullMetrics.Instance;
        _auditHandlers = auditHandlers == null ? [] : auditHandlers.ToList();
        _eventHandlers = eventHandlers == null ? [] : eventHandlers.ToList();
        _copyToBufferSizeBytes = copyToBufferSizeBytes;
    }

    public event EventHandler<StagedUploadPresignedCreatedEventArgs>? PresignedCreated;

    public event EventHandler<StagedUploadCompletedEventArgs>? UploadCompleted;

    public event EventHandler<StagedUploadFailedEventArgs>? UploadFailed;

    public event EventHandler<StagedUploadCommittedEventArgs>? Committed;

    /// <summary>Creates the stage row, issues presigned PUT, and raises <see cref="PresignedCreated" />.</summary>
    public async Task<(StagedUploadBeginResult Result, StagedFileUploadRecord Record)> BeginCoreAsync(StagedUploadBeginRequest request, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(request);
        OperationHelpers.ThrowIfLessThanOrEqual(request.DeclaredMaxSizeBytes, 0, message: "DeclaredMaxSizeBytes must be positive.");
        FileHelpers.ThrowIfPathPrefixTraversal(request.PathPrefix);
        var normalizedPrefix = FileHelpers.NormalizePathPrefix(request.PathPrefix);
        var tenant = request.TenantId ?? _operationContextAccessor.Current?.TenantId;
        var contentType = ResolveContentType(request.ContentType, request.OriginalFileName);
        if (_options.MaxUploadSizeBytes.HasValue && request.DeclaredMaxSizeBytes > _options.MaxUploadSizeBytes.Value)
            throw new FilePolicyRejectedException($"DeclaredMaxSizeBytes {request.DeclaredMaxSizeBytes} exceeds configured MaxUploadSizeBytes.");

        await _contentPolicy.ValidateAsync(
                new() {
                    ByteLength = request.DeclaredMaxSizeBytes,
                    ContentType = contentType,
                    OriginalFileName = request.OriginalFileName,
                    TenantId = tenant
                }, ct)
            .ConfigureAwait(false);

        var stageId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var sessionTtl = request.SessionTtl ?? TimeSpan.FromHours(24);
        var urlExpiry = request.UrlExpiration ?? TimeSpan.FromHours(1);
        ArgumentHelpers.ThrowIfNotInRange(urlExpiry, TimeSpan.Zero, TimeSpan.FromDays(7));
        var urlExpiresUtc = DateTimeOffset.UtcNow.Add(urlExpiry);
        var storageLocation = _physicalIo.BuildStageStorageLocation(stageId, normalizedPrefix);
        var record = new StagedFileUploadRecord(
            stageId, tenant, _operationContextAccessor.Current?.ActorId is { } actor && Guid.TryParse(actor, out var ownerId) ? ownerId : null, now, now.Add(sessionTtl),
            StagedUploadStatus.PendingUpload, storageLocation, normalizedPrefix, request.OriginalFileName ?? stageId.ToString(), contentType, request.DeclaredMaxSizeBytes, null,
            null, _options.HashAlgorithm, _physicalIo.ProviderKind, "{}", null, null);

        await _store.CreateAsync(record, ct).ConfigureAwait(false);
        var presigned = await _physicalIo.GeneratePresignedPutUrlAsync(stageId, normalizedPrefix, request, urlExpiresUtc, ct).ConfigureAwait(false);
        await PublishAuditAsync(FileAuditEventType.StagedUploadBegin, stageId, tenant, FileAuditOutcome.Success, ct: ct).ConfigureAwait(false);
        var snapshot = StagedFileUploadMappings.ToResult(record);
        var eventArgs = new StagedUploadPresignedCreatedEventArgs { StageId = stageId, TenantId = tenant, Snapshot = snapshot };
        PresignedCreated?.Invoke(this, eventArgs);
        await PublishEventHandlersAsync(h => h.OnPresignedCreatedAsync(eventArgs, ct)).ConfigureAwait(false);
        return (new() {
            StageId = stageId,
            PresignedPutUrl = presigned.Url,
            UrlExpiresUtc = urlExpiresUtc,
            StorageLocation = storageLocation,
            RequiredPutHeaders = presigned.RequiredPutHeaders,
            ProviderKind = _physicalIo.ProviderKind
        }, record);
    }

    /// <summary>Verifies staging object, hashes content, and transitions to <see cref="StagedUploadStatus.Uploaded" />.</summary>
    public async Task<StagedFileResult> CompleteCoreAsync(Guid stageId, StagedUploadCompleteRequest? request, CancellationToken ct)
    {
        var record = await _store.GetAsync(stageId, ct).ConfigureAwait(false);
        if (record == null)
            throw new FileNotFoundException($"Staged upload {stageId} was not found.");

        if (record.Status == StagedUploadStatus.Uploaded)
            return StagedFileUploadMappings.ToResult(record);

        OperationHelpers.ThrowIf(record.Status != StagedUploadStatus.PendingUpload, $"Stage {stageId} is not pending upload (status={record.Status}).");
        EnsureScanRequirementSatisfied();
        try {
            if (!await _physicalIo.ObjectExistsAsync(record, ct).ConfigureAwait(false))
                throw new FileNotFoundException($"No backing object exists for staged upload {stageId}");

            var observedLength = await _physicalIo.GetObjectSizeAsync(record, ct).ConfigureAwait(false);
            OperationHelpers.ThrowIfLessThan(observedLength, 1, "Staged uploaded object was empty.");
            OperationHelpers.ThrowIf(
                _options.MaxUploadSizeBytes.HasValue && observedLength > _options.MaxUploadSizeBytes.Value,
                $"Uploaded payload length {observedLength} exceeds MaxUploadSizeBytes.");

            OperationHelpers.ThrowIf(
                request?.ExpectedByteLength.HasValue == true && request.ExpectedByteLength!.Value != observedLength,
                $"Expected byte length {request?.ExpectedByteLength} but observed {observedLength}.");

            byte[] contentHash;
#if NETSTANDARD2_0
            using (var readStream = await _physicalIo.OpenReadStreamAsync(record, ct).ConfigureAwait(false)) {
                var spoolPath = Path.Combine(Path.GetTempPath(), $"lyo-fs-stage-{stageId:N}.tmp");
                try {
                    using (var spoolWrite = new FileStream(spoolPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
                        await readStream.CopyToAsync(spoolWrite, _copyToBufferSizeBytes, ct).ConfigureAwait(false);

                    using var ha = _options.HashAlgorithm.Create();
                    using (var hashStream = File.OpenRead(spoolPath))
                        contentHash = ha.ComputeHash(hashStream);

                    if (_options.RequireScanBeforeAvailable) {
                        using var scanStream = File.OpenRead(spoolPath);
                        var scan = await _malwareScanner.ScanAsync(scanStream, record.ContentType, record.OriginalFileName, ct).ConfigureAwait(false);
                        if (scan.ThreatLevel == FileScanThreatLevel.Threat)
                            throw new FilePolicyRejectedException(scan.Detail ?? "Malware scan rejected staged upload.");
                    }
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
#else
            await using (var readStream = await _physicalIo.OpenReadStreamAsync(record, ct).ConfigureAwait(false)) {
                var spoolPath = Path.Combine(Path.GetTempPath(), $"lyo-fs-stage-{stageId:N}.tmp");
                try {
                    await using (var spoolWrite = new FileStream(spoolPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
                        await readStream.CopyToAsync(spoolWrite, _copyToBufferSizeBytes, ct).ConfigureAwait(false);

                    using var ha = _options.HashAlgorithm.Create();
                    await using var hashStream = File.OpenRead(spoolPath);
                    contentHash = await ha.ComputeHashAsync(hashStream, ct).ConfigureAwait(false);
                    if (_options.RequireScanBeforeAvailable) {
                        await using var scanStream = File.OpenRead(spoolPath);
                        var scan = await _malwareScanner.ScanAsync(scanStream, record.ContentType, record.OriginalFileName, ct).ConfigureAwait(false);
                        if (scan.ThreatLevel == FileScanThreatLevel.Threat)
                            throw new FilePolicyRejectedException(scan.Detail ?? "Malware scan rejected staged upload.");
                    }
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
#endif
            var updated = record with {
                Status = StagedUploadStatus.Uploaded,
                ObservedSizeBytes = observedLength,
                ContentHash = contentHash,
                OriginalFileName = request?.OriginalFileName ?? record.OriginalFileName,
                FailureReason = null
            };

            await _store.UpdateAsync(updated, ct).ConfigureAwait(false);
            await PublishAuditAsync(FileAuditEventType.StagedUploadComplete, stageId, updated.TenantId, FileAuditOutcome.Success, ct: ct).ConfigureAwait(false);
            var result = StagedFileUploadMappings.ToResult(updated);
            var eventArgs = new StagedUploadCompletedEventArgs { StageId = stageId, TenantId = updated.TenantId, Snapshot = result };
            UploadCompleted?.Invoke(this, eventArgs);
            await PublishEventHandlersAsync(h => h.OnUploadCompletedAsync(eventArgs, ct)).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex) {
            await HandleFailureAsync(stageId, record, ex, ct).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Streams staged bytes through the normal save pipeline and marks <see cref="StagedUploadStatus.Committed" />.</summary>
    public async Task<FileStoreResult> CommitCoreAsync(Guid stageId, StagedUploadCommitRequest request, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(request);
        OperationHelpers.ThrowIf(request.Encrypt && string.IsNullOrWhiteSpace(request.KeyId), "Commit Encrypt=true requires KeyId.");
        var record = await _store.GetAsync(stageId, ct).ConfigureAwait(false);
        if (record == null)
            throw new FileNotFoundException($"Staged upload {stageId} was not found.");

        OperationHelpers.ThrowIf(record.Status == StagedUploadStatus.Committed, $"Stage {stageId} is already committed.");
        if (!await _store.TryTransitionStatusAsync(stageId, StagedUploadStatus.Uploaded, StagedUploadStatus.Committing, ct).ConfigureAwait(false))
            throw new ConflictException($"Stage {stageId} is not available for commit (status={record.Status}).");

        try {
            record = (await _store.GetAsync(stageId, ct).ConfigureAwait(false))!;
#if NETSTANDARD2_0
            using var input = await _physicalIo.OpenReadStreamAsync(record, ct).ConfigureAwait(false);
#else
            await using var input = await _physicalIo.OpenReadStreamAsync(record, ct).ConfigureAwait(false);
#endif
            var destPrefix = FileHelpers.NormalizePathPrefix(request.PathPrefix ?? record.PathPrefix);
            var fileResult = await _storage.SaveFromStreamAsync(
                    input, record.ObservedSizeBytes ?? 0, record.OriginalFileName, request.Compress, request.Encrypt, request.KeyId, destPrefix, request.ChunkSize,
                    record.ContentType,
                    record.TenantId, ct: ct)
                .ConfigureAwait(false);

            await _physicalIo.DeleteStageObjectAsync(record, ct).ConfigureAwait(false);
            var committed = record with { Status = StagedUploadStatus.Committed, CommittedFileId = fileResult.Id, FailureReason = null };
            await _store.UpdateAsync(committed, ct).ConfigureAwait(false);
            await PublishAuditAsync(FileAuditEventType.StagedUploadCommit, stageId, committed.TenantId, FileAuditOutcome.Success, correlationId: fileResult.Id, ct: ct)
                .ConfigureAwait(false);

            var eventArgs = new StagedUploadCommittedEventArgs {
                StageId = stageId,
                TenantId = committed.TenantId,
                CommittedFileId = fileResult.Id,
                FileResult = fileResult
            };

            Committed?.Invoke(this, eventArgs);
            await PublishEventHandlersAsync(h => h.OnCommittedAsync(eventArgs, ct)).ConfigureAwait(false);
            return fileResult;
        }
        catch (Exception ex) {
            var latest = await _store.GetAsync(stageId, ct).ConfigureAwait(false);
            if (latest?.Status == StagedUploadStatus.Committing) {
                var failed = latest with { Status = StagedUploadStatus.Failed, FailureReason = SanitizeAuditError(ex.Message) };
                await _store.UpdateAsync(failed, ct).ConfigureAwait(false);
            }

            await PublishAuditAsync(FileAuditEventType.StagedUploadFailed, stageId, record.TenantId, FileAuditOutcome.Failure, SanitizeAuditError(ex.Message), ct: ct)
                .ConfigureAwait(false);

            var failArgs = new StagedUploadFailedEventArgs {
                StageId = stageId,
                TenantId = record.TenantId,
                Snapshot = latest == null ? null : StagedFileUploadMappings.ToResult(latest),
                ErrorMessage = ex.Message
            };

            UploadFailed?.Invoke(this, failArgs);
            await PublishEventHandlersAsync(h => h.OnUploadFailedAsync(failArgs, ct)).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Best-effort delete of staging object and transition to <see cref="StagedUploadStatus.Aborted" />.</summary>
    public async Task AbortCoreAsync(Guid stageId, CancellationToken ct)
    {
        var record = await _store.GetAsync(stageId, ct).ConfigureAwait(false);
        if (record == null)
            throw new FileNotFoundException($"Staged upload {stageId} was not found.");

        OperationHelpers.ThrowIf(record.Status is StagedUploadStatus.Committing or StagedUploadStatus.Committed, $"Stage {stageId} cannot be aborted (status={record.Status}).");
        if (record.Status is StagedUploadStatus.Aborted or StagedUploadStatus.Expired)
            return;

        try {
            if (await _physicalIo.ObjectExistsAsync(record, ct).ConfigureAwait(false))
                await _physicalIo.DeleteStageObjectAsync(record, ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            _logger.LogDebug(ex, "Best-effort stage object delete failed for {StageId}", stageId);
        }

        var aborted = record with { Status = StagedUploadStatus.Aborted, FailureReason = null };
        await _store.UpdateAsync(aborted, ct).ConfigureAwait(false);
        await PublishAuditAsync(FileAuditEventType.StagedUploadAbort, stageId, aborted.TenantId, FileAuditOutcome.Success, ct: ct).ConfigureAwait(false);
    }

    private async Task HandleFailureAsync(Guid stageId, StagedFileUploadRecord record, Exception ex, CancellationToken ct)
    {
        var failed = record with { Status = StagedUploadStatus.Failed, FailureReason = SanitizeAuditError(ex.Message) };
        try {
            await _store.UpdateAsync(failed, ct).ConfigureAwait(false);
        }
        catch (Exception updateEx) {
            _logger.LogWarning(updateEx, "Failed to persist Failed status for stage {StageId}", stageId);
        }

        await PublishAuditAsync(FileAuditEventType.StagedUploadFailed, stageId, record.TenantId, FileAuditOutcome.Failure, SanitizeAuditError(ex.Message), ct: ct)
            .ConfigureAwait(false);

        var failArgs = new StagedUploadFailedEventArgs {
            StageId = stageId,
            TenantId = record.TenantId,
            Snapshot = StagedFileUploadMappings.ToResult(failed),
            ErrorMessage = ex.Message
        };

        UploadFailed?.Invoke(this, failArgs);
        await PublishEventHandlersAsync(h => h.OnUploadFailedAsync(failArgs, ct)).ConfigureAwait(false);
    }

    private void EnsureScanRequirementSatisfied()
    {
        if (_options.RequireScanBeforeAvailable && _malwareScanner is NullFileMalwareScanner) {
            throw new ConfigurationException(
                "RequireScanBeforeAvailable is set but no IFileMalwareScanner is configured. " +
                "Register a real malware scanner (e.g. via DI) or disable RequireScanBeforeAvailable.", "RequireScanBeforeAvailable");
        }
    }

    private static string? ResolveContentType(string? contentType, string? originalFileName)
    {
        if (!contentType.IsNullOrWhitespace())
            return contentType.Trim();

        var fileType = originalFileName.GetFileTypeFromExtension();
        return fileType == FileTypeInfo.Unknown ? null : fileType.MimeType;
    }

    private async Task PublishAuditAsync(
        FileAuditEventType eventType,
        Guid stageId,
        string? tenantId,
        FileAuditOutcome outcome,
        string? detail = null,
        Guid? correlationId = null,
        CancellationToken ct = default)
        => await FileAuditPublication.PublishAsync(
                _auditHandlers, null, null,
                new(eventType, DateTime.UtcNow, stageId, tenantId, _operationContextAccessor.Current?.ActorId, null, null, outcome, detail, correlationId),
                ct, _logger, _metrics, Constants.Metrics.AuditAppendFailed, _options.ThrowOnAuditFailure)
            .ConfigureAwait(false);

    private async Task PublishEventHandlersAsync(Func<IStagedFileUploadEventHandler, Task> invoke)
    {
        foreach (var handler in _eventHandlers) {
            try {
                await invoke(handler).ConfigureAwait(false);
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Staged file upload event handler {Handler} failed.", handler.GetType().Name);
            }
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
}