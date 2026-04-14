using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Lyo.Common.Records;
using Lyo.Exceptions;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using CompleteMultipartUploadRequest = Lyo.FileStorage.Multipart.CompleteMultipartUploadRequest;

namespace Lyo.FileStorage.S3.Multipart;

/// <summary>
/// Multipart uploads using S3 multipart API with staging under <c>.multipart/{sessionId}/staging</c>, then streaming through
/// <see cref="S3FileStorageService.SaveFromStreamAsync" />.
/// </summary>
public sealed class S3MultipartUploadService : IMultipartUploadService
{
    private readonly IReadOnlyList<IFileAuditEventHandler> _auditHandlers;
    private readonly ILogger<S3MultipartUploadService> _logger;
    private readonly IMetrics _metrics;
    private readonly IFileMalwareScanner _malwareScanner;
    private readonly IFileOperationContextAccessor _operationContextAccessor;
    private readonly S3FileStorageOptions _options;
    private readonly IAmazonS3 _s3;
    private readonly IMultipartUploadSessionStore _sessions;
    private readonly S3FileStorageService _storage;

    public S3MultipartUploadService(
        S3FileStorageService storage,
        S3FileStorageOptions options,
        IAmazonS3 s3,
        IMultipartUploadSessionStore sessions,
        IFileMalwareScanner? malwareScanner = null,
        IEnumerable<IFileAuditEventHandler>? auditHandlers = null,
        IFileOperationContextAccessor? operationContextAccessor = null,
        ILoggerFactory? loggerFactory = null,
        IMetrics? metrics = null)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(s3);
        ArgumentNullException.ThrowIfNull(sessions);
        _storage = storage;
        _options = options;
        _s3 = s3;
        _sessions = sessions;
        _malwareScanner = malwareScanner ?? NullFileMalwareScanner.Instance;
        _auditHandlers = auditHandlers == null ? [] : auditHandlers.ToList();
        _operationContextAccessor = operationContextAccessor ?? NullFileOperationContextAccessor.Instance;
        _metrics = metrics ?? NullMetrics.Instance;
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<S3MultipartUploadService>();
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
        var stagingKey = BuildStagingKey(request.PathPrefix, sessionId);
        string uploadId;
        try {
            var init = await _s3.InitiateMultipartUploadAsync(new() { BucketName = _options.BucketName, Key = stagingKey, ContentType = FileTypeInfo.Unknown.MimeType }, ct)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(init.UploadId))
                throw new InvalidOperationException("S3 InitiateMultipartUpload returned no UploadId.");

            uploadId = init.UploadId;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "InitiateMultipartUpload failed for staging key {Key}", stagingKey);
            throw;
        }

        var state = JsonSerializer.Serialize(new S3ProviderState { StagingKey = stagingKey, UploadId = uploadId });
        var tenant = request.TenantId ?? _operationContextAccessor.Current?.TenantId;
        var record = new MultipartUploadSessionRecord(
            sessionId, tenant, now, now.Add(ttl), targetFileId, request.PathPrefix, request.Compress, request.Encrypt, request.KeyId, request.OriginalFileName, request.ContentType,
            MultipartSessionStatus.Active, MultipartUploadProviderKind.AwsS3, state, request.DeclaredContentLength, request.PartSizeBytes);

        try {
            await _sessions.CreateAsync(record, ct).ConfigureAwait(false);
        }
        catch (Exception) {
            await TryAbortS3Async(stagingKey, uploadId, ct).ConfigureAwait(false);
            throw;
        }

        await FileAuditPublication.PublishAsync(
                _auditHandlers, null, null,
                new(
                    FileAuditEventType.MultipartBegin, DateTime.UtcNow, targetFileId, tenant, _operationContextAccessor.Current?.ActorId, request.KeyId, null,
                    FileAuditOutcome.Success), ct, _logger, _metrics, FileStorage.Constants.Metrics.AuditAppendFailed, _options.ThrowOnAuditFailure)
            .ConfigureAwait(false);

        return new(sessionId, targetFileId, request.PartSizeBytes, record.ExpiresUtc, MultipartUploadProviderKind.AwsS3);
    }

    /// <inheritdoc />
    public Task<MultipartPartDescriptor> GetPresignedPartUploadAsync(Guid sessionId, int partNumber, CancellationToken ct = default)
    {
        if (partNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(partNumber), "Part number must be at least 1.");

        return GetPresignedPartUploadCoreAsync(sessionId, partNumber, ct);
    }

    /// <inheritdoc />
    public Task UploadPartAsync(Guid sessionId, int partNumber, Stream content, CancellationToken ct = default)
        => throw new NotSupportedException("S3 multipart uploads use presigned PUT URLs per part; use GetPresignedPartUploadAsync.");

    /// <inheritdoc />
    public async Task<FileStoreResult> CompleteAsync(CompleteMultipartUploadRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var session = await GetActiveSessionAsync(request.SessionId, MultipartUploadProviderKind.AwsS3, ct).ConfigureAwait(false);
        var state = JsonSerializer.Deserialize<S3ProviderState>(session.ProviderStateJson);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(state?.StagingKey, nameof(session.ProviderStateJson));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(state.UploadId, nameof(session.ProviderStateJson));
        var orderedParts = request.Parts.OrderBy(p => p.PartNumber).ToList();
        OperationHelpers.ThrowIf(orderedParts.Count == 0, "At least one part is required.");
        var partEtags = orderedParts.Select(p => new PartETag(p.PartNumber, NormalizeS3Etag(p.ETagOrBlockId))).ToList();
        try {
            await _s3.CompleteMultipartUploadAsync(
                    new() {
                        BucketName = _options.BucketName,
                        Key = state.StagingKey,
                        UploadId = state.UploadId,
                        PartETags = partEtags
                    }, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "CompleteMultipartUpload failed for session {SessionId}", request.SessionId);
            throw;
        }

        FileStoreResult result;
        var tempPath = Path.Combine(Path.GetTempPath(), $"lyo-mpu-{request.SessionId:N}.bin");
        try {
            if (_options.RequireScanBeforeAvailable && _malwareScanner is not NullFileMalwareScanner) {
                using (var getResponse = await _s3.GetObjectAsync(new() { BucketName = _options.BucketName, Key = state.StagingKey }, ct).ConfigureAwait(false)) {
                    await using var fs = File.Create(tempPath);
                    await getResponse.ResponseStream.CopyToAsync(fs, ct).ConfigureAwait(false);
                }

                var len = new FileInfo(tempPath).Length;
                FileAvailability? availabilityOverride = null;
                await using (var scanStream = File.OpenRead(tempPath)) {
                    var scan = await _malwareScanner.ScanAsync(scanStream, session.ContentType, session.OriginalFileName, ct).ConfigureAwait(false);
                    availabilityOverride = scan.ThreatLevel switch {
                        FileScanThreatLevel.Clean => FileAvailability.Available,
                        FileScanThreatLevel.Suspect => FileAvailability.Quarantined,
                        FileScanThreatLevel.Threat => throw new FilePolicyRejectedException(scan.Detail ?? "Malware scan rejected the multipart payload."),
                        var _ => FileAvailability.Available
                    };
                }

                await using var input = File.OpenRead(tempPath);
                result = await _storage.SaveFromStreamAsync(
                        input, len, session.OriginalFileName ?? session.TargetFileId.ToString(), session.Compress, session.Encrypt, session.KeyId, session.PathPrefix, null,
                        session.ContentType, session.TenantId, availabilityOverride, session.TargetFileId, ct)
                    .ConfigureAwait(false);
            }
            else {
                using var getResponse = await _s3.GetObjectAsync(new() { BucketName = _options.BucketName, Key = state.StagingKey }, ct).ConfigureAwait(false);
                var len = getResponse.ContentLength;
                result = await _storage.SaveFromStreamAsync(
                        getResponse.ResponseStream, len, session.OriginalFileName ?? session.TargetFileId.ToString(), session.Compress, session.Encrypt, session.KeyId,
                        session.PathPrefix, null, session.ContentType, session.TenantId, null, session.TargetFileId, ct)
                    .ConfigureAwait(false);
            }
        }
        finally {
            TryDeleteFile(tempPath);
        }

        try {
            await _s3.DeleteObjectAsync(new() { BucketName = _options.BucketName, Key = state.StagingKey }, ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to delete staging object {Key} after multipart complete", state.StagingKey);
        }

        await _sessions.SetStatusAsync(session.SessionId, MultipartSessionStatus.Completed, ct).ConfigureAwait(false);
        await _sessions.DeleteAsync(session.SessionId, ct).ConfigureAwait(false);
        await FileAuditPublication.PublishAsync(
                _auditHandlers, null, null,
                new(
                    FileAuditEventType.MultipartComplete, DateTime.UtcNow, result.Id, session.TenantId, _operationContextAccessor.Current?.ActorId, result.DataEncryptionKeyId,
                    result.DataEncryptionKeyVersion, FileAuditOutcome.Success), ct, _logger, _metrics, FileStorage.Constants.Metrics.AuditAppendFailed,
                _options.ThrowOnAuditFailure)
            .ConfigureAwait(false);

        return result;
    }

    /// <inheritdoc />
    public async Task AbortAsync(Guid sessionId, CancellationToken ct = default)
    {
        var s = await _sessions.GetAsync(sessionId, ct).ConfigureAwait(false);
        if (s == null)
            return;

        if (s.ProviderKind == MultipartUploadProviderKind.AwsS3) {
            var state = JsonSerializer.Deserialize<S3ProviderState>(s.ProviderStateJson);
            if (!string.IsNullOrWhiteSpace(state?.StagingKey) && !string.IsNullOrWhiteSpace(state.UploadId))
                await TryAbortS3Async(state.StagingKey, state.UploadId, ct).ConfigureAwait(false);
        }

        await _sessions.DeleteAsync(sessionId, ct).ConfigureAwait(false);
        await FileAuditPublication.PublishAsync(
                _auditHandlers, null, null,
                new(
                    FileAuditEventType.MultipartAbort, DateTime.UtcNow, s.TargetFileId, s.TenantId, _operationContextAccessor.Current?.ActorId, s.KeyId, null,
                    FileAuditOutcome.Success), ct, _logger, _metrics, FileStorage.Constants.Metrics.AuditAppendFailed, _options.ThrowOnAuditFailure)
            .ConfigureAwait(false);
    }

    private async Task<MultipartPartDescriptor> GetPresignedPartUploadCoreAsync(Guid sessionId, int partNumber, CancellationToken ct)
    {
        var session = await GetActiveSessionAsync(sessionId, MultipartUploadProviderKind.AwsS3, ct).ConfigureAwait(false);
        var state = JsonSerializer.Deserialize<S3ProviderState>(session.ProviderStateJson);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(state?.StagingKey, nameof(session.ProviderStateJson));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(state.UploadId, nameof(session.ProviderStateJson));
        var request = new GetPreSignedUrlRequest {
            BucketName = _options.BucketName,
            Key = state.StagingKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddHours(1),
            PartNumber = partNumber,
            UploadId = state.UploadId
        };

        var url = await _s3.GetPreSignedURLAsync(request).ConfigureAwait(false);
        return new(partNumber, url);
    }

    private async Task TryAbortS3Async(string stagingKey, string uploadId, CancellationToken ct)
    {
        try {
            await _s3.AbortMultipartUploadAsync(new() { BucketName = _options.BucketName, Key = stagingKey, UploadId = uploadId }, ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            _logger.LogDebug(ex, "AbortMultipartUpload for {Key} (best effort)", stagingKey);
        }
    }

    private static void TryDeleteFile(string path)
    {
        try {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch {
            // best effort
        }
    }

    private static string NormalizeS3Etag(string etag)
    {
        if (string.IsNullOrEmpty(etag))
            return etag;

        var t = etag.Trim();
        return t.StartsWith('"') && t.EndsWith('"') && t.Length >= 2 ? t.Substring(1, t.Length - 2) : t;
    }

    private string BuildStagingKey(string? pathPrefix, Guid sessionId)
    {
        var keyParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(_options.KeyPrefix))
            keyParts.Add(_options.KeyPrefix.Trim().TrimStart('/', '\\').TrimEnd('/', '\\'));

        if (!string.IsNullOrWhiteSpace(pathPrefix))
            keyParts.Add(pathPrefix!.Trim().Trim('/'));

        keyParts.Add(".multipart");
        keyParts.Add(sessionId.ToString("N"));
        keyParts.Add("staging");
        return string.Join("/", keyParts);
    }

    private async Task<MultipartUploadSessionRecord> GetActiveSessionAsync(Guid sessionId, MultipartUploadProviderKind expectedKind, CancellationToken ct)
    {
        var session = await _sessions.GetAsync(sessionId, ct).ConfigureAwait(false);
        OperationHelpers.ThrowIfNull(session, $"Multipart session {sessionId} was not found.");
        if (session.ProviderKind != expectedKind)
            throw new InvalidOperationException($"Session {sessionId} is not an {expectedKind} session.");

        if (session.Status != MultipartSessionStatus.Active)
            throw new InvalidOperationException($"Session {sessionId} is not active.");

        if (DateTime.UtcNow > session.ExpiresUtc)
            throw new InvalidOperationException($"Session {sessionId} has expired.");

        return session;
    }

    private sealed class S3ProviderState
    {
        public string StagingKey { get; set; } = "";

        public string UploadId { get; set; } = "";
    }
}