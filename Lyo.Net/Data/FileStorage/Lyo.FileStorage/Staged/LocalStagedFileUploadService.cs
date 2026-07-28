using System.Buffers;
using Lyo.Common.Extensions;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using DiskFileStorageOptions = Lyo.FileStorage.Models.DiskFileStorageOptions;

namespace Lyo.FileStorage.Staged;

/// <summary>Staged uploads with on-disk staging under <c>{RootDirectoryPath}/.stage/</c> and API receive PUT URLs.</summary>
public sealed class LocalStagedFileUploadService : IStagedFileUploadService
{
    private readonly StagedUploadCoordinator _coordinator;
    private readonly DiskFileStorageOptions _options;
    private readonly LocalStagedFilePhysicalIo _physicalIo;
    private readonly IStagedFileUploadStore _store;

    public LocalStagedFileUploadService(
        LocalFileStorageService storage,
        IStagedFileUploadStore store,
        DiskFileStorageOptions options,
        IFileMalwareScanner? malwareScanner = null,
        IFileContentPolicy? contentPolicy = null,
        IEnumerable<IFileAuditEventHandler>? auditHandlers = null,
        IEnumerable<IStagedFileUploadEventHandler>? eventHandlers = null,
        IFileOperationContextAccessor? operationContextAccessor = null,
        ILoggerFactory? loggerFactory = null,
        IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(storage);
        ArgumentHelpers.ThrowIfNull(store);
        ArgumentHelpers.ThrowIfNull(options);
        _store = store;
        _options = options;
        _physicalIo = new(options);
        var logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<LocalStagedFileUploadService>();
        _coordinator = new(store, _physicalIo, storage, options, contentPolicy, malwareScanner, operationContextAccessor, logger, metrics, auditHandlers, eventHandlers);
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
        if (string.IsNullOrWhiteSpace(_options.DirectUploadReceiveBaseUri))
            throw new ConfigurationException(
                "Local staged upload requires DiskFileStorageOptions.DirectUploadReceiveBaseUri.", nameof(DiskFileStorageOptions.DirectUploadReceiveBaseUri));

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

    /// <summary>Accepts a raw HTTP PUT body for a pending staged upload.</summary>
    public async Task ReceiveWorkbenchStagePutAsync(Guid stageId, Stream body, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(body);
        var record = await _store.GetAsync(stageId, ct).ConfigureAwait(false);
        if (record == null)
            throw new FileNotFoundException($"Staged upload {stageId} was not found.");

        if (record.Status != StagedUploadStatus.PendingUpload)
            throw new ConflictException($"Stage {stageId} is not pending upload (status={record.Status}).");

        var path = _physicalIo.GetAbsolutePath(record);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

#if NETSTANDARD2_0
        using (var output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous)) {
            var max = _options.MaxUploadSizeBytes;
            if (max is null) {
                await body.CopyToAsync(output, 81920, ct).ConfigureAwait(false);
                return;
            }

            const int bufferSize = 81920;
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try {
                long total = 0;
                int read;
                while ((read = await body.ReadAsync(buffer, 0, bufferSize, ct).ConfigureAwait(false)) > 0) {
                    total += read;
                    if (total > max.Value)
                        throw new FilePolicyRejectedException($"PUT body for stage {stageId} exceeded MaxUploadSizeBytes ({max.Value} bytes) during receive.");

                    await output.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
                }
            }
            finally {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
#else
        await using var output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        var max = _options.MaxUploadSizeBytes;
        if (max is null) {
            await body.CopyToAsync(output, 81920, ct).ConfigureAwait(false);
            return;
        }

        const int bufferSize = 81920;
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try {
            long total = 0;
            int read;
            while ((read = await body.ReadAsync(buffer, 0, bufferSize, ct).ConfigureAwait(false)) > 0) {
                total += read;
                if (total > max.Value)
                    throw new FilePolicyRejectedException($"PUT body for stage {stageId} exceeded MaxUploadSizeBytes ({max.Value} bytes) during receive.");

                await output.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
            }
        }
        finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }
#endif
    }

    private sealed class LocalStagedFilePhysicalIo : IStagedFilePhysicalIo
    {
        private readonly DiskFileStorageOptions _options;

        internal LocalStagedFilePhysicalIo(DiskFileStorageOptions options) => _options = options;

        public MultipartUploadProviderKind ProviderKind => MultipartUploadProviderKind.Local;

        public string BuildStageStorageLocation(Guid stageId, string? pathPrefix)
        {
            var relative = BuildRelativePath(stageId, pathPrefix);
            return relative;
        }

        public Task<StagedPresignedPutResult> GeneratePresignedPutUrlAsync(
            Guid stageId,
            string normalizedPathPrefix,
            StagedUploadBeginRequest request,
            DateTimeOffset urlExpiresUtc,
            CancellationToken ct)
        {
            var trimmedBase = _options.DirectUploadReceiveBaseUri!.Trim().TrimEnd('/');
            var routeTrim = _options.StagePutRouteRelativePath.Trim().Trim('/');
            var url = $"{trimmedBase}/{routeTrim}/{stageId:D}/put";
            return Task.FromResult(new StagedPresignedPutResult(url, null));
        }

        public Task<bool> ObjectExistsAsync(StagedFileUploadRecord record, CancellationToken ct) => Task.FromResult(File.Exists(GetAbsolutePath(record)));

        public Task<long> GetObjectSizeAsync(StagedFileUploadRecord record, CancellationToken ct)
        {
            var path = GetAbsolutePath(record);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Staged object not found at {path}");

            return Task.FromResult(new FileInfo(path).Length);
        }

        public Task<Stream> OpenReadStreamAsync(StagedFileUploadRecord record, CancellationToken ct)
        {
            var path = GetAbsolutePath(record);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Staged object not found at {path}");

            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
            return Task.FromResult(stream);
        }

        public Task DeleteStageObjectAsync(StagedFileUploadRecord record, CancellationToken ct)
        {
            var path = GetAbsolutePath(record);
            if (File.Exists(path))
                File.Delete(path);

            var stageDir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(stageDir) && Directory.Exists(stageDir) && !Directory.EnumerateFileSystemEntries(stageDir).Any())
                Directory.Delete(stageDir);

            return Task.CompletedTask;
        }

        internal string GetAbsolutePath(StagedFileUploadRecord record)
        {
            var relative = record.StorageLocation.Replace('\\', '/').TrimStart('/');
            var full = Path.GetFullPath(Path.Combine(_options.RootDirectoryPath, relative.Replace('/', Path.DirectorySeparatorChar)));
            var rootFull = Path.GetFullPath(_options.RootDirectoryPath);
            if (!full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Resolved staged path escapes the storage root.");

            return full;
        }

        private static string BuildRelativePath(Guid stageId, string? pathPrefix)
        {
            var prefix = pathPrefix.IsNullOrWhitespace() ? "" : pathPrefix.Trim().Trim('/') + "/";
            return $"{prefix}.stage/{stageId:N}/object";
        }
    }
}