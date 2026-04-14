using System.Text.Json;
using Lyo.Exceptions;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using LocalFileStorageServiceOptions = Lyo.FileStorage.Models.LocalFileStorageServiceOptions;

namespace Lyo.FileStorage.Multipart;

/// <summary>Multipart uploads with on-disk staging under the local file storage root (server-side <see cref="UploadPartAsync" />).</summary>
public sealed class LocalMultipartUploadService : IMultipartUploadService
{
    private readonly IReadOnlyList<IFileAuditEventHandler> _auditHandlers;
    private readonly ILogger<LocalMultipartUploadService> _logger;
    private readonly IMetrics _metrics;
    private readonly IFileMalwareScanner _malwareScanner;
    private readonly IFileOperationContextAccessor _operationContextAccessor;
    private readonly LocalFileStorageServiceOptions _options;
    private readonly IMultipartUploadSessionStore _sessions;
    private readonly LocalFileStorageService _storage;

    public LocalMultipartUploadService(
        LocalFileStorageService storage,
        IMultipartUploadSessionStore sessions,
        LocalFileStorageServiceOptions options,
        IFileMalwareScanner? malwareScanner = null,
        IEnumerable<IFileAuditEventHandler>? auditHandlers = null,
        IFileOperationContextAccessor? operationContextAccessor = null,
        ILoggerFactory? loggerFactory = null,
        IMetrics? metrics = null)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(options);
        _storage = storage;
        _sessions = sessions;
        _options = options;
        _malwareScanner = malwareScanner ?? NullFileMalwareScanner.Instance;
        _auditHandlers = auditHandlers == null ? [] : auditHandlers.ToList();
        _operationContextAccessor = operationContextAccessor ?? NullFileOperationContextAccessor.Instance;
        _metrics = metrics ?? NullMetrics.Instance;
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<LocalMultipartUploadService>();
    }

    /// <inheritdoc />
    public async Task<MultipartBeginResult> BeginAsync(MultipartBeginRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        OperationHelpers.ThrowIf(request.PartSizeBytes < 1024, "PartSizeBytes must be at least 1024.");
        var sessionId = Guid.NewGuid();
        var targetFileId = Guid.NewGuid();
        var ttl = request.SessionTtl ?? TimeSpan.FromHours(24);
        var now = DateTime.UtcNow;
        var stagingDir = Path.Combine(_options.RootDirectoryPath, ".multipart", sessionId.ToString("N"));
        Directory.CreateDirectory(stagingDir);
        var state = JsonSerializer.Serialize(new LocalProviderState { StagingDirectory = stagingDir });
        var tenant = request.TenantId ?? _operationContextAccessor.Current?.TenantId;
        var record = new MultipartUploadSessionRecord(
            sessionId, tenant, now, now.Add(ttl), targetFileId, request.PathPrefix, request.Compress, request.Encrypt, request.KeyId, request.OriginalFileName, request.ContentType,
            MultipartSessionStatus.Active, MultipartUploadProviderKind.Local, state, request.DeclaredContentLength, request.PartSizeBytes);

        await _sessions.CreateAsync(record, ct).ConfigureAwait(false);
        await FileAuditPublication.PublishAsync(
                _auditHandlers, null, null,
                new(
                    FileAuditEventType.MultipartBegin, DateTime.UtcNow, targetFileId, tenant, _operationContextAccessor.Current?.ActorId, request.KeyId, null,
                    FileAuditOutcome.Success), ct, _logger, _metrics, Constants.Metrics.AuditAppendFailed, _options.ThrowOnAuditFailure)
            .ConfigureAwait(false);

        return new(sessionId, targetFileId, request.PartSizeBytes, record.ExpiresUtc, MultipartUploadProviderKind.Local);
    }

    /// <inheritdoc />
    public Task<MultipartPartDescriptor> GetPresignedPartUploadAsync(Guid sessionId, int partNumber, CancellationToken ct = default)
        => throw new NotSupportedException("Local multipart uses server-side UploadPartAsync; presigned part URLs are not available.");

    /// <inheritdoc />
    public async Task UploadPartAsync(Guid sessionId, int partNumber, Stream content, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        var session = await GetActiveSessionAsync(sessionId, ct).ConfigureAwait(false);
        var dir = GetStagingDir(session);
        var partPath = Path.Combine(dir, $"part-{partNumber:D5}.bin");
        await using var fs = File.Create(partPath);
        await content.CopyToAsync(fs, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FileStoreResult> CompleteAsync(CompleteMultipartUploadRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var session = await GetActiveSessionAsync(request.SessionId, ct).ConfigureAwait(false);
        var dir = GetStagingDir(session);
        var parts = request.Parts.OrderBy(p => p.PartNumber).ToList();
        OperationHelpers.ThrowIf(parts.Count == 0, "At least one part is required.");
        var mergedPath = Path.Combine(dir, "merged.bin");
        await using (var merged = File.Create(mergedPath)) {
            foreach (var p in parts) {
                var partPath = Path.Combine(dir, $"part-{p.PartNumber:D5}.bin");
                if (!File.Exists(partPath))
                    throw new FileNotFoundException($"Part {p.PartNumber} not found for session {request.SessionId}.");

                await using var partStream = File.OpenRead(partPath);
                await partStream.CopyToAsync(merged, ct).ConfigureAwait(false);
            }
        }

        var mergedInfo = new FileInfo(mergedPath);
        FileAvailability? availabilityOverride = null;
        if (_options.RequireScanBeforeAvailable && _malwareScanner is not NullFileMalwareScanner) {
            await using var scanStream = File.OpenRead(mergedPath);
            var scan = await _malwareScanner.ScanAsync(scanStream, session.ContentType, session.OriginalFileName, ct).ConfigureAwait(false);
            availabilityOverride = scan.ThreatLevel switch {
                FileScanThreatLevel.Clean => FileAvailability.Available,
                FileScanThreatLevel.Suspect => FileAvailability.Quarantined,
                FileScanThreatLevel.Threat => throw new FilePolicyRejectedException(scan.Detail ?? "Malware scan rejected the multipart payload."),
                var _ => FileAvailability.Available
            };
        }

        await using var input = File.OpenRead(mergedPath);
        var result = await _storage.SaveFromStreamAsync(
                input, mergedInfo.Length, session.OriginalFileName ?? session.TargetFileId.ToString(), session.Compress, session.Encrypt, session.KeyId, session.PathPrefix, null,
                session.ContentType, session.TenantId, availabilityOverride, session.TargetFileId, ct)
            .ConfigureAwait(false);

        await _sessions.SetStatusAsync(session.SessionId, MultipartSessionStatus.Completed, ct).ConfigureAwait(false);
        TryDeleteDir(dir);
        await _sessions.DeleteAsync(session.SessionId, ct).ConfigureAwait(false);
        await FileAuditPublication.PublishAsync(
                _auditHandlers, null, null,
                new(
                    FileAuditEventType.MultipartComplete, DateTime.UtcNow, result.Id, session.TenantId, _operationContextAccessor.Current?.ActorId, result.DataEncryptionKeyId,
                    result.DataEncryptionKeyVersion, FileAuditOutcome.Success), ct, _logger, _metrics, Constants.Metrics.AuditAppendFailed, _options.ThrowOnAuditFailure)
            .ConfigureAwait(false);

        return result;
    }

    /// <inheritdoc />
    public async Task AbortAsync(Guid sessionId, CancellationToken ct = default)
    {
        var s = await _sessions.GetAsync(sessionId, ct).ConfigureAwait(false);
        if (s == null)
            return;

        try {
            var dir = GetStagingDir(s);
            TryDeleteDir(dir);
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to delete multipart staging for session {SessionId}", sessionId);
        }

        await _sessions.DeleteAsync(sessionId, ct).ConfigureAwait(false);
        await FileAuditPublication.PublishAsync(
                _auditHandlers, null, null,
                new(
                    FileAuditEventType.MultipartAbort, DateTime.UtcNow, s.TargetFileId, s.TenantId, _operationContextAccessor.Current?.ActorId, s.KeyId, null,
                    FileAuditOutcome.Success), ct, _logger, _metrics, Constants.Metrics.AuditAppendFailed, _options.ThrowOnAuditFailure)
            .ConfigureAwait(false);
    }

    private async Task<MultipartUploadSessionRecord> GetActiveSessionAsync(Guid sessionId, CancellationToken ct)
    {
        var session = await _sessions.GetAsync(sessionId, ct).ConfigureAwait(false);
        OperationHelpers.ThrowIfNull(session, $"Multipart session {sessionId} was not found.");
        if (session.Status != MultipartSessionStatus.Active)
            throw new InvalidOperationException($"Session {sessionId} is not active.");

        if (DateTime.UtcNow > session.ExpiresUtc)
            throw new InvalidOperationException($"Session {sessionId} has expired.");

        return session;
    }

    private static string GetStagingDir(MultipartUploadSessionRecord session)
    {
        var state = JsonSerializer.Deserialize<LocalProviderState>(session.ProviderStateJson);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(state?.StagingDirectory, nameof(session.ProviderStateJson));
        return state!.StagingDirectory;
    }

    private static void TryDeleteDir(string dir)
    {
        try {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
        catch {
            // best effort
        }
    }

    private sealed class LocalProviderState
    {
        public string StagingDirectory { get; set; } = "";
    }
}