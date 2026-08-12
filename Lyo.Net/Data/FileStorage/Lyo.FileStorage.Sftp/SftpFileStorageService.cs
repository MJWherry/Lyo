using System.Diagnostics;
using Lyo.Common.Pathing;
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
using Lyo.Sftp.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FileStorage.Sftp;

/// <summary>SFTP-backed <see cref="IFileStorageService" /> (physical IO only; presigned/multipart unsupported).</summary>
public sealed class SftpFileStorageService : FileStorageServiceBase
{
    private readonly ISftpClient _sftp;
    private readonly string _root;

    /// <summary>Creates an SFTP file storage service.</summary>
    public SftpFileStorageService(
        SftpFileStorageOptions options,
        IFileMetadataStore metadataService,
        ISftpClient sftpClient,
        ILoggerFactory? loggerFactory = null,
        ICompressionService? compressionService = null,
        ITwoKeyEncryptionService? twoKeyEncryptionService = null,
        IMetrics? metrics = null,
        IFileOperationContextAccessor? operationContextAccessor = null,
        IEnumerable<IFileAuditEventHandler>? auditHandlers = null,
        IFileContentPolicy? contentPolicy = null,
        IFileMalwareScanner? malwareScanner = null)
        : base(
            ArgumentHelpers.ThrowIfNullReturn(options), ArgumentHelpers.ThrowIfNullReturn(metadataService),
            (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<SftpFileStorageService>(), compressionService, twoKeyEncryptionService, metrics,
            operationContextAccessor, auditHandlers, contentPolicy, malwareScanner)
    {
        ArgumentHelpers.ThrowIfNull(sftpClient);
        _sftp = sftpClient;
        _root = sftpClient.RootRemoteDirectory;
        Logger.LogInformation("Initialized SFTP file storage under {Root}", _root);
    }

    /// <inheritdoc />
    protected override async Task<HealthResult> CheckHealthLightweightAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try {
            await _sftp.HealthPingAsync(ct).ConfigureAwait(false);
            sw.Stop();
            return HealthResult.Healthy(sw.Elapsed, null, new Dictionary<string, object?> { ["root"] = _root });
        }
        catch (Exception ex) {
            sw.Stop();
            return HealthResult.Unhealthy(sw.Elapsed, ex.Message, null, ex);
        }
    }

    /// <inheritdoc />
    protected override async Task<Stream> CreateOutputStreamAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
    {
        var path = GetFilePath(fileId, extension, pathPrefix);
        var parent = PathHelpers.GetDirectoryName(PathStyle.Posix, path);
        if (!string.IsNullOrEmpty(parent))
            await _sftp.CreateDirectoryAsync(parent, ct).ConfigureAwait(false);
        return await _sftp.OpenCreateAsync(path, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task<long> GetStorageSizeAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
    {
        var path = GetFilePath(fileId, extension, pathPrefix);
        return await _sftp.FileExistsAsync(path, ct).ConfigureAwait(false)
            ? await _sftp.GetLengthAsync(path, ct).ConfigureAwait(false)
            : 0L;
    }

    /// <inheritdoc />
    protected override async Task<Stream?> ReadFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
    {
        var path = await FindFilePathAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
        return path is null ? null : await _sftp.OpenReadAsync(path, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task<bool> DeleteFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
    {
        var path = await FindFilePathAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
        if (path is null)
            return false;
        await _sftp.DeleteFileAsync(path, ct).ConfigureAwait(false);
        Logger.LogDebug("Deleted SFTP file {FileId} at {Path}", fileId, path);
        return true;
    }

    /// <inheritdoc />
    protected override async Task<EncryptionHeaderInfo> ExtractEncryptionHeaderAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
    {
        var path = GetFilePath(fileId, extension, pathPrefix);
        if (!await _sftp.FileExistsAsync(path, ct).ConfigureAwait(false))
            throw new FileNotFoundException($"SFTP file not found for {fileId}", path);
        await using var stream = await _sftp.OpenReadAsync(path, ct).ConfigureAwait(false);
        var header = EncryptionHeader.Read(stream);
        return new EncryptionHeaderInfo(header.EncryptedDataEncryptionKey, header.KeyId, header.KeyVersion, header.DekKeyMaterialBytes);
    }

    /// <inheritdoc />
    protected override async Task UpdateFileHeaderAsync(Guid fileId, string? pathPrefix, string targetKeyId, string targetKeyVersion, byte[] newEncryptedDek, CancellationToken ct)
    {
        var path = await FindFilePathAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
        if (path is null || !await _sftp.FileExistsAsync(path, ct).ConfigureAwait(false))
            throw new FileNotFoundException($"File {fileId} not found on SFTP; cannot rotate header.", fileId.ToString());

        var bytes = await _sftp.DownloadBytesAsync(path, ct).ConfigureAwait(false);
        if (bytes.Length < 13)
            throw new InvalidDataException($"File {fileId} has a truncated or invalid encryption header ({bytes.Length} bytes); cannot rotate DEK.");

        await using var read = new MemoryStream(bytes, writable: false);
        var oldHeader = EncryptionHeader.Read(read);
        var oldHeaderSize = (int)read.Position;
        var updated = oldHeader.With(targetKeyId, targetKeyVersion, newEncryptedDek);
        using var headerMs = new MemoryStream(updated.GetHeaderSize());
        updated.Write(headerMs);
        var newHeaderBytes = headerMs.ToArray();

        await using var dest = new MemoryStream();
#if NETSTANDARD2_0
        await dest.WriteAsync(newHeaderBytes, 0, newHeaderBytes.Length, ct).ConfigureAwait(false);
        await dest.WriteAsync(bytes, oldHeaderSize, bytes.Length - oldHeaderSize, ct).ConfigureAwait(false);
#else
        await dest.WriteAsync(newHeaderBytes, ct).ConfigureAwait(false);
        await dest.WriteAsync(bytes.AsMemory(oldHeaderSize), ct).ConfigureAwait(false);
#endif
        dest.Position = 0;
        await _sftp.UploadAsync(path, dest, ct).ConfigureAwait(false);
        Logger.LogDebug("Updated SFTP file header for {FileId}", fileId);
    }

    /// <inheritdoc />
    protected override async Task CleanupPartialFileAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
    {
        try {
            var path = await FindFilePathAsync(fileId, pathPrefix, ct).ConfigureAwait(false);
            if (path is not null && await _sftp.FileExistsAsync(path, ct).ConfigureAwait(false))
                await _sftp.DeleteFileAsync(path, ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "Failed to cleanup partial SFTP file {FileId}", fileId);
        }
    }

    /// <inheritdoc />
    public override async Task<FileStoreResult> CopyFileAsync(Guid sourceFileId, CopyFileRequest? request = null, CancellationToken ct = default)
    {
        var meta = await GetMetadataAsync(sourceFileId, ct).ConfigureAwait(false);
        EnsureReadableAvailability(meta);
        var src = await FindFilePathAsync(sourceFileId, meta.PathPrefix, ct).ConfigureAwait(false);
        if (src is null)
            throw new FileNotFoundException($"Source SFTP file missing for id {sourceFileId}");

        var destId = Guid.NewGuid();
        var destPrefix = NormalizePathPrefix(request?.PathPrefix ?? meta.PathPrefix);
        var suffix = InferTrailingSuffixAfterFileId(meta.Id, meta.SourceFileName);
        var dest = GetFilePath(destId, suffix, destPrefix);
        await _sftp.CopyFileAsync(src, dest, ct).ConfigureAwait(false);
        return await RecordCopyMetadataAsync(sourceFileId, meta, destId, request, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task<FileStoreResult> MoveFileAsync(Guid fileId, MoveFileRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ValidatePathPrefix(request.PathPrefix);
        var destPrefix = NormalizePathPrefix(request.PathPrefix);
        var meta = await GetMetadataAsync(fileId, ct).ConfigureAwait(false);
        EnsureReadableAvailability(meta);
        var previous = meta.PathPrefix;
        if (string.Equals(previous, destPrefix, StringComparison.Ordinal))
            return meta;

        var src = await FindFilePathAsync(fileId, previous, ct).ConfigureAwait(false);
        if (src is null)
            throw new FileNotFoundException($"Source file path not found for id {fileId}");

        var suffix = InferTrailingSuffixAfterFileId(meta.Id, meta.SourceFileName);
        var dest = GetFilePath(fileId, suffix, destPrefix);
        await _sftp.RenameAsync(src, dest, ct).ConfigureAwait(false);
        var movedMeta = await RecordMoveMetadataAsync(meta, destPrefix, ct).ConfigureAwait(false);
        RaiseFileMoved(fileId, FileStoreSnapshot.From(movedMeta), previous);
        return movedMeta;
    }

    private string GetFilePath(Guid fileId, string extension = "", string? pathPrefix = null)
    {
        var fileName = fileId.ToString("N") + extension;
        string combined;
        if (!string.IsNullOrWhiteSpace(pathPrefix))
            combined = PathHelpers.Combine(PathStyle.Posix, _root, pathPrefix.Replace('\\', '/').Trim('/'), fileName);
        else {
            var id = fileId.ToString("N");
            combined = PathHelpers.Combine(PathStyle.Posix, _root, id[..2], id.Substring(2, 2), fileName);
        }

        var full = PathHelpers.GetFullPath(PathStyle.Posix, combined);
        PathHelpers.ThrowIfEscapesRoot(PathStyle.Posix, _root, full);
        return full;
    }

    private async Task<string?> FindFilePathAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
    {
        var basePath = GetFilePath(fileId, "", pathPrefix);
        if (await _sftp.FileExistsAsync(basePath, ct).ConfigureAwait(false))
            return basePath;

        var candidates = new List<string>();
        if (CompressionService != null)
            candidates.Add(basePath + CompressionService.FileExtension);
        if (TwoKeyEncryptionService != null) {
            candidates.Add(basePath + TwoKeyEncryptionService.FileExtension);
            if (CompressionService != null)
                candidates.Add(basePath + CompressionService.FileExtension + TwoKeyEncryptionService.FileExtension);
        }

        foreach (var ext in FileTypeInfo.CommonStorageResolutionSuffixes)
            candidates.Add(basePath + ext);

        foreach (var c in candidates) {
            if (await _sftp.FileExistsAsync(c, ct).ConfigureAwait(false))
                return c;
        }

        return null;
    }
}
