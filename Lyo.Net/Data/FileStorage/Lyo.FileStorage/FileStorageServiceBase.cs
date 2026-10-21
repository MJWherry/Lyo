using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Text.Json;
using Lyo.Common.Core.Extensions;
using Lyo.Common.Core.Identifiers;
using Lyo.Common.Core.Pathing;
using Lyo.Common.Metadata.Records;
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
/// Shared implementation of <see cref="IFileStorageService" /> for filesystem, object-storage, and blob backends. Owns the compress, encrypt, audit, metrics, and DEK
/// pipelines. Derived types supply storage I/O through <see cref="CreateOutputStreamAsync" />, <see cref="ReadFromStorageAsync" />, and the other abstract members.
/// </summary>
/// <remarks>
/// <para>
/// Members sit in <c>#region</c> slices (core wiring, save, retrieve, delete/metadata, DEK, direct upload/copy façade) in this one compilation unit. Heavy work is delegated to
/// types wired in the ctor: <see cref="FileStorageStreamingPipelines" />, <see cref="FileStorageDekOperations" />, and <see cref="PlainDirectUploadCoordinator" />. Those types talk
/// to the narrow internal interfaces this class implements explicitly (<see cref="IFileStoragePhysicalIO" />, <see cref="IFileAuditPublisher" />, and similar).
/// </para>
/// </remarks>
public abstract class FileStorageServiceBase
    : IFileStorageService, IFileStorageTags, IDisposable, IFileStoragePhysicalIO, IFileAuditPublisher, IFileStorageMetadataNormalization, IFileStorageMetadataLookup
{
#region Core

    private const int CopyToBufferSizeBytes = 81920;

    private readonly FileStorageStreamingPipelines _streamingPipelines;
    private readonly FileStorageDekOperations _dekOperations;
    private readonly PlainDirectUploadCoordinator _plainDirectUpload;

    private readonly IReadOnlyList<IFileAuditEventHandler> _auditHandlers;
    private readonly IFileTypeSniffer _fileTypeSniffer;
    protected readonly ICompressionService? CompressionService;
    protected readonly IFileContentPolicy ContentPolicy;
    protected readonly ILogger Logger;
    protected readonly IFileMetadataStore MetadataService;

    /// <summary>Catalog used by save, get, delete, and folder listing. Same instance the service was constructed with.</summary>
    public IFileMetadataStore MetadataStore => MetadataService;

    protected readonly IMetrics Metrics;
    protected readonly IFileOperationContextAccessor OperationContextAccessor;
    protected readonly FileStorageServiceBaseOptions Options;
    protected readonly ITwoKeyEncryptionService? TwoKeyEncryptionService;
    private readonly ConcurrentDictionary<Guid, EncryptedStreamLayout> _encryptedRangeLayouts = new();

    /// <summary>Start offset of the last backend range read. Tests use this to prove a seek did not start at byte 0.</summary>
    internal long? PlainByteRangeReadStart { get; private set; }

    protected bool Disposed;

    /// <summary>Maps logical metric keys to the names that are actually emitted. Derived types may mutate this dictionary.</summary>
    protected Dictionary<string, string> MetricNames { get; }

    /// <summary>Builds the shared pipeline with optional compression, encryption, metrics, auditing, and content-policy collaborators.</summary>
    /// <param name="options">Baseline settings: hashing, quotas, duplicate handling, and health-check mode.</param>
    /// <param name="metadataService">Store that persists <see cref="FileStoreResult" /> rows.</param>
    /// <param name="logger">Optional logger for operational diagnostics.</param>
    /// <param name="compressionService">Optional service used when saves may compress payloads.</param>
    /// <param name="twoKeyEncryptionService">Optional service used when saves may encrypt payloads.</param>
    /// <param name="metrics">Optional metrics sink. Starts as null metrics.</param>
    /// <param name="operationContextAccessor">Optional ambient tenant/actor lookup for auditing.</param>
    /// <param name="auditHandlers">Optional handlers fed through the publication pipeline beside <see cref="FileAuditOccurred" />.</param>
    /// <param name="contentPolicy">Optional content policy. Starts as <see cref="DefaultFileContentPolicy" />.</param>
    /// <param name="fileTypeSniffer">Optional header sniffer. Starts as <see cref="MagicFileTypeSniffer" />.</param>
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
        IFileTypeSniffer? fileTypeSniffer = null)
    {
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(metadataService);
        options.ValidateUploadFilters();
        Options = options;
        MetadataService = metadataService;
        Logger = logger ?? NullLogger.Instance;
        CompressionService = compressionService;
        TwoKeyEncryptionService = twoKeyEncryptionService;
        Metrics = metrics ?? NullMetrics.Instance;
        OperationContextAccessor = operationContextAccessor ?? NullFileOperationContextAccessor.Instance;
        _auditHandlers = auditHandlers == null ? [] : auditHandlers.ToList();
        _fileTypeSniffer = fileTypeSniffer ?? new MagicFileTypeSniffer();
        ContentPolicy = contentPolicy ?? new DefaultFileContentPolicy(options);
        MetricNames = CreateMetricNamesDictionary();
        _streamingPipelines = new(this, CompressionService, TwoKeyEncryptionService, Options, Logger, CopyToBufferSizeBytes);
        _dekOperations = new(MetadataService, TwoKeyEncryptionService, OperationContextAccessor, Logger, Options, this, this, CopyToBufferSizeBytes);
        _plainDirectUpload = new(
            ContentPolicy, MetadataService, OperationContextAccessor, Options, Logger, this, this, this, this, _fileTypeSniffer, CopyToBufferSizeBytes,
            this as IDirectUploadVerification);
    }

    /// <summary>Connectivity-only probe that implementations call when options use lightweight health-check mode.</summary>
    protected abstract Task<HealthResult> CheckHealthLightweightAsync(CancellationToken ct);

    /// <inheritdoc />
    public virtual void Dispose()
    {
        if (Disposed)
            return;

        foreach (var layout in _encryptedRangeLayouts.Values)
            layout.Dispose();

        _encryptedRangeLayouts.Clear();
        Disposed = true;
    }

    public virtual string HealthCheckName => "filestorage";

    /// <summary>
    /// Probes the backend. Lightweight mode calls <see cref="CheckHealthLightweightAsync" />. Full mode round-trips a throwaway payload through the physical I/O seam (
    /// <see cref="CreateOutputStreamAsync" /> / <see cref="ReadFromStorageAsync" /> / <see cref="DeleteFromStorageAsync" />) under the <c>.lyo-health</c> prefix.
    /// </summary>
    /// <remarks>
    /// The probe skips <see cref="SaveFileAsync(byte[], string?, bool, bool, string?, string?, int?, string?, string?, string?, JsonElement?, CancellationToken)" /> and the
    /// other public save APIs on purpose. Sending it through those methods wrote a metadata row, ran content policy (a configured <c>AllowedContentTypes</c> that omitted the probe type
    /// would mark the service permanently unhealthy), joined duplicate detection, and emitted three audit events.
    /// </remarks>
    public virtual async Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        if (Options.HealthCheckMode == FileStorageHealthCheckMode.Lightweight)
            return await CheckHealthLightweightAsync(ct).ConfigureAwait(false);

        var sw = Stopwatch.StartNew();
        var fileId = LyoGuid.CreateV7();
        const string healthPrefix = ".lyo-health";
        try {
            var probeData = fileId.ToByteArray();
            var output = await CreateOutputStreamAsync(fileId, "", healthPrefix, ct).ConfigureAwait(false);
            try {
                await output.WriteAsync(probeData, 0, probeData.Length, ct).ConfigureAwait(false);
                await output.FlushAsync(ct).ConfigureAwait(false);
            }
            finally {
                await DisposeStreamAsync(output).ConfigureAwait(false);
            }

            byte[] roundTripped;
            var input = await ReadFromStorageAsync(fileId, healthPrefix, ct).ConfigureAwait(false);
            if (input == null) {
                sw.Stop();
                return HealthResult.Unhealthy(sw.Elapsed, "Health probe object was not readable immediately after being written.");
            }

            try {
                using var buffer = new MemoryStream(probeData.Length);
                await input.CopyToAsync(buffer, CopyToBufferSizeBytes, ct).ConfigureAwait(false);
                roundTripped = buffer.ToArray();
            }
            finally {
                await DisposeStreamAsync(input).ConfigureAwait(false);
            }

            sw.Stop();
            return SequencesEqual(probeData, roundTripped)
                ? HealthResult.Healthy(sw.Elapsed, null, new Dictionary<string, object?> { ["fileId"] = fileId })
                : HealthResult.Unhealthy(sw.Elapsed, "Health probe round-tripped different bytes than were written.");
        }
        catch (Exception ex) {
            sw.Stop();
            return HealthResult.Unhealthy(sw.Elapsed, ex.Message, null, ex);
        }
        finally {
            try {
                await DeleteFromStorageAsync(fileId, healthPrefix, ct).ConfigureAwait(false);
            }
            catch (Exception cleanupEx) {
                Logger.LogDebug(cleanupEx, "Best-effort cleanup of health probe object {FileId} failed", fileId);
            }
        }
    }

    private static bool SequencesEqual(byte[] expected, byte[] actual)
    {
        if (expected.Length != actual.Length)
            return false;

        for (var i = 0; i < expected.Length; i++) {
            if (expected[i] != actual[i])
                return false;
        }

        return true;
    }

    public event EventHandler<FileSavedResult>? FileSaved;

    public event EventHandler<FileRetrievedResult>? FileRetrieved;

    public event EventHandler<FileDeletedResult>? FileDeleted;

    public event EventHandler<FileMovedResult>? FileMoved;

    public event EventHandler<FileRenamedResult>? FileRenamed;

    public event EventHandler<FileMetadataRetrievedResult>? FileMetadataRetrieved;

    public event EventHandler<FileAuditEventArgs>? FileAuditOccurred;

    /// <summary>Raises <see cref="FileSaved" /> from subclasses that skip <see cref="SaveFromStreamAsync" /> (server-side multipart finalize paths, for example).</summary>
    protected void RaiseFileSaved(Guid fileId, FileStoreSnapshot snapshot, long originalSize, long finalSize, bool compress, bool encrypt)
        => FileSaved?.Invoke(this, new(fileId, snapshot, originalSize, finalSize, compress, encrypt));

    /// <summary>
    /// Raises <see cref="FileMetadataRetrieved" /> from subclasses (an override of <c>GetFileMetadataAsync</c>, for example) so listeners see both metadata-only and
    /// payload-bearing reads.
    /// </summary>
    protected void RaiseFileMetadataRetrieved(Guid fileId, FileStoreSnapshot snapshot) => FileMetadataRetrieved?.Invoke(this, new(fileId, snapshot));

    /// <summary>Raises <see cref="FileMoved" /> after a path-prefix relocate succeeds.</summary>
    protected void RaiseFileMoved(Guid fileId, FileStoreSnapshot snapshot, string? previousPathPrefix) => FileMoved?.Invoke(this, new(fileId, snapshot, previousPathPrefix));

    /// <summary>Raises <see cref="FileRenamed" /> after a display-name update succeeds.</summary>
    protected void RaiseFileRenamed(Guid fileId, FileStoreSnapshot snapshot, string? previousOriginalFileName)
        => FileRenamed?.Invoke(this, new(fileId, snapshot, previousOriginalFileName));

    /// <summary>
    /// Reports presigned read, direct upload, copy, move, and physical listing for this backend. The base reports presign, direct upload, copy, and move unsupported. Physical
    /// listing is reported when this instance implements <see cref="IFileStoragePhysical" />. Hosts add multipart, encryption, archive, and access-link flags separately.
    /// </summary>
    public virtual FileStorageCapabilities GetCapabilities() => new(false, false, false, false, this is IFileStoragePhysical);

    public virtual Task<string> GetPreSignedReadUrlAsync(Guid fileId, TimeSpan? expiration = null, string? pathPrefix = null, CancellationToken ct = default)
        => GetPreSignedReadUrlAsync(fileId, expiration, pathPrefix, null, ct);

    public virtual Task<string> GetPreSignedReadUrlAsync(Guid fileId, TimeSpan? expiration, string? pathPrefix, PreSignedReadUrlOptions? urlResponseOptions, CancellationToken ct)
        => Task.FromException<string>(
            new NotSupportedException("Pre-signed read URLs are not supported by this storage backend. Use Blob or AWS S3 file storage implementations."));

    public virtual Task<DirectUploadBeginResult> BeginDirectUploadAsync(DirectUploadBeginRequest request, CancellationToken ct = default)
        => Task.FromException<DirectUploadBeginResult>(new NotSupportedException("Direct uploads are not supported by this backend."));

    public virtual Task<FileStoreResult> CompleteDirectUploadAsync(Guid fileId, DirectUploadCompleteRequest? completeRequest = null, CancellationToken ct = default)
        => Task.FromException<FileStoreResult>(new NotSupportedException("Direct uploads are not supported by this backend."));

    /// <inheritdoc />
    public Task<int> SweepExpiredDirectUploadsAsync(int maxUploads = 1000, CancellationToken ct = default) => _plainDirectUpload.SweepExpiredDirectUploadsAsync(maxUploads, ct);

    public virtual Task<FileStoreResult> CopyFileAsync(Guid sourceFileId, CopyFileRequest? request = null, CancellationToken ct = default)
        => Task.FromException<FileStoreResult>(new NotSupportedException("Server-side copies are not supported by this backend."));

    public virtual Task<FileStoreResult> MoveFileAsync(Guid fileId, MoveFileRequest request, CancellationToken ct = default)
        => Task.FromException<FileStoreResult>(new NotSupportedException("Server-side moves are not supported by this backend."));

    /// <inheritdoc />
    public virtual async Task<FileStoreResult> RenameFileAsync(Guid fileId, RenameFileRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        OperationHelpers.ThrowIfNullOrWhiteSpace(request.OriginalFileName, "OriginalFileName is required for rename.");
        var meta = await GetMetadataAsync(fileId, ct).ConfigureAwait(false);
        EnsureReadableAvailability(meta);
        OperationHelpers.ThrowIf(meta.Availability == FileAvailability.PendingDirectUpload, $"Cannot rename file {fileId}; it is awaiting direct-upload finalize.");
        if (string.Equals(meta.OriginalFileName, request.OriginalFileName, StringComparison.Ordinal))
            return meta;

        try {
            FileNamePatternFilter.ThrowIfRejected(Options, request.OriginalFileName);
            // Ciphertext and compressed bytes are not the original header. Those payloads were checked before the transform on save.
            if (FileUploadGuards.RequiresHeaderInspection(Options) && !meta.IsEncrypted && !meta.IsCompressed)
                await EnforceStoredHeaderAsync(fileId, meta.PathPrefix, request.OriginalFileName, meta.ContentType, ct).ConfigureAwait(false);

            var renamed = await RecordRenameMetadataAsync(meta, request.OriginalFileName, ct).ConfigureAwait(false);
            RaiseFileRenamed(fileId, FileStoreSnapshot.From(renamed), meta.OriginalFileName);
            return renamed;
        }
        catch (Exception ex) {
            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.Rename, DateTime.UtcNow, fileId, meta.TenantId, OperationContextAccessor.Current?.ActorId, meta.DataEncryptionKeyId,
                        meta.DataEncryptionKeyVersion, FileAuditOutcome.Failure, SanitizeAuditError(ex.Message)), ct)
                .ConfigureAwait(false);

            throw;
        }
    }

    /// <summary>Disposes a stream with <see cref="Stream.DisposeAsync" /> when that API exists; otherwise disposes synchronously.</summary>
    internal static Task DisposeStreamAsync(Stream? stream) => FileStorageStreamingPipelines.DisposeStreamAsync(stream);

    /// <summary>Reads the stored plaintext header and applies signature and sniffed-type rules for a new name.</summary>
    private async Task EnforceStoredHeaderAsync(Guid fileId, string? pathPrefix, string? originalFileName, string? contentType, CancellationToken ct)
    {
        var raw = await ReadFromStorageAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
        if (raw == null)
            throw new FilePolicyRejectedException($"Stored bytes for {fileId} are not available to verify the file header.");

        using (raw) {
            var header = await FileHeaderInspection.ReadPrefixAsync(raw, ct).ConfigureAwait(false);
            FileUploadGuards.EnforceHeader(Options, _fileTypeSniffer, header, originalFileName, contentType);
        }
    }

    /// <summary>Returns <paramref name="explicitTenantId" /> when set; otherwise takes tenant from operation context.</summary>
    protected string? ResolveTenantId(string? explicitTenantId) => explicitTenantId ?? OperationContextAccessor.Current?.TenantId;

    /// <summary>
    /// Sends a file audit event to registered handlers and the <see cref="FileAuditOccurred" /> event. <see cref="FileAuditEvent.Error" /> is cleaned by
    /// <see cref="SanitizeAuditError" />, and the operation-context correlation id is filled in when the caller omitted one, so raw exception text never leaks newlines or oversized
    /// payloads into audit sinks and downstream consumers always see a correlation id when one is in scope.
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

    /// <summary>Builds the default map from logical metric property names to published metric identifiers.</summary>
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

    /// <summary>Opens a writable stream that persists ciphertext or compressed payloads for <paramref name="fileId" />.</summary>
    protected abstract Task<Stream> CreateOutputStreamAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct);

    /// <summary>Persisted byte length, including format-specific headers or wrappers.</summary>
    protected abstract Task<long> GetStorageSizeAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct);

    /// <summary>Reads raw storage bytes for the file. Implementations return <see langword="null" /> when the blob is missing.</summary>
    protected abstract Task<Stream?> ReadFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct);

    /// <summary>
    /// True when this backend can read a byte slice of a stored object without downloading the preceding bytes. FTP and SFTP stay false. Local disk, S3, and Azure Blob override
    /// this.
    /// </summary>
    public virtual bool SupportsPlainByteRange => false;

    /// <summary>
    /// Opens the stored object at <paramref name="startInclusive" /> through <paramref name="endInclusive" /> using <paramref name="metadata" /> for the object name, so the read
    /// does not look up metadata or probe extensions again. The default calls <see cref="ReadPlainByteRangeAsync(Guid, string?, long, long, CancellationToken)" />.
    /// </summary>
    protected virtual Task<Stream?> ReadPlainByteRangeAsync(FileStoreResult metadata, long startInclusive, long endInclusive, CancellationToken ct)
        => ReadPlainByteRangeAsync(metadata.Id, metadata.PathPrefix, startInclusive, endInclusive, ct);

    /// <summary>
    /// Opens the stored object at <paramref name="startInclusive" /> through <paramref name="endInclusive" />. The base returns null. Backends that set
    /// <see cref="SupportsPlainByteRange" /> return a stream positioned at the start, or null when the object is missing. The caller caps a plain read.
    /// </summary>
    protected virtual Task<Stream?> ReadPlainByteRangeAsync(Guid fileId, string? pathPrefix, long startInclusive, long endInclusive, CancellationToken ct)
        => Task.FromResult<Stream?>(null);

    /// <summary>Removes the backing object referenced by metadata and reports whether an object was removed.</summary>
    protected abstract Task<bool> DeleteFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct);

    /// <summary>Reads header material after encryption so metadata can store DEKs, versions, and key identifiers.</summary>
    protected abstract Task<EncryptionHeaderInfo> ExtractEncryptionHeaderAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct);

    /// <summary>Rewrites the encryption header envelope after a KEK migrate or rotate.</summary>
    protected abstract Task UpdateFileHeaderAsync(Guid fileId, string? pathPrefix, string targetKeyId, string targetKeyVersion, byte[] newEncryptedDek, CancellationToken ct);

    /// <summary>
    /// Cleans a tenant or logical prefix for storage keys. Calls <see cref="FileHelpers.NormalizePathPrefix" /> and turns an empty result back into <see langword="null" /> so
    /// persisted metadata can tell "no prefix supplied" from "explicit empty string".
    /// </summary>
    protected static string? NormalizePathPrefix(string? pathPrefix)
    {
        var normalized = FileHelpers.NormalizePathPrefix(pathPrefix);
        return normalized.Length == 0 ? null : normalized;
    }

    /// <summary>
    /// Shared path-prefix safety check. Calls <see cref="FileHelpers.ThrowIfPathPrefixTraversal" /> so every save and direct-upload entry point uses the same traversal rejection
    /// rules (segments equal to <c>..</c>, doubled separators, embedded <c>\0</c>) instead of each backend re-implementing them.
    /// </summary>
    /// <exception cref="ArgumentException">When the prefix contains a traversal pattern.</exception>
    public static void ValidatePathPrefix(string? pathPrefix) => FileHelpers.ThrowIfPathPrefixTraversal(pathPrefix);

    /// <summary>Removes a partially written payload when an upload fails before metadata commit.</summary>
    protected abstract Task CleanupPartialFileAsync(Guid fileId, string? pathPrefix, CancellationToken ct);

    /// <summary>Picks the MIME type stored on metadata: original filename extension first, then a declared MIME hint, then unknown.</summary>
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
    /// Trims a client-declared charset. Empty or whitespace becomes null. Values longer than <see cref="FileStoreResult.MaxCharsetLength" /> throw. The string is not converted
    /// or resolved against a catalog.
    /// </summary>
    public static string? NormalizeCharset(string? charset)
    {
        if (charset.IsNullOrWhitespace())
            return null;

        var trimmed = charset.Trim();
        ArgumentHelpers.ThrowIfGreaterThan(
            trimmed.Length, FileStoreResult.MaxCharsetLength, nameof(charset), $"Charset must be at most {FileStoreResult.MaxCharsetLength} characters.");

        return trimmed;
    }

    /// <summary>
    /// Maps a file extension to the registered compression algorithm used in <see cref="FileStoreResult" />. Returns <see langword="null" /> for unknown extensions. Recognition
    /// is dynamic and only covers algorithms whose addon assemblies have been loaded (typically via <c>services.Add{Algo}Compressor()</c>).
    /// </summary>
    protected internal static CompressionAlgorithm? DetermineCompressionAlgorithm(string fileExtension) => CompressionAlgorithm.TryFromExtension(fileExtension);

    /// <summary>Computes a digest for byte arrays using the configured <see cref="Lyo.FileMetadataStore.Models.HashAlgorithm" />.</summary>
    protected static byte[] ComputeHash(byte[] data, HashAlgorithm algorithm = HashAlgorithm.Sha256)
    {
        using var algo = algorithm.Create();
        return algo.ComputeHash(data);
    }

    /// <summary>Whether two hash buffers match, with explicit null semantics.</summary>
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

    /// <summary>DEK material and key-identifying fields read from a freshly written ciphertext header.</summary>
    /// <param name="EncryptedDataEncryptionKey">Envelope-encrypted plaintext DEK bytes from the serialized header.</param>
    /// <param name="DataEncryptionKeyId">Logical KMS or keystore identifier used for encryption.</param>
    /// <param name="DataEncryptionKeyVersion">Key version paired with <paramref name="DataEncryptionKeyId" />.</param>
    /// <param name="DekKeyMaterialBytes">DEK-material width required by the installed encryption codec.</param>
    protected internal sealed record EncryptionHeaderInfo(
        byte[]? EncryptedDataEncryptionKey,
        string? DataEncryptionKeyId,
        string? DataEncryptionKeyVersion,
        byte DekKeyMaterialBytes);

    Task<Stream?> IFileStoragePhysicalIO.ReadFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct) => ReadFromStorageAsync(fileId, pathPrefix, ct);

    Task<bool> IFileStoragePhysicalIO.DeleteFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct) => DeleteFromStorageAsync(fileId, pathPrefix, ct);

    Task<Stream> IFileStoragePhysicalIO.CreateOutputStreamAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
        => CreateOutputStreamAsync(fileId, extension, pathPrefix, ct);

    Task<long> IFileStoragePhysicalIO.GetStorageSizeAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
        => GetStorageSizeAsync(fileId, extension, pathPrefix, ct);

    Task<EncryptionHeaderInfo> IFileStoragePhysicalIO.ExtractEncryptionHeaderAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
        => ExtractEncryptionHeaderAsync(fileId, extension, pathPrefix, ct);

    Task IFileStoragePhysicalIO.UpdateFileHeaderAsync(Guid fileId, string? pathPrefix, string targetKeyId, string targetKeyVersion, byte[] newEncryptedDek, CancellationToken ct)
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
    /// Thin wrapper around <see cref="SaveFromStreamAsync" /> that keeps the byte[] surface for older callers and tests. Hashing, dedup, compression, encryption, audit, and
    /// cleanup all live in the stream path so the two entry points cannot drift.
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
        string? charset = null,
        string? tenantId = null,
        JsonElement? metadata = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(data);
        using var ms = new MemoryStream(data, false);
        return await SaveFromStreamAsync(
                ms, data.LongLength, originalFileName, compress, encrypt, keyId, pathPrefix, chunkSize, contentType, charset, tenantId, null, null, metadata, ct)
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
        string? charset = null,
        string? tenantId = null,
        JsonElement? metadata = null,
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
        var resolvedCharset = NormalizeCharset(charset);
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

            var availability = Options.DefaultAvailability;
            var fileId = LyoGuid.CreateV7();
            createdFileId = fileId;
            var timestamp = DateTime.UtcNow;

            // Pick a chunk size when the caller did not supply one
            var effectiveChunkSize = chunkSize ?? StreamChunkSizeHelper.DetermineChunkSize(filePath);

            // Open the file for the streaming pipeline
            using var inputStream = File.OpenRead(filePath);
            if (FileUploadGuards.RequiresHeaderInspection(Options)) {
                var header = await FileHeaderInspection.ReadPrefixAsync(inputStream, ct).ConfigureAwait(false);
                inputStream.Position = 0;
                FileUploadGuards.EnforceHeader(Options, _fileTypeSniffer, header, actualOriginalFileName, resolvedContentType);
            }

            // Run the streaming save pipeline
            var result = await ProcessAndSaveStreamAsync(
                    inputStream, fileId, actualOriginalFileName, originalSize, compress, encrypt, keyId, normalizedPathPrefix, timestamp, effectiveChunkSize, contentType,
                    resolvedCharset, resolvedTenant, availability, metadata, ct)
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
        string? charset = null,
        string? tenantId = null,
        FileAvailability? availabilityOverride = null,
        Guid? fileId = null,
        JsonElement? metadata = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(input);
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.SaveDuration)]);
        var sw = Stopwatch.StartNew();
        var resolvedTenant = ResolveTenantId(tenantId);
        var resolvedContentType = ResolveStoredContentType(contentType, originalFileName);
        var resolvedCharset = NormalizeCharset(charset);
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

            var body = input;
            IDisposable? headerReplay = null;
            FileStoreResult result;
            try {
                if (FileUploadGuards.RequiresHeaderInspection(Options)) {
                    var peeked = await FileHeaderInspection.PeekAsync(input, ct).ConfigureAwait(false);
                    headerReplay = peeked.Owned;
                    FileUploadGuards.EnforceHeader(Options, _fileTypeSniffer, peeked.Header, originalFileName, resolvedContentType);
                    body = peeked.Stream;
                }

                ValidatePathPrefix(pathPrefix);
                var normalizedPathPrefix = NormalizePathPrefix(pathPrefix);
                var id = fileId ?? LyoGuid.CreateV7();
                createdFileId = id;
                var timestamp = DateTime.UtcNow;
                var effectiveChunkSize = chunkSize ?? StreamChunkSizeHelper.DetermineChunkSize(declaredLength);
                var availability = availabilityOverride ?? Options.DefaultAvailability;
                result = await ProcessAndSaveStreamAsync(
                        body, id, originalFileName ?? id.ToString(), declaredLength, compress, encrypt, keyId, normalizedPathPrefix, timestamp, effectiveChunkSize, contentType,
                        resolvedCharset, resolvedTenant, availability, metadata, ct)
                    .ConfigureAwait(false);
            }
            finally {
                headerReplay?.Dispose();
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

    /// <summary>Hashes, optionally compresses, optionally encrypts, and persists payloads from any readable stream.</summary>
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
        string? charset,
        string? tenantId,
        FileAvailability availability,
        JsonElement? metadata,
        CancellationToken ct)
    {
        try {
            return await ProcessAndSaveStreamCoreAsync(
                    inputStream, fileId, originalFileName, originalSize, compress, encrypt, keyId, normalizedPathPrefix, timestamp, chunkSize, contentType, charset, tenantId,
                    availability, metadata, ct)
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
        string? charset,
        string? tenantId,
        FileAvailability availability,
        JsonElement? metadata,
        CancellationToken ct)
    {
        contentType = ResolveStoredContentType(contentType, originalFileName);
        var selectionContext = FileStorageCompression.BuildSelectionContext(originalSize, contentType, originalFileName, tenantId);
        var (shouldCompress, selectedCompressAlgorithm) = FileStorageCompression.ResolveForSave(compress, selectionContext, CompressionService, Logger);
        compress = shouldCompress;
        // Single-pass streaming pipeline: input → compression → encryption → storage
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
        var fileExtension = "";
        long finalSize;
        byte[]? sourceFileHash;
        // Stays null through the single-pass branch until the stage that drains the plaintext finishes the hash.
        byte[]? originalHash = null;

        // originalSize is only the caller's declaration. Policy was checked against it, but nothing has read the payload yet. Bound and count the real bytes so an understated
        // length cannot slip past MaxUploadSizeBytes and so metadata records what was actually stored.
        using var boundedInput = new MaxUploadBytesReadStream(inputStream, Options.MaxUploadSizeBytes);
        var processingStream = (Stream)boundedInput;

        // Pick the file extension up front
        if (encrypt && TwoKeyEncryptionService != null)
            fileExtension = TwoKeyEncryptionService.FileExtension;
        else if (compress && selectedCompressAlgorithm != null)
            fileExtension = selectedCompressAlgorithm.Extension;

        // Pipeline path: compress → pipe → encrypt when both are on and duplicate detection is off
        if (compress && encrypt && !Options.EnableDuplicateDetection) {
            OperationHelpers.ThrowIfNullOrWhiteSpace(
                keyId, "Encryption was requested but no keyId was provided. When encrypting files, you must provide a keyId parameter to identify the encryption key to use.");

            var pipelineOutputStream = await CreateOutputStreamAsync(fileId, fileExtension, normalizedPathPrefix, ct).ConfigureAwait(false);
            try {
                var pipelineResult = await _streamingPipelines.SaveWithCompressEncryptPipelineAsync(
                        boundedInput, pipelineOutputStream, fileId, keyId, normalizedPathPrefix, chunkSize, originalSize, selectedCompressAlgorithm!, ct)
                    .ConfigureAwait(false);

                // SaveWithCompressEncryptPipelineAsync flushes and disposes the output stream as part of its commit. Skip a second dispose in the finally below.
                pipelineOutputStream = null;
                originalHash = pipelineResult.OriginalHash;
                fileExtension = pipelineResult.FileExtension;
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
                var actualSize = ResolveActualOriginalSize(fileId, originalSize, boundedInput.BytesRead);
                var stored = new FileStoreResult(
                    fileId, originalFileName, actualSize, originalHash!, pipelineResult.SourceFileName, finalSize, sourceFileHash!, compress, compressionAlgorithm, compressedSize,
                    compressedHash, encrypt, dataEncryptionKeyAlgorithm, keyEncryptionKeyAlgorithm, encryptedSize, encryptedHash, encryptedDataEncryptionKey, dataEncryptionKeyId,
                    dataEncryptionKeyVersion, keyEncryptionKeySalt, timestamp, normalizedPathPrefix, Options.HashAlgorithm, contentType, charset, tenantId, availability,
                    dekKeyMaterialBytes, Metadata: metadata);

                await MetadataService.SaveMetadataAsync(fileId, stored, ct).ConfigureAwait(false);
                await EnsureAutomaticTagsAsync(stored, ct).ConfigureAwait(false);
                FileSaved?.Invoke(this, new(fileId, FileStoreSnapshot.From(stored), actualSize, finalSize, compress, encrypt));
                return stored;
            }
            finally {
                if (pipelineOutputStream != null)
                    await DisposeStreamAsync(pipelineOutputStream).ConfigureAwait(false);
            }
        }

        var outputStream = await CreateOutputStreamAsync(fileId, fileExtension, normalizedPathPrefix, ct).ConfigureAwait(false);

        // Non-null only on the single-pass path, where the plaintext hash is finished by whichever stage below drains it.
        HashingStream? undrainedPlaintext = null;
        try {
            // Hash the original first; duplicate detection needs it
            var hashAlg = Options.HashAlgorithm;
            using var originalHashAlgo = hashAlg.Create();

            // Staging exists so the plaintext hash is known before anything is committed, and only duplicate detection needs that.
            // With detection off the payload streams straight into the backend instead of being buffered and then copied a second time.
            // compress && encrypt never reaches here with detection off; the pipe path above owns that pairing.
            using var intermediateStream = Options.EnableDuplicateDetection ? CreateSequentialStagingStream(originalSize, compress) : null;

            // Compression pass
            if (compress) {
                OperationHelpers.ThrowIfNull(selectedCompressAlgorithm, "Compression was requested but no compression algorithm was resolved.");
                OperationHelpers.ThrowIfNull(
                    CompressionService, "Compression was requested but no compression service is configured. Provide an ICompressionService when creating FileStorageService.");

                using var inputHashStream = new HashingStream(processingStream, originalHashAlgo);
                using var compressedHashAlgo = hashAlg.Create();
                using var compressedCounter = new CountingStream(intermediateStream ?? outputStream);
                using var compressedHashStream = new HashingStream(compressedCounter, compressedHashAlgo);
                await CompressionService.Resolver.CompressAsync(inputHashStream, compressedHashStream, selectedCompressAlgorithm, chunkSize, ct: ct).ConfigureAwait(false);
                await compressedHashStream.FlushAsync(ct).ConfigureAwait(false);
                await inputHashStream.FlushAsync(ct).ConfigureAwait(false);
                compressedSize = compressedCounter.BytesWritten;
                compressedHash = compressedHashStream.GetHash();
                originalHash = inputHashStream.GetHash();
                fileExtension = selectedCompressAlgorithm.Extension;
                compressionAlgorithm = selectedCompressAlgorithm;

                // Without staging the compressed bytes already landed in storage, so nothing is left for the stage below to forward.
                processingStream = intermediateStream ?? Stream.Null;
                Logger.LogDebug("Compressed file {FileId}: {OriginalSize} -> {CompressedSize} bytes", fileId, originalSize, compressedSize);
            }
            else if (intermediateStream != null) {
                // No compression: hash the original while copying to the intermediate
                using var outputHashStream = new HashingStream(intermediateStream, originalHashAlgo);
                await processingStream.CopyToAsync(outputHashStream, CopyToBufferSizeBytes, ct).ConfigureAwait(false);
                await outputHashStream.FlushAsync(ct).ConfigureAwait(false);
                originalHash = outputHashStream.GetHash();
                processingStream = intermediateStream;
            }
            else {
                // Single pass, no compression: hash the plaintext as the stage below pulls it through.
                undrainedPlaintext = new(processingStream, originalHashAlgo);
                processingStream = undrainedPlaintext;
            }

            if (intermediateStream != null)
                processingStream.Position = 0;

            // Duplicate check only after the hash is known
            if (Options.EnableDuplicateDetection) {
                // Detection on means staging ran, so the hash is already settled here.
                var existingMetadata = await MetadataService.FindByHashAsync(originalHash!, ct).ConfigureAwait(false);
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

            // Encryption pass
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
                originalHash ??= undrainedPlaintext!.GetHash();
                await DisposeStreamAsync(outputStream).ConfigureAwait(false);
                outputStream = null; // Prevent double disposal
                fileExtension = TwoKeyEncryptionService.FileExtension;

                // Read the encrypted DEK from the file we just wrote
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
                sourceFileHash = encryptedHash;
                Logger.LogDebug("Encrypted file {FileId} using two-key encryption: {Size} bytes, KeyVersion: {KeyVersion}", fileId, encryptedSize, dataEncryptionKeyVersion);
            }
            else {
                // No encryption: write straight to output. On the single-pass compress path the bytes are already there and this copies nothing.
                await processingStream.CopyToAsync(outputStream, CopyToBufferSizeBytes, ct).ConfigureAwait(false);
                await outputStream.FlushAsync(ct).ConfigureAwait(false);
                originalHash ??= undrainedPlaintext!.GetHash();
                await DisposeStreamAsync(outputStream).ConfigureAwait(false);
                outputStream = null; // Prevent double disposal
                sourceFileHash = compress ? compressedHash : originalHash;
            }

            finalSize = await GetStorageSizeAsync(fileId, fileExtension, normalizedPathPrefix, ct).ConfigureAwait(false);

            // originalHash was captured during the compression / pass-through stage above. Do not rewind inputStream:
            // it may not be seekable (S3 GetObject / Blob OpenRead streams used by multipart complete, for example).
            var actualOriginalSize = ResolveActualOriginalSize(fileId, originalSize, boundedInput.BytesRead);

            // Derived last so it always names the id the bytes actually landed under. The Overwrite duplicate strategy swaps fileId mid-method.
            var sourceFileName = fileId + fileExtension;
            var stored = new FileStoreResult(
                fileId, originalFileName, actualOriginalSize, originalHash, sourceFileName, finalSize, sourceFileHash ?? originalHash, compress, compressionAlgorithm,
                compressedSize, compressedHash, encrypt, dataEncryptionKeyAlgorithm, keyEncryptionKeyAlgorithm, encryptedSize, encryptedHash, encryptedDataEncryptionKey,
                dataEncryptionKeyId, dataEncryptionKeyVersion, keyEncryptionKeySalt, timestamp, normalizedPathPrefix, hashAlg, contentType, charset, tenantId, availability,
                dekKeyMaterialBytes, Metadata: metadata);

            // Persist metadata through the metadata service
            await MetadataService.SaveMetadataAsync(fileId, stored, ct).ConfigureAwait(false);
            await EnsureAutomaticTagsAsync(stored, ct).ConfigureAwait(false);
            FileSaved?.Invoke(this, new(fileId, FileStoreSnapshot.From(stored), actualOriginalSize, finalSize, compress, encrypt));
            return stored;
        }
        finally {
            undrainedPlaintext?.Dispose();
            if (outputStream != null)
                await DisposeStreamAsync(outputStream).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Returns the byte count actually consumed from the upload and warns when it disagrees with what the caller declared. Does not throw on purpose: staged and multipart
    /// commits pass a declared length they may not know (<c>ObservedSizeBytes ?? 0</c>), so a strict check would break those flows.
    /// </summary>
    private long ResolveActualOriginalSize(Guid fileId, long declaredSize, long actualSize)
    {
        if (declaredSize != actualSize)
            Logger.LogWarning("File {FileId} declared {DeclaredSize} bytes but {ActualSize} bytes were read; recording the actual size", fileId, declaredSize, actualSize);

        return actualSize;
    }

    /// <summary>Buffer for the sequential compress/hash/encrypt path. Large or compressed payloads use a temp file so the full plaintext or compressed blob is not held in memory.</summary>
    private static Stream CreateSequentialStagingStream(long originalSize, bool compress)
    {
        const long largePlainThresholdBytes = 64L * 1024 * 1024;
        const long compressStagingThresholdBytes = 16L * 1024 * 1024;
        var useTempFile = originalSize > largePlainThresholdBytes || (compress && originalSize > compressStagingThresholdBytes);
        if (!useTempFile) {
            // originalSize is caller-declared and unverified, so it only picks the spool shape and never the up-front buffer. MemoryStream doubles as it fills, so an
            // overstated length costs a few extra growths rather than a buffer the payload never needs.
            const long maxInitialCapacityBytes = 1024 * 1024;
            return new MemoryStream((int)Math.Max(0, Math.Min(originalSize, maxInitialCapacityBytes)));
        }

        return TempSpool.CreateStream(TempSpool.CreatePath("fs-staging"));
    }

    /// <summary>Best-effort cleanup of a partially written backend object when a save pipeline fails after the object was created. Never throws.</summary>
    protected async Task TryCleanupPartialFileAsync(Guid fileId, string? normalizedPathPrefix)
    {
        try {
            await CleanupPartialFileAsync(fileId, normalizedPathPrefix, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "Best-effort cleanup of partial file {FileId} failed", fileId);
        }
    }

    /// <summary>Single-line, length-capped, audit-safe cleanup of failure messages written into audit events.</summary>
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

        // Load metadata before reading bytes
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

        // Read file data through the streaming pipeline
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

        // Decode through the same pipeline GetFileStreamAsync uses, so encrypted and compressed payloads stay bounded by pipe backpressure instead of being staged in a
        // MemoryStream per transform. HashVerifyingReadStream checks integrity inline, which also removes the second linear pass over the payload.
        var decoded = storageStream;
        if (metadata.IsEncrypted || metadata.IsCompressed) {
            int? chunkSize = metadata.CompressedFileSize.HasValue ? StreamChunkSizeHelper.DetermineChunkSize(metadata.CompressedFileSize.Value) : null;
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var pipePlain = new Pipe();
            var pipelineTask = _streamingPipelines.RunStreamingDecodePipelineAsync(
                storageStream, metadata, pipePlain.Writer, chunkSize, compressionAlgorithmOverride, linkedCts.Token);

            decoded = new PipelineFileReadStream(pipePlain.Reader.AsStream(), pipelineTask, linkedCts);
            if (Options.MaxDecompressedFileSize is { } maxDecompressedBytes)
                decoded = new MaxDecompressedBytesReadStream(decoded, maxDecompressedBytes, fileId);
        }

        var hashAlg = metadata.HashAlgorithm ?? HashAlgorithm.Sha256;
        var verifying = new HashVerifyingReadStream(decoded, hashAlg.Create(), metadata.OriginalFileHash, Options.ThrowOnHashMismatch, Logger, fileId);
        try {
            data = await ReadAllBytesAsync(verifying, metadata.OriginalFileSize, ct).ConfigureAwait(false);
        }
        finally {
            // Disposal is what runs the integrity check, so it has to finish before the success signals below. It only throws once the stream reached EOF, which means a
            // failed read above can never be masked by a mismatch raised here.
            await DisposeStreamAsync(verifying).ConfigureAwait(false);
        }

        sw.Stop();
        Logger.LogInformation("Retrieved file {FileId} successfully. Size: {Size} bytes", fileId, data.Length);

        // Raise FileRetrieved
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

    /// <summary>
    /// Drains <paramref name="source" /> into a single array sized from <paramref name="expectedLength" />, so a payload that matches its metadata costs one allocation. Always
    /// reads through to EOF: a hash-verifying source only checks integrity once the stream is fully drained. Tampered objects that are shorter or longer than the recorded length still
    /// come back in full, which is what makes the mismatch visible to the caller.
    /// </summary>
    private static async Task<byte[]> ReadAllBytesAsync(Stream source, long expectedLength, CancellationToken ct)
    {
        if (expectedLength <= 0 || expectedLength > int.MaxValue) {
            using var growable = new MemoryStream();
            await source.CopyToAsync(growable, CopyToBufferSizeBytes, ct).ConfigureAwait(false);
            return growable.ToArray();
        }

        var buffer = new byte[(int)expectedLength];
        var filled = 0;
        while (filled < buffer.Length) {
            var read = await source.ReadAsync(buffer, filled, buffer.Length - filled, ct).ConfigureAwait(false);
            if (read == 0)
                break;

            filled += read;
        }

        if (filled < buffer.Length)
            return buffer.AsSpan(0, filled).ToArray();

        // Metadata says the payload ends here. Confirm it does, which is also what takes the source to EOF and arms its integrity check.
        var probe = new byte[1];
        if (await source.ReadAsync(probe, 0, 1, ct).ConfigureAwait(false) == 0)
            return buffer;

        using var spill = new MemoryStream();
        await spill.WriteAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
        await spill.WriteAsync(probe, 0, 1, ct).ConfigureAwait(false);
        await source.CopyToAsync(spill, CopyToBufferSizeBytes, ct).ConfigureAwait(false);
        return spill.ToArray();
    }

    /// <summary>
    /// Opens a byte slice of an uncompressed object. Returns null when <see cref="SupportsPlainByteRange" /> is false, or when the object is missing and
    /// <see cref="FileStorageServiceBaseOptions.ThrowOnFileNotFound" /> is false. Encrypted objects are decrypted from the chunk that holds <paramref name="startInclusive" />. Compressed
    /// objects throw. The slice is not hash-verified. The caller disposes the stream.
    /// </summary>
    /// <param name="fileId">Id of the file to read.</param>
    /// <param name="pathPrefix">Prefix stored on the file. Null uses the prefix on metadata.</param>
    /// <param name="startInclusive">First plaintext byte, zero-based.</param>
    /// <param name="endInclusive">Last plaintext byte, inclusive.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<Stream?> OpenPlainByteRangeAsync(Guid fileId, string? pathPrefix, long startInclusive, long endInclusive, CancellationToken ct = default)
        => OpenPlainByteRangeAsync(fileId, pathPrefix, null, startInclusive, endInclusive, ct);

    /// <summary>
    /// Opens a byte slice using metadata the caller already loaded. Does not read metadata again. Same rules as
    /// <see cref="OpenPlainByteRangeAsync(Guid, string?, long, long, CancellationToken)" />.
    /// </summary>
    /// <param name="metadata">Stored file row, including encryption flags and <see cref="FileStoreResult.SourceFileName" />.</param>
    /// <param name="startInclusive">First plaintext byte, zero-based.</param>
    /// <param name="endInclusive">Last plaintext byte, inclusive.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<Stream?> OpenPlainByteRangeAsync(FileStoreResult metadata, long startInclusive, long endInclusive, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(metadata);
        return OpenPlainByteRangeAsync(metadata.Id, metadata.PathPrefix, metadata, startInclusive, endInclusive, ct);
    }

    private async Task<Stream?> OpenPlainByteRangeAsync(Guid fileId, string? pathPrefix, FileStoreResult? metadata, long startInclusive, long endInclusive, CancellationToken ct)
    {
        if (!SupportsPlainByteRange)
            return null;

        ArgumentHelpers.ThrowIfNegative(startInclusive);
        ArgumentHelpers.ThrowIfLessThan(endInclusive, startInclusive);
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.GetDuration)]);
        var sw = Stopwatch.StartNew();
        if (metadata == null) {
            try {
                metadata = await GetMetadataAsync(fileId, ct).ConfigureAwait(false);
            }
            catch (FileNotFoundException) when (!Options.ThrowOnFileNotFound) {
                sw.Stop();
                return null;
            }

            OperationHelpers.ThrowIfNull(metadata, $"Metadata for file {fileId} was not found. The file may have been deleted or the metadata store may be unavailable.");
        }

        if (pathPrefix != null)
            metadata = metadata with { PathPrefix = pathPrefix };

        if (metadata.IsCompressed)
            throw new InvalidOperationException($"File {fileId} is compressed. Byte-range reads are only valid for uncompressed objects.");

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

        Stream? stream;
        if (metadata.IsEncrypted)
            stream = await OpenEncryptedPlainByteRangeAsync(metadata, startInclusive, endInclusive, ct).ConfigureAwait(false);
        else {
            var storageStream = await ReadStoredRangeAsync(metadata, startInclusive, endInclusive, ct).ConfigureAwait(false);
            stream = storageStream == null ? null : new BoundedReadStream(storageStream, endInclusive - startInclusive + 1);
        }

        if (stream == null) {
            sw.Stop();
            if (Options.ThrowOnFileNotFound)
                throw new FileNotFoundException($"File with ID {fileId} not found", fileId.ToString());

            Logger.LogDebug("File storage stream not found for {FileId}, returning null", fileId);
            return null;
        }

        sw.Stop();
        Logger.LogInformation("Streaming plain byte range {Start}-{End} of file {FileId}", startInclusive, endInclusive, fileId);
        Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.GetSuccess)]);
        RaiseReadAuditInBackground(metadata);
        FileRetrieved?.Invoke(this, new(fileId, metadata.OriginalFileSize, metadata.IsCompressed, metadata.IsEncrypted));
        return stream;
    }

    private async Task<Stream?> OpenEncryptedPlainByteRangeAsync(FileStoreResult metadata, long startInclusive, long endInclusive, CancellationToken ct)
    {
        OperationHelpers.ThrowIfNull(
            TwoKeyEncryptionService,
            $"File {metadata.Id} is encrypted but no encryption service is configured. Provide an ITwoKeyEncryptionService instance when creating FileStorageService.");

        var layout = await GetEncryptedLayoutAsync(metadata, ct).ConfigureAwait(false);
        var length = endInclusive - startInclusive + 1;
        var chunkIndex = startInclusive / layout.ChunkSize;
        var skip = (int)(startInclusive % layout.ChunkSize);
        var cipherStart = layout.HeaderLength + chunkIndex * layout.FrameSize;
        var encryptedSize = metadata.EncryptedFileSize ?? 0;
        if (encryptedSize <= 0 || cipherStart >= encryptedSize)
            throw new InvalidDataException($"File {metadata.Id} has no ciphertext at plaintext offset {startInclusive}.");

        var cipher = await ReadStoredRangeAsync(metadata, cipherStart, encryptedSize - 1, ct).ConfigureAwait(false);
        if (cipher == null)
            return null;

        var pipe = new Pipe();
        var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var task = DecryptRangeIntoAsync(cipher, pipe.Writer, layout, chunkIndex, skip, length, linked.Token);
        return new PipelineFileReadStream(new BoundedReadStream(pipe.Reader.AsStream(), length), task, linked);
    }

    private async Task<EncryptedStreamLayout> GetEncryptedLayoutAsync(FileStoreResult metadata, CancellationToken ct)
    {
        if (_encryptedRangeLayouts.TryGetValue(metadata.Id, out var cached))
            return cached;

        var encryptedSize = metadata.EncryptedFileSize ?? 0;
        if (encryptedSize <= 0)
            throw new InvalidDataException($"File {metadata.Id} has no encrypted size, so a byte range cannot be mapped onto ciphertext.");

        var prefixEnd = Math.Min(encryptedSize, 64 * 1024) - 1;
        var prefix = await ReadStoredRangeAsync(metadata, 0, prefixEnd, ct).ConfigureAwait(false);
        if (prefix == null)
            throw new FileNotFoundException($"File with ID {metadata.Id} not found", metadata.Id.ToString());

        EncryptedStreamLayout layout;
        try {
            layout = await TwoKeyEncryptionService!.ReadEncryptedStreamLayoutAsync(prefix, ct).ConfigureAwait(false);
        }
        finally {
            prefix.Dispose();
        }

        var stored = _encryptedRangeLayouts.GetOrAdd(metadata.Id, layout);
        if (!ReferenceEquals(stored, layout))
            layout.Dispose();

        return stored;
    }

    private async Task DecryptRangeIntoAsync(Stream cipher, PipeWriter writer, EncryptedStreamLayout layout, long chunkIndex, int skip, long length, CancellationToken ct)
    {
        var plainOut = writer.AsStream(true);
        try {
            try {
                await TwoKeyEncryptionService!.DecryptFromChunkAsync(layout.Header, cipher, plainOut, chunkIndex, skip, length, layout.Dek, ct).ConfigureAwait(false);
            }
            finally {
                cipher.Dispose();
            }

            await writer.CompleteAsync().ConfigureAwait(false);
        }
        catch (Exception ex) {
            try {
                await writer.CompleteAsync(ex).ConfigureAwait(false);
            }
            catch (InvalidOperationException) {
                // The writer was already completed.
            }

            if (ex is not OperationCanceledException)
                throw;
        }
        finally {
            plainOut.Dispose();
        }
    }

    private async Task<Stream?> ReadStoredRangeAsync(FileStoreResult metadata, long startInclusive, long endInclusive, CancellationToken ct)
    {
        PlainByteRangeReadStart = startInclusive;
        return await ReadPlainByteRangeAsync(metadata, startInclusive, endInclusive, ct).ConfigureAwait(false);
    }

    private void RaiseReadAuditInBackground(FileStoreResult metadata)
    {
        var task = RaiseFileAuditAsync(
            new(
                FileAuditEventType.Read, DateTime.UtcNow, metadata.Id, metadata.TenantId, OperationContextAccessor.Current?.ActorId, metadata.DataEncryptionKeyId,
                metadata.DataEncryptionKeyVersion, FileAuditOutcome.Success), CancellationToken.None);

        _ = task.ContinueWith(
            t => Logger.LogWarning(t.Exception, "File read audit failed for {FileId}", metadata.Id), CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }

    /// <inheritdoc />
    public async Task<Stream?> GetFileStreamAsync(Guid fileId, CompressionAlgorithm? compressionAlgorithmOverride = null, CancellationToken ct = default)
    {
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.GetDuration)]);
        var sw = Stopwatch.StartNew();
        Logger.LogDebug("Retrieving file {FileId} as stream", fileId);

        // Load metadata before reading bytes
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

        // Plain files: wrap the storage stream directly. True end-to-end streaming, no buffering.
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

        // Encrypted and/or compressed: decrypt → [compressed pipe] → decompress via System.IO.Pipelines (bounded RAM via backpressure, same idea as the save pipeline).
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

            // Tombstone (or purge) metadata first so a transient storage-delete failure does not leave a callable record pointing at a half-deleted blob.
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

            // Metadata is tombstoned or purged before the storage delete, so by the time we get here the record may already be gone. Returning false would tell the caller
            // "nothing changed", which is wrong. Rethrow so they can see that the delete is in an indeterminate state.
            throw;
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

    /// <inheritdoc />
    public virtual async Task<FileContentIntegrity> GetContentIntegrityAsync(Guid fileId, CancellationToken ct = default)
    {
        var metadata = await GetMetadataAsync(fileId, ct).ConfigureAwait(false);
        return FileContentIntegrity.From(metadata);
    }

    /// <summary>Raises <see cref="FileNotAvailableException" /> when metadata says the payload is not readable under the current policy.</summary>
    protected void EnsureReadableAvailability(FileStoreResult metadata)
    {
        if (metadata.Availability == FileAvailability.Available)
            return;

        if (metadata.Availability == FileAvailability.Quarantined && Options.AllowReadQuarantinedForAdmin)
            return;

        throw new FileNotAvailableException(metadata.Id, metadata.Availability);
    }

    /// <summary>Applies <see cref="FileStorageServiceBaseOptions.DuplicateStrategy" /> after the canonical hash for a new upload is known.</summary>
    /// <returns>Existing metadata that short-circuits the save, or <see langword="null" /> to keep writing a new object.</returns>
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

                // Drop any file we started creating
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
                // Drop the file we started creating (it will be recreated with the same id)
                await CleanupPartialFileAsync(newFileId, normalizedPathPrefix, ct).ConfigureAwait(false);
                // Remove the old file
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
    protected Task<FileStoreResult> PersistPendingPlainDirectUploadMetadataAsync(
        Guid fileId,
        DirectUploadBeginRequest request,
        string normalizedPathPrefix,
        CancellationToken ct,
        HashAlgorithm? verifiedChecksumAlgorithm = null)
        => _plainDirectUpload.PersistPendingPlainDirectUploadMetadataAsync(fileId, request, normalizedPathPrefix, ct, verifiedChecksumAlgorithm);

    /// <inheritdoc cref="PlainDirectUploadCoordinator.FinalizePendingPlainDirectUploadCoreAsync" />
    protected async Task<FileStoreResult> FinalizePendingPlainDirectUploadCoreAsync(Guid fileId, DirectUploadCompleteRequest? completeRequest, CancellationToken ct)
    {
        var stored = await _plainDirectUpload.FinalizePendingPlainDirectUploadCoreAsync(fileId, completeRequest, ct).ConfigureAwait(false);
        await EnsureAutomaticTagsAsync(stored, ct).ConfigureAwait(false);
        return stored;
    }

    /// <summary>Matches stored <see cref="FileStoreResult.SourceFileName" /> suffix rules (plaintext uses <see cref="Guid.ToString()" />; hashed paths append extensions).</summary>
    protected static string InferTrailingSuffixAfterFileId(Guid id, string? sourceFileName) => PlainDirectUploadCoordinator.InferTrailingSuffixAfterFileId(id, sourceFileName);

    /// <inheritdoc cref="PlainDirectUploadCoordinator.RecordCopyMetadataAsync" />
    protected async Task<FileStoreResult> RecordCopyMetadataAsync(Guid sourceFileId, FileStoreResult sourceMeta, Guid destId, CopyFileRequest? request, CancellationToken ct)
    {
        var stored = await _plainDirectUpload.RecordCopyMetadataAsync(sourceFileId, sourceMeta, destId, request, ct).ConfigureAwait(false);
        foreach (var tag in await MetadataService.ListFileTagsAsync(sourceFileId, ct).ConfigureAwait(false)) {
            if (!tag.Automatic)
                await MetadataService.AddFileTagAsync(destId, tag.TagType, tag.Value, tag.Slug, false, ct).ConfigureAwait(false);
        }

        await EnsureAutomaticTagsAsync(stored, ct).ConfigureAwait(false);
        return stored;
    }

    /// <inheritdoc cref="PlainDirectUploadCoordinator.RecordMoveMetadataAsync" />
    protected Task<FileStoreResult> RecordMoveMetadataAsync(FileStoreResult sourceMeta, string? destPathPrefix, CancellationToken ct)
        => _plainDirectUpload.RecordMoveMetadataAsync(sourceMeta, destPathPrefix, ct);

    /// <inheritdoc cref="PlainDirectUploadCoordinator.RecordRenameMetadataAsync" />
    protected async Task<FileStoreResult> RecordRenameMetadataAsync(FileStoreResult sourceMeta, string originalFileName, CancellationToken ct)
    {
        var stored = await _plainDirectUpload.RecordRenameMetadataAsync(sourceMeta, originalFileName, ct).ConfigureAwait(false);
        await EnsureAutomaticTagsAsync(stored, ct).ConfigureAwait(false);
        return stored;
    }

    /// <summary>Copies every tag onto the destination catalog for a byte-preserving transfer that keeps <paramref name="row" />'s id, then fills <c>imported</c> when the source had none.</summary>
    internal async Task CopyCatalogTagsAsync(FileStorageServiceBase source, FileStoreResult row, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(source);
        ArgumentHelpers.ThrowIfNull(row);
        var tags = await source.MetadataService.ListFileTagsAsync(row.Id, ct).ConfigureAwait(false);
        foreach (var tag in tags)
            await MetadataService.AddFileTagAsync(row.Id, tag.TagType, tag.Value, tag.Slug, tag.Automatic, ct).ConfigureAwait(false);

        if (!tags.Any(t => t.Automatic && t.TagType == FileTagTypes.Imported))
            await EnsureAutomaticTagsAsync(row, ct).ConfigureAwait(false);
    }

    /// <summary>Inserts <c>imported</c> once and replaces <c>file-type</c> from the display name. No-ops when this service has no tag store.</summary>
    protected async Task EnsureAutomaticTagsAsync(FileStoreResult stored, CancellationToken ct)
    {
        var existing = await MetadataService.ListFileTagsAsync(stored.Id, ct).ConfigureAwait(false);
        if (!existing.Any(t => t.Automatic && t.TagType == FileTagTypes.Imported))
            await MetadataService.AddFileTagAsync(stored.Id, FileTagTypes.Imported, FileTagTypes.FormatImported(stored.Timestamp), null, true, ct).ConfigureAwait(false);

        var fileType = FileTagTypes.FileTypeValue(stored.OriginalFileName);
        var current = existing.Where(t => t.Automatic && t.TagType == FileTagTypes.FileType).ToArray();
        if (current.Length == 1 && current[0].Value == fileType)
            return;

        foreach (var old in current)
            await MetadataService.RemoveFileTagAsync(stored.Id, old.TagType, old.Value, ct).ConfigureAwait(false);

        await MetadataService.AddFileTagAsync(stored.Id, FileTagTypes.FileType, fileType, null, true, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FileTag> AddTagAsync(Guid fileId, string tagType, string value, string? slug = null, CancellationToken ct = default)
    {
        FileTagTypes.Validate(tagType, value, slug);
        if (FileTagTypes.IsReserved(tagType))
            throw new ArgumentException($"Tag type '{tagType}' is reserved.", nameof(tagType));

        _ = await GetMetadataAsync(fileId, ct).ConfigureAwait(false);
        return await MetadataService.AddFileTagAsync(fileId, tagType, value, slug, false, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> RemoveTagAsync(Guid fileId, string tagType, string value, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tagType);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value);
        if (FileTagTypes.IsReserved(tagType))
            throw new ArgumentException($"Tag type '{tagType}' is reserved.", nameof(tagType));

        return await MetadataService.RemoveFileTagAsync(fileId, tagType, value, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<FileTag>> GetTagsAsync(Guid fileId, CancellationToken ct = default) => MetadataService.ListFileTagsAsync(fileId, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileTagTypeInfo>> GetTagTypesAsync(CancellationToken ct = default)
    {
        var manual = await MetadataService.ListManualTagTypesAsync(ct).ConfigureAwait(false);
        var types = new List<FileTagTypeInfo> {
            new(FileTagTypes.FileType, true, FileTagValueKind.String, FileTagSort.Ordinal, [FileTagGrain.Value]),
            new(FileTagTypes.Imported, true, FileTagValueKind.DateTime, FileTagSort.Chronological, [FileTagGrain.Year, FileTagGrain.Month, FileTagGrain.Day])
        };

        foreach (var name in manual.OrderBy(static n => n, StringComparer.OrdinalIgnoreCase)) {
            if (!FileTagTypes.IsReserved(name))
                types.Add(new(name, false, FileTagValueKind.String, FileTagSort.Ordinal, [FileTagGrain.Value]));
        }

        return types;
    }

    /// <inheritdoc />
    public async Task<int> StampAutomaticTagsAsync(CancellationToken ct = default)
    {
        var files = await MetadataService.ListByPathPrefixAsync(null, true, int.MaxValue, ct).ConfigureAwait(false);
        var checkedFiles = 0;
        foreach (var file in files) {
            if (file.Availability == FileAvailability.PendingDirectUpload)
                continue;

            await EnsureAutomaticTagsAsync(file, ct).ConfigureAwait(false);
            checkedFiles++;
        }

        return checkedFiles;
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<FileStorageFolderEntry> Entries, bool Truncated)> ListTagGroupAsync(
        IReadOnlyList<FileTagGroupKey> groupBy,
        string? path,
        int maxKeys = 10_000,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(groupBy);
        var page = await MetadataService.ListTagGroupAsync(groupBy, path, maxKeys, ct).ConfigureAwait(false);
        var entries = new List<FileStorageFolderEntry>(page.Entries.Count);
        foreach (var entry in page.Entries) {
            entries.Add(new(entry.Name, entry.IsDirectory, entry.VirtualPath, entry.FileId, FileStoragePresence.Store, entry.OriginalFileName, entry.OriginalFileSize, null));
        }

        return (entries, page.Truncated);
    }

#endregion
}