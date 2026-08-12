using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Lyo.Exceptions;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.FileStorage.Staged;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FileStorage.AzureBlob.Staged;

/// <summary>Staged uploads using Azure Blob SAS PUT URLs under <c>.stage/{stageId}/object</c>.</summary>
public sealed class AzureBlobStagedFileUploadService : IStagedFileUploadService
{
    private readonly StagedUploadCoordinator _coordinator;
    private readonly AzureBlobStagedFilePhysicalIO _physicalIO;
    private readonly IStagedFileUploadStore _store;

    public AzureBlobStagedFileUploadService(
        AzureBlobFileStorageService storage,
        AzureBlobFileStorageOptions blobOptions,
        IStagedFileUploadStore store,
        BlobContainerClient? containerClient = null,
        IFileMalwareScanner? malwareScanner = null,
        IFileContentPolicy? contentPolicy = null,
        IEnumerable<IFileAuditEventHandler>? auditHandlers = null,
        IEnumerable<IStagedFileUploadEventHandler>? eventHandlers = null,
        IFileOperationContextAccessor? operationContextAccessor = null,
        ILoggerFactory? loggerFactory = null,
        IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(storage);
        ArgumentHelpers.ThrowIfNull(blobOptions);
        ArgumentHelpers.ThrowIfNull(store);
        _store = store;
        var container = containerClient ?? new BlobContainerClient(blobOptions.ConnectionString, blobOptions.ContainerName);
        _physicalIO = new(blobOptions, container);
        var logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<AzureBlobStagedFileUploadService>();
        _coordinator = new(store, _physicalIO, storage, blobOptions, contentPolicy, malwareScanner, operationContextAccessor, logger, metrics, auditHandlers, eventHandlers);
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
        OperationHelpers.ThrowIf(
            _physicalIO.UsesCustomerProvidedKey, "Staged upload presigned PUT is not compatible with SSE-C CustomerProvidedKey; remove it or disable staged uploads.");

        var (result, _) = await _coordinator.BeginCoreAsync(request, ct).ConfigureAwait(false);
        return result;
    }

    public Task<StagedFileResult> CompleteAsync(Guid stageId, StagedUploadCompleteRequest? request = null, CancellationToken ct = default)
        => _coordinator.CompleteCoreAsync(stageId, request, ct);

    public Task<FileStoreResult> CommitAsync(Guid stageId, StagedUploadCommitRequest request, CancellationToken ct = default) => _coordinator.CommitCoreAsync(stageId, request, ct);

    public Task AbortAsync(Guid stageId, CancellationToken ct = default) => _coordinator.AbortCoreAsync(stageId, ct);

    public async Task<StagedFileResult> GetAsync(Guid stageId, CancellationToken ct = default)
    {
        var record = await _store.GetAsync(stageId, ct).ConfigureAwait(false);
        if (record == null)
            throw new FileNotFoundException($"Staged upload {stageId} was not found.");

        return StagedFileUploadMappings.ToResult(record);
    }

    private sealed class AzureBlobStagedFilePhysicalIO : IStagedFilePhysicalIO
    {
        private readonly BlobContainerClient _container;
        private readonly AzureBlobFileStorageOptions _options;

        internal bool UsesCustomerProvidedKey => _options.UsesCustomerProvidedKey;

        internal AzureBlobStagedFilePhysicalIO(AzureBlobFileStorageOptions options, BlobContainerClient container)
        {
            _options = options;
            _container = container;
        }

        public MultipartUploadProviderKind ProviderKind => MultipartUploadProviderKind.AzureBlob;

        public string BuildStageStorageLocation(Guid stageId, string? pathPrefix) => StagedObjectKeyBuilder.Build(stageId, pathPrefix, _options.BlobPrefix);

        public Task<StagedPresignedPutResult> GeneratePresignedPutUrlAsync(
            Guid stageId,
            string normalizedPathPrefix,
            StagedUploadBeginRequest request,
            DateTimeOffset urlExpiresUtc,
            CancellationToken ct)
        {
            var blobName = BuildStageStorageLocation(stageId, normalizedPathPrefix);
            var blockBlob = _container.GetBlockBlobClient(blobName);
            var sas = new BlobSasBuilder {
                BlobContainerName = _container.Name,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = urlExpiresUtc
            };

            sas.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);
            if (!string.IsNullOrWhiteSpace(_options.EncryptionScope))
                sas.EncryptionScope = _options.EncryptionScope;

            var url = blockBlob.GenerateSasUri(sas);
            var requiredHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["x-ms-blob-type"] = "BlockBlob" };
            if (!string.IsNullOrWhiteSpace(request.ContentType)) {
                var trimmed = request.ContentType.Trim();
                requiredHeaders["x-ms-blob-content-type"] = trimmed;
                requiredHeaders["Content-Type"] = trimmed;
            }

            if (!string.IsNullOrWhiteSpace(_options.EncryptionScope))
                requiredHeaders["x-ms-encryption-scope"] = _options.EncryptionScope!;

            return Task.FromResult(new StagedPresignedPutResult(url.ToString(), requiredHeaders));
        }

        public async Task<bool> ObjectExistsAsync(StagedFileUploadRecord record, CancellationToken ct)
            => await _container.GetBlobClient(record.StorageLocation).ExistsAsync(ct).ConfigureAwait(false);

        public async Task<long> GetObjectSizeAsync(StagedFileUploadRecord record, CancellationToken ct)
        {
            var props = await _container.GetBlobClient(record.StorageLocation).GetPropertiesAsync(cancellationToken: ct).ConfigureAwait(false);
            return props.Value.ContentLength;
        }

        public async Task<Stream> OpenReadStreamAsync(StagedFileUploadRecord record, CancellationToken ct)
        {
            var response = await _container.GetBlobClient(record.StorageLocation).DownloadStreamingAsync(cancellationToken: ct).ConfigureAwait(false);
            return response.Value.Content;
        }

        public Task DeleteStageObjectAsync(StagedFileUploadRecord record, CancellationToken ct)
            => _container.GetBlobClient(record.StorageLocation).DeleteIfExistsAsync(cancellationToken: ct);
    }
}