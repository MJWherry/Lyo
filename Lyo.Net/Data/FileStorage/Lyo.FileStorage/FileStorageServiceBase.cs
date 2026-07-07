using System.Diagnostics;
using System.IO.Pipelines;
using Lyo.Common.Extensions;
using Lyo.Common.Records;
using Lyo.Compression;
using Lyo.Compression.Models;
using Lyo.Encryption;
using Lyo.Encryption.Extensions;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.Hashing;
using Lyo.Health;
using Lyo.Metrics;
using Lyo.Streams;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using HashAlgorithm = Lyo.FileMetadataStore.Models.HashAlgorithm;

namespace Lyo.FileStorage;

/// <summary>
/// Base class for filesystem, object-storage, or blob-backed file operations. Implements <see cref="IFileStorageService" /> with shared pipelines for compression,
/// encryption, malware policy, auditing, metrics, and DEK maintenance. Derived types supply storage I/O via <see cref="CreateOutputStreamAsync" />,
/// <see cref="ReadFromStorageAsync" />, and related abstract members.
/// </summary>
/// <remarks>
/// <para>
/// Members are grouped with <c>#region</c> slices (core wiring, save, retrieve, delete/metadata, DEK, direct upload/copy façade) within this single compilation unit. Heavy
/// logic delegates to internal types wired in the ctor: <see cref="FileStorageStreamingPipelines" />, <see cref="FileStorageDekOperations" />, and
/// <see cref="PlainDirectUploadCoordinator" />, which depend on narrow internal interfaces implemented explicitly by this class (<see cref="IFileStoragePhysicalIo" />,
/// <see cref="IFileAuditPublisher" />, etc.).
/// </para>
/// </remarks>
public abstract class FileStorageServiceBase
    : IFileStorageService, IDisposable, IFileStoragePhysicalIo, IFileAuditPublisher, IFileStorageMetadataNormalization, IFileStorageMetadataLookup
{
#region Core

    private const int CopyToBufferSizeBytes = 81920;

    private readonly FileStorageStreamingPipelines _streamingPipelines;
    private readonly FileStorageDekOperations _dekOperations;
    private readonly PlainDirectUploadCoordinator _plainDirectUpload;

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

    /// <summary>Maps logical metric keys to externally emitted metric names; derived implementations may mutate this dictionary.</summary>
    protected Dictionary<string, string> MetricNames { get; }

    /// <summary>Initializes a new instance with optional compression, encryption, metrics, auditing, and content-policy collaborators.</summary>
    /// <param name="options">Baseline configuration such as hashing, quotas, duplicate handling, and health-check mode.</param>
    /// <param name="metadataService">Backing store for <see cref="FileStoreResult" /> persistence.</param>
    /// <param name="logger">Optional logger for operational diagnostics.</param>
    /// <param name="compressionService">Optional service when saves may compress payloads.</param>
    /// <param name="twoKeyEncryptionService">Optional service when saves may encrypt payloads.</param>
    /// <param name="metrics">Optional metrics sink; defaults to null metrics.</param>
    /// <param name="operationContextAccessor">Optional ambient tenant / actor resolution for auditing.</param>
    /// <param name="auditHandlers">Optional handlers subscribed through the publication pipeline alongside <see cref="FileAuditOccurred" />.</param>
    /// <param name="contentPolicy">Optional content policy validator; defaults to <see cref="DefaultFileContentPolicy" />.</param>
    /// <param name="malwareScanner">Optional scanner when <see cref="FileStorageServiceBaseOptions.RequireScanBeforeAvailable" /> is enforced.</param>
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
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(metadataService);
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
        _streamingPipelines = new(this, CompressionService, TwoKeyEncryptionService, Options, Logger, CopyToBufferSizeBytes);
        _dekOperations = new(MetadataService, TwoKeyEncryptionService, OperationContextAccessor, Logger, Options, this, this, CopyToBufferSizeBytes);
        _plainDirectUpload = new(ContentPolicy, MalwareScanner, MetadataService, OperationContextAccessor, Options, Logger, this, this, this, this, CopyToBufferSizeBytes);
    }

    /// <summary>Connectivity-only probe implementations call when options use lightweight health-check mode.</summary>
    protected abstract Task<HealthResult> CheckHealthLightweightAsync(CancellationToken ct);

    /// <inheritdoc />
    public virtual void Dispose()
    {
        if (!Disposed)
            Disposed = true;
    }

    public virtual string HealthCheckName => "filestorage";

    public virtual async Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        if (Options.HealthCheckMode == FileStorageHealthCheckMode.Lightweight)
            return await CheckHealthLightweightAsync(ct).ConfigureAwait(false);

        var sw = Stopwatch.StartNew();
        try {
            var testData = Guid.NewGuid().ToByteArray();
            var result = await SaveFileAsync(testData, "health-check.tmp", false, false, null, ".lyo-health", null, null, null, ct).ConfigureAwait(false);
            var fileId = result.Id;
            var retrieved = await GetFileAsync(fileId, ct: ct).ConfigureAwait(false);
            await DeleteFileAsync(fileId, ct: ct).ConfigureAwait(false);
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

    public event EventHandler<FileSavedResult>? FileSaved;

    public event EventHandler<FileRetrievedResult>? FileRetrieved;

    public event EventHandler<FileDeletedResult>? FileDeleted;

    public event EventHandler<FileMetadataRetrievedResult>? FileMetadataRetrieved;

    public event EventHandler<FileAuditEventArgs>? FileAuditOccurred;

    /// <summary>Raises <see cref="FileSaved" /> from subclasses that bypass <see cref="SaveFromStreamAsync" /> (e.g. server-side multipart finalize paths).</summary>
    protected void RaiseFileSaved(Guid fileId, FileStoreSnapshot snapshot, long originalSize, long finalSize, bool compress, bool encrypt)
        => FileSaved?.Invoke(this, new(fileId, snapshot, originalSize, finalSize, compress, encrypt));

    /// <summary>
    /// Raises <see cref="FileMetadataRetrieved" /> from subclasses (e.g. override of <c>GetFileMetadataAsync</c>) so listeners observe both metadata-only and payload-bearing
    /// reads.
    /// </summary>
    protected void RaiseFileMetadataRetrieved(Guid fileId, FileStoreSnapshot snapshot) => FileMetadataRetrieved?.Invoke(this, new(fileId, snapshot));

    public virtual Task<string> GetPreSignedReadUrlAsync(Guid fileId, TimeSpan? expiration = null, string? pathPrefix = null, CancellationToken ct = default)
        => GetPreSignedReadUrlAsync(fileId, expiration, pathPrefix, null, ct);

    public virtual Task<string> GetPreSignedReadUrlAsync(Guid fileId, TimeSpan? expiration, string? pathPrefix, PreSignedReadUrlOptions? urlResponseOptions, CancellationToken ct)
        => Task.FromException<string>(
            new NotSupportedException("Pre-signed read URLs are not supported by this storage backend. Use Blob or AWS S3 file storage implementations."));

    public virtual Task<DirectUploadBeginResult> BeginDirectUploadAsync(DirectUploadBeginRequest request, CancellationToken ct = default)
        => Task.FromException<DirectUploadBeginResult>(new NotSupportedException("Direct uploads are not supported by this backend."));

    public virtual Task<FileStoreResult> CompleteDirectUploadAsync(Guid fileId, DirectUploadCompleteRequest? completeRequest = null, CancellationToken ct = default)
        => Task.FromException<FileStoreResult>(new NotSupportedException("Direct uploads are not supported by this backend."));

    public virtual Task<FileStoreResult> CopyFileAsync(Guid sourceFileId, CopyFileRequest? request = null, CancellationToken ct = default)
        => Task.FromException<FileStoreResult>(new NotSupportedException("Server-side copies are not supported by this backend."));

    /// <summary>Async-disposes a stream using <see cref="Stream.DisposeAsync" /> when available, otherwise synchronously disposes.</summary>
    internal static Task DisposeStreamAsync(Stream? stream) => FileStorageStreamingPipelines.DisposeStreamAsync(stream);

    /// <summary>Returns <paramref name="explicitTenantId" /> when set; otherwise resolves tenant from operation context.</summary>
    protected string? ResolveTenantId(string? explicitTenantId) => explicitTenantId ?? OperationContextAccessor.Current?.TenantId;

    /// <summary>
    /// Publishes a file audit event to registered handlers and the <see cref="FileAuditOccurred" /> event. <see cref="FileAuditEvent.Error" /> is sanitized via
    /// <see cref="SanitizeAuditError" />, and the operation-context correlation id is back-filled when the caller did not provide one, so unstructured exception text never leaks newlines
    /// or oversized payloads into audit sinks and downstream consumers always see a correlation id when one is in scope.
    /// </summary>
    protected Task RaiseFileAuditAsync(FileAuditEvent auditEvent, CancellationToken ct)
    {
        var enriched = auditEvent;
        if (enriched.Error is not null)
            enriched = enriched with { Error = SanitizeAuditError(enriched.Error) };

        if (enriched.CorrelationId is null && OperationContextAccessor.Current?.CorrelationId is { } ctxCorrelation)
            enriched = enriched with { CorrelationId = ctxCorrelation };

        return FileAuditPublication.PublishAsync(
            _auditHandlers, FileAuditOccurred, this, enriched, ct, Logger, Metrics, MetricNames[nameof(Constants.Metrics.AuditAppendFailed)], Options.ThrowOnAuditFailure);
    }

    /// <summary>Produces the default map from logical metric property names to published metric identifiers.</summary>
    protected Dictionary<string, string> CreateMetricNamesDictionary()
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
            { nameof(Constants.Metrics.FileStoragePreSignedUrlGenerated), Constants.Metrics.FileStoragePreSignedUrlGenerated },
            { nameof(Constants.Metrics.FileStoragePreSignedUrlGenerationFailed), Constants.Metrics.FileStoragePreSignedUrlGenerationFailed },
            { nameof(Constants.Metrics.AuditAppendFailed), Constants.Metrics.AuditAppendFailed }
        };

    /// <summary>Opens a writable stream for persisting ciphertext or compressed payloads for <paramref name="fileId" />.</summary>
    protected abstract Task<Stream> CreateOutputStreamAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct);

    /// <summary>Returns the persisted byte length including format-specific headers or wrappers.</summary>
    protected abstract Task<long> GetStorageSizeAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct);

    /// <summary>Reads raw storage bytes associated with metadata; implementations return <see langword="null" /> when the blob is absent.</summary>
    protected abstract Task<Stream?> ReadFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct);

    /// <summary>Deletes the backing object referenced by metadata and returns whether an object was removed.</summary>
    protected abstract Task<bool> DeleteFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct);

    /// <summary>Parses header material after encryption so metadata can persist DEKs, versions, and key identifiers.</summary>
    protected abstract Task<EncryptionHeaderInfo> ExtractEncryptionHeaderAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct);

    /// <summary>Rewrites the encryption header envelope after migrating or rotating key-encryption-keys.</summary>
    protected abstract Task UpdateFileHeaderAsync(Guid fileId, string? pathPrefix, string targetKeyId, string targetKeyVersion, byte[] newEncryptedDek, CancellationToken ct);

    /// <summary>
    /// Sanitizes a tenant or logical prefix for storage keys: delegates to <see cref="FileHelpers.NormalizePathPrefix" /> and collapses an empty result back to
    /// <see langword="null" /> so persisted metadata distinguishes "no prefix supplied" from "explicit empty string".
    /// </summary>
    protected static string? NormalizePathPrefix(string? pathPrefix)
    {
        var normalized = FileHelpers.NormalizePathPrefix(pathPrefix);
        return normalized.Length == 0 ? null : normalized;
    }

    /// <summary>
    /// Centralized path-prefix safety validator. Delegates to <see cref="FileHelpers.ThrowIfPathPrefixTraversal" /> so every save / direct-upload entry point shares the same
    /// traversal rejection rules (segments equal to <c>..</c>, doubled separators, embedded <c>\0</c>) without each backend re-implementing them.
    /// </summary>
    /// <exception cref="ArgumentException">When the prefix contains a traversal pattern.</exception>
    public static void ValidatePathPrefix(string? pathPrefix) => FileHelpers.ThrowIfPathPrefixTraversal(pathPrefix);

    /// <summary>Deletes partially written payloads when uploads fail before metadata commit succeeds.</summary>
    protected abstract Task CleanupPartialFileAsync(Guid fileId, string? pathPrefix, CancellationToken ct);

    /// <summary>Determines MIME type persisted in metadata from original filename extensions, declared MIME hints, then fallback unknown.</summary>
    protected static string ResolveStoredContentType(string? declaredContentType, string? originalFileName)
    {
        var fromName = FileTypeInfo.FromFilePath(originalFileName);
        if (fromName != FileTypeInfo.Unknown)
            return fromName.MimeType;

        var fromDeclaredMime = FileTypeInfo.FromMimeType(declaredContentType);
        if (fromDeclaredMime != FileTypeInfo.Unknown)
            return fromDeclaredMime.MimeType;

        return !declaredContentType.IsNullOrWhitespace() ? declaredContentType.Trim() : FileTypeInfo.Unknown.MimeType;
    }

    /// <summary>
    /// Maps a file extension to the registered compression algorithm used in <see cref="FileStoreResult" />. Returns <see langword="null" /> for unknown extensions; recognition
    /// is dynamic and only covers algorithms whose addon assemblies have been loaded (typically via <c>services.Add{Algo}Compressor()</c>).
    /// </summary>
    protected internal static CompressionAlgorithm? DetermineCompressionAlgorithm(string fileExtension) => CompressionAlgorithm.TryFromExtension(fileExtension);

    /// <summary>Computes a cryptographic digest for byte arrays using the configured <see cref="Lyo.FileMetadataStore.Models.HashAlgorithm" />.</summary>
    protected static byte[] ComputeHash(byte[] data, HashAlgorithm algorithm = HashAlgorithm.Sha256)
    {
        using var algo = algorithm.Create();
        return algo.ComputeHash(data);
    }

    /// <summary>Determines whether two hash buffers match with explicit null semantics.</summary>
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

    /// <summary>Dek-material and key-identifying fields read from freshly written ciphertext headers.</summary>
    /// <param name="EncryptedDataEncryptionKey">Envelope-encrypted plaintext DEK bytes from the serialized header.</param>
    /// <param name="DataEncryptionKeyId">Logical KMS or keystore identifier associated with encryption.</param>
    /// <param name="DataEncryptionKeyVersion">Key version paired with <paramref name="DataEncryptionKeyId" />.</param>
    /// <param name="DekKeyMaterialBytes">DEK-material width metadata required by the installed encryption codec.</param>
    protected internal sealed record EncryptionHeaderInfo(
        byte[]? EncryptedDataEncryptionKey,
        string? DataEncryptionKeyId,
        string? DataEncryptionKeyVersion,
        byte DekKeyMaterialBytes);

    Task<Stream?> IFileStoragePhysicalIo.ReadFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct) => ReadFromStorageAsync(fileId, pathPrefix, ct);

    Task<Stream> IFileStoragePhysicalIo.CreateOutputStreamAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
        => CreateOutputStreamAsync(fileId, extension, pathPrefix, ct);

    Task<long> IFileStoragePhysicalIo.GetStorageSizeAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
        => GetStorageSizeAsync(fileId, extension, pathPrefix, ct);

    Task<EncryptionHeaderInfo> IFileStoragePhysicalIo.ExtractEncryptionHeaderAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
        => ExtractEncryptionHeaderAsync(fileId, extension, pathPrefix, ct);

    Task IFileStoragePhysicalIo.UpdateFileHeaderAsync(Guid fileId, string? pathPrefix, string targetKeyId, string targetKeyVersion, byte[] newEncryptedDek, CancellationToken ct)
        => UpdateFileHeaderAsync(fileId, pathPrefix, targetKeyId, targetKeyVersion, newEncryptedDek, ct);

    Task IFileAuditPublisher.PublishAuditAsync(FileAuditEvent auditEvent, CancellationToken ct) => RaiseFileAuditAsync(auditEvent, ct);

    string? IFileStorageMetadataNormalization.ResolveTenantId(string? explicitTenantId) => ResolveTenantId(explicitTenantId);

    string IFileStorageMetadataNormalization.ResolveStoredContentType(string? declaredContentType, string? originalFileName)
        => ResolveStoredContentType(declaredContentType, originalFileName);

    string? IFileStorageMetadataNormalization.NormalizePathPrefix(string? pathPrefix) => NormalizePathPrefix(pathPrefix);

    Task<FileStoreResult> IFileStorageMetadataLookup.GetMetadataForStorageAsync(Guid fileId, CancellationToken ct) => GetMetadataAsync(fileId, ct);

#endregion

#region Save

    /// <inheritdoc />
    /// <remarks>
    /// Thin wrapper around <see cref="SaveFromStreamAsync" /> that exposes the byte[] surface for legacy callers and tests. All hashing, scanning, dedup, compression,
    /// encryption, audit, and cleanup logic lives in the stream path so the two entry points cannot diverge.
    /// </remarks>
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
        ArgumentHelpers.ThrowIfNullOrEmpty(data);
        using var ms = new MemoryStream(data, false);
        return await SaveFromStreamAsync(ms, data.LongLength, originalFileName, compress, encrypt, keyId, pathPrefix, chunkSize, contentType, tenantId, null, null, ct)
            .ConfigureAwait(false);
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
        ArgumentHelpers.ThrowIfFileNotFound(filePath);
        ValidatePathPrefix(pathPrefix);
        var normalizedPathPrefix = NormalizePathPrefix(pathPrefix);
        var fileInfo = new FileInfo(filePath);
        var originalSize = fileInfo.Length;
        ArgumentHelpers.ThrowIfZero(originalSize);
        var actualOriginalFileName = originalFileName ?? Path.GetFileName(filePath);
        Logger.LogDebug(
            "Saving file from path {FilePath} to storage, Size: {Size} bytes, Compress: {Compress}, Encrypt: {Encrypt}, PathPrefix: {PathPrefix}", filePath, originalSize, compress,
            encrypt, normalizedPathPrefix ?? "none");

        var resolvedTenant = ResolveTenantId(tenantId);
        var resolvedContentType = ResolveStoredContentType(contentType, actualOriginalFileName);
        Guid? createdFileId = null;
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
            createdFileId = fileId;
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
                        FileAuditEventType.Save, DateTime.UtcNow, createdFileId, resolvedTenant, OperationContextAccessor.Current?.ActorId, keyId, null, FileAuditOutcome.Failure,
                        ex.Message), ct)
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
        ArgumentHelpers.ThrowIfNull(input);
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.SaveDuration)]);
        var sw = Stopwatch.StartNew();
        var resolvedTenant = ResolveTenantId(tenantId);
        var resolvedContentType = ResolveStoredContentType(contentType, originalFileName);
        Guid? createdFileId = null;
        try {
            await ContentPolicy.ValidateAsync(
                    new() {
                        ByteLength = declaredLength,
                        ContentType = resolvedContentType,
                        OriginalFileName = originalFileName,
                        TenantId = resolvedTenant
                    }, ct)
                .ConfigureAwait(false);

            ValidatePathPrefix(pathPrefix);
            var normalizedPathPrefix = NormalizePathPrefix(pathPrefix);
            var id = fileId ?? Guid.NewGuid();
            createdFileId = id;
            var timestamp = DateTime.UtcNow;
            var effectiveChunkSize = chunkSize ?? StreamChunkSizeHelper.DetermineChunkSize(declaredLength);

            // If scanning is required and the caller did not supply a prior scan result, perform the scan inline.
            // For non-seekable inputs we spool to a temp file so the scanner and the processor can each read from a fresh stream.
            FileAvailability availability;
            FileStoreResult result;
            if (availabilityOverride.HasValue) {
                availability = availabilityOverride.Value;
                result = await ProcessAndSaveStreamAsync(
                        input, id, originalFileName ?? id.ToString(), declaredLength, compress, encrypt, keyId, normalizedPathPrefix, timestamp, effectiveChunkSize, contentType,
                        resolvedTenant, availability, ct)
                    .ConfigureAwait(false);
            }
            else {
                EnsureScanRequirementSatisfied();
                if (!Options.RequireScanBeforeAvailable) {
                    availability = Options.DefaultAvailability;
                    result = await ProcessAndSaveStreamAsync(
                            input, id, originalFileName ?? id.ToString(), declaredLength, compress, encrypt, keyId, normalizedPathPrefix, timestamp, effectiveChunkSize,
                            contentType,
                            resolvedTenant, availability, ct)
                        .ConfigureAwait(false);
                }
                else {
                    // Scan-required path: spool the input to a temp file so we can scan and then re-read for processing.
                    var spoolPath = Path.Combine(Path.GetTempPath(), $"lyo-fs-scan-{id:N}.tmp");
                    try {
#if NETSTANDARD2_0
                        using (var spoolWrite = new FileStream(spoolPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
                            await input.CopyToAsync(spoolWrite, CopyToBufferSizeBytes, ct).ConfigureAwait(false);
#else
                        await using (var spoolWrite = new FileStream(spoolPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
                            await input.CopyToAsync(spoolWrite, CopyToBufferSizeBytes, ct).ConfigureAwait(false);
#endif
                        availability = await DetermineAvailabilityAfterScanningFileAsync(spoolPath, new FileInfo(spoolPath).Length, resolvedContentType, originalFileName, ct)
                            .ConfigureAwait(false);
#if NETSTANDARD2_0
                        using var spoolRead = File.OpenRead(spoolPath);
#else
                        await using var spoolRead = File.OpenRead(spoolPath);
#endif
                        result = await ProcessAndSaveStreamAsync(
                                spoolRead, id, originalFileName ?? id.ToString(), declaredLength, compress, encrypt, keyId, normalizedPathPrefix, timestamp, effectiveChunkSize,
                                contentType, resolvedTenant, availability, ct)
                            .ConfigureAwait(false);
                    }
                    finally {
                        try {
                            if (File.Exists(spoolPath))
                                File.Delete(spoolPath);
                        }
                        catch (Exception ex) {
                            Logger.LogDebug(ex, "Best-effort spool cleanup failed for {Path}", spoolPath);
                        }
                    }
                }
            }

            sw.Stop();
            Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.SaveSuccess)]);
            Metrics.RecordGauge(MetricNames[nameof(Constants.Metrics.SaveFileSizeBytes)], result.SourceFileSize);
            Metrics.RecordHistogram(MetricNames[nameof(Constants.Metrics.SaveDurationMs)], sw.ElapsedMilliseconds);
            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.Save, DateTime.UtcNow, result.Id, resolvedTenant, OperationContextAccessor.Current?.ActorId, result.DataEncryptionKeyId,
                        result.DataEncryptionKeyVersion, FileAuditOutcome.Success), ct)
                .ConfigureAwait(false);

            return result;
        }
        catch (Exception ex) {
            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.Save, DateTime.UtcNow, createdFileId, resolvedTenant, OperationContextAccessor.Current?.ActorId, keyId, null, FileAuditOutcome.Failure,
                        ex.Message), ct)
                .ConfigureAwait(false);

            throw;
        }
    }

    /// <summary>
    /// Runs configured malware scanners against an in-memory plaintext buffer and translates results into availability policy. Fails closed when scanning is required but no
    /// scanner is configured.
    /// </summary>
    protected async Task<FileAvailability> DetermineAvailabilityAfterScanningPlaintextAsync(byte[] data, string? contentType, string? originalFileName, CancellationToken ct)
    {
        EnsureScanRequirementSatisfied();
        if (!Options.RequireScanBeforeAvailable)
            return Options.DefaultAvailability;

        using var ms = new MemoryStream(data, false);
        return await ScanAndMapAvailabilityAsync(ms, contentType, originalFileName, ct).ConfigureAwait(false);
    }

    /// <summary>Evaluates availability for on-disk sources by scanning the file stream directly (no full-buffer materialization).</summary>
    protected async Task<FileAvailability> DetermineAvailabilityAfterScanningFileAsync(
        string filePath,
        long length,
        string? contentType,
        string? originalFileName,
        CancellationToken ct)
    {
        EnsureScanRequirementSatisfied();
        if (!Options.RequireScanBeforeAvailable)
            return Options.DefaultAvailability;

#if NETSTANDARD2_0
        using var fs = File.OpenRead(filePath);
#else
        await using var fs = File.OpenRead(filePath);
#endif
        return await ScanAndMapAvailabilityAsync(fs, contentType, originalFileName, ct).ConfigureAwait(false);
    }

    /// <summary>Shared malware-scan-to-availability mapping (Clean→Available, Suspect→Quarantined, Threat→throw, unknown→Quarantined).</summary>
    protected async Task<FileAvailability> ScanAndMapAvailabilityAsync(Stream stream, string? contentType, string? originalFileName, CancellationToken ct)
    {
        var scan = await MalwareScanner.ScanAsync(stream, contentType, originalFileName, ct).ConfigureAwait(false);
        return scan.ThreatLevel switch {
            FileScanThreatLevel.Clean => FileAvailability.Available,
            FileScanThreatLevel.Suspect => FileAvailability.Quarantined,
            FileScanThreatLevel.Threat => throw new FilePolicyRejectedException(scan.Detail ?? "Malware scan rejected the file."),
            // Unknown / future enum values: fail closed to quarantine.
            var _ => FileAvailability.Quarantined
        };
    }

    /// <summary>Fails closed when <see cref="FileStorageServiceBaseOptions.RequireScanBeforeAvailable" /> is set but no real scanner is wired.</summary>
    protected internal void EnsureScanRequirementSatisfied()
    {
        if (Options.RequireScanBeforeAvailable && MalwareScanner is NullFileMalwareScanner) {
            throw new InvalidOperationException(
                "RequireScanBeforeAvailable is set but no IFileMalwareScanner is configured. " +
                "Register a real malware scanner (e.g. via DI) or disable RequireScanBeforeAvailable.");
        }
    }

    /// <summary>Hashes, optionally compresses, optionally encrypts, and persists payloads arriving from arbitrary readable streams.</summary>
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
        try {
            return await ProcessAndSaveStreamCoreAsync(
                    inputStream, fileId, originalFileName, originalSize, compress, encrypt, keyId, normalizedPathPrefix, timestamp, chunkSize, contentType, tenantId, availability,
                    ct)
                .ConfigureAwait(false);
        }
        catch {
            await TryCleanupPartialFileAsync(fileId, normalizedPathPrefix).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<FileStoreResult> ProcessAndSaveStreamCoreAsync(
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
        var selectionContext = FileStorageCompression.BuildSelectionContext(originalSize, contentType, originalFileName, tenantId);
        var (shouldCompress, selectedCompressAlgorithm) = FileStorageCompression.ResolveForSave(compress, selectionContext, CompressionService, Logger);
        compress = shouldCompress;
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
        else if (compress && selectedCompressAlgorithm != null)
            fileExtension = selectedCompressAlgorithm.Extension;

        // Pipeline path: compress -> pipe -> encrypt when both enabled and duplicate detection is off
        if (compress && encrypt && !Options.EnableDuplicateDetection) {
            OperationHelpers.ThrowIfNullOrWhiteSpace(
                keyId, "Encryption was requested but no keyId was provided. When encrypting files, you must provide a keyId parameter to identify the encryption key to use.");

            var pipelineOutputStream = await CreateOutputStreamAsync(fileId, fileExtension, normalizedPathPrefix, ct).ConfigureAwait(false);
            try {
                var pipelineResult = await _streamingPipelines.SaveWithCompressEncryptPipelineAsync(
                        inputStream, pipelineOutputStream, fileId, keyId, normalizedPathPrefix, chunkSize, originalSize, selectedCompressAlgorithm!, ct)
                    .ConfigureAwait(false);

                // SaveWithCompressEncryptPipelineAsync flushes and disposes the output stream as part of its commit; avoid a double dispose in the finally below.
                pipelineOutputStream = null;
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
                    fileId, originalFileName, originalSize, originalHash!, sourceFileName, finalSize, sourceFileHash!, compress, compressionAlgorithm, compressedSize,
                    compressedHash, encrypt, dataEncryptionKeyAlgorithm, keyEncryptionKeyAlgorithm, encryptedSize, encryptedHash, encryptedDataEncryptionKey, dataEncryptionKeyId,
                    dataEncryptionKeyVersion, keyEncryptionKeySalt, timestamp, normalizedPathPrefix, Options.HashAlgorithm, contentType, tenantId, availability,
                    dekKeyMaterialBytes);

                await MetadataService.SaveMetadataAsync(fileId, metadata, ct).ConfigureAwait(false);
                FileSaved?.Invoke(this, new(fileId, FileStoreSnapshot.From(metadata), originalSize, finalSize, compress, encrypt));
                return metadata;
            }
            finally {
                if (pipelineOutputStream != null)
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
                OperationHelpers.ThrowIfNull(selectedCompressAlgorithm, "Compression was requested but no compression algorithm was resolved.");
                OperationHelpers.ThrowIfNull(
                    CompressionService, "Compression was requested but no compression service is configured. Provide an ICompressionService when creating FileStorageService.");

                using var inputHashStream = new HashingStream(processingStream, originalHashAlgo);
                using var compressedHashAlgo = hashAlg.Create();
                using var compressedHashStream = new HashingStream(intermediateStream, compressedHashAlgo);
                await CompressionService.Resolver.CompressAsync(inputHashStream, compressedHashStream, selectedCompressAlgorithm, chunkSize, ct: ct).ConfigureAwait(false);
                await compressedHashStream.FlushAsync(ct).ConfigureAwait(false);
                await inputHashStream.FlushAsync(ct).ConfigureAwait(false);
                compressedSize = intermediateStream.Length;
                compressedHash = compressedHashStream.GetHash();
                originalHash = inputHashStream.GetHash();
                fileExtension = selectedCompressAlgorithm.Extension;
                sourceFileName += fileExtension;
                compressionAlgorithm = selectedCompressAlgorithm;
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
                    var duplicateResult = await HandleDuplicateAsync(
                            existingMetadata, fileId, originalSize, normalizedPathPrefix, compress, encrypt, keyId, selectedCompressAlgorithm, ct)
                        .ConfigureAwait(false);

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

            // originalHash was captured during the compression / pass-through stage above; do NOT rewind inputStream,
            // because it may not be seekable (e.g. S3 GetObject / Blob OpenRead streams used by multipart complete).
            var metadata = new FileStoreResult(
                fileId, originalFileName, originalSize, originalHash, sourceFileName, finalSize, sourceFileHash ?? originalHash, compress, compressionAlgorithm, compressedSize,
                compressedHash, encrypt, dataEncryptionKeyAlgorithm, keyEncryptionKeyAlgorithm, encryptedSize, encryptedHash, encryptedDataEncryptionKey, dataEncryptionKeyId,
                dataEncryptionKeyVersion, keyEncryptionKeySalt, timestamp, normalizedPathPrefix, hashAlg, contentType, tenantId, availability, dekKeyMaterialBytes);

            // Save metadata using metadata service
            await MetadataService.SaveMetadataAsync(fileId, metadata, ct).ConfigureAwait(false);
            FileSaved?.Invoke(this, new(fileId, FileStoreSnapshot.From(metadata), originalSize, finalSize, compress, encrypt));
            return metadata;
        }
        finally {
            if (outputStream != null)
                await DisposeStreamAsync(outputStream).ConfigureAwait(false);
        }
    }

    /// <summary>Buffer for the sequential compress/hash/encrypt path. Large or compressed payloads use a temp file so we do not hold the full plaintext or compressed blob in memory.</summary>
    private static Stream CreateSequentialStagingStream(long originalSize, bool compress)
    {
        const long largePlainThresholdBytes = 64L * 1024 * 1024;
        const long compressStagingThresholdBytes = 16L * 1024 * 1024;
        var useTempFile = originalSize > largePlainThresholdBytes || (compress && originalSize > compressStagingThresholdBytes);
        if (!useTempFile) {
            if (originalSize <= 0)
                return new MemoryStream();

            var cap = (int)Math.Min(originalSize, int.MaxValue);
            return new MemoryStream(cap);
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

    /// <summary>Best-effort cleanup when <see cref="FileStream" /> construction fails for temporary staging files.</summary>
    private static void TryDeleteStagingFile(string path)
    {
        try {
            File.Delete(path);
        }
        catch {
            // best effort
        }
    }

    /// <summary>Best-effort cleanup of a partially-written backend object/file when a save pipeline fails after it has been created. Never throws.</summary>
    protected async Task TryCleanupPartialFileAsync(Guid fileId, string? normalizedPathPrefix)
    {
        try {
            await CleanupPartialFileAsync(fileId, normalizedPathPrefix, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "Best-effort cleanup of partial file {FileId} failed", fileId);
        }
    }

    /// <summary>Single-line, length-capped, audit-safe sanitization for failure messages emitted in audit events.</summary>
    public static string SanitizeAuditError(string? message)
    {
        if (message.IsNullOrEmpty())
            return string.Empty;

        const int max = 512;
        var s = message.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return s.Length > max ? s[..max] : s;
    }

#endregion

#region Retrieve

    /// <inheritdoc />
    public async Task<byte[]> GetFileAsync(Guid fileId, CompressionAlgorithm? compressionAlgorithmOverride = null, CancellationToken ct = default)
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
                "Provide an ICompressionService when creating FileStorageService to decompress compressed files.");

            var decompressAlgorithm = FileStorageCompression.ResolveDecompressionAlgorithm(
                metadata, compressionAlgorithmOverride, Options.DecompressionAlgorithmOverride, CompressionService, Logger, fileId);

            int? chunkSize = metadata.CompressedFileSize.HasValue ? StreamChunkSizeHelper.DetermineChunkSize(metadata.CompressedFileSize.Value) : null;
            var decompressedStream = new MemoryStream();
            if (Options.MaxDecompressedFileSize is { } maxDecompressedBytes) {
                // Bound during decompression rather than after, so malicious payloads cannot exhaust memory before the size check.
                var bounded = new MaxBytesWriteStream(decompressedStream, maxDecompressedBytes, fileId);
                await CompressionService.Resolver.DecompressAsync(processingStream, bounded, decompressAlgorithm, chunkSize, ct).ConfigureAwait(false);
            }
            else
                await CompressionService.Resolver.DecompressAsync(processingStream, decompressedStream, decompressAlgorithm, chunkSize, ct).ConfigureAwait(false);

            decompressedStream.Position = 0;
            processingStream = decompressedStream;
            Logger.LogDebug("Decompressed file {FileId}: {CompressedSize} -> {DecompressedSize} bytes", fileId, metadata.CompressedFileSize, decompressedStream.Length);
        }

        // Materialize the byte[] in one pass while hashing — saves a second linear scan of the payload.
        var hashAlg = metadata.HashAlgorithm ?? HashAlgorithm.Sha256;
        byte[] computedHash;
        if (processingStream is MemoryStream ms) {
            data = ms.ToArray();
            computedHash = ComputeHash(data, hashAlg);
        }
        else {
            var capacity = metadata.OriginalFileSize > 0 ? (int)Math.Min(metadata.OriginalFileSize, int.MaxValue) : 0;
            using var resultStream = capacity > 0 ? new MemoryStream(capacity) : new MemoryStream();
            using (var hashing = new HashingStream(resultStream, hashAlg.Create())) {
                await processingStream.CopyToAsync(hashing, CopyToBufferSizeBytes, ct).ConfigureAwait(false);
                computedHash = hashing.GetHash();
            }

            data = resultStream.ToArray();
        }

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
    public async Task<Stream?> GetFileStreamAsync(Guid fileId, CompressionAlgorithm? compressionAlgorithmOverride = null, CancellationToken ct = default)
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
            FileRetrieved?.Invoke(this, new(fileId, metadata.OriginalFileSize, metadata.IsCompressed, metadata.IsEncrypted));
            return new HashVerifyingReadStream(storageStream, hashAlgo, metadata.OriginalFileHash, Options.ThrowOnHashMismatch, Logger, fileId);
        }

        // Encrypted and/or compressed: decrypt → [compressed pipe] → decompress using System.IO.Pipelines (bounded RAM via backpressure, same idea as save pipeline).
        int? chunkSize = metadata.CompressedFileSize.HasValue ? StreamChunkSizeHelper.DetermineChunkSize(metadata.CompressedFileSize.Value) : null;
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var pipePlain = new Pipe();
        var pipelineTask = _streamingPipelines.RunStreamingDecodePipelineAsync(storageStream, metadata, pipePlain.Writer, chunkSize, compressionAlgorithmOverride, linkedCts.Token);
        Stream decoded = new PipelineFileReadStream(pipePlain.Reader.AsStream(), pipelineTask, linkedCts);
        if (Options.MaxDecompressedFileSize is { } maxDecompressed)
            decoded = new MaxDecompressedBytesReadStream(decoded, maxDecompressed, fileId);

        sw.Stop();
        Logger.LogInformation(
            "Retrieved file {FileId} as stream (encrypted={Encrypted}, compressed={Compressed}); streaming decode via pipes", fileId, metadata.IsEncrypted, metadata.IsCompressed);

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

#endregion

#region Delete.Metadata

    /// <inheritdoc />
    public async Task<bool> DeleteFileAsync(Guid fileId, FileDeletionMode mode = FileDeletionMode.RemoveObjectAndTombstoneMetadata, CancellationToken ct = default)
    {
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.DeleteDuration)]);
        var sw = Stopwatch.StartNew();
        Logger.LogDebug("Deleting file {FileId} with mode {Mode}", fileId, mode);
        try {
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

            // Tombstone (or purge) metadata FIRST so a transient storage-delete failure does not leave a callable record pointing at a half-deleted blob.
            // Worst case: a blob is orphaned until a background sweeper reclaims it, but no caller sees a broken FileStoreResult.
            if (mode == FileDeletionMode.RemoveObjectAndPurgeMetadata)
                await MetadataService.PurgeMetadataAsync(fileId, ct).ConfigureAwait(false);
            else
                await MetadataService.DeleteMetadataAsync(fileId, ct).ConfigureAwait(false);

            var deleted = await DeleteFromStorageAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
            sw.Stop();
            Logger.LogInformation("Successfully deleted file {FileId} (mode {Mode})", fileId, mode);
            FileDeleted?.Invoke(this, new(fileId, deleted));
            Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.DeleteSuccess)]);
            Metrics.RecordHistogram(MetricNames[nameof(Constants.Metrics.DeleteDurationMs)], sw.ElapsedMilliseconds);
            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.Delete, DateTime.UtcNow, fileId, metadata.TenantId, OperationContextAccessor.Current?.ActorId, metadata.DataEncryptionKeyId,
                        metadata.DataEncryptionKeyVersion, FileAuditOutcome.Success), ct)
                .ConfigureAwait(false);

            return deleted;
        }
        catch (FileNotFoundException ex) when (Options.ThrowOnDeleteNotFound) {
            sw.Stop();
            Logger.LogWarning("File not found: {FileId}", fileId);
            Metrics.IncrementCounter(Constants.Metrics.DeleteFailure);
            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.Delete, DateTime.UtcNow, fileId, null, OperationContextAccessor.Current?.ActorId, null, null, FileAuditOutcome.Failure,
                        SanitizeAuditError(ex.Message)), ct)
                .ConfigureAwait(false);

            throw;
        }
        catch (Exception ex) {
            sw.Stop();
            Logger.LogError(ex, "Failed to delete file {FileId}", fileId);
            FileDeleted?.Invoke(this, new(fileId, false, ex.Message));
            Metrics.IncrementCounter(Constants.Metrics.DeleteFailure);
            Metrics.RecordError(MetricNames[nameof(Constants.Metrics.DeleteDuration)], ex);
            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.Delete, DateTime.UtcNow, fileId, null, OperationContextAccessor.Current?.ActorId, null, null, FileAuditOutcome.Failure,
                        SanitizeAuditError(ex.Message)), ct)
                .ConfigureAwait(false);

            return false;
        }
    }

    /// <inheritdoc />
    public virtual async Task<FileStoreResult> GetMetadataAsync(Guid fileId, CancellationToken ct = default)
    {
        Logger.LogDebug("Retrieving metadata for file {FileId}", fileId);
        var metadata = await MetadataService.GetMetadataAsync(fileId, ct).ConfigureAwait(false);
        Logger.LogDebug("Retrieved metadata for file {FileId}", fileId);
        RaiseFileMetadataRetrieved(fileId, FileStoreSnapshot.From(metadata));
        return metadata;
    }

    /// <summary>Throws <see cref="FileNotAvailableException" /> when metadata indicates the payload is not readable for the current policy.</summary>
    protected void EnsureReadableAvailability(FileStoreResult metadata)
    {
        if (metadata.Availability == FileAvailability.Available)
            return;

        if (metadata.Availability == FileAvailability.Quarantined && Options.AllowReadQuarantinedForAdmin)
            return;

        throw new FileNotAvailableException(metadata.Id, metadata.Availability);
    }

    /// <summary>Applies <see cref="FileStorageServiceBaseOptions.DuplicateStrategy" /> after computing the canonical hash for a new upload.</summary>
    /// <returns>Existing metadata to short-circuit the save, or <see langword="null" /> to continue writing a new object.</returns>
    protected async Task<FileStoreResult?> HandleDuplicateAsync(
        FileStoreResult existingMetadata,
        Guid newFileId,
        long originalSize,
        string? normalizedPathPrefix,
        bool compress,
        bool encrypt,
        string? keyId,
        CompressionAlgorithm? compressionAlgorithm,
        CancellationToken ct)
    {
        switch (Options.DuplicateStrategy) {
            case DuplicateHandlingStrategy.ReturnExisting:
                if (!FileStorageDuplicateProfile.Matches(existingMetadata, compress, encrypt, keyId, compressionAlgorithm)) {
                    await CleanupPartialFileAsync(newFileId, normalizedPathPrefix, ct).ConfigureAwait(false);
                    var message = FileStorageDuplicateProfile.BuildMismatchMessage(existingMetadata.Id, existingMetadata, compress, encrypt, keyId, compressionAlgorithm);
                    Logger.LogWarning("Duplicate file detected for hash but storage profile does not match. Existing file ID: {FileId}", existingMetadata.Id);
                    throw new ConflictException(message);
                }

                Logger.LogInformation("Duplicate file detected for hash. Returning existing file ID: {FileId}", existingMetadata.Id);

                // Clean up any file we started creating
                await CleanupPartialFileAsync(newFileId, normalizedPathPrefix, ct).ConfigureAwait(false);
                FileSaved?.Invoke(
                    this,
                    new(
                        existingMetadata.Id, FileStoreSnapshot.From(existingMetadata), originalSize, existingMetadata.SourceFileSize, existingMetadata.IsCompressed,
                        existingMetadata.IsEncrypted));

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

#endregion

#region Dek

    /// <inheritdoc />
    public virtual Task<DekMigrationResult> MigrateDeksAsync(
        string sourceKeyId,
        string? sourceKeyVersion = null,
        string? targetKeyId = null,
        string? targetKeyVersion = null,
        int batchSize = 100,
        CancellationToken ct = default)
        => _dekOperations.MigrateDeksAsync(sourceKeyId, sourceKeyVersion, targetKeyId, targetKeyVersion, batchSize, ct);

    /// <inheritdoc />
    public virtual Task<DekMigrationResult> RotateDeksAsync(
        IReadOnlyCollection<Guid> fileIds,
        string? targetKeyId = null,
        string? targetKeyVersion = null,
        int batchSize = 100,
        CancellationToken ct = default)
        => _dekOperations.RotateDeksAsync(fileIds, targetKeyId, targetKeyVersion, batchSize, ct);

#endregion

#region DirectUpload.Copy

    /// <inheritdoc cref="PlainDirectUploadCoordinator.PersistPendingPlainDirectUploadMetadataAsync" />
    protected Task<FileStoreResult> PersistPendingPlainDirectUploadMetadataAsync(Guid fileId, DirectUploadBeginRequest request, string normalizedPathPrefix, CancellationToken ct)
        => _plainDirectUpload.PersistPendingPlainDirectUploadMetadataAsync(fileId, request, normalizedPathPrefix, ct);

    /// <inheritdoc cref="PlainDirectUploadCoordinator.FinalizePendingPlainDirectUploadCoreAsync" />
    protected Task<FileStoreResult> FinalizePendingPlainDirectUploadCoreAsync(Guid fileId, DirectUploadCompleteRequest? completeRequest, CancellationToken ct)
        => _plainDirectUpload.FinalizePendingPlainDirectUploadCoreAsync(fileId, completeRequest, ct);

    /// <summary>Matches stored <see cref="FileStoreResult.SourceFileName" /> suffix conventions (plaintext uses <see cref="Guid.ToString()" />, hashed paths append extensions).</summary>
    protected static string InferTrailingSuffixAfterFileId(Guid id, string? sourceFileName) => PlainDirectUploadCoordinator.InferTrailingSuffixAfterFileId(id, sourceFileName);

    /// <inheritdoc cref="PlainDirectUploadCoordinator.RecordCopyMetadataAsync" />
    protected Task<FileStoreResult> RecordCopyMetadataAsync(Guid sourceFileId, FileStoreResult sourceMeta, Guid destId, CopyFileRequest? request, CancellationToken ct)
        => _plainDirectUpload.RecordCopyMetadataAsync(sourceFileId, sourceMeta, destId, request, ct);

#endregion
}