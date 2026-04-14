using System.Globalization;
using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Lyo.Exceptions;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FileStorage.Azure.Multipart;

/// <summary>
/// Multipart uploads using Azure block staging under <c>.multipart/{sessionId}/staging</c>, then streaming through <see cref="AzureFileStorageService.SaveFromStreamAsync" />
/// .
/// </summary>
public sealed class AzureMultipartUploadService : IMultipartUploadService
{
    private readonly IReadOnlyList<IFileAuditEventHandler> _auditHandlers;
    private readonly ILogger<AzureMultipartUploadService> _logger;
    private readonly IMetrics _metrics;
    private readonly IFileMalwareScanner _malwareScanner;
    private readonly IFileOperationContextAccessor _operationContextAccessor;
    private readonly AzureFileStorageOptions _options;
    private readonly IMultipartUploadSessionStore _sessions;
    private readonly AzureFileStorageService _storage;

    public AzureMultipartUploadService(
        AzureFileStorageService storage,
        AzureFileStorageOptions options,
        IMultipartUploadSessionStore sessions,
        IFileMalwareScanner? malwareScanner = null,
        IEnumerable<IFileAuditEventHandler>? auditHandlers = null,
        IFileOperationContextAccessor? operationContextAccessor = null,
        ILoggerFactory? loggerFactory = null,
        IMetrics? metrics = null)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(sessions);
        _storage = storage;
        _options = options;
        _sessions = sessions;
        _malwareScanner = malwareScanner ?? NullFileMalwareScanner.Instance;
        _auditHandlers = auditHandlers == null ? [] : auditHandlers.ToList();
        _operationContextAccessor = operationContextAccessor ?? NullFileOperationContextAccessor.Instance;
        _metrics = metrics ?? NullMetrics.Instance;
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<AzureMultipartUploadService>();
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
        var blobName = BuildStagingBlobName(request.PathPrefix, sessionId);
        var state = JsonSerializer.Serialize(new AzureProviderState { StagingBlobName = blobName });
        var tenant = request.TenantId ?? _operationContextAccessor.Current?.TenantId;
        var record = new MultipartUploadSessionRecord(
            sessionId, tenant, now, now.Add(ttl), targetFileId, request.PathPrefix, request.Compress, request.Encrypt, request.KeyId, request.OriginalFileName, request.ContentType,
            MultipartSessionStatus.Active, MultipartUploadProviderKind.AzureBlob, state, request.DeclaredContentLength, request.PartSizeBytes);

        await _sessions.CreateAsync(record, ct).ConfigureAwait(false);
        await FileAuditPublication.PublishAsync(
                _auditHandlers, null, null,
                new(
                    FileAuditEventType.MultipartBegin, DateTime.UtcNow, targetFileId, tenant, _operationContextAccessor.Current?.ActorId, request.KeyId, null,
                    FileAuditOutcome.Success), ct, _logger, _metrics, FileStorage.Constants.Metrics.AuditAppendFailed, _options.ThrowOnAuditFailure)
            .ConfigureAwait(false);

        return new(sessionId, targetFileId, request.PartSizeBytes, record.ExpiresUtc, MultipartUploadProviderKind.AzureBlob);
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
        => throw new NotSupportedException("Azure multipart uploads use presigned PUT URLs per block; use GetPresignedPartUploadAsync.");

    /// <inheritdoc />
    public async Task<FileStoreResult> CompleteAsync(CompleteMultipartUploadRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var session = await GetActiveSessionAsync(request.SessionId, MultipartUploadProviderKind.AzureBlob, ct).ConfigureAwait(false);
        var state = JsonSerializer.Deserialize<AzureProviderState>(session.ProviderStateJson);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(state?.StagingBlobName, nameof(session.ProviderStateJson));
        var ordered = request.Parts.OrderBy(p => p.PartNumber).ToList();
        OperationHelpers.ThrowIf(ordered.Count == 0, "At least one part is required.");
        var blockIds = ordered.Select(p => ToBlockId(p.PartNumber)).ToList();
        var blockBlob = GetBlockBlobClient(state.StagingBlobName);
        try {
            await blockBlob.CommitBlockListAsync(blockIds, cancellationToken: ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "CommitBlockList failed for session {SessionId}", request.SessionId);
            throw;
        }

        FileStoreResult result;
        var tempPath = Path.Combine(Path.GetTempPath(), $"lyo-azure-mpu-{request.SessionId:N}.bin");
        try {
            if (_options.RequireScanBeforeAvailable && _malwareScanner is not NullFileMalwareScanner) {
                using (var read = await blockBlob.OpenReadAsync(cancellationToken: ct).ConfigureAwait(false)) {
                    await using var fs = File.Create(tempPath);
                    await read.CopyToAsync(fs, ct).ConfigureAwait(false);
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
                using var readStream = await blockBlob.OpenReadAsync(cancellationToken: ct).ConfigureAwait(false);
                var len = readStream.Length;
                result = await _storage.SaveFromStreamAsync(
                        readStream, len, session.OriginalFileName ?? session.TargetFileId.ToString(), session.Compress, session.Encrypt, session.KeyId, session.PathPrefix, null,
                        session.ContentType, session.TenantId, null, session.TargetFileId, ct)
                    .ConfigureAwait(false);
            }
        }
        finally {
            TryDeleteFile(tempPath);
        }

        try {
            await blockBlob.DeleteIfExistsAsync(cancellationToken: ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to delete staging blob {Blob} after multipart complete", state.StagingBlobName);
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

        if (s.ProviderKind == MultipartUploadProviderKind.AzureBlob) {
            var state = JsonSerializer.Deserialize<AzureProviderState>(s.ProviderStateJson);
            if (!string.IsNullOrWhiteSpace(state?.StagingBlobName)) {
                try {
                    await GetBlockBlobClient(state.StagingBlobName).DeleteIfExistsAsync(cancellationToken: ct).ConfigureAwait(false);
                }
                catch (Exception ex) {
                    _logger.LogDebug(ex, "Delete staging blob {Blob} (best effort)", state.StagingBlobName);
                }
            }
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
        var session = await GetActiveSessionAsync(sessionId, MultipartUploadProviderKind.AzureBlob, ct).ConfigureAwait(false);
        var state = JsonSerializer.Deserialize<AzureProviderState>(session.ProviderStateJson);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(state?.StagingBlobName, nameof(session.ProviderStateJson));
        var blockBlob = GetBlockBlobClient(state.StagingBlobName);
        var blockId = ToBlockId(partNumber);
        var sasBuilder = new BlobSasBuilder {
            BlobContainerName = _options.ContainerName,
            BlobName = state.StagingBlobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);
        var sasUri = blockBlob.GenerateSasUri(sasBuilder);
        var baseUrl = sasUri.ToString();
        var joiner = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        var url = $"{baseUrl}{joiner}comp=block&blockid={Uri.EscapeDataString(blockId)}";
        return new(partNumber, url);
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

    private BlockBlobClient GetBlockBlobClient(string blobName) => new(_options.ConnectionString, _options.ContainerName, blobName);

    private string BuildStagingBlobName(string? pathPrefix, Guid sessionId)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(_options.BlobPrefix))
            parts.Add(_options.BlobPrefix.Trim().TrimStart('/', '\\').TrimEnd('/', '\\'));

        if (!string.IsNullOrWhiteSpace(pathPrefix))
            parts.Add(pathPrefix!.Trim().Trim('/'));

        parts.Add(".multipart");
        parts.Add(sessionId.ToString("N"));
        parts.Add("staging");
        return string.Join("/", parts);
    }

    private static string ToBlockId(int partNumber) => Convert.ToBase64String(Encoding.UTF8.GetBytes(partNumber.ToString("d6", CultureInfo.InvariantCulture)));

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

    private sealed class AzureProviderState
    {
        public string StagingBlobName { get; set; } = "";
    }
}