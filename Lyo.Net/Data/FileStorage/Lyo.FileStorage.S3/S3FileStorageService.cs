using System.Net;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
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

namespace Lyo.FileStorage.S3;

public class S3FileStorageService : FileStorageServiceBase
{
    private readonly S3FileStorageOptions _options;
    private readonly bool _ownsS3Client;
    private readonly IAmazonS3 _s3Client;

    public S3FileStorageService(
        S3FileStorageOptions options,
        IFileMetadataStore metadataService,
        ILoggerFactory? loggerFactory = null,
        ICompressionService? compressionService = null,
        ITwoKeyEncryptionService? twoKeyEncryptionService = null,
        IAmazonS3? s3Client = null,
        IMetrics? metrics = null,
        IFileOperationContextAccessor? operationContextAccessor = null,
        IEnumerable<IFileAuditEventHandler>? auditHandlers = null,
        IFileContentPolicy? contentPolicy = null,
        IFileMalwareScanner? malwareScanner = null)
        : base(
            ArgumentHelpers.ThrowIfNullReturn(options, nameof(options)), ArgumentHelpers.ThrowIfNullReturn(metadataService, nameof(metadataService)),
            (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<S3FileStorageService>(), compressionService, twoKeyEncryptionService, metrics, operationContextAccessor,
            auditHandlers, contentPolicy, malwareScanner)
    {
        _options = options;
        if (s3Client != null) {
            _s3Client = s3Client;
            _ownsS3Client = false;
        }
        else {
            _s3Client = CreateS3Client();
            _ownsS3Client = true;
        }

        Logger.LogInformation("Initialized S3 file storage for bucket: {BucketName}", _options.BucketName);

        // Override base metric names with S3-specific ones
        MetricNames[nameof(FileStorage.Constants.Metrics.FileStoragePreSignedUrlGenerated)] = Constants.Metrics.FileStoragePreSignedUrlGenerated;
        MetricNames[nameof(FileStorage.Constants.Metrics.FileStoragePreSignedUrlGenerationFailed)] = Constants.Metrics.FileStoragePreSignedUrlGenerationFailed;
    }

    protected override async Task<long> GetStorageSizeAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
    {
        var objectKey = GetObjectKey(fileId, extension, pathPrefix);
        try {
            var request = new GetObjectMetadataRequest { BucketName = _options.BucketName, Key = objectKey };
            var response = await _s3Client.GetObjectMetadataAsync(request, ct).ConfigureAwait(false);
            return response.ContentLength;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
            return 0;
        }
    }

    protected override async Task<Stream?> ReadFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
    {
        var objectKey = await FindObjectKeyAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
        if (objectKey == null)
            return null;

        var getRequest = new GetObjectRequest { BucketName = _options.BucketName, Key = objectKey };
        var response = await _s3Client.GetObjectAsync(getRequest, ct).ConfigureAwait(false);
        return response.ResponseStream;
    }

    protected override async Task<bool> DeleteFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
    {
        var objectKey = await FindObjectKeyAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
        if (objectKey != null) {
            var deleteRequest = new DeleteObjectRequest { BucketName = _options.BucketName, Key = objectKey };
            await _s3Client.DeleteObjectAsync(deleteRequest, ct).ConfigureAwait(false);
            Logger.LogDebug("Deleted file {FileId} from S3 at key {ObjectKey}", fileId, objectKey);
            return true;
        }

        return false;
    }

    protected override async Task<EncryptionHeaderInfo> ExtractEncryptionHeaderAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
    {
        var objectKey = GetObjectKey(fileId, extension, pathPrefix);
        var getRequest = new GetObjectRequest { BucketName = _options.BucketName, Key = objectKey };
        using var response = await _s3Client.GetObjectAsync(getRequest, ct).ConfigureAwait(false);

        // Buffer the stream to read the header
        using var bufferStream = new MemoryStream();
        await response.ResponseStream.CopyToAsync(bufferStream, ct).ConfigureAwait(false);
        bufferStream.Position = 0;
        var header = EncryptionHeader.Read(bufferStream);
        return new(header.EncryptedDataEncryptionKey, header.KeyId, header.KeyVersion, header.DekKeyMaterialBytes);
    }

    protected override async Task UpdateFileHeaderAsync(Guid fileId, string? pathPrefix, string targetKeyId, string targetKeyVersion, byte[] newEncryptedDek, CancellationToken ct)
    {
        var objectKey = await FindObjectKeyAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
        if (objectKey != null) {
            // Download object from S3
            var getRequest = new GetObjectRequest { BucketName = _options.BucketName, Key = objectKey };
            using var getResponse = await _s3Client.GetObjectAsync(getRequest, ct).ConfigureAwait(false);
            var fileBytes = new byte[getResponse.ContentLength];
            await using var responseStream = getResponse.ResponseStream;
            var bytesRead = 0;
            while (bytesRead < fileBytes.Length) {
                var read = await responseStream.ReadAsync(fileBytes, bytesRead, fileBytes.Length - bytesRead, ct).ConfigureAwait(false);
                if (read == 0)
                    break;

                bytesRead += read;
            }

            if (fileBytes.Length >= 13) // Minimum header size
            {
                // Read existing header with new format
                using var fileStream = new MemoryStream(fileBytes, false);
                var header = EncryptionHeader.Read(fileStream);

                // Position after reading header (where chunks start)
                var chunksStartPos = (int)fileStream.Position;

                // Create updated header with new keyId, keyVersion, and encrypted DEK
                var updatedHeader = header.With(targetKeyId, targetKeyVersion, newEncryptedDek);

                // Build new file with updated header
                var newFileBytes = new List<byte>();
                updatedHeader.Write(newFileBytes);

                // Add rest of file (encrypted data chunks)
                newFileBytes.AddRange(fileBytes.Skip(chunksStartPos));

                // Upload updated object back to S3
                using var updatedStream = new MemoryStream(newFileBytes.ToArray());
                var putRequest = new PutObjectRequest {
                    BucketName = _options.BucketName,
                    Key = objectKey,
                    InputStream = updatedStream,
                    ContentType = FileTypeInfo.Unknown.MimeType
                };

                await _s3Client.PutObjectAsync(putRequest, ct).ConfigureAwait(false);
                Logger.LogDebug("Updated S3 object header for {FileId} with new keyId '{KeyId}', version {Version}, and encrypted DEK", fileId, targetKeyId, targetKeyVersion);
            }
        }
    }

    protected override async Task CleanupPartialFileAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
    {
        // Try to find and delete any object with this ID (could have different extensions)
        var objectKey = await FindObjectKeyAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
        if (objectKey != null) {
            try {
                var deleteRequest = new DeleteObjectRequest { BucketName = _options.BucketName, Key = objectKey };
                await _s3Client.DeleteObjectAsync(deleteRequest, ct).ConfigureAwait(false);
            }
            catch {
                // Ignore errors during cleanup
            }
        }
    }

    public override void Dispose()
    {
        if (Disposed)
            return;

        if (_ownsS3Client)
            _s3Client.Dispose();

        base.Dispose();
    }

    protected override Task<Stream> CreateOutputStreamAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
    {
        var objectKey = GetObjectKey(fileId, extension, pathPrefix);
        return Task.FromResult<Stream>(new S3UploadStream(_s3Client, _options.BucketName, objectKey, ct));
    }

    /// <inheritdoc />
    public override async Task<string> GetPreSignedReadUrlAsync(Guid fileId, TimeSpan? expiration = null, string? pathPrefix = null, CancellationToken ct = default)
    {
        var meta = await GetMetadataAsync(fileId, ct).ConfigureAwait(false);
        EnsureReadableAvailability(meta);
        var expirationTime = expiration ?? TimeSpan.FromHours(1);
        ArgumentHelpers.ThrowIfNotInRange(expirationTime, TimeSpan.Zero, TimeSpan.FromDays(7), nameof(expirationTime));
        var objectKey = await FindObjectKeyAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
        if (objectKey == null) {
            Logger.LogWarning("File {FileId} not found in S3, cannot generate pre-signed URL", fileId);
            throw new FileNotFoundException($"File with ID {fileId} was not found in storage.");
        }

        try {
            var request = new GetPreSignedUrlRequest {
                BucketName = _options.BucketName,
                Key = objectKey,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.Add(expirationTime)
            };

            // GetPreSignedURL is synchronous, but the method is async to support cancellation token
            var url = await _s3Client.GetPreSignedURLAsync(request).ConfigureAwait(false);
            Logger.LogDebug("Generated pre-signed URL for file {FileId} at key {ObjectKey}, expires in {Expiration}", fileId, objectKey, expirationTime);
            Metrics.IncrementCounter(MetricNames[nameof(FileStorage.Constants.Metrics.FileStoragePreSignedUrlGenerated)], tags: [("bucket", _options.BucketName)]);
            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.PresignedRead, DateTime.UtcNow, fileId, meta.TenantId, OperationContextAccessor.Current?.ActorId, meta.DataEncryptionKeyId,
                        meta.DataEncryptionKeyVersion, FileAuditOutcome.Success), ct)
                .ConfigureAwait(false);

            return url;
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Failed to generate pre-signed URL for file {FileId} at key {ObjectKey}", fileId, objectKey);
            Metrics.IncrementCounter(
                MetricNames[nameof(FileStorage.Constants.Metrics.FileStoragePreSignedUrlGenerationFailed)], tags: [("bucket", _options.BucketName), ("error", ex.GetType().Name)]);

            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.PresignedRead, DateTime.UtcNow, fileId, meta.TenantId, OperationContextAccessor.Current?.ActorId, meta.DataEncryptionKeyId,
                        meta.DataEncryptionKeyVersion, FileAuditOutcome.Failure, ex.Message), ct)
                .ConfigureAwait(false);

            throw;
        }
    }

    private string GetObjectKey(Guid fileId, string extension = "", string? pathPrefix = null)
    {
        var fileName = fileId.ToString("N") + extension;
        var keyParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(_options.KeyPrefix))
            keyParts.Add(_options.KeyPrefix.Trim().TrimStart('/', '\\').TrimEnd('/', '\\'));

        if (!string.IsNullOrWhiteSpace(pathPrefix))
            keyParts.Add(pathPrefix);
        else {
            var idString = fileId.ToString("N");
            keyParts.Add(idString.Substring(0, 2));
            keyParts.Add(idString.Substring(2, 2));
        }

        keyParts.Add(fileName);
        return string.Join("/", keyParts);
    }

    private async Task<string?> FindObjectKeyAsync(Guid fileId, string? pathPrefix = null, CancellationToken ct = default)
    {
        var baseKey = GetObjectKey(fileId, "", pathPrefix);
        if (await ObjectExistsAsync(baseKey, ct).ConfigureAwait(false))
            return baseKey;

        if (CompressionService != null) {
            var key = GetObjectKey(fileId, CompressionService.FileExtension, pathPrefix);
            if (await ObjectExistsAsync(key, ct).ConfigureAwait(false))
                return key;
        }

        if (TwoKeyEncryptionService != null) {
            var key = GetObjectKey(fileId, TwoKeyEncryptionService.FileExtension, pathPrefix);
            if (await ObjectExistsAsync(key, ct).ConfigureAwait(false))
                return key;

            if (CompressionService != null) {
                key = GetObjectKey(fileId, CompressionService.FileExtension + TwoKeyEncryptionService.FileExtension, pathPrefix);
                if (await ObjectExistsAsync(key, ct).ConfigureAwait(false))
                    return key;
            }
        }

        var commonExtensions = FileTypeInfo.CommonStorageResolutionSuffixes;
        foreach (var ext in commonExtensions) {
            ct.ThrowIfCancellationRequested();
            var key = GetObjectKey(fileId, ext, pathPrefix);
            if (await ObjectExistsAsync(key, ct).ConfigureAwait(false))
                return key;
        }

        return null;
    }

    private async Task<bool> ObjectExistsAsync(string objectKey, CancellationToken ct = default)
    {
        try {
            var request = new GetObjectMetadataRequest { BucketName = _options.BucketName, Key = objectKey };
            await _s3Client.GetObjectMetadataAsync(request, ct).ConfigureAwait(false);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
            return false;
        }
        catch {
            return false;
        }
    }

    private IAmazonS3 CreateS3Client()
    {
        var config = new AmazonS3Config();
        if (!string.IsNullOrWhiteSpace(_options.Region)) {
            var region = RegionEndpoint.GetBySystemName(_options.Region);
            config.RegionEndpoint = region;
        }

        if (!string.IsNullOrWhiteSpace(_options.ServiceUrl)) {
            config.ServiceURL = _options.ServiceUrl;
            config.ForcePathStyle = true; // Required for S3-compatible services
        }

        if (!string.IsNullOrWhiteSpace(_options.AccessKeyId) && !string.IsNullOrWhiteSpace(_options.SecretAccessKey)) {
            var credentials = new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey);
            return new AmazonS3Client(credentials, config);
        }

        return new AmazonS3Client(config);
    }
}