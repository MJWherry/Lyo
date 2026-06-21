using Amazon.S3;
using Amazon.S3.Model;
using Lyo.Exceptions;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.Staged;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FileStorage.S3.Staged;

/// <summary>Staged uploads using S3 presigned PUT URLs under <c>.stage/{stageId}/object</c>.</summary>
public sealed class S3StagedFileUploadService : IStagedFileUploadService
{
    private readonly StagedUploadCoordinator _coordinator;
    private readonly S3StagedFilePhysicalIo _physicalIo;
    private readonly IStagedFileUploadStore _store;

    public S3StagedFileUploadService(
        S3FileStorageService storage,
        S3FileStorageOptions options,
        IAmazonS3 s3,
        IStagedFileUploadStore store,
        IFileMalwareScanner? malwareScanner = null,
        IFileContentPolicy? contentPolicy = null,
        IEnumerable<IFileAuditEventHandler>? auditHandlers = null,
        IEnumerable<IStagedFileUploadEventHandler>? eventHandlers = null,
        IFileOperationContextAccessor? operationContextAccessor = null,
        ILoggerFactory? loggerFactory = null,
        IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(storage);
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(s3);
        ArgumentHelpers.ThrowIfNull(store);
        _store = store;
        _physicalIo = new(options, s3);
        var logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<S3StagedFileUploadService>();
        _coordinator = new(
            store,
            _physicalIo,
            storage,
            contentPolicy ?? new AllowAllFileContentPolicy(),
            malwareScanner ?? NullFileMalwareScanner.Instance,
            operationContextAccessor ?? NullFileOperationContextAccessor.Instance,
            options,
            logger,
            metrics ?? NullMetrics.Instance,
            auditHandlers,
            eventHandlers);
        _coordinator.PresignedCreated += (_, args) => PresignedCreated?.Invoke(this, args);
        _coordinator.UploadCompleted += (_, args) => UploadCompleted?.Invoke(this, args);
        _coordinator.UploadFailed += (_, args) => UploadFailed?.Invoke(this, args);
        _coordinator.Committed += (_, args) => Committed?.Invoke(this, args);
    }

    public event EventHandler<StagedUploadPresignedCreatedEventArgs>? PresignedCreated;

    public event EventHandler<StagedUploadCompletedEventArgs>? UploadCompleted;

    public event EventHandler<StagedUploadFailedEventArgs>? UploadFailed;

    public event EventHandler<StagedUploadCommittedEventArgs>? Committed;

    public async Task<StagedUploadBeginResult> BeginAsync(StagedUploadBeginRequest request, CancellationToken ct = default)
    {
        var (result, _) = await _coordinator.BeginCoreAsync(request, ct).ConfigureAwait(false);
        return result;
    }

    public Task<StagedFileResult> CompleteAsync(Guid stageId, StagedUploadCompleteRequest? request = null, CancellationToken ct = default)
        => _coordinator.CompleteCoreAsync(stageId, request, ct);

    public Task<FileStoreResult> CommitAsync(Guid stageId, StagedUploadCommitRequest request, CancellationToken ct = default)
        => _coordinator.CommitCoreAsync(stageId, request, ct);

    public Task AbortAsync(Guid stageId, CancellationToken ct = default)
        => _coordinator.AbortCoreAsync(stageId, ct);

    public async Task<StagedFileResult> GetAsync(Guid stageId, CancellationToken ct = default)
    {
        var record = await _store.GetAsync(stageId, ct).ConfigureAwait(false);
        if (record == null)
            throw new FileNotFoundException($"Staged upload {stageId} was not found.");

        return StagedFileUploadMappings.ToResult(record);
    }

    private sealed class S3StagedFilePhysicalIo : IStagedFilePhysicalIo
    {
        private readonly S3FileStorageOptions _options;
        private readonly IAmazonS3 _s3;

        internal S3StagedFilePhysicalIo(S3FileStorageOptions options, IAmazonS3 s3)
        {
            _options = options;
            _s3 = s3;
        }

        public MultipartUploadProviderKind ProviderKind => MultipartUploadProviderKind.AwsS3;

        public string BuildStageStorageLocation(Guid stageId, string? pathPrefix)
            => StagedObjectKeyBuilder.Build(stageId, pathPrefix, _options.KeyPrefix);

        public async Task<StagedPresignedPutResult> GeneratePresignedPutUrlAsync(
            Guid stageId,
            string normalizedPathPrefix,
            StagedUploadBeginRequest request,
            DateTimeOffset urlExpiresUtc,
            CancellationToken ct)
        {
            var storageKey = BuildStageStorageLocation(stageId, normalizedPathPrefix);
            var presign = new GetPreSignedUrlRequest {
                BucketName = _options.BucketName,
                Key = storageKey,
                Verb = HttpVerb.PUT,
                Expires = urlExpiresUtc.UtcDateTime
            };

            string? signedContentType = null;
            if (!string.IsNullOrWhiteSpace(request.ContentType)) {
                signedContentType = request.ContentType.Trim();
                presign.ContentType = signedContentType;
            }

            S3UploadServerSideEncryption.ApplyToPresignedPut(presign, _options);
            var url = await _s3.GetPreSignedURLAsync(presign).ConfigureAwait(false);
            return new(url, S3UploadServerSideEncryption.BuildRequiredPutHeaders(_options, signedContentType));
        }

        public async Task<bool> ObjectExistsAsync(StagedFileUploadRecord record, CancellationToken ct)
        {
            try {
                await _s3.GetObjectMetadataAsync(_options.BucketName, record.StorageLocation, ct).ConfigureAwait(false);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) {
                return false;
            }
        }

        public async Task<long> GetObjectSizeAsync(StagedFileUploadRecord record, CancellationToken ct)
        {
            var meta = await _s3.GetObjectMetadataAsync(_options.BucketName, record.StorageLocation, ct).ConfigureAwait(false);
            return meta.ContentLength;
        }

        public async Task<Stream> OpenReadStreamAsync(StagedFileUploadRecord record, CancellationToken ct)
        {
            var response = await _s3.GetObjectAsync(_options.BucketName, record.StorageLocation, ct).ConfigureAwait(false);
            return response.ResponseStream;
        }

        public Task DeleteStageObjectAsync(StagedFileUploadRecord record, CancellationToken ct)
            => _s3.DeleteObjectAsync(_options.BucketName, record.StorageLocation, ct);
    }
}
