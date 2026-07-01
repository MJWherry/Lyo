using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Lyo.Common.Records;
using Lyo.Compression;
using Lyo.Encryption;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.Health;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using DiskFileStorageOptions = Lyo.FileStorage.Models.DiskFileStorageOptions;

namespace Lyo.FileStorage;

/// <summary>Filesystem-backed <see cref="IFileStorageService" /> under <see cref="DiskFileStorageOptions.RootDirectoryPath" />.</summary>
public class LocalFileStorageService : FileStorageServiceBase, IFileStorageDiagnosticsService
{
    private readonly DiskFileStorageOptions _options;
    private readonly bool _ownsMetadataService;

    public LocalFileStorageService(
        DiskFileStorageOptions options,
        ILoggerFactory? loggerFactory = null,
        ICompressionService? compressionService = null,
        ITwoKeyEncryptionService? twoKeyEncryptionService = null,
        IFileMetadataStore? metadataService = null,
        IMetrics? metrics = null,
        IFileOperationContextAccessor? operationContextAccessor = null,
        IEnumerable<IFileAuditEventHandler>? auditHandlers = null,
        IFileContentPolicy? contentPolicy = null,
        IFileMalwareScanner? malwareScanner = null)
        : base(
            ArgumentHelpers.ThrowIfNullReturn(options), metadataService ?? new LocalFileMetadataStore(options.RootDirectoryPath, loggerFactory),
            (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<LocalFileStorageService>(), compressionService, twoKeyEncryptionService,
            options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance, operationContextAccessor, auditHandlers, contentPolicy, malwareScanner)
    {
        _options = options;
        _ownsMetadataService = metadataService == null;
        if (_ownsMetadataService)
            Logger.LogInformation("Using JSON file-based metadata storage");

        if (Directory.Exists(_options.RootDirectoryPath))
            return;

        Directory.CreateDirectory(_options.RootDirectoryPath);
        Logger.LogInformation("Created root directory: {RootPath}", _options.RootDirectoryPath);
    }

    /// <inheritdoc />
    Task<IReadOnlyList<string>> IFileStorageDiagnosticsService.ListStorageKeysAsync(string? prefix, int maxKeys, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentHelpers.ThrowIfLessThan(maxKeys, 1);
        var cap = Math.Min(maxKeys, 10_000);
        var storageRootFull = NormalizeStorageRootFullPath();
        var startDir = ResolveListingDirectory(storageRootFull, prefix);
        var list = new List<string>();
        foreach (var absolute in EnumerateLimitedFilesUnderDirectory(storageRootFull, startDir, cap, ct))
            list.Add(ToStorageKeyRelativeToRoot(storageRootFull, absolute));

        return Task.FromResult<IReadOnlyList<string>>(list);
    }

    protected override async Task<HealthResult> CheckHealthLightweightAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try {
            Directory.CreateDirectory(_options.RootDirectoryPath);
            var probe = Path.Combine(_options.RootDirectoryPath, ".lyo-fs-health-write.tmp");
#if NETSTANDARD2_0
            ct.ThrowIfCancellationRequested();
            File.WriteAllText(probe, DateTime.UtcNow.ToString("O"));
#else
            await File.WriteAllTextAsync(probe, DateTime.UtcNow.ToString("O"), ct).ConfigureAwait(false);
#endif
            File.Delete(probe);
            sw.Stop();
            return HealthResult.Healthy(sw.Elapsed, null, new Dictionary<string, object?> { ["rootDirectoryPath"] = _options.RootDirectoryPath });
        }
        catch (Exception ex) {
            sw.Stop();
            return HealthResult.Unhealthy(sw.Elapsed, ex.Message, null, ex);
        }
    }

    /// <inheritdoc />
    public override Task<FileStoreResult> CompleteDirectUploadAsync(Guid fileId, DirectUploadCompleteRequest? completeRequest = null, CancellationToken ct = default)
        => FinalizePendingPlainDirectUploadCoreAsync(fileId, completeRequest, ct);

    /// <inheritdoc />
    public override async Task<DirectUploadBeginResult> BeginDirectUploadAsync(DirectUploadBeginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.DirectUploadReceiveBaseUri))
            return await base.BeginDirectUploadAsync(request, ct).ConfigureAwait(false);

        ArgumentHelpers.ThrowIfNull(request);
        var normalized = NormalizePathPrefix(request.PathPrefix) ?? "";
        var fileId = Guid.NewGuid();
        await PersistPendingPlainDirectUploadMetadataAsync(fileId, request, normalized, ct).ConfigureAwait(false);
        var trimmedBase = _options.DirectUploadReceiveBaseUri!.Trim().TrimEnd('/');
        var routeTrim = _options.DirectUploadPutRouteRelativePath.Trim().Trim('/');
        var putUrl = $"{trimmedBase}/{routeTrim}/{fileId:D}/put";
        var expiry = request.UrlExpiration ?? TimeSpan.FromHours(1);
        var storageRootFull = NormalizeStorageRootFullPath();
        var fullPathForId = TrimEnds(Path.GetFullPath(GetFilePath(fileId, "", normalized)));
        EnsurePathUnder(storageRootFull, fullPathForId);
        var storageRelative = ToStorageKeyRelativeToRoot(storageRootFull, fullPathForId);
        return new() {
            FileId = fileId,
            PresignedPutUrl = putUrl,
            UrlExpiresUtc = DateTimeOffset.UtcNow.Add(expiry),
            StorageLocation = storageRelative,
            RequiredPutHeaders = null
        };
    }

    /// <summary>Accepts a raw HTTP PUT body for a pending plaintext direct upload. Use with a trusted host (e.g. <c>Lyo.TestApi</c> Workbench).</summary>
    /// <remarks>Enforces <see cref="FileStorageServiceBaseOptions.MaxUploadSizeBytes" /> during the copy so an attacker cannot exhaust disk before finalize re-checks the size.</remarks>
    public async Task ReceiveWorkbenchDirectPutAsync(Guid fileId, Stream body, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(body);
        var meta = await GetMetadataAsync(fileId, ct).ConfigureAwait(false);
        if (meta.Availability != FileAvailability.PendingDirectUpload)
            throw new InvalidOperationException($"File {fileId} is not pending direct upload (availability={meta.Availability}).");

        var output = await CreateOutputStreamAsync(fileId, "", meta.PathPrefix, ct).ConfigureAwait(false);
        try {
            var max = Options.MaxUploadSizeBytes;
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
                        throw new InvalidOperationException($"PUT body for {fileId} exceeded MaxUploadSizeBytes ({max.Value} bytes) during receive.");

                    await output.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
                }
            }
            finally {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        finally {
            output.Dispose();
        }
    }

    /// <inheritdoc />
    public override async Task<FileStoreResult> CopyFileAsync(Guid sourceFileId, CopyFileRequest? request = null, CancellationToken ct = default)
    {
        var meta = await GetMetadataAsync(sourceFileId, ct).ConfigureAwait(false);
        EnsureReadableAvailability(meta);
        OperationHelpers.ThrowIf(meta.Availability == FileAvailability.PendingDirectUpload, $"Cannot copy file {sourceFileId} pending direct upload.");
        var srcPath = FindFilePath(sourceFileId, meta.PathPrefix);
        if (srcPath == null || !File.Exists(srcPath))
            throw new FileNotFoundException($"Source file path not found for id {sourceFileId}");

        var destId = Guid.NewGuid();
        var destPrefix = NormalizePathPrefix(request?.PathPrefix ?? meta.PathPrefix);
        var suffix = InferTrailingSuffixAfterFileId(meta.Id, meta.SourceFileName);
        try {
            var destPath = GetFilePath(destId, suffix, destPrefix);
            var destDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDir))
                Directory.CreateDirectory(destDir);

            File.Copy(srcPath, destPath, true);
            return await RecordCopyMetadataAsync(sourceFileId, meta, destId, request, ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            await RaiseFileAuditAsync(
                    new(
                        FileAuditEventType.Copy, DateTime.UtcNow, destId, meta.TenantId, OperationContextAccessor.Current?.ActorId, meta.DataEncryptionKeyId,
                        meta.DataEncryptionKeyVersion, FileAuditOutcome.Failure, SanitizeAuditError(ex.Message), sourceFileId), ct)
                .ConfigureAwait(false);

            throw;
        }
    }

    /// <inheritdoc />
    public override async Task<DekMigrationResult> MigrateDeksAsync(
        string sourceKeyId,
        string? sourceKeyVersion = null,
        string? targetKeyId = null,
        string? targetKeyVersion = null,
        int batchSize = 100,
        CancellationToken ct = default)
    {
        OperationHelpers.ThrowIfNull(TwoKeyEncryptionService, "ITwoKeyEncryptionService is not configured. Cannot migrate DEKs without encryption service.");
        return await base.MigrateDeksAsync(sourceKeyId, sourceKeyVersion, targetKeyId, targetKeyVersion, batchSize, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task<DekMigrationResult> RotateDeksAsync(
        IReadOnlyCollection<Guid> fileIds,
        string? targetKeyId = null,
        string? targetKeyVersion = null,
        int batchSize = 100,
        CancellationToken ct = default)
    {
        OperationHelpers.ThrowIfNull(TwoKeyEncryptionService, "ITwoKeyEncryptionService is not configured. Cannot rotate DEKs without encryption service.");
        return await base.RotateDeksAsync(fileIds, targetKeyId, targetKeyVersion, batchSize, ct).ConfigureAwait(false);
    }

    protected override Task<Stream> CreateOutputStreamAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
    {
        var filePath = GetFilePath(fileId, extension, pathPrefix);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        return Task.FromResult<Stream>(File.Create(filePath));
    }

    protected override Task<long> GetStorageSizeAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
    {
        var filePath = GetFilePath(fileId, extension, pathPrefix);
        return File.Exists(filePath) ? Task.FromResult(new FileInfo(filePath).Length) : Task.FromResult<long>(0);
    }

    protected override Task<Stream?> ReadFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
    {
        var filePath = FindFilePath(fileId, pathPrefix);
        if (filePath != null && File.Exists(filePath))
            return Task.FromResult<Stream?>(File.OpenRead(filePath));

        return Task.FromResult<Stream?>(null);
    }

    protected override Task<bool> DeleteFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
    {
        var filePath = FindFilePath(fileId, pathPrefix);
        if (filePath == null || !File.Exists(filePath))
            return Task.FromResult(false);

        File.Delete(filePath);
        Logger.LogDebug("Deleted file {FileId} at {FilePath}", fileId, filePath);
        return Task.FromResult(true);
    }

    protected override async Task<EncryptionHeaderInfo> ExtractEncryptionHeaderAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
    {
        var filePath = GetFilePath(fileId, extension, pathPrefix);
        ArgumentHelpers.ThrowIfFileNotFound(filePath);
        using var headerReader = File.OpenRead(filePath);
        var header = EncryptionHeader.Read(headerReader);
        return new(header.EncryptedDataEncryptionKey, header.KeyId, header.KeyVersion, header.DekKeyMaterialBytes);
    }

    protected override async Task UpdateFileHeaderAsync(Guid fileId, string? pathPrefix, string targetKeyId, string targetKeyVersion, byte[] newEncryptedDek, CancellationToken ct)
    {
        var filePath = FindFilePath(fileId, pathPrefix);
        if (filePath == null || !File.Exists(filePath))
            throw new FileNotFoundException($"File {fileId} not found on disk; cannot rotate header.", fileId.ToString());

        var fileLength = new FileInfo(filePath).Length;
        if (fileLength < 13)
            throw new InvalidDataException($"File {fileId} has a truncated or invalid encryption header ({fileLength} bytes); cannot rotate DEK.");

        EncryptionHeader oldHeader;
        int oldHeaderSize;
        var readStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
        try {
            ct.ThrowIfCancellationRequested();
            oldHeader = EncryptionHeader.Read(readStream);
            oldHeaderSize = (int)readStream.Position;
        }
        finally {
            readStream.Dispose();
        }

        var updatedHeader = oldHeader.With(targetKeyId, targetKeyVersion, newEncryptedDek);
        var newHeaderBytes = SerializeHeaderToArray(updatedHeader);

        // If the header size is unchanged, do an in-place overwrite to avoid touching the rest of the file.
        if (newHeaderBytes.Length == oldHeaderSize) {
            var writeStream = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
            try {
                await writeStream.WriteAsync(newHeaderBytes, 0, newHeaderBytes.Length, ct).ConfigureAwait(false);
                await writeStream.FlushAsync(ct).ConfigureAwait(false);
            }
            finally {
                writeStream.Dispose();
            }
        }
        else {
            // Stream the rest of the original file (after the old header) into a sibling temp file with the new header prefix, then atomically replace.
            var tempPath = filePath + ".dek-rotate-" + Guid.NewGuid().ToString("N") + ".tmp";
            try {
                var source = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
                try {
                    var destination = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
                    try {
                        await destination.WriteAsync(newHeaderBytes, 0, newHeaderBytes.Length, ct).ConfigureAwait(false);
                        source.Position = oldHeaderSize;
                        await source.CopyToAsync(destination, 81920, ct).ConfigureAwait(false);
                        await destination.FlushAsync(ct).ConfigureAwait(false);
                    }
                    finally {
                        destination.Dispose();
                    }
                }
                finally {
                    source.Dispose();
                }

                File.Replace(tempPath, filePath, null);
            }
            catch {
                try {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch (Exception cleanupEx) {
                    Logger.LogDebug(cleanupEx, "Best-effort delete of DEK-rotate temp file failed: {Path}", tempPath);
                }

                throw;
            }
        }

        Logger.LogDebug("Updated file header for {FileId} with new keyId '{KeyId}', version {Version}, and encrypted DEK", fileId, targetKeyId, targetKeyVersion);
    }

    private static byte[] SerializeHeaderToArray(EncryptionHeader header)
    {
        using var ms = new MemoryStream(header.GetHeaderSize());
        header.Write(ms);
        return ms.ToArray();
    }

    /// <inheritdoc />
    public override Task<string> GetPreSignedReadUrlAsync(Guid fileId, TimeSpan? expiration = null, string? pathPrefix = null, CancellationToken ct = default)
        => GetPreSignedReadUrlAsync(fileId, expiration, pathPrefix, null, ct);

    /// <inheritdoc />
    public override async Task<string> GetPreSignedReadUrlAsync(
        Guid fileId,
        TimeSpan? expiration,
        string? pathPrefix,
        PreSignedReadUrlOptions? urlResponseOptions,
        CancellationToken ct)
    {
        if (!_options.AllowFileUriPresignedUrls)
            return await base.GetPreSignedReadUrlAsync(fileId, expiration, pathPrefix, urlResponseOptions, ct).ConfigureAwait(false);

        if (urlResponseOptions is not null)
            Logger.LogDebug("Ignoring {Options}; file:// presigned URLs do not support response-header overrides.", nameof(PreSignedReadUrlOptions));

        var meta = await GetMetadataAsync(fileId, ct).ConfigureAwait(false);
        EnsureReadableAvailability(meta);
        var filePath = FindFilePath(fileId, pathPrefix);
        if (filePath == null || !File.Exists(filePath))
            throw new FileNotFoundException($"File with ID {fileId} was not found in storage.");

        var uri = new Uri(filePath);
        await RaiseFileAuditAsync(
                new(
                    FileAuditEventType.PresignedRead, DateTime.UtcNow, fileId, meta.TenantId, OperationContextAccessor.Current?.ActorId, meta.DataEncryptionKeyId,
                    meta.DataEncryptionKeyVersion, FileAuditOutcome.Success), ct)
            .ConfigureAwait(false);

        return uri.AbsoluteUri;
    }

    protected override Task CleanupPartialFileAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
    {
        // Try to find and delete any file with this ID (could have different extensions)
        var filePath = FindFilePath(fileId, pathPrefix);
        if (filePath == null || !File.Exists(filePath))
            return Task.CompletedTask;

        try {
            File.Delete(filePath);
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "Failed to delete partial file during cleanup at {Path}", filePath);
        }

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        if (Disposed)
            return;

        if (_ownsMetadataService && MetadataService is IDisposable disposableMetadataService)
            disposableMetadataService.Dispose();

        base.Dispose();
    }

    private string GetFilePath(Guid fileId, string extension = "", string? pathPrefix = null)
    {
        var fileName = fileId.ToString("N") + extension;
        string combined;
        if (!string.IsNullOrWhiteSpace(pathPrefix))
            combined = Path.Combine(_options.RootDirectoryPath, pathPrefix, fileName);
        else {
            var idString = fileId.ToString("N");
            var subDir = Path.Combine(idString.Substring(0, 2), idString.Substring(2, 2));
            combined = Path.Combine(_options.RootDirectoryPath, subDir, fileName);
        }

        // Defense in depth: ensure the resolved path stays under the storage root even if a caller bypassed ValidatePathPrefix upstream.
        var storageRootFull = TrimEnds(Path.GetFullPath(_options.RootDirectoryPath));
        var resolved = TrimEnds(Path.GetFullPath(combined));
        EnsurePathUnder(storageRootFull, resolved);
        return combined;
    }

    private string? FindFilePath(Guid fileId, string? pathPrefix = null)
    {
        var basePath = GetFilePath(fileId, "", pathPrefix);
        var directory = Path.GetDirectoryName(basePath);
        var fileNameWithoutExt = Path.GetFileName(basePath);
        if (directory == null || !Directory.Exists(directory))
            return null;

        var filePath = Path.Combine(directory, fileNameWithoutExt);
        if (File.Exists(filePath))
            return filePath;

        if (CompressionService != null) {
            filePath = Path.Combine(directory, fileNameWithoutExt + CompressionService.FileExtension);
            if (File.Exists(filePath))
                return filePath;
        }

        if (TwoKeyEncryptionService != null) {
            filePath = Path.Combine(directory, fileNameWithoutExt + TwoKeyEncryptionService.FileExtension);
            if (File.Exists(filePath))
                return filePath;

            // Check for compression + two-key encryption combination
            if (CompressionService != null) {
                filePath = Path.Combine(directory, fileNameWithoutExt + CompressionService.FileExtension + TwoKeyEncryptionService.FileExtension);
                if (File.Exists(filePath))
                    return filePath;
            }
        }

        // Try common extensions as fallback
        var commonExtensions = FileTypeInfo.CommonStorageResolutionSuffixes;
        foreach (var ext in commonExtensions) {
            filePath = Path.Combine(directory, fileNameWithoutExt + ext);
            if (File.Exists(filePath))
                return filePath;
        }

        return null;
    }

    private string NormalizeStorageRootFullPath()
    {
        try {
            Directory.CreateDirectory(_options.RootDirectoryPath);
            return TrimEnds(Path.GetFullPath(_options.RootDirectoryPath));
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "Could not normalize root directory path {Root}", _options.RootDirectoryPath);
            throw;
        }
    }

    private static string TrimEnds(string path) => Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private static string NormalizeDiagnosticListingPrefix(string? prefix)
        => prefix == null || string.IsNullOrWhiteSpace(prefix) ? "" : prefix.Trim().TrimStart('/', '\\').TrimEnd('/', '\\');

    /// <exception cref="UnauthorizedAccessException">When <paramref name="extendedPrefix" /> attempts to escape storage root.</exception>
    private static string ResolveListingDirectory(string storageRootFull, string? extendedPrefix)
    {
        var trimmed = NormalizeDiagnosticListingPrefix(extendedPrefix);
        if (string.IsNullOrEmpty(trimmed))
            return storageRootFull;

        if (trimmed.Split('/', '\\').Any(p => string.Equals(p, "..", StringComparison.Ordinal)))
            throw new UnauthorizedAccessException("Diagnostic listing prefix contains path traversal.");

        var relative = trimmed.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        var resolved = TrimEnds(Path.Combine(storageRootFull, relative));
        EnsurePathUnder(storageRootFull, resolved);
        return resolved;
    }

    private static void EnsurePathUnder(string storageRootFull, string resolvedPathAbsolute)
    {
        var cmp = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var rootTrim = TrimEnds(storageRootFull);
        var candTrim = TrimEnds(resolvedPathAbsolute);
        var rootWithSep = rootTrim + Path.DirectorySeparatorChar;
        if (!(candTrim.Equals(rootTrim, cmp) || candTrim.StartsWith(rootWithSep, cmp)))
            throw new UnauthorizedAccessException($"Path '{resolvedPathAbsolute}' is outside storage root.");
    }

    private static IEnumerable<string> EnumerateLimitedFilesUnderDirectory(string storageRootFull, string startDirectory, int maxEntries, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfLessThan(maxEntries, 1);
        if (!Directory.Exists(startDirectory))
            yield break;

        var dirs = new Queue<string>();
        dirs.Enqueue(startDirectory);
        var counted = 0;
        while (dirs.Count > 0 && counted < maxEntries) {
            ct.ThrowIfCancellationRequested();
            string dir;
            try {
                dir = dirs.Dequeue();
            }
            catch {
                yield break;
            }

            IEnumerable<string>? filesEnumerable;
            try {
                filesEnumerable = Directory.EnumerateFiles(dir);
            }
            catch (UnauthorizedAccessException) {
                continue;
            }
            catch (DirectoryNotFoundException) {
                continue;
            }

            if (filesEnumerable != null) {
                foreach (var file in filesEnumerable) {
                    ct.ThrowIfCancellationRequested();
                    var full = TrimEnds(Path.GetFullPath(file));
                    EnsurePathUnder(storageRootFull, full);
                    counted++;
                    yield return full;

                    if (counted >= maxEntries)
                        yield break;
                }
            }

            IEnumerable<string>? subdirs;
            try {
                subdirs = Directory.EnumerateDirectories(dir);
            }
            catch (UnauthorizedAccessException) {
                continue;
            }
            catch (DirectoryNotFoundException) {
                continue;
            }

            if (subdirs != null) {
                foreach (var sub in subdirs)
                    dirs.Enqueue(sub);
            }
        }
    }

    private static string ToStorageKeyRelativeToRoot(string storageRootFull, string absoluteFile)
    {
        var cmp = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var rootTrim = TrimEnds(storageRootFull);
        var candTrim = TrimEnds(Path.GetFullPath(absoluteFile));
        EnsurePathUnder(storageRootFull, candTrim);
        var rootWithSep = rootTrim + Path.DirectorySeparatorChar;
        if (candTrim.Equals(rootTrim, cmp))
            return "";

        if (!candTrim.StartsWith(rootWithSep, cmp))
            throw new UnauthorizedAccessException($"Path '{absoluteFile}' is outside storage root.");

        return candTrim.Substring(rootWithSep.Length).Replace(Path.DirectorySeparatorChar, '/');
    }
}