using System.Diagnostics;
using System.IO.Pipelines;
using Lyo.Common.Records;
using Lyo.Compression;
using Lyo.Compression.Models;
using Lyo.Encryption;
using Lyo.Encryption.Extensions;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.Health;
using Lyo.Metrics;
using Lyo.Streams;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static Lyo.Compression.Constants.Data;
using HashAlgorithm = Lyo.FileMetadataStore.Models.HashAlgorithm;

namespace Lyo.FileStorage;

/// <summary>Base abstract class for file storage services providing common functionality.</summary>
public abstract class FileStorageServiceBase : IFileStorageService, IDisposable
{
    private const int CopyToBufferSizeBytes = 81920;

    private static Task DisposeStreamAsync(Stream? stream)
    {
        if (stream == null)
            return Task.CompletedTask;
#if NET5_0_OR_GREATER
        return stream.DisposeAsync().AsTask();
#else
        stream.Dispose();
        return Task.CompletedTask;
#endif
    }

    private readonly IReadOnlyList<IFileAuditEventHandler> _auditHandlers;
    protected readonly ICompressionService? CompressionService;
    protected readonly IFileContentPolicy ContentPolicy;
    protected readonly ILogger Logger;
    protected readonly IFileMalwareScanner MalwareScanner;
    protected readonly IFileMetadataStore MetadataService;
    protected readonly IMetrics Metrics;
    protected readonly IFileOperationContextAccessor OperationContextAccessor;
    protected readonly FileStorageServiceBaseOptions Options;
    protected readonly ITwoKeyEncryptionService? TwoKeyEncryptionService;

    protected bool Disposed;

    /// <summary>Gets the metric names dictionary. Derived classes can modify this dictionary to override metric names.</summary>
    protected Dictionary<string, string> MetricNames { get; }

    protected FileStorageServiceBase(
        FileStorageServiceBaseOptions options,
        IFileMetadataStore metadataService,
        ILogger? logger = null,
        ICompressionService? compressionService = null,
        ITwoKeyEncryptionService? twoKeyEncryptionService = null,
        IMetrics? metrics = null,
        IFileOperationContextAccessor? operationContextAccessor = null,
        IEnumerable<IFileAuditEventHandler>? auditHandlers = null,
        IFileContentPolicy? contentPolicy = null,
        IFileMalwareScanner? malwareScanner = null)
    {
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ArgumentHelpers.ThrowIfNull(metadataService, nameof(metadataService));
        Options = options;
        MetadataService = metadataService;
        Logger = logger ?? NullLogger.Instance;
        CompressionService = compressionService;
        TwoKeyEncryptionService = twoKeyEncryptionService;
        Metrics = metrics ?? NullMetrics.Instance;
        OperationContextAccessor = operationContextAccessor ?? NullFileOperationContextAccessor.Instance;
        _auditHandlers = auditHandlers == null ? [] : auditHandlers.ToList();
        ContentPolicy = contentPolicy ?? new DefaultFileContentPolicy(options);
        MalwareScanner = malwareScanner ?? NullFileMalwareScanner.Instance;
        MetricNames = CreateMetricNamesDictionary();
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        if (!Disposed)
            Disposed = true;
    }

    /// <inheritdoc />
    public virtual string HealthCheckName => "filestorage";

    /// <inheritdoc />
    public virtual async Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try {
            var testData = Guid.NewGuid().ToByteArray();
            var result = await SaveFileAsync(testData, "health-check.tmp", false, false, null, ".lyo-health", null, null, null, ct).ConfigureAwait(false);
            var fileId = result.Id;
            var retrieved = await GetFileAsync(fileId, ct).ConfigureAwait(false);
            await DeleteFileAsync(fileId, ct).ConfigureAwait(false);
            sw.Stop();
            var ok = retrieved.Length == testData.Length;
            return ok
                ? HealthResult.Healthy(sw.Elapsed, null, new Dictionary<string, object?> { ["fileId"] = fileId })
                : HealthResult.Unhealthy(sw.Elapsed, "Retrieved data length mismatch");
        }
        catch (Exception ex) {
            sw.Stop();
            return HealthResult.Unhealthy(sw.Elapsed, ex.Message, null, ex);
        }
    }

    /// <inheritdoc />
    public event EventHandler<FileSavedResult>? FileSaved;

    /// <inheritdoc />
    public event EventHandler<FileRetrievedResult>? FileRetrieved;

    /// <inheritdoc />
    public event EventHandler<FileDeletedResult>? FileDeleted;

    /// <inheritdoc />
    public event EventHandler<FileAuditEventArgs>? FileAuditOccurred;

    /// <inheritdoc />
    public async Task<FileStoreResult> SaveFileAsync(
        byte[] data,
        string? originalFileName = null,
        bool compress = false,
        bool encrypt = false,
        string? keyId = null,
        string? pathPrefix = null,
        int? chunkSize = null,
        string? contentType = null,
        string? tenantId = null,
        CancellationToken ct = default)
    {
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.SaveDuration)]);
        var sw = Stopwatch.StartNew();
        ArgumentHelpers.ThrowIfNullOrEmpty(data, nameof(data));
        OperationHelpers.ThrowIf(
            encrypt && string.IsNullOrWhiteSpace(keyId),
            "Encryption was requested but no keyId was provided. When encrypting files, you must provide a keyId parameter to identify the encryption key to use.");

        OperationHelpers.ThrowIf(
            encrypt && TwoKeyEncryptionService == null,
            "Encryption was requested but no encryption service is configured. Provide an ITwoKeyEncryptionService instance when creating FileStorageService.");

        OperationHelpers.ThrowIf(
            compress && CompressionService == null,
            "Compression was requested but no compression service is configured. Provide an ICompressionService instance when creating FileStorageService.");

        var resolvedTenant = ResolveTenantId(tenantId);
        var storedContentType = ResolveStoredContentType(contentType, originalFileName);
        try {
            await ContentPolicy.ValidateAsync(
                    new() {
                        ByteLength = data.LongLength,
                        ContentType = storedContentType,
                        OriginalFileName = originalFileName,
                        TenantId = resolvedTenant
                    }, ct)
                .ConfigureAwait(false);

            var availability = await DetermineAvailabilityAfterScanningPlaintextAsync(data, storedContentType, originalFileName, ct).ConfigureAwait(false);
            var normalizedPathPrefix = NormalizePathPrefix(pathPrefix);

            // Validate path prefix for security
            if (!string.IsNullOrWhiteSpace(normalizedPathPrefix)) {
                ArgumentHelpers.ThrowIf(
                    normalizedPathPrefix.Contains("..") || normalizedPathPrefix.Contains("//") || normalizedPathPrefix.Contains("\\\\"),
                    $"Invalid pathPrefix '{pathPrefix}'. Path prefixes cannot contain '..', '//', or '\\\\' to prevent path traversal attacks.", nameof(pathPrefix));
            }

            // Determine chunk size if not provided
            var effectiveChunkSize = chunkSize ?? StreamChunkSizeHelper.DetermineChunkSize(data.Length);
            var originalSize = data.Length;
            var fileId = Guid.NewGuid();
            var timestamp = DateTime.UtcNow;
            Logger.LogDebug(
                "Saving file {FileId}, Size: {Size} bytes, Compress: {Compress}, Encrypt: {Encrypt}, PathPrefix: {PathPrefix}", fileId, data.Length, compress, encrypt,
                normalizedPathPrefix ?? "none");

            // Get final file path/key (determine extension early)
            var fileExtension = "";
            if (encrypt && TwoKeyEncryptionService != null)
                fileExtension = TwoKeyEncryptionService.FileExtension;
            else if (compress && CompressionService != null)
                fileExtension = CompressionService.FileExtension;

            // Process using streaming pipeline: input -> compression -> [pipe] -> encryption -> output
            using var inputStream = new MemoryStream(data);
            var outputStream = await CreateOutputStreamAsync(fileId, fileExtension, normalizedPathPrefix, ct).ConfigureAwait(false);
            try {
                long? compressedSize = null;
                byte[]? compressedHash = null;
                CompressionAlgorithm? compressionAlgorithm = null;
                EncryptionAlgorithm? dataEncryptionKeyAlgorithm = null;
                EncryptionAlgorithm? keyEncryptionKeyAlgorithm = null;
                long? encryptedSize = null;
                byte[]? encryptedHash = null;
                byte[]? encryptedDataEncryptionKey = null;
                string? dataEncryptionKeyId = null;
                string? dataEncryptionKeyVersion = null;
                byte[]? keyEncryptionKeySalt = null;
                byte? dekKeyMaterialBytes = null;
                var sourceFileName = fileId.ToString();
                byte[]? originalHash;
                if (compress && encrypt && !Options.EnableDuplicateDetection) {
                    // Single-pass pipeline: compress -> pipe -> encrypt without buffering full compressed stream
                    // (When duplicate detection is enabled, we need originalHash before encryption, so use sequential path)
                    var pipelineResult = await SaveWithCompressEncryptPipelineAsync(
                            inputStream, outputStream, fileId, keyId!, normalizedPathPrefix, effectiveChunkSize, originalSize, ct)
                        .ConfigureAwait(false);

                    originalHash = pipelineResult.OriginalHash;
                    fileExtension = pipelineResult.FileExtension;
                    sourceFileName = pipelineResult.SourceFileName;
                    compressedSize = pipelineResult.CompressedSize;
                    compressedHash = pipelineResult.CompressedHash;
                    compressionAlgorithm = pipelineResult.CompressionAlgorithm;
                    encryptedHash = pipelineResult.EncryptedHash;
                    encryptedDataEncryptionKey = pipelineResult.EncryptedDataEncryptionKey;
                    dataEncryptionKeyId = pipelineResult.DataEncryptionKeyId;
                    dataEncryptionKeyVersion = pipelineResult.DataEncryptionKeyVersion;
                    keyEncryptionKeySalt = pipelineResult.KeyEncryptionKeySalt;
                    encryptedSize = pipelineResult.EncryptedSize;
                    dataEncryptionKeyAlgorithm = pipelineResult.DataEncryptionKeyAlgorithm;
                    keyEncryptionKeyAlgorithm = pipelineResult.KeyEncryptionKeyAlgorithm;
                    dekKeyMaterialBytes = pipelineResult.DekKeyMaterialBytes;
                    outputStream = null; // Already disposed inside pipeline
                }
                else {
                    // Sequential path: compress then encrypt (or just one)
                    Stream processingStream = inputStream;
                    using var intermediateStream = CreateSequentialStagingStream(originalSize, compress);
                    var hashAlg = Options.HashAlgorithm;
                    using var originalHashAlgo = hashAlg.Create();
                    if (compress) {
                        OperationHelpers.ThrowIfNull(
                            CompressionService,
                            "Compression was requested but no compression service is configured. Provide an ICompressionService instance when creating FileStorageService.");

                        using var compressedHashAlgo = hashAlg.Create();
                        using var compressedHashStream = new HashingStream(intermediateStream, compressedHashAlgo);
                        using var inputForHash = new HashingStream(inputStream, originalHashAlgo);
                        await CompressionService.CompressAsync(inputForHash, compressedHashStream, effectiveChunkSize, ct).ConfigureAwait(false);
                        await compressedHashStream.FlushAsync(ct).ConfigureAwait(false);
                        compressedSize = intermediateStream.Length;
                        compressedHash = compressedHashStream.GetHash();
                        originalHash = inputForHash.GetHash();
                        fileExtension = CompressionService.FileExtension;
                        sourceFileName += fileExtension;
                        compressionAlgorithm = DetermineCompressionAlgorithm(fileExtension);
                        processingStream = intermediateStream;
                        processingStream.Position = 0;
                        Logger.LogDebug("Compressed file {FileId}: {OriginalSize} -> {CompressedSize} bytes", fileId, originalSize, compressedSize);
                    }
                    else {
                        using var outputHashStream = new HashingStream(intermediateStream, originalHashAlgo);
                        await processingStream.CopyToAsync(outputHashStream, CopyToBufferSizeBytes, ct).ConfigureAwait(false);
                        await outputHashStream.FlushAsync(ct).ConfigureAwait(false);
                        originalHash = outputHashStream.GetHash();
                        processingStream = intermediateStream;
                        processingStream.Position = 0;
                    }

                    if (Options.EnableDuplicateDetection) {
                        var existingMetadata = await MetadataService.FindByHashAsync(originalHash, ct).ConfigureAwait(false);
                        if (existingMetadata != null) {
                            var duplicateResult = await HandleDuplicateAsync(existingMetadata, fileId, originalSize, normalizedPathPrefix, ct).ConfigureAwait(false);
                            if (duplicateResult != null)
                                return duplicateResult;

                            if (Options.DuplicateStrategy == DuplicateHandlingStrategy.Overwrite) {
                                fileId = existingMetadata.Id;
                                await DisposeStreamAsync(outputStream).ConfigureAwait(false);
                                outputStream = await CreateOutputStreamAsync(fileId, fileExtension, normalizedPathPrefix, ct).ConfigureAwait(false);
                            }
                        }
                    }

                    if (encrypt) {
                        OperationHelpers.ThrowIfNull(
                            TwoKeyEncryptionService,
                            "Encryption was requested but no encryption service is configured. " +
                            "Provide an ITwoKeyEncryptionService instance when creating FileStorageService.");

                        OperationHelpers.ThrowIfNullOrWhiteSpace(
                            keyId,
                            "Encryption was requested but no keyId was provided. When encrypting files, you must provide a keyId parameter to identify the encryption key to use.");

                        using var encryptedHashAlgo = hashAlg.Create();
                        using var encryptedHashStream = new HashingStream(outputStream, encryptedHashAlgo);
                        await TwoKeyEncryptionService.EncryptToStreamAsync(processingStream, encryptedHashStream, keyId, null, effectiveChunkSize, ct).ConfigureAwait(false);
                        await encryptedHashStream.FlushAsync(ct).ConfigureAwait(false);
                        await outputStream.FlushAsync(ct).ConfigureAwait(false);
                        encryptedHash = encryptedHashStream.GetHash();
                        await DisposeStreamAsync(outputStream).ConfigureAwait(false);
                        outputStream = null;
                        fileExtension = TwoKeyEncryptionService.FileExtension;
                        var headerInfo = await ExtractEncryptionHeaderAsync(fileId, fileExtension, normalizedPathPrefix, ct).ConfigureAwait(false);
                        encryptedDataEncryptionKey = headerInfo.EncryptedDataEncryptionKey;
                        dataEncryptionKeyId = headerInfo.DataEncryptionKeyId ?? keyId;
                        dataEncryptionKeyVersion = headerInfo.DataEncryptionKeyVersion;
                        dekKeyMaterialBytes = headerInfo.DekKeyMaterialBytes;
                        if (TwoKeyEncryptionService != null && dataEncryptionKeyVersion != null)
                            keyEncryptionKeySalt = TwoKeyEncryptionService.GetSaltForVersion(dataEncryptionKeyId, dataEncryptionKeyVersion);

                        encryptedSize = await GetStorageSizeAsync(fileId, fileExtension, normalizedPathPrefix, ct).ConfigureAwait(false);
                        dataEncryptionKeyAlgorithm = EncryptionServiceExtensions.DetermineDekAlgorithm(TwoKeyEncryptionService);
                        keyEncryptionKeyAlgorithm = EncryptionServiceExtensions.DetermineKekAlgorithm(TwoKeyEncryptionService);
                        sourceFileName = fileId + fileExtension;
                        Logger.LogDebug(
                            "Encrypted file {FileId} using two-key encryption: {Size} bytes, KeyVersion: {KeyVersion}", fileId, encryptedSize, dataEncryptionKeyVersion);
                    }
                    else {
                        processingStream.Position = 0;
                        await processingStream.CopyToAsync(outputStream, CopyToBufferSizeBytes, ct).ConfigureAwait(false);
                        await outputStream.FlushAsync(ct).ConfigureAwait(false);
                        await DisposeStreamAsync(outputStream).ConfigureAwait(false);
                        outputStream = null;
                    }
                }

                var finalSize = await GetStorageSizeAsync(fileId, fileExtension, normalizedPathPrefix, ct).ConfigureAwait(false);
                var sourceFileHash = encrypt && encryptedHash != null ? encryptedHash : compress && compressedHash != null ? compressedHash : originalHash;
                var metadata = new FileStoreResult(
                    fileId, originalFileName ?? fileId.ToString(), originalSize, originalHash!, sourceFileName, finalSize, sourceFileHash!, compress, compressionAlgorithm,
                    compressedSize, compressedHash, encrypt, dataEncryptionKeyAlgorithm, keyEncryptionKeyAlgorithm, encryptedSize, encryptedHash, encryptedDataEncryptionKey,
                    dataEncryptionKeyId, dataEncryptionKeyVersion, keyEncryptionKeySalt, timestamp, normalizedPathPrefix, Options.HashAlgorithm, storedContentType, resolvedTenant,
                    availability, dekKeyMaterialBytes);

                await MetadataService.SaveMetadataAsync(fileId, metadata, ct).ConfigureAwait(false);
                sw.Stop();
                Logger.LogInformation("Saved file {FileId} successfully. Original: {OriginalSize} bytes, Final: {FinalSize} bytes", fileId, originalSize, finalSize);
                FileSaved?.Invoke(this, new(fileId, metadata, originalSize, finalSize, compress, encrypt));
                Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.SaveSuccess)]);
                Metrics.RecordGauge(MetricNames[nameof(Constants.Metrics.SaveFileSizeBytes)], finalSize);
                Metrics.RecordHistogram(MetricNames[nameof(Constants.Metrics.SaveDurationMs)], sw.ElapsedMilliseconds);
                await RaiseFileAuditAsync(
                        new(
                            FileAuditEventType.Save, DateTime.UtcNow, fileId, resolvedTenant, OperationContextAccessor.Current?.ActorId, dataEncryptionKeyId,
                            dataEncryptionKeyVersion,
                            FileAuditOutcome.Success), ct)
                    .ConfigureAwait(false);

                return metadata;
            }
            finally {
                if (outputStream != null)
                    await DisposeStreamAsync(outputStream).ConfigureAwait(false);
            }
        }
        catch (Exception ex) {
            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.Save, DateTime.UtcNow, null, resolvedTenant, OperationContextAccessor.Current?.ActorId, keyId, null, FileAuditOutcome.Failure,
                        ex.Message),
                    ct)
                .ConfigureAwait(false);

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FileStoreResult> SaveFileAsync(
        string filePath,
        string? originalFileName = null,
        bool compress = false,
        bool encrypt = false,
        string? keyId = null,
        string? pathPrefix = null,
        int? chunkSize = null,
        string? contentType = null,
        string? tenantId = null,
        CancellationToken ct = default)
    {
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.SaveDuration)]);
        var sw = Stopwatch.StartNew();
        ArgumentHelpers.ThrowIfFileNotFound(filePath, nameof(filePath));
        var normalizedPathPrefix = NormalizePathPrefix(pathPrefix);
        var fileInfo = new FileInfo(filePath);
        var originalSize = fileInfo.Length;
        ArgumentHelpers.ThrowIfZero(originalSize, nameof(originalSize));
        var actualOriginalFileName = originalFileName ?? Path.GetFileName(filePath);
        Logger.LogDebug(
            "Saving file from path {FilePath} to storage, Size: {Size} bytes, Compress: {Compress}, Encrypt: {Encrypt}, PathPrefix: {PathPrefix}", filePath, originalSize, compress,
            encrypt, normalizedPathPrefix ?? "none");

        var resolvedTenant = ResolveTenantId(tenantId);
        var resolvedContentType = ResolveStoredContentType(contentType, actualOriginalFileName);
        try {
            await ContentPolicy.ValidateAsync(
                    new() {
                        ByteLength = originalSize,
                        ContentType = resolvedContentType,
                        OriginalFileName = actualOriginalFileName,
                        TenantId = resolvedTenant
                    }, ct)
                .ConfigureAwait(false);

            var availability = await DetermineAvailabilityAfterScanningFileAsync(filePath, originalSize, resolvedContentType, actualOriginalFileName, ct).ConfigureAwait(false);
            var fileId = Guid.NewGuid();
            var timestamp = DateTime.UtcNow;

            // Determine chunk size if not provided
            var effectiveChunkSize = chunkSize ?? StreamChunkSizeHelper.DetermineChunkSize(filePath);

            // Open file stream for processing
            using var inputStream = File.OpenRead(filePath);
            // Process using streaming pipeline
            var result = await ProcessAndSaveStreamAsync(
                    inputStream, fileId, actualOriginalFileName, originalSize, compress, encrypt, keyId, normalizedPathPrefix, timestamp, effectiveChunkSize, contentType,
                    resolvedTenant, availability, ct)
                .ConfigureAwait(false);

            sw.Stop();
            Logger.LogInformation(
                "Saved file from path {FilePath} successfully. FileId: {FileId}, Original: {OriginalSize} bytes, Final: {FinalSize} bytes", filePath, fileId, originalSize,
                result.SourceFileSize);

            Metrics.IncrementCounter(Constants.Metrics.SaveSuccess);
            Metrics.RecordGauge(Constants.Metrics.SaveFileSizeBytes, result.SourceFileSize);
            Metrics.RecordHistogram(Constants.Metrics.SaveDurationMs, sw.ElapsedMilliseconds);
            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.Save, DateTime.UtcNow, fileId, resolvedTenant, OperationContextAccessor.Current?.ActorId, result.DataEncryptionKeyId,
                        result.DataEncryptionKeyVersion, FileAuditOutcome.Success), ct)
                .ConfigureAwait(false);

            return result;
        }
        catch (Exception ex) {
            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.Save, DateTime.UtcNow, null, resolvedTenant, OperationContextAccessor.Current?.ActorId, keyId, null, FileAuditOutcome.Failure,
                        ex.Message),
                    ct)
                .ConfigureAwait(false);

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FileStoreResult> SaveFromStreamAsync(
        Stream input,
        long declaredLength,
        string? originalFileName = null,
        bool compress = false,
        bool encrypt = false,
        string? keyId = null,
        string? pathPrefix = null,
        int? chunkSize = null,
        string? contentType = null,
        string? tenantId = null,
        FileAvailability? availabilityOverride = null,
        Guid? fileId = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(input, nameof(input));
        var resolvedTenant = ResolveTenantId(tenantId);
        await ContentPolicy.ValidateAsync(
                new() {
                    ByteLength = declaredLength,
                    ContentType = ResolveStoredContentType(contentType, originalFileName),
                    OriginalFileName = originalFileName,
                    TenantId = resolvedTenant
                }, ct)
            .ConfigureAwait(false);

        var availability = availabilityOverride ?? (Options.RequireScanBeforeAvailable ? FileAvailability.PendingScan : Options.DefaultAvailability);
        var id = fileId ?? Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var normalizedPathPrefix = NormalizePathPrefix(pathPrefix);
        var effectiveChunkSize = chunkSize ?? StreamChunkSizeHelper.DetermineChunkSize(declaredLength);
        return await ProcessAndSaveStreamAsync(
                input, id, originalFileName ?? id.ToString(), declaredLength, compress, encrypt, keyId, normalizedPathPrefix, timestamp, effectiveChunkSize, contentType,
                resolvedTenant, availability, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual Task<string> GetPreSignedReadUrlAsync(Guid fileId, TimeSpan? expiration = null, string? pathPrefix = null, CancellationToken ct = default)
        => Task.FromException<string>(new NotSupportedException("Pre-signed read URLs are not supported by this storage backend. Use Azure or AWS file storage implementations."));

    /// <inheritdoc />
    public async Task<byte[]> GetFileAsync(Guid fileId, CancellationToken ct = default)
    {
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.GetDuration)]);
        var sw = Stopwatch.StartNew();
        Logger.LogDebug("Retrieving file {FileId}", fileId);

        // Load metadata first
        FileStoreResult? metadata;
        try {
            metadata = await GetMetadataAsync(fileId, ct).ConfigureAwait(false);
        }
        catch (FileNotFoundException) when (!Options.ThrowOnFileNotFound) {
            sw.Stop();
            Logger.LogDebug("File metadata not found for {FileId}, returning empty array", fileId);
            return [];
        }

        OperationHelpers.ThrowIfNull(metadata, $"Metadata for file {fileId} was not found. The file may have been deleted or the metadata store may be unavailable.");
        try {
            EnsureReadableAvailability(metadata);
        }
        catch (FileNotAvailableException) {
            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.Read, DateTime.UtcNow, fileId, metadata.TenantId, OperationContextAccessor.Current?.ActorId, metadata.DataEncryptionKeyId,
                        metadata.DataEncryptionKeyVersion, FileAuditOutcome.Failure, "File not available for read"), ct)
                .ConfigureAwait(false);

            throw;
        }

        // Read file data using streaming pipeline
        byte[] data;
        using var storageStream = await ReadFromStorageAsync(fileId, metadata.PathPrefix, ct).ConfigureAwait(false);
        if (storageStream == null) {
            if (Options.ThrowOnFileNotFound) {
                sw.Stop();
                throw new FileNotFoundException($"File with ID {fileId} not found", fileId.ToString());
            }

            sw.Stop();
            Logger.LogDebug("File storage stream not found for {FileId}, returning empty array", fileId);
            return [];
        }

        var processingStream = storageStream;

        // Decryption stage
        MemoryStream? bufferedStream = null;
        try {
            if (metadata.IsEncrypted) {
                OperationHelpers.ThrowIfNull(
                    TwoKeyEncryptionService,
                    $"File {fileId} is encrypted but no encryption service is configured. Provide an ITwoKeyEncryptionService instance when creating FileStorageService to decrypt encrypted files.");

                // Buffer non-seekable streams
                if (!processingStream.CanSeek) {
                    bufferedStream = new();
                    await processingStream.CopyToAsync(bufferedStream, CopyToBufferSizeBytes, ct).ConfigureAwait(false);
                    bufferedStream.Position = 0;
                    processingStream = bufferedStream;
                }
                else
                    processingStream.Position = 0;

                var decryptedStream = new MemoryStream();
                // Pass null for keyId to read from stream header
                await TwoKeyEncryptionService.DecryptToStreamAsync(processingStream, decryptedStream, null, null, ct).ConfigureAwait(false);
                decryptedStream.Position = 0;
                processingStream = decryptedStream;
                if (bufferedStream != null) {
                    await DisposeStreamAsync(bufferedStream).ConfigureAwait(false);
                    bufferedStream = null;
                }

                Logger.LogDebug("Decrypted file {FileId} using two-key encryption (keyId and keyVersion read from stream header)", fileId);
            }
        }
        finally {
            bufferedStream?.Dispose();
        }

        // Decompression stage
        if (metadata.IsCompressed) {
            OperationHelpers.ThrowIfNull(
                CompressionService,
                $"File {fileId} is compressed but no compression service is configured. " +
                "Provide an ICompressionService instance when creating FileStorageService to decompress compressed files.");

            int? chunkSize = metadata.CompressedFileSize.HasValue ? StreamChunkSizeHelper.DetermineChunkSize(metadata.CompressedFileSize.Value) : null;
            var decompressedStream = new MemoryStream();
            await CompressionService.DecompressAsync(processingStream, decompressedStream, chunkSize, ct).ConfigureAwait(false);
            decompressedStream.Position = 0;
            if (Options.MaxDecompressedFileSize.HasValue && decompressedStream.Length > Options.MaxDecompressedFileSize.Value) {
                throw new InvalidDataException(
                    $"Decompressed file size ({decompressedStream.Length} bytes) exceeds maximum allowed ({Options.MaxDecompressedFileSize.Value} bytes). Possible decompression bomb.");
            }

            processingStream = decompressedStream;
            Logger.LogDebug("Decompressed file {FileId}: {CompressedSize} -> {DecompressedSize} bytes", fileId, metadata.CompressedFileSize, decompressedStream.Length);
        }

        // Read final result into byte array
        if (processingStream is MemoryStream ms)
            data = ms.ToArray();
        else {
            using var resultStream = new MemoryStream();
            await processingStream.CopyToAsync(resultStream, CopyToBufferSizeBytes, ct).ConfigureAwait(false);
            data = resultStream.ToArray();
        }

        var hashAlg = metadata.HashAlgorithm ?? HashAlgorithm.Sha256;
        var computedHash = ComputeHash(data, hashAlg);
        if (!ByteArraysEqual(computedHash, metadata.OriginalFileHash)) {
            if (Options.ThrowOnHashMismatch)
                throw new InvalidDataException($"Hash mismatch for file {fileId}. File may be corrupted.");

            Logger.LogWarning("Hash mismatch for file {FileId}. File may be corrupted.", fileId);
        }

        sw.Stop();
        Logger.LogInformation("Retrieved file {FileId} successfully. Size: {Size} bytes", fileId, data.Length);

        // Raise FileRetrieved event
        FileRetrieved?.Invoke(this, new(fileId, data.Length, metadata.IsCompressed, metadata.IsEncrypted));
        Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.GetSuccess)]);
        Metrics.RecordGauge(MetricNames[nameof(Constants.Metrics.GetFileSizeBytes)], data.Length);
        Metrics.RecordHistogram(MetricNames[nameof(Constants.Metrics.GetDurationMs)], sw.ElapsedMilliseconds);
        await RaiseFileAuditAsync(
                new(
                    FileAuditEventType.Read, DateTime.UtcNow, fileId, metadata.TenantId, OperationContextAccessor.Current?.ActorId, metadata.DataEncryptionKeyId,
                    metadata.DataEncryptionKeyVersion, FileAuditOutcome.Success), ct)
            .ConfigureAwait(false);

        return data;
    }

    /// <inheritdoc />
    public async Task<Stream?> GetFileStreamAsync(Guid fileId, CancellationToken ct = default)
    {
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.GetDuration)]);
        var sw = Stopwatch.StartNew();
        Logger.LogDebug("Retrieving file {FileId} as stream", fileId);

        // Load metadata first
        FileStoreResult? metadata;
        try {
            metadata = await GetMetadataAsync(fileId, ct).ConfigureAwait(false);
        }
        catch (FileNotFoundException) when (!Options.ThrowOnFileNotFound) {
            sw.Stop();
            Logger.LogDebug("File metadata not found for {FileId}, returning null", fileId);
            return null;
        }

        OperationHelpers.ThrowIfNull(metadata, $"Metadata for file {fileId} was not found. The file may have been deleted or the metadata store may be unavailable.");
        try {
            EnsureReadableAvailability(metadata);
        }
        catch (FileNotAvailableException) {
            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.Read, DateTime.UtcNow, fileId, metadata.TenantId, OperationContextAccessor.Current?.ActorId, metadata.DataEncryptionKeyId,
                        metadata.DataEncryptionKeyVersion, FileAuditOutcome.Failure, "File not available for read"), ct)
                .ConfigureAwait(false);

            throw;
        }

        var storageStream = await ReadFromStorageAsync(fileId, metadata.PathPrefix, ct).ConfigureAwait(false);
        if (storageStream == null) {
            if (Options.ThrowOnFileNotFound) {
                sw.Stop();
                throw new FileNotFoundException($"File with ID {fileId} not found", fileId.ToString());
            }

            sw.Stop();
            Logger.LogDebug("File storage stream not found for {FileId}, returning null", fileId);
            return null;
        }

        // Plain files: wrap the storage stream directly — true end-to-end streaming, no buffering.
        if (!metadata.IsEncrypted && !metadata.IsCompressed) {
            sw.Stop();
            Logger.LogInformation("Streaming plain file {FileId} directly from storage", fileId);
            Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.GetSuccess)]);
            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.Read, DateTime.UtcNow, fileId, metadata.TenantId, OperationContextAccessor.Current?.ActorId, metadata.DataEncryptionKeyId,
                        metadata.DataEncryptionKeyVersion, FileAuditOutcome.Success), ct)
                .ConfigureAwait(false);

            var hashAlgo = (metadata.HashAlgorithm ?? HashAlgorithm.Sha256).Create();
            FileRetrieved?.Invoke(this, new(fileId, 0, false, false));
            return new HashVerifyingReadStream(storageStream, hashAlgo, metadata.OriginalFileHash, Options.ThrowOnHashMismatch, Logger, fileId);
        }

        // Encrypted and/or compressed: decrypt → [compressed pipe] → decompress using System.IO.Pipelines (bounded RAM via backpressure, same idea as save pipeline).
        int? chunkSize = metadata.CompressedFileSize.HasValue ? StreamChunkSizeHelper.DetermineChunkSize(metadata.CompressedFileSize.Value) : null;
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var pipePlain = new Pipe();
        var pipelineTask = RunStreamingDecodePipelineAsync(storageStream, metadata, pipePlain.Writer, chunkSize, linkedCts.Token);

        Stream decoded = new PipelineFileReadStream(pipePlain.Reader.AsStream(), pipelineTask, linkedCts);
        if (Options.MaxDecompressedFileSize is { } maxDecompressed)
            decoded = new MaxDecompressedBytesReadStream(decoded, maxDecompressed, fileId);

        sw.Stop();
        Logger.LogInformation(
            "Retrieved file {FileId} as stream (encrypted={Encrypted}, compressed={Compressed}); streaming decode via pipes", fileId, metadata.IsEncrypted,
            metadata.IsCompressed);
        Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.GetSuccess)]);
        await RaiseFileAuditAsync(
                new(
                    FileAuditEventType.Read, DateTime.UtcNow, fileId, metadata.TenantId, OperationContextAccessor.Current?.ActorId, metadata.DataEncryptionKeyId,
                    metadata.DataEncryptionKeyVersion, FileAuditOutcome.Success), ct)
            .ConfigureAwait(false);

        var verifyHashAlgo = (metadata.HashAlgorithm ?? HashAlgorithm.Sha256).Create();
        FileRetrieved?.Invoke(this, new(fileId, metadata.OriginalFileSize, metadata.IsCompressed, metadata.IsEncrypted));
        return new HashVerifyingReadStream(decoded, verifyHashAlgo, metadata.OriginalFileHash, Options.ThrowOnHashMismatch, Logger, fileId);
    }

    /// <summary>Decrypts and/or decompresses from storage into <paramref name="plainWriter"/> using pipes so work runs concurrently with the HTTP response (backpressure).</summary>
    private async Task RunStreamingDecodePipelineAsync(
        Stream storageStream,
        FileStoreResult metadata,
        PipeWriter plainWriter,
        int? chunkSize,
        CancellationToken ct)
    {
        using (storageStream) {
            if (metadata.IsEncrypted && metadata.IsCompressed) {
                OperationHelpers.ThrowIfNull(
                    TwoKeyEncryptionService,
                    $"File {metadata.Id} is encrypted but no encryption service is configured. " +
                    "Provide an ITwoKeyEncryptionService instance when creating FileStorageService to decrypt encrypted files.");
                OperationHelpers.ThrowIfNull(
                    CompressionService,
                    $"File {metadata.Id} is compressed but no compression service is configured. " +
                    "Provide an ICompressionService instance when creating FileStorageService to decompress compressed files.");

                if (storageStream.CanSeek)
                    storageStream.Position = 0;

                var pipeCompressed = new Pipe();
                await Task.WhenAll(
                        DecryptStorageIntoCompressedPipeAsync(storageStream, pipeCompressed, TwoKeyEncryptionService!, ct),
                        DecompressCompressedPipeToPlainAsync(pipeCompressed, plainWriter, CompressionService!, chunkSize, ct))
                    .ConfigureAwait(false);
                Logger.LogDebug("Stream pipeline complete for file {FileId} (decrypt + decompress)", metadata.Id);
                return;
            }

            if (metadata.IsEncrypted) {
                OperationHelpers.ThrowIfNull(
                    TwoKeyEncryptionService,
                    $"File {metadata.Id} is encrypted but no encryption service is configured. " +
                    "Provide an ITwoKeyEncryptionService instance when creating FileStorageService to decrypt encrypted files.");

                if (storageStream.CanSeek)
                    storageStream.Position = 0;

                try {
                    using (var plainOut = plainWriter.AsStream(true))
                        await TwoKeyEncryptionService!.DecryptToStreamAsync(storageStream, plainOut, null, null, ct).ConfigureAwait(false);

                    await plainWriter.CompleteAsync().ConfigureAwait(false);
                }
                catch (Exception ex) {
                    await plainWriter.CompleteAsync(ex).ConfigureAwait(false);
                    throw;
                }

                Logger.LogDebug("Stream pipeline complete for file {FileId} (decrypt only)", metadata.Id);
                return;
            }

            if (metadata.IsCompressed) {
                OperationHelpers.ThrowIfNull(
                    CompressionService,
                    $"File {metadata.Id} is compressed but no compression service is configured. " +
                    "Provide an ICompressionService instance when creating FileStorageService to decompress compressed files.");

                if (storageStream.CanSeek)
                    storageStream.Position = 0;

                try {
                    using (var plainOut = plainWriter.AsStream(true))
                        await CompressionService!.DecompressAsync(storageStream, plainOut, chunkSize, ct).ConfigureAwait(false);

                    await plainWriter.CompleteAsync().ConfigureAwait(false);
                }
                catch (Exception ex) {
                    await plainWriter.CompleteAsync(ex).ConfigureAwait(false);
                    throw;
                }

                Logger.LogDebug("Stream pipeline complete for file {FileId} (decompress only)", metadata.Id);
                return;
            }

            await plainWriter.CompleteAsync(
                    new InvalidOperationException(
                        $"File {metadata.Id} was expected to be encrypted and/or compressed for streaming decode, but metadata flags do not indicate either."))
                .ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteFileAsync(Guid fileId, CancellationToken ct = default)
    {
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.DeleteDuration)]);
        var sw = Stopwatch.StartNew();
        Logger.LogDebug("Deleting file {FileId}", fileId);
        try {
            // Get metadata to find the correct path
            FileStoreResult? metadata;
            try {
                metadata = await GetMetadataAsync(fileId, ct).ConfigureAwait(false);
            }
            catch (FileNotFoundException) when (!Options.ThrowOnDeleteNotFound) {
                sw.Stop();
                Logger.LogDebug("File not found for deletion: {FileId}, returning false", fileId);
                Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.DeleteFailure)]);
                return false;
            }

            var pathPrefix = metadata.PathPrefix;

            // Delete from storage
            var deleted = await DeleteFromStorageAsync(fileId, pathPrefix, ct).ConfigureAwait(false);

            // Delete metadata using metadata service (idempotent)
            await MetadataService.DeleteMetadataAsync(fileId, ct).ConfigureAwait(false);
            sw.Stop();
            Logger.LogInformation("Successfully deleted file {FileId}", fileId);
            FileDeleted?.Invoke(this, new(fileId, deleted));
            Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.DeleteSuccess)]);
            Metrics.RecordHistogram(MetricNames[nameof(Constants.Metrics.DeleteDurationMs)], sw.ElapsedMilliseconds);
            await RaiseFileAuditAsync(new(
                    FileAuditEventType.Delete, DateTime.UtcNow, fileId, metadata.TenantId, OperationContextAccessor.Current?.ActorId, metadata.DataEncryptionKeyId,
                    metadata.DataEncryptionKeyVersion, FileAuditOutcome.Success), ct)
            .ConfigureAwait(false);

            return deleted;
        }
        catch (FileNotFoundException) when (Options.ThrowOnDeleteNotFound) {
            sw.Stop();
            Logger.LogWarning("File not found: {FileId}", fileId);
            Metrics.IncrementCounter(Constants.Metrics.DeleteFailure);
            throw;
        }
        catch (Exception ex) {
            sw.Stop();
            Logger.LogError(ex, "Failed to delete file {FileId}", fileId);
            FileDeleted?.Invoke(this, new(fileId, false, ex.Message));
            Metrics.IncrementCounter(Constants.Metrics.DeleteFailure);
            Metrics.RecordError(MetricNames[nameof(Constants.Metrics.DeleteDuration)], ex);
            return false;
        }
    }

    /// <inheritdoc />
    public virtual async Task<FileStoreResult> GetMetadataAsync(Guid fileId, CancellationToken ct = default)
    {
        Logger.LogDebug("Retrieving metadata for file {FileId}", fileId);
        var metadata = await MetadataService.GetMetadataAsync(fileId, ct).ConfigureAwait(false);
        Logger.LogDebug("Retrieved metadata for file {FileId}", fileId);
        return metadata;
    }

    /// <inheritdoc />
    public virtual async Task<DekMigrationResult> MigrateDeksAsync(
        string sourceKeyId,
        string? sourceKeyVersion = null,
        string? targetKeyId = null,
        string? targetKeyVersion = null,
        int batchSize = 100,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sourceKeyId, nameof(sourceKeyId));
        ArgumentHelpers.ThrowIfNotInRange(batchSize, 1, int.MaxValue, nameof(batchSize));
        OperationHelpers.ThrowIfNull(TwoKeyEncryptionService, "ITwoKeyEncryptionService is not configured. Cannot migrate DEKs without encryption service.");
        Logger.LogInformation(
            "Starting DEK migration: sourceKeyId='{SourceKeyId}', sourceKeyVersion={SourceKeyVersion}, targetKeyId='{TargetKeyId}', targetKeyVersion={TargetKeyVersion}",
            sourceKeyId, sourceKeyVersion ?? "all", targetKeyId ?? sourceKeyId, targetKeyVersion ?? "current");

        // Determine target keyId (defaults to source if not specified)
        var actualTargetKeyId = targetKeyId ?? sourceKeyId;

        // Get target key version if not specified
        string actualTargetVersion;
        if (!string.IsNullOrWhiteSpace(targetKeyVersion))
            actualTargetVersion = targetKeyVersion;
        else {
            // Get current version from encryption service
            var retrievedKeyVersion = TwoKeyEncryptionService.GetKeyVersion(actualTargetKeyId);
            OperationHelpers.ThrowIfNullOrWhiteSpace(
                retrievedKeyVersion, $"No current key version available for key ID '{actualTargetKeyId}'. Ensure the keystore is properly initialized.");

            actualTargetVersion = retrievedKeyVersion;
        }

        // Find all files matching the criteria
        var filesToMigrate = await MetadataService.FindByKeyIdAndVersionAsync(sourceKeyId, sourceKeyVersion, ct).ConfigureAwait(false);
        var filesList = filesToMigrate.ToList();
        Logger.LogInformation("Found {Count} files to migrate", filesList.Count);
        if (filesList.Count == 0)
            return new(0, 0, 0, [], []);

        var successfullyMigrated = 0;
        var failedFileIds = new List<Guid>();
        var errors = new List<string>();

        // Process files in batches
        for (var i = 0; i < filesList.Count; i += batchSize) {
            ct.ThrowIfCancellationRequested();
            var batch = filesList.Skip(i).Take(batchSize).ToList();
            Logger.LogDebug(
                "Processing batch {BatchNumber} ({StartIndex}-{EndIndex} of {Total})", i / batchSize + 1, i + 1, Math.Min(i + batchSize, filesList.Count), filesList.Count);

            foreach (var fileMetadata in batch) {
                ct.ThrowIfCancellationRequested();
                try {
                    // Skip if already migrated to target version
                    if (fileMetadata.DataEncryptionKeyId == actualTargetKeyId && fileMetadata.DataEncryptionKeyVersion == actualTargetVersion) {
                        Logger.LogDebug(
                            "File {FileId} already migrated to target keyId '{TargetKeyId}' version {TargetVersion}, skipping", fileMetadata.Id, actualTargetKeyId,
                            actualTargetVersion);

                        successfullyMigrated++;
                        continue;
                    }

                    // Skip if not encrypted or missing required fields
                    if (!fileMetadata.IsEncrypted || fileMetadata.EncryptedDataEncryptionKey == null || string.IsNullOrWhiteSpace(fileMetadata.DataEncryptionKeyVersion)) {
                        Logger.LogWarning("File {FileId} is not encrypted or missing encryption metadata, skipping", fileMetadata.Id);
                        continue;
                    }

                    // Re-encrypt the DEK
                    var newEncryptedDek = await TwoKeyEncryptionService!.ReEncryptDekAsync(
                            fileMetadata.EncryptedDataEncryptionKey, fileMetadata.DataEncryptionKeyId ?? sourceKeyId, fileMetadata.DataEncryptionKeyVersion, actualTargetKeyId,
                            actualTargetVersion, ct)
                        .ConfigureAwait(false);

                    // Get salt for target key version (if available)
                    byte[]? newSalt = null;
                    if (TwoKeyEncryptionService != null)
                        newSalt = TwoKeyEncryptionService.GetSaltForVersion(actualTargetKeyId, actualTargetVersion);

                    // Update file header with new encrypted DEK, keyId, and keyVersion
                    await UpdateFileHeaderAsync(fileMetadata.Id, fileMetadata.PathPrefix, actualTargetKeyId, actualTargetVersion, newEncryptedDek, ct).ConfigureAwait(false);

                    // Update metadata with new encrypted DEK and version
                    var updatedMetadata = fileMetadata with {
                        EncryptedDataEncryptionKey = newEncryptedDek,
                        DataEncryptionKeyId = actualTargetKeyId,
                        DataEncryptionKeyVersion = actualTargetVersion,
                        KeyEncryptionKeySalt = newSalt
                    };

                    // Save updated metadata
                    await MetadataService.SaveMetadataAsync(fileMetadata.Id, updatedMetadata, ct).ConfigureAwait(false);
                    successfullyMigrated++;
                    Logger.LogDebug("Successfully migrated DEK for file {FileId}", fileMetadata.Id);
                }
                catch (Exception ex) {
                    failedFileIds.Add(fileMetadata.Id);
                    var errorMessage = $"Failed to migrate file {fileMetadata.Id}: {ex.Message}";
                    errors.Add(errorMessage);
                    Logger.LogError(ex, "Failed to migrate DEK for file {FileId}", fileMetadata.Id);
                }
            }
        }

        Logger.LogInformation(
            "DEK migration completed: {SuccessfullyMigrated} succeeded, {Failed} failed out of {Total} files", successfullyMigrated, failedFileIds.Count, filesList.Count);

        var migResult = new DekMigrationResult(filesList.Count, successfullyMigrated, failedFileIds.Count, failedFileIds, errors);
        await RaiseFileAuditAsync(
                new(
                    FileAuditEventType.MigrateDeks, DateTime.UtcNow, null, OperationContextAccessor.Current?.TenantId, OperationContextAccessor.Current?.ActorId, sourceKeyId,
                    sourceKeyVersion, failedFileIds.Count == 0 ? FileAuditOutcome.Success : FileAuditOutcome.Failure,
                    failedFileIds.Count == 0 ? null : $"{failedFileIds.Count} files failed"), ct)
            .ConfigureAwait(false);

        return migResult;
    }

    /// <inheritdoc />
    public virtual async Task<DekMigrationResult> RotateDeksAsync(
        IReadOnlyCollection<Guid> fileIds,
        string? targetKeyId = null,
        string? targetKeyVersion = null,
        int batchSize = 100,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(fileIds, nameof(fileIds));
        ArgumentHelpers.ThrowIfNotInRange(batchSize, 1, int.MaxValue, nameof(batchSize));
        OperationHelpers.ThrowIfNull(TwoKeyEncryptionService, "ITwoKeyEncryptionService is not configured. Cannot rotate DEKs without encryption service.");
        var requestedFileIds = fileIds.Where(fileId => fileId != Guid.Empty).Distinct().ToList();
        if (requestedFileIds.Count == 0)
            return new(0, 0, 0, [], []);

        Logger.LogInformation(
            "Starting DEK rotation for {Count} files. targetKeyId='{TargetKeyId}', targetKeyVersion={TargetKeyVersion}", requestedFileIds.Count, targetKeyId ?? "per-file",
            targetKeyVersion ?? "per-file/current");

        var successfullyRotated = 0;
        var failedFileIds = new List<Guid>();
        var errors = new List<string>();
        for (var i = 0; i < requestedFileIds.Count; i += batchSize) {
            ct.ThrowIfCancellationRequested();
            var batch = requestedFileIds.Skip(i).Take(batchSize).ToList();
            Logger.LogDebug(
                "Processing DEK rotation batch {BatchNumber} ({StartIndex}-{EndIndex} of {Total})", i / batchSize + 1, i + 1, Math.Min(i + batchSize, requestedFileIds.Count),
                requestedFileIds.Count);

            foreach (var fileId in batch) {
                ct.ThrowIfCancellationRequested();
                try {
                    var fileMetadata = await MetadataService.GetMetadataAsync(fileId, ct).ConfigureAwait(false);
                    ValidateDekRotationMetadata(fileMetadata);
                    var (resolvedTargetKeyId, resolvedTargetKeyVersion) = ResolveDekRotationTarget(fileMetadata, targetKeyId, targetKeyVersion);
                    using var decryptedPayloadStream = await ReadEncryptedPayloadAsync(fileMetadata, ct).ConfigureAwait(false);
                    await RewriteEncryptedFileAsync(fileMetadata, decryptedPayloadStream, resolvedTargetKeyId, resolvedTargetKeyVersion, ct).ConfigureAwait(false);
                    successfullyRotated++;
                    Logger.LogDebug(
                        "Successfully rotated DEK for file {FileId} using keyId '{TargetKeyId}' version {TargetKeyVersion}", fileId, resolvedTargetKeyId, resolvedTargetKeyVersion);
                }
                catch (Exception ex) {
                    failedFileIds.Add(fileId);
                    var errorMessage = $"Failed to rotate DEK for file {fileId}: {ex.Message}";
                    errors.Add(errorMessage);
                    Logger.LogError(ex, "Failed to rotate DEK for file {FileId}", fileId);
                }
            }
        }

        Logger.LogInformation(
            "DEK rotation completed: {SuccessfullyRotated} succeeded, {Failed} failed out of {Total} requested files", successfullyRotated, failedFileIds.Count,
            requestedFileIds.Count);

        var rotResult = new DekMigrationResult(requestedFileIds.Count, successfullyRotated, failedFileIds.Count, failedFileIds, errors);
        await RaiseFileAuditAsync(
                new(
                    FileAuditEventType.RotateDeks, DateTime.UtcNow, null, OperationContextAccessor.Current?.TenantId, OperationContextAccessor.Current?.ActorId, targetKeyId,
                    targetKeyVersion, failedFileIds.Count == 0 ? FileAuditOutcome.Success : FileAuditOutcome.Failure,
                    failedFileIds.Count == 0 ? null : $"{failedFileIds.Count} files failed"), ct)
            .ConfigureAwait(false);

        return rotResult;
    }

    protected string? ResolveTenantId(string? explicitTenantId) => explicitTenantId ?? OperationContextAccessor.Current?.TenantId;

    protected Task RaiseFileAuditAsync(FileAuditEvent auditEvent, CancellationToken ct)
        => FileAuditPublication.PublishAsync(
            _auditHandlers, FileAuditOccurred, this, auditEvent, ct, Logger, Metrics, MetricNames[nameof(Constants.Metrics.AuditAppendFailed)], Options.ThrowOnAuditFailure);

    protected void EnsureReadableAvailability(FileStoreResult metadata)
    {
        if (metadata.Availability == FileAvailability.Available)
            return;

        if (metadata.Availability == FileAvailability.Quarantined && Options.AllowReadQuarantinedForAdmin)
            return;

        throw new FileNotAvailableException(metadata.Id, metadata.Availability);
    }

    /// <summary>Creates the metric names dictionary. Override in derived classes to customize metric names.</summary>
    protected virtual Dictionary<string, string> CreateMetricNamesDictionary()
        => new() {
            { nameof(Constants.Metrics.SaveDuration), Constants.Metrics.SaveDuration },
            { nameof(Constants.Metrics.SaveSuccess), Constants.Metrics.SaveSuccess },
            { nameof(Constants.Metrics.SaveCompressed), Constants.Metrics.SaveCompressed },
            { nameof(Constants.Metrics.SaveEncrypted), Constants.Metrics.SaveEncrypted },
            { nameof(Constants.Metrics.SaveFileSizeBytes), Constants.Metrics.SaveFileSizeBytes },
            { nameof(Constants.Metrics.SaveFinalSizeBytes), Constants.Metrics.SaveFinalSizeBytes },
            { nameof(Constants.Metrics.SaveDurationMs), Constants.Metrics.SaveDurationMs },
            { nameof(Constants.Metrics.GetDuration), Constants.Metrics.GetDuration },
            { nameof(Constants.Metrics.GetSuccess), Constants.Metrics.GetSuccess },
            { nameof(Constants.Metrics.GetFileSizeBytes), Constants.Metrics.GetFileSizeBytes },
            { nameof(Constants.Metrics.GetDurationMs), Constants.Metrics.GetDurationMs },
            { nameof(Constants.Metrics.DeleteDuration), Constants.Metrics.DeleteDuration },
            { nameof(Constants.Metrics.DeleteSuccess), Constants.Metrics.DeleteSuccess },
            { nameof(Constants.Metrics.DeleteFailure), Constants.Metrics.DeleteFailure },
            { nameof(Constants.Metrics.DeleteDurationMs), Constants.Metrics.DeleteDurationMs },
            { nameof(Constants.Metrics.FileSizeBytes), Constants.Metrics.FileSizeBytes },
            { nameof(Constants.Metrics.FileStoragePreSignedUrlGenerated), Constants.Metrics.FileStoragePreSignedUrlGenerated },
            { nameof(Constants.Metrics.FileStoragePreSignedUrlGenerationFailed), Constants.Metrics.FileStoragePreSignedUrlGenerationFailed },
            { nameof(Constants.Metrics.AuditAppendFailed), Constants.Metrics.AuditAppendFailed }
        };

    // Abstract methods that must be implemented by derived classes

    /// <summary>Creates an output stream for saving a file to storage.</summary>
    protected abstract Task<Stream> CreateOutputStreamAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct);

    /// <summary>Gets the size of a file in storage.</summary>
    protected abstract Task<long> GetStorageSizeAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct);

    /// <summary>Reads a file from storage as a stream.</summary>
    protected abstract Task<Stream?> ReadFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct);

    /// <summary>Deletes a file from storage.</summary>
    protected abstract Task<bool> DeleteFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct);

    /// <summary>Extracts encryption header information from a stored file.</summary>
    protected abstract Task<EncryptionHeaderInfo> ExtractEncryptionHeaderAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct);

    /// <summary>Updates the encryption header of a stored file.</summary>
    protected abstract Task UpdateFileHeaderAsync(Guid fileId, string? pathPrefix, string targetKeyId, string targetKeyVersion, byte[] newEncryptedDek, CancellationToken ct);

    // Protected helper methods

    protected static string? NormalizePathPrefix(string? pathPrefix)
    {
        if (string.IsNullOrWhiteSpace(pathPrefix))
            return null;

        var normalized = pathPrefix.Trim().TrimStart('/', '\\').TrimEnd('/', '\\');
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    protected async Task<FileStoreResult?> HandleDuplicateAsync(
        FileStoreResult existingMetadata,
        Guid newFileId,
        long originalSize,
        string? normalizedPathPrefix,
        CancellationToken ct)
    {
        switch (Options.DuplicateStrategy) {
            case DuplicateHandlingStrategy.ReturnExisting:
                Logger.LogInformation("Duplicate file detected for hash. Returning existing file ID: {FileId}", existingMetadata.Id);

                // Clean up any file we started creating
                await CleanupPartialFileAsync(newFileId, normalizedPathPrefix, ct).ConfigureAwait(false);
                FileSaved?.Invoke(
                    this, new(existingMetadata.Id, existingMetadata, originalSize, existingMetadata.SourceFileSize, existingMetadata.IsCompressed, existingMetadata.IsEncrypted));

                return existingMetadata;
            case DuplicateHandlingStrategy.AllowDuplicate:
                Logger.LogInformation("Duplicate file detected for hash. Existing file ID: {ExistingFileId}, but allowing duplicate. Creating new file.", existingMetadata.Id);
                return null; // Continue with new file creation
            case DuplicateHandlingStrategy.Overwrite:
                Logger.LogInformation("Duplicate file detected for hash. Existing file ID: {ExistingFileId}, overwriting with new file.", existingMetadata.Id);
                // Clean up the file we started creating (will recreate with same ID)
                await CleanupPartialFileAsync(newFileId, normalizedPathPrefix, ct).ConfigureAwait(false);
                // Delete the old file
                await DeleteFromStorageAsync(existingMetadata.Id, existingMetadata.PathPrefix, ct).ConfigureAwait(false);
                return null; // Continue with overwrite
            default:
                return null;
        }
    }

    /// <summary>Cleans up a partially created file if save operation is aborted.</summary>
    protected abstract Task CleanupPartialFileAsync(Guid fileId, string? pathPrefix, CancellationToken ct);

    private void ValidateDekRotationMetadata(FileStoreResult metadata)
    {
        OperationHelpers.ThrowIf(!metadata.IsEncrypted, $"File {metadata.Id} is not encrypted. DEK rotation is only supported for encrypted files.");
        OperationHelpers.ThrowIf(
            metadata.EncryptedDataEncryptionKey == null || metadata.EncryptedDataEncryptionKey.Length == 0, $"File {metadata.Id} is missing its encrypted DEK.");

        OperationHelpers.ThrowIfNullOrWhiteSpace(metadata.DataEncryptionKeyId, $"File {metadata.Id} is missing its key ID. Cannot rotate its DEK.");
        OperationHelpers.ThrowIfNullOrWhiteSpace(metadata.DataEncryptionKeyVersion, $"File {metadata.Id} is missing its key version. Cannot rotate its DEK.");
    }

    private (string TargetKeyId, string TargetKeyVersion) ResolveDekRotationTarget(FileStoreResult metadata, string? targetKeyId, string? targetKeyVersion)
    {
        var resolvedTargetKeyId = string.IsNullOrWhiteSpace(targetKeyId) ? metadata.DataEncryptionKeyId! : targetKeyId;
        string resolvedTargetKeyVersion;
        if (!string.IsNullOrWhiteSpace(targetKeyVersion))
            resolvedTargetKeyVersion = targetKeyVersion;
        else if (!string.IsNullOrWhiteSpace(targetKeyId)) {
            resolvedTargetKeyVersion = TwoKeyEncryptionService!.GetKeyVersion(resolvedTargetKeyId) ?? throw new InvalidOperationException(
                $"No current key version available for key ID '{resolvedTargetKeyId}'. Ensure the keystore is properly initialized.");
        }
        else
            resolvedTargetKeyVersion = metadata.DataEncryptionKeyVersion!;

        return (resolvedTargetKeyId, resolvedTargetKeyVersion);
    }

    private async Task<MemoryStream> ReadEncryptedPayloadAsync(FileStoreResult metadata, CancellationToken ct)
    {
        using var storageStream = await ReadFromStorageAsync(metadata.Id, metadata.PathPrefix, ct).ConfigureAwait(false);
        if (storageStream == null)
            throw new FileNotFoundException($"File with ID {metadata.Id} was not found in storage.", metadata.Id.ToString());

        var processingStream = storageStream;
        MemoryStream? bufferedStream = null;
        try {
            if (!processingStream.CanSeek) {
                bufferedStream = new();
                await processingStream.CopyToAsync(bufferedStream, CopyToBufferSizeBytes, ct).ConfigureAwait(false);
                bufferedStream.Position = 0;
                processingStream = bufferedStream;
            }
            else
                processingStream.Position = 0;

            var decryptedStream = new MemoryStream();
            await TwoKeyEncryptionService!.DecryptToStreamAsync(processingStream, decryptedStream, null, null, ct).ConfigureAwait(false);
            decryptedStream.Position = 0;
            return decryptedStream;
        }
        finally {
            bufferedStream?.Dispose();
        }
    }

    private async Task RewriteEncryptedFileAsync(FileStoreResult metadata, Stream decryptedPayloadStream, string targetKeyId, string targetKeyVersion, CancellationToken ct)
    {
        var fileExtension = TwoKeyEncryptionService!.FileExtension;
        var chunkSize = StreamChunkSizeHelper.DetermineChunkSize(metadata.CompressedFileSize ?? metadata.OriginalFileSize);
        byte[] encryptedHash;
        using (var outputStream = await CreateOutputStreamAsync(metadata.Id, fileExtension, metadata.PathPrefix, ct).ConfigureAwait(false)) {
            using (var encryptedHashAlgo = Options.HashAlgorithm.Create()) {
                using (var encryptedHashStream = new HashingStream(outputStream, encryptedHashAlgo)) {
                    decryptedPayloadStream.Position = 0;
                    await TwoKeyEncryptionService.EncryptToStreamAsync(decryptedPayloadStream, encryptedHashStream, targetKeyId, null, chunkSize, ct).ConfigureAwait(false);
                    await encryptedHashStream.FlushAsync(ct).ConfigureAwait(false);
                    await outputStream.FlushAsync(ct).ConfigureAwait(false);
                    encryptedHash = encryptedHashStream.GetHash();
                }
            }
        }

        var headerInfo = await ExtractEncryptionHeaderAsync(metadata.Id, fileExtension, metadata.PathPrefix, ct).ConfigureAwait(false);
        var resolvedKeyId = headerInfo.DataEncryptionKeyId ?? targetKeyId;
        var resolvedKeyVersion = headerInfo.DataEncryptionKeyVersion ?? targetKeyVersion;
        OperationHelpers.ThrowIfNullOrWhiteSpace(resolvedKeyVersion, $"Unable to determine the target key version for file {metadata.Id} after rewriting its encrypted payload.");
        var encryptedSize = await GetStorageSizeAsync(metadata.Id, fileExtension, metadata.PathPrefix, ct).ConfigureAwait(false);
        var keyEncryptionKeySalt = TwoKeyEncryptionService.GetSaltForVersion(resolvedKeyId, resolvedKeyVersion);
        var updatedMetadata = metadata with {
            SourceFileName = metadata.Id + fileExtension,
            SourceFileSize = encryptedSize,
            SourceFileHash = encryptedHash,
            DataEncryptionKeyAlgorithm = EncryptionServiceExtensions.DetermineDekAlgorithm(TwoKeyEncryptionService),
            KeyEncryptionKeyAlgorithm = EncryptionServiceExtensions.DetermineKekAlgorithm(TwoKeyEncryptionService),
            EncryptedFileSize = encryptedSize,
            EncryptedFileHash = encryptedHash,
            EncryptedDataEncryptionKey = headerInfo.EncryptedDataEncryptionKey,
            DataEncryptionKeyId = resolvedKeyId,
            DataEncryptionKeyVersion = resolvedKeyVersion,
            KeyEncryptionKeySalt = keyEncryptionKeySalt,
            DekKeyMaterialBytes = headerInfo.DekKeyMaterialBytes
        };

        await MetadataService.SaveMetadataAsync(metadata.Id, updatedMetadata, ct).ConfigureAwait(false);
    }

    private static async Task RunCompressIntoPipeWriterAsync(
        ICompressionService compressionService,
        HashingStream inputForHash,
        HashingStream compressedHashStream,
        int chunkSize,
        Pipe pipe,
        CancellationToken ct)
    {
        try {
            await compressionService.CompressAsync(inputForHash, compressedHashStream, chunkSize, ct).ConfigureAwait(false);
            await compressedHashStream.FlushAsync(ct).ConfigureAwait(false);
        }
        finally {
            await pipe.Writer.CompleteAsync().ConfigureAwait(false);
        }
    }

    private static Task RunEncryptFromPipeReaderAsync(
        ITwoKeyEncryptionService encryptionService,
        Stream pipeReader,
        Stream encryptedHashStream,
        string keyId,
        int chunkSize,
        CancellationToken ct)
        => encryptionService.EncryptToStreamAsync(pipeReader, encryptedHashStream, keyId, null, chunkSize, ct);

    private static async Task DecryptStorageIntoCompressedPipeAsync(
        Stream storageStream,
        Pipe pipeCompressed,
        ITwoKeyEncryptionService twoKey,
        CancellationToken ct)
    {
        try {
            using (var w = pipeCompressed.Writer.AsStream(true))
                await twoKey.DecryptToStreamAsync(storageStream, w, null, null, ct).ConfigureAwait(false);

            await pipeCompressed.Writer.CompleteAsync().ConfigureAwait(false);
        }
        catch (Exception ex) {
            await pipeCompressed.Writer.CompleteAsync(ex).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task DecompressCompressedPipeToPlainAsync(
        Pipe pipeCompressed,
        PipeWriter plainWriter,
        ICompressionService compression,
        int? chunkSize,
        CancellationToken ct)
    {
        try {
            using var compressedRead = pipeCompressed.Reader.AsStream(true);
            using (var plainOut = plainWriter.AsStream(true))
                await compression.DecompressAsync(compressedRead, plainOut, chunkSize, ct).ConfigureAwait(false);

            await plainWriter.CompleteAsync().ConfigureAwait(false);
        }
        catch (Exception ex) {
            await plainWriter.CompleteAsync(ex).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Single-pass pipeline: input -> compress -> pipe -> encrypt -> output. Used when both compress and encrypt are enabled and duplicate detection is off.</summary>
    private async Task<CompressEncryptPipelineResult> SaveWithCompressEncryptPipelineAsync(
        Stream inputStream,
        Stream outputStream,
        Guid fileId,
        string keyId,
        string? normalizedPathPrefix,
        int chunkSize,
        long originalSize,
        CancellationToken ct)
    {
        OperationHelpers.ThrowIfNull(CompressionService, "Compression service required for pipeline.");
        OperationHelpers.ThrowIfNull(TwoKeyEncryptionService, "Encryption service required for pipeline.");
        var compressionExt = CompressionService.FileExtension;
        var fileExtension = TwoKeyEncryptionService.FileExtension; // Output uses encryption extension
        var sourceFileName = fileId + fileExtension;
        var compressionAlgorithm = DetermineCompressionAlgorithm(compressionExt);
        var pipe = new Pipe();
        var hashAlg = Options.HashAlgorithm;
        using var compressedHashAlgo = hashAlg.Create();
        var pipeWriterStream = pipe.Writer.AsStream(true);
        using var countingStream = new CountingStream(pipeWriterStream);
        using var compressedHashStream = new HashingStream(countingStream, compressedHashAlgo);
        using var inputForHash = new HashingStream(inputStream, hashAlg.Create());
        using var encryptedHashAlgo = hashAlg.Create();
        using var encryptedHashStream = new HashingStream(outputStream, encryptedHashAlgo);
        var pipeReaderStream = pipe.Reader.AsStream(true);

        // Static helpers avoid async local functions capturing `using`-scoped streams (analyzer: captured variable disposed in outer scope).
        var compressionTask = RunCompressIntoPipeWriterAsync(CompressionService!, inputForHash, compressedHashStream, chunkSize, pipe, ct);
        var encryptionTask = RunEncryptFromPipeReaderAsync(TwoKeyEncryptionService!, pipeReaderStream, encryptedHashStream, keyId, chunkSize, ct);
        await Task.WhenAll(compressionTask, encryptionTask).ConfigureAwait(false);
        await encryptedHashStream.FlushAsync(ct).ConfigureAwait(false);
        await outputStream.FlushAsync(ct).ConfigureAwait(false);
        await DisposeStreamAsync(outputStream).ConfigureAwait(false);
        var compressedSize = countingStream.BytesWritten;
        var compressedHash = compressedHashStream.GetHash();
        var encryptedHash = encryptedHashStream.GetHash();
        var encryptedSize = await GetStorageSizeAsync(fileId, fileExtension, normalizedPathPrefix, ct).ConfigureAwait(false);
        var dataEncryptionKeyAlgorithm = EncryptionServiceExtensions.DetermineDekAlgorithm(TwoKeyEncryptionService);
        var keyEncryptionKeyAlgorithm = EncryptionServiceExtensions.DetermineKekAlgorithm(TwoKeyEncryptionService);
        var headerInfo = await ExtractEncryptionHeaderAsync(fileId, fileExtension, normalizedPathPrefix, ct).ConfigureAwait(false);
        var dataEncryptionKeyId = headerInfo.DataEncryptionKeyId ?? keyId;
        var dataEncryptionKeyVersion = headerInfo.DataEncryptionKeyVersion;
        var keyEncryptionKeySalt = dataEncryptionKeyVersion != null ? TwoKeyEncryptionService.GetSaltForVersion(dataEncryptionKeyId, dataEncryptionKeyVersion) : null;
        Logger.LogDebug(
            "Compressed and encrypted file {FileId} in single pass: {OriginalSize} -> {CompressedSize} -> {EncryptedSize} bytes", fileId, originalSize, compressedSize,
            encryptedSize);

        return new(
            inputForHash.GetHash(), fileExtension, sourceFileName, compressedSize, compressedHash, compressionAlgorithm, encryptedHash, headerInfo.EncryptedDataEncryptionKey,
            dataEncryptionKeyId, dataEncryptionKeyVersion, keyEncryptionKeySalt, encryptedSize, dataEncryptionKeyAlgorithm, keyEncryptionKeyAlgorithm,
            headerInfo.DekKeyMaterialBytes);
    }

    protected async Task<FileAvailability> DetermineAvailabilityAfterScanningPlaintextAsync(byte[] data, string? contentType, string? originalFileName, CancellationToken ct)
    {
        if (!Options.RequireScanBeforeAvailable)
            return Options.DefaultAvailability;

        if (MalwareScanner is NullFileMalwareScanner) {
            Logger.LogWarning("RequireScanBeforeAvailable is set but IFileMalwareScanner is not configured; files are marked Available without scanning.");
            return FileAvailability.Available;
        }

        using var ms = new MemoryStream(data, false);
        var scan = await MalwareScanner.ScanAsync(ms, contentType, originalFileName, ct).ConfigureAwait(false);
        return scan.ThreatLevel switch {
            FileScanThreatLevel.Clean => FileAvailability.Available,
            FileScanThreatLevel.Suspect => FileAvailability.Quarantined,
            FileScanThreatLevel.Threat => throw new FilePolicyRejectedException(scan.Detail ?? "Malware scan rejected the file."),
            var _ => FileAvailability.Available
        };
    }

    protected async Task<FileAvailability> DetermineAvailabilityAfterScanningFileAsync(
        string filePath,
        long length,
        string? contentType,
        string? originalFileName,
        CancellationToken ct)
    {
        if (!Options.RequireScanBeforeAvailable)
            return Options.DefaultAvailability;

        if (MalwareScanner is NullFileMalwareScanner) {
            Logger.LogWarning("RequireScanBeforeAvailable is set but IFileMalwareScanner is not configured; files are marked Available without scanning.");
            return FileAvailability.Available;
        }

        const long maxInline = 64L * 1024 * 1024;
        if (length > maxInline) {
            Logger.LogWarning("File length {Length} exceeds inline malware scan threshold; marking PendingScan.", length);
            return FileAvailability.PendingScan;
        }

#if NET5_0_OR_GREATER
        var bytes = await File.ReadAllBytesAsync(filePath, ct).ConfigureAwait(false);
#else
        ct.ThrowIfCancellationRequested();
        var bytes = File.ReadAllBytes(filePath);
#endif
        return await DetermineAvailabilityAfterScanningPlaintextAsync(bytes, contentType, originalFileName, ct).ConfigureAwait(false);
    }

    protected async Task<FileStoreResult> ProcessAndSaveStreamAsync(
        Stream inputStream,
        Guid fileId,
        string originalFileName,
        long originalSize,
        bool compress,
        bool encrypt,
        string? keyId,
        string? normalizedPathPrefix,
        DateTime timestamp,
        int chunkSize,
        string? contentType,
        string? tenantId,
        FileAvailability availability,
        CancellationToken ct)
    {
        contentType = ResolveStoredContentType(contentType, originalFileName);
        // Single-pass streaming pipeline: input -> compression -> encryption -> storage
        long? compressedSize = null;
        byte[]? compressedHash = null;
        CompressionAlgorithm? compressionAlgorithm = null;
        EncryptionAlgorithm? dataEncryptionKeyAlgorithm = null;
        EncryptionAlgorithm? keyEncryptionKeyAlgorithm = null;
        long? encryptedSize = null;
        byte[]? encryptedHash = null;
        byte[]? encryptedDataEncryptionKey = null;
        string? dataEncryptionKeyId = null;
        string? dataEncryptionKeyVersion = null;
        byte[]? keyEncryptionKeySalt = null;
        byte? dekKeyMaterialBytes = null;
        var sourceFileName = fileId.ToString();
        var fileExtension = "";
        long finalSize;
        byte[]? sourceFileHash;
        byte[]? originalHash;
        var processingStream = inputStream;

        // Determine file extension early
        if (encrypt && TwoKeyEncryptionService != null)
            fileExtension = TwoKeyEncryptionService.FileExtension;
        else if (compress && CompressionService != null)
            fileExtension = CompressionService.FileExtension;

        // Pipeline path: compress -> pipe -> encrypt when both enabled and duplicate detection is off
        if (compress && encrypt && !Options.EnableDuplicateDetection) {
            OperationHelpers.ThrowIfNullOrWhiteSpace(
                keyId, "Encryption was requested but no keyId was provided. When encrypting files, you must provide a keyId parameter to identify the encryption key to use.");

            var pipelineOutputStream = await CreateOutputStreamAsync(fileId, fileExtension, normalizedPathPrefix, ct).ConfigureAwait(false);
            try {
                var pipelineResult = await SaveWithCompressEncryptPipelineAsync(
                        inputStream, pipelineOutputStream, fileId, keyId, normalizedPathPrefix, chunkSize, originalSize, ct)
                    .ConfigureAwait(false);

                originalHash = pipelineResult.OriginalHash;
                fileExtension = pipelineResult.FileExtension;
                sourceFileName = pipelineResult.SourceFileName;
                compressedSize = pipelineResult.CompressedSize;
                compressedHash = pipelineResult.CompressedHash;
                compressionAlgorithm = pipelineResult.CompressionAlgorithm;
                encryptedHash = pipelineResult.EncryptedHash;
                encryptedDataEncryptionKey = pipelineResult.EncryptedDataEncryptionKey;
                dataEncryptionKeyId = pipelineResult.DataEncryptionKeyId;
                dataEncryptionKeyVersion = pipelineResult.DataEncryptionKeyVersion;
                keyEncryptionKeySalt = pipelineResult.KeyEncryptionKeySalt;
                encryptedSize = pipelineResult.EncryptedSize;
                dataEncryptionKeyAlgorithm = pipelineResult.DataEncryptionKeyAlgorithm;
                keyEncryptionKeyAlgorithm = pipelineResult.KeyEncryptionKeyAlgorithm;
                dekKeyMaterialBytes = pipelineResult.DekKeyMaterialBytes;
                sourceFileHash = encryptedHash;
                finalSize = await GetStorageSizeAsync(fileId, fileExtension, normalizedPathPrefix, ct).ConfigureAwait(false);
                var metadata = new FileStoreResult(
                    fileId, originalFileName, originalSize, originalHash!, sourceFileName, finalSize, sourceFileHash!, compress, compressionAlgorithm,
                    compressedSize, compressedHash, encrypt, dataEncryptionKeyAlgorithm, keyEncryptionKeyAlgorithm, encryptedSize, encryptedHash, encryptedDataEncryptionKey,
                    dataEncryptionKeyId, dataEncryptionKeyVersion, keyEncryptionKeySalt, timestamp, normalizedPathPrefix, Options.HashAlgorithm, contentType, tenantId,
                    availability, dekKeyMaterialBytes);

                await MetadataService.SaveMetadataAsync(fileId, metadata, ct).ConfigureAwait(false);
                FileSaved?.Invoke(this, new(fileId, metadata, originalSize, finalSize, compress, encrypt));
                return metadata;
            }
            finally {
                await DisposeStreamAsync(pipelineOutputStream).ConfigureAwait(false);
            }
        }

        var outputStream = await CreateOutputStreamAsync(fileId, fileExtension, normalizedPathPrefix, ct).ConfigureAwait(false);
        try {
            // Compute original hash first (needed for duplicate check)
            var hashAlg = Options.HashAlgorithm;
            using var originalHashAlgo = hashAlg.Create();

            // Compression stage
            using var intermediateStream = CreateSequentialStagingStream(originalSize, compress);
            if (compress) {
                OperationHelpers.ThrowIfNull(
                    CompressionService,
                    "Compression was requested but no compression service is configured. Provide an ICompressionService instance when creating FileStorageService.");

                using var inputHashStream = new HashingStream(processingStream, originalHashAlgo);
                using var compressedHashAlgo = hashAlg.Create();
                using var compressedHashStream = new HashingStream(intermediateStream, compressedHashAlgo);
                await CompressionService.CompressAsync(inputHashStream, compressedHashStream, chunkSize, ct).ConfigureAwait(false);
                await compressedHashStream.FlushAsync(ct).ConfigureAwait(false);
                await inputHashStream.FlushAsync(ct).ConfigureAwait(false);
                compressedSize = intermediateStream.Length;
                compressedHash = compressedHashStream.GetHash();
                originalHash = inputHashStream.GetHash();
                fileExtension = CompressionService.FileExtension;
                sourceFileName += fileExtension;
                compressionAlgorithm = DetermineCompressionAlgorithm(fileExtension);
                processingStream = intermediateStream;
                processingStream.Position = 0;
                Logger.LogDebug("Compressed file {FileId}: {OriginalSize} -> {CompressedSize} bytes", fileId, originalSize, compressedSize);
            }
            else {
                // No compression - compute original hash while copying to intermediate
                using var outputHashStream = new HashingStream(intermediateStream, originalHashAlgo);
                await processingStream.CopyToAsync(outputHashStream, CopyToBufferSizeBytes, ct).ConfigureAwait(false);
                await outputHashStream.FlushAsync(ct).ConfigureAwait(false);
                originalHash = outputHashStream.GetHash();
                processingStream = intermediateStream;
                processingStream.Position = 0;
            }

            // Check for duplicate AFTER computing hash
            if (Options.EnableDuplicateDetection) {
                var existingMetadata = await MetadataService.FindByHashAsync(originalHash, ct).ConfigureAwait(false);
                if (existingMetadata != null) {
                    var duplicateResult = await HandleDuplicateAsync(existingMetadata, fileId, originalSize, normalizedPathPrefix, ct).ConfigureAwait(false);
                    if (duplicateResult != null)
                        return duplicateResult;

                    if (Options.DuplicateStrategy == DuplicateHandlingStrategy.Overwrite) {
                        fileId = existingMetadata.Id;
                        await DisposeStreamAsync(outputStream).ConfigureAwait(false);
                        outputStream = await CreateOutputStreamAsync(fileId, fileExtension, normalizedPathPrefix, ct).ConfigureAwait(false);
                    }
                }
            }

            // Encryption stage
            if (encrypt) {
                OperationHelpers.ThrowIfNull(
                    TwoKeyEncryptionService,
                    "Encryption was requested but no encryption service is configured. Provide an ITwoKeyEncryptionService instance when creating FileStorageService.");

                OperationHelpers.ThrowIfNullOrWhiteSpace(
                    keyId, "Encryption was requested but no keyId was provided. When encrypting files, you must provide a keyId parameter to identify the encryption key to use.");

                using var encryptedHashAlgo = hashAlg.Create();
                using var encryptedHashStream = new HashingStream(outputStream, encryptedHashAlgo);
                await TwoKeyEncryptionService.EncryptToStreamAsync(processingStream, encryptedHashStream, keyId, null, chunkSize, ct).ConfigureAwait(false);
                await encryptedHashStream.FlushAsync(ct).ConfigureAwait(false);
                await outputStream.FlushAsync(ct).ConfigureAwait(false);
                encryptedHash = encryptedHashStream.GetHash();
                await DisposeStreamAsync(outputStream).ConfigureAwait(false);
                outputStream = null; // Prevent double disposal
                fileExtension = TwoKeyEncryptionService.FileExtension;

                // Extract encrypted DEK from output - read from the file we just wrote
                var headerInfo = await ExtractEncryptionHeaderAsync(fileId, fileExtension, normalizedPathPrefix, ct).ConfigureAwait(false);
                encryptedDataEncryptionKey = headerInfo.EncryptedDataEncryptionKey;
                dataEncryptionKeyId = headerInfo.DataEncryptionKeyId ?? keyId;
                dataEncryptionKeyVersion = headerInfo.DataEncryptionKeyVersion;
                dekKeyMaterialBytes = headerInfo.DekKeyMaterialBytes;
                if (TwoKeyEncryptionService != null && dataEncryptionKeyVersion != null)
                    keyEncryptionKeySalt = TwoKeyEncryptionService.GetSaltForVersion(dataEncryptionKeyId, dataEncryptionKeyVersion);

                encryptedSize = await GetStorageSizeAsync(fileId, fileExtension, normalizedPathPrefix, ct).ConfigureAwait(false);
                dataEncryptionKeyAlgorithm = EncryptionServiceExtensions.DetermineDekAlgorithm(TwoKeyEncryptionService);
                keyEncryptionKeyAlgorithm = EncryptionServiceExtensions.DetermineKekAlgorithm(TwoKeyEncryptionService);
                sourceFileName = fileId + fileExtension;
                sourceFileHash = encryptedHash;
                Logger.LogDebug("Encrypted file {FileId} using two-key encryption: {Size} bytes, KeyVersion: {KeyVersion}", fileId, encryptedSize, dataEncryptionKeyVersion);
            }
            else {
                // No encryption - write directly to output
                processingStream.Position = 0;
                await processingStream.CopyToAsync(outputStream, CopyToBufferSizeBytes, ct).ConfigureAwait(false);
                await outputStream.FlushAsync(ct).ConfigureAwait(false);
                await DisposeStreamAsync(outputStream).ConfigureAwait(false);
                outputStream = null; // Prevent double disposal
                sourceFileHash = compress ? compressedHash : originalHash;
            }

            finalSize = await GetStorageSizeAsync(fileId, fileExtension, normalizedPathPrefix, ct).ConfigureAwait(false);

            // Ensure originalHash is computed
            inputStream.Position = 0;
            using var hashAlgoForCompute = hashAlg.Create();
#if NET5_0_OR_GREATER
            originalHash = await hashAlgoForCompute.ComputeHashAsync(inputStream, ct).ConfigureAwait(false);
#else
            const int hashBufferSize = 81920;
            var hashBuffer = new byte[hashBufferSize];
            int hashRead;
            while ((hashRead = await inputStream.ReadAsync(hashBuffer, 0, hashBufferSize, ct).ConfigureAwait(false)) > 0)
                hashAlgoForCompute.TransformBlock(hashBuffer, 0, hashRead, null, 0);
            hashAlgoForCompute.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            originalHash = hashAlgoForCompute.Hash!;
#endif
            
            var metadata = new FileStoreResult(
                fileId, originalFileName, originalSize, originalHash, sourceFileName, finalSize, sourceFileHash ?? originalHash, compress,
                compressionAlgorithm, compressedSize, compressedHash, encrypt, dataEncryptionKeyAlgorithm, keyEncryptionKeyAlgorithm, encryptedSize, encryptedHash,
                encryptedDataEncryptionKey, dataEncryptionKeyId, dataEncryptionKeyVersion, keyEncryptionKeySalt, timestamp, normalizedPathPrefix, hashAlg, contentType, tenantId,
                availability, dekKeyMaterialBytes);

            // Save metadata using metadata service
            await MetadataService.SaveMetadataAsync(fileId, metadata, ct).ConfigureAwait(false);

            // Raise FileSaved event
            FileSaved?.Invoke(this, new(fileId, metadata, originalSize, finalSize, compress, encrypt));
            return metadata;
        }
        finally {
            if (outputStream != null)
                await DisposeStreamAsync(outputStream).ConfigureAwait(false);
        }
    }

    /// <summary>Sets stored <see cref="FileStoreResult.ContentType" /> from a known extension (via <see cref="FileTypeInfo.FromFilePath" />), then a known declared MIME, then the declared value, then <see cref="FileTypeInfo.Unknown" />.</summary>
    protected static string ResolveStoredContentType(string? declaredContentType, string? originalFileName)
    {
        var fromName = FileTypeInfo.FromFilePath(originalFileName);
        if (fromName != FileTypeInfo.Unknown)
            return fromName.MimeType;

        var fromDeclaredMime = FileTypeInfo.FromMimeType(declaredContentType);
        if (fromDeclaredMime != FileTypeInfo.Unknown)
            return fromDeclaredMime.MimeType;

        if (!string.IsNullOrWhiteSpace(declaredContentType))
            return declaredContentType.Trim();

        return FileTypeInfo.Unknown.MimeType;
    }

    protected static CompressionAlgorithm? DetermineCompressionAlgorithm(string fileExtension)
        => fileExtension switch {
            _ when fileExtension == GZipExtension => CompressionAlgorithm.GZip,
#if !NETSTANDARD2_0
            _ when fileExtension == BrotliExtension => CompressionAlgorithm.Brotli,
            _ when fileExtension == ZLibExtension => CompressionAlgorithm.ZLib,
#endif
            _ when fileExtension == DeflateExtension => CompressionAlgorithm.Deflate,
            _ when fileExtension == SnappierExtension => CompressionAlgorithm.Snappier,
            _ when fileExtension == ZstdSharpExtension => CompressionAlgorithm.ZstdSharp,
            _ => null
        };

    protected static byte[] ComputeHash(byte[] data, HashAlgorithm algorithm = HashAlgorithm.Sha256)
    {
        using var algo = algorithm.Create();
        return algo.ComputeHash(data);
    }

    protected static bool ByteArraysEqual(byte[]? a, byte[]? b)
    {
        if (a == null || b == null)
            return a == b;

        if (a.Length != b.Length)
            return false;

        for (var i = 0; i < a.Length; i++) {
            if (a[i] != b[i])
                return false;
        }

        return true;
    }

    /// <summary>
    /// Buffer for the sequential compress/hash/encrypt path. Large or compressed payloads use a temp file so we do not hold the full plaintext or compressed blob in memory.
    /// </summary>
    private static Stream CreateSequentialStagingStream(long originalSize, bool compress)
    {
        const long largePlainThresholdBytes = 64L * 1024 * 1024;
        const long compressStagingThresholdBytes = 16L * 1024 * 1024;
        var useTempFile = originalSize > largePlainThresholdBytes || (compress && originalSize > compressStagingThresholdBytes);
        if (!useTempFile) {
            if (originalSize <= 0)
                return new MemoryStream();

            var cap = (int)Math.Min(originalSize, int.MaxValue);
            return new MemoryStream(capacity: cap);
        }

        var path = Path.Combine(Path.GetTempPath(), $"lyo-fs-staging-{Guid.NewGuid():N}.tmp");
        try {
            return new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.DeleteOnClose);
        }
        catch {
            TryDeleteStagingFile(path);
            throw;
        }
    }

    private static void TryDeleteStagingFile(string path)
    {
        try {
            File.Delete(path);
        }
        catch {
            // best effort
        }
    }

    private sealed record CompressEncryptPipelineResult(
        byte[]? OriginalHash,
        string FileExtension,
        string SourceFileName,
        long CompressedSize,
        byte[]? CompressedHash,
        CompressionAlgorithm? CompressionAlgorithm,
        byte[]? EncryptedHash,
        byte[]? EncryptedDataEncryptionKey,
        string? DataEncryptionKeyId,
        string? DataEncryptionKeyVersion,
        byte[]? KeyEncryptionKeySalt,
        long EncryptedSize,
        EncryptionAlgorithm? DataEncryptionKeyAlgorithm,
        EncryptionAlgorithm? KeyEncryptionKeyAlgorithm,
        byte DekKeyMaterialBytes);

    /// <summary>Information extracted from encryption header.</summary>
    protected sealed record EncryptionHeaderInfo(byte[]? EncryptedDataEncryptionKey, string? DataEncryptionKeyId, string? DataEncryptionKeyVersion, byte DekKeyMaterialBytes);
}