using System.Net;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Lyo.Common.Records;
using Lyo.Compression;
using Lyo.Encryption;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FileStorage.Azure;

public class AzureFileStorageService : FileStorageServiceBase
{
    private readonly BlobContainerClient _containerClient;
    private readonly AzureFileStorageOptions _options;

    public AzureFileStorageService(
        AzureFileStorageOptions options,
        IFileMetadataStore metadataService,
        ILoggerFactory? loggerFactory = null,
        ICompressionService? compressionService = null,
        ITwoKeyEncryptionService? twoKeyEncryptionService = null,
        BlobContainerClient? containerClient = null,
        IMetrics? metrics = null,
        IFileOperationContextAccessor? operationContextAccessor = null,
        IEnumerable<IFileAuditEventHandler>? auditHandlers = null,
        IFileContentPolicy? contentPolicy = null,
        IFileMalwareScanner? malwareScanner = null)
        : base(
            ArgumentHelpers.ThrowIfNullReturn(options, nameof(options)), ArgumentHelpers.ThrowIfNullReturn(metadataService, nameof(metadataService)),
            (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<AzureFileStorageService>(), compressionService, twoKeyEncryptionService, metrics, operationContextAccessor,
            auditHandlers, contentPolicy, malwareScanner)
    {
        _options = options;
        _containerClient = containerClient ?? new BlobContainerClient(options.ConnectionString, options.ContainerName);
        Logger.LogInformation("Initialized Azure Blob File Storage Service for container: {ContainerName}", _options.ContainerName);
        MetricNames[nameof(FileStorage.Constants.Metrics.FileStoragePreSignedUrlGenerated)] = Constants.Metrics.FileStoragePreSignedUrlGenerated;
        MetricNames[nameof(FileStorage.Constants.Metrics.FileStoragePreSignedUrlGenerationFailed)] = Constants.Metrics.FileStoragePreSignedUrlGenerationFailed;
    }

    protected override async Task<Stream> CreateOutputStreamAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
    {
        var blobName = GetBlobName(fileId, extension, pathPrefix);
        var blockBlob = _containerClient.GetBlockBlobClient(blobName);
        return await blockBlob.OpenWriteAsync(true, cancellationToken: ct).ConfigureAwait(false);
    }

    protected override async Task<long> GetStorageSizeAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
    {
        var blobClient = GetBlobClient(fileId, extension, pathPrefix);
        try {
            var props = await blobClient.GetPropertiesAsync(cancellationToken: ct).ConfigureAwait(false);
            return props.Value.ContentLength;
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound) {
            return 0;
        }
    }

    protected override async Task<Stream?> ReadFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
    {
        var blobName = await FindBlobNameAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
        if (blobName == null)
            return null;

        var blobClient = _containerClient.GetBlobClient(blobName);
        var response = await blobClient.DownloadStreamingAsync(cancellationToken: ct).ConfigureAwait(false);
        return response.Value.Content;
    }

    protected override async Task<bool> DeleteFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
    {
        var blobName = await FindBlobNameAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
        if (blobName == null)
            return false;

        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(cancellationToken: ct).ConfigureAwait(false);
        Logger.LogDebug("Deleted file {FileId} from Azure Blob at {BlobName}", fileId, blobName);
        return true;
    }

    protected override async Task<EncryptionHeaderInfo> ExtractEncryptionHeaderAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
    {
        var blobClient = GetBlobClient(fileId, extension, pathPrefix);
        var response = await blobClient.DownloadStreamingAsync(cancellationToken: ct).ConfigureAwait(false);
        await using var stream = response.Value.Content;
        using var bufferStream = new MemoryStream();
        await stream.CopyToAsync(bufferStream, ct).ConfigureAwait(false);
        bufferStream.Position = 0;
        var header = EncryptionHeader.Read(bufferStream);
        return new(header.EncryptedDataEncryptionKey, header.KeyId, header.KeyVersion, header.DekKeyMaterialBytes);
    }

    protected override async Task UpdateFileHeaderAsync(Guid fileId, string? pathPrefix, string targetKeyId, string targetKeyVersion, byte[] newEncryptedDek, CancellationToken ct)
    {
        var blobName = await FindBlobNameAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
        if (blobName != null) {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var downloadResponse = await blobClient.DownloadContentAsync(ct).ConfigureAwait(false);
            var fileBytes = downloadResponse.Value.Content.ToArray();
            if (fileBytes.Length >= 13) {
                using var fileStream = new MemoryStream(fileBytes, false);
                var header = EncryptionHeader.Read(fileStream);
                var chunksStartPos = (int)fileStream.Position;
                var updatedHeader = header.With(targetKeyId, targetKeyVersion, newEncryptedDek);
                var newFileBytes = new List<byte>();
                updatedHeader.Write(newFileBytes);
                newFileBytes.AddRange(fileBytes.AsSpan(chunksStartPos).ToArray());
                await blobClient.UploadAsync(new MemoryStream(newFileBytes.ToArray()), true, ct).ConfigureAwait(false);
                Logger.LogDebug("Updated Azure Blob header for {FileId} with new keyId '{KeyId}', version {Version}", fileId, targetKeyId, targetKeyVersion);
            }
        }
    }

    protected override async Task CleanupPartialFileAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
    {
        var blobName = await FindBlobNameAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
        if (blobName != null) {
            try {
                var blobClient = _containerClient.GetBlobClient(blobName);
                await blobClient.DeleteIfExistsAsync(cancellationToken: ct).ConfigureAwait(false);
            }
            catch {
                // Ignore errors during cleanup
            }
        }
    }

    /// <inheritdoc />
    public override async Task<string> GetPreSignedReadUrlAsync(Guid fileId, TimeSpan? expiration = null, string? pathPrefix = null, CancellationToken ct = default)
    {
        var meta = await GetMetadataAsync(fileId, ct).ConfigureAwait(false);
        EnsureReadableAvailability(meta);
        var expirationTime = expiration ?? TimeSpan.FromHours(1);
        ArgumentHelpers.ThrowIfNotInRange(expirationTime, TimeSpan.Zero, TimeSpan.FromDays(7), nameof(expirationTime));
        var blobName = await FindBlobNameAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
        if (blobName == null) {
            Logger.LogWarning("File {FileId} not found in Azure Blob, cannot generate SAS URL", fileId);
            throw new FileNotFoundException($"File with ID {fileId} was not found in storage.");
        }

        try {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var sasBuilder = new BlobSasBuilder {
                BlobContainerName = _containerClient.Name,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.Add(expirationTime)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);
            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            Logger.LogDebug("Generated SAS URL for file {FileId} at {BlobName}, expires in {Expiration}", fileId, blobName, expirationTime);
            Metrics.IncrementCounter(MetricNames[nameof(FileStorage.Constants.Metrics.FileStoragePreSignedUrlGenerated)], tags: [("container", _options.ContainerName)]);
            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.PresignedRead, DateTime.UtcNow, fileId, meta.TenantId, OperationContextAccessor.Current?.ActorId, meta.DataEncryptionKeyId,
                        meta.DataEncryptionKeyVersion, FileAuditOutcome.Success), ct)
                .ConfigureAwait(false);

            return sasUri.ToString();
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Failed to generate SAS URL for file {FileId} at {BlobName}", fileId, blobName);
            Metrics.IncrementCounter(
                MetricNames[nameof(FileStorage.Constants.Metrics.FileStoragePreSignedUrlGenerationFailed)],
                tags: [("container", _options.ContainerName), ("error", ex.GetType().Name)]);

            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.PresignedRead, DateTime.UtcNow, fileId, meta.TenantId, OperationContextAccessor.Current?.ActorId, meta.DataEncryptionKeyId,
                        meta.DataEncryptionKeyVersion, FileAuditOutcome.Failure, ex.Message), ct)
                .ConfigureAwait(false);

            throw;
        }
    }

    private BlobClient GetBlobClient(Guid fileId, string extension, string? pathPrefix)
    {
        var blobName = GetBlobName(fileId, extension, pathPrefix);
        return _containerClient.GetBlobClient(blobName);
    }

    private string GetBlobName(Guid fileId, string extension = "", string? pathPrefix = null)
    {
        var fileName = fileId.ToString("N") + extension;
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(_options.BlobPrefix))
            parts.Add(_options.BlobPrefix.Trim().TrimStart('/', '\\').TrimEnd('/', '\\'));

        if (!string.IsNullOrWhiteSpace(pathPrefix))
            parts.Add(pathPrefix);
        else {
            var idString = fileId.ToString("N");
            parts.Add(idString.Substring(0, 2));
            parts.Add(idString.Substring(2, 2));
        }

        parts.Add(fileName);
        return string.Join("/", parts);
    }

    private async Task<string?> FindBlobNameAsync(Guid fileId, string? pathPrefix = null, CancellationToken ct = default)
    {
        var baseName = GetBlobName(fileId, "", pathPrefix);
        if (await BlobExistsAsync(baseName, ct).ConfigureAwait(false))
            return baseName;

        if (CompressionService != null) {
            var name = GetBlobName(fileId, CompressionService.FileExtension, pathPrefix);
            if (await BlobExistsAsync(name, ct).ConfigureAwait(false))
                return name;
        }

        if (TwoKeyEncryptionService != null) {
            var name = GetBlobName(fileId, TwoKeyEncryptionService.FileExtension, pathPrefix);
            if (await BlobExistsAsync(name, ct).ConfigureAwait(false))
                return name;

            if (CompressionService != null) {
                name = GetBlobName(fileId, CompressionService.FileExtension + TwoKeyEncryptionService.FileExtension, pathPrefix);
                if (await BlobExistsAsync(name, ct).ConfigureAwait(false))
                    return name;
            }
        }

        var commonExtensions = FileTypeInfo.CommonStorageResolutionSuffixes;
        foreach (var ext in commonExtensions) {
            ct.ThrowIfCancellationRequested();
            var name = GetBlobName(fileId, ext, pathPrefix);
            if (await BlobExistsAsync(name, ct).ConfigureAwait(false))
                return name;
        }

        return null;
    }

    private async Task<bool> BlobExistsAsync(string blobName, CancellationToken ct = default)
    {
        try {
            var blobClient = _containerClient.GetBlobClient(blobName);
            return await blobClient.ExistsAsync(ct).ConfigureAwait(false);
        }
        catch {
            return false;
        }
    }
}