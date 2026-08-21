using System.IO.Compression;
using Lyo.Common.Pathing;
using Lyo.Exceptions;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Models;
using Lyo.IO.Temp;
using Lyo.IO.Temp.Enums;
using Lyo.IO.Temp.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FileStorage;

/// <summary>Spools files through <see cref="IIOTempService" /> and zips the resulting tree.</summary>
public sealed class FileStorageArchiveService(
    IFileStorageService fileStorage,
    IIOTempService temp,
    FileStorageArchiveOptions options,
    ILogger<FileStorageArchiveService>? logger = null) : IFileStorageArchiveService
{
    private const string PayloadDirectoryName = "payload";
    private readonly IFileStorageService _fileStorage = ArgumentHelpers.ThrowIfNullReturn(fileStorage);
    private readonly ILogger<FileStorageArchiveService> _logger = logger ?? NullLogger<FileStorageArchiveService>.Instance;
    private readonly FileStorageArchiveOptions _options = ArgumentHelpers.ThrowIfNullReturn(options);
    private readonly IIOTempService _temp = ArgumentHelpers.ThrowIfNullReturn(temp);

    /// <inheritdoc />
    public async Task<FileStorageArchive> CreateArchiveAsync(
        IReadOnlyList<FileStorageArchiveEntry> entries,
        string? fileName = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(entries);
        OperationHelpers.ThrowIf(entries.Count == 0, "At least one file is required.");
        _options.Validate();

        var unique = Deduplicate(entries);
        if (unique.Count > _options.MaxFileCount)
            throw new FileStorageArchiveLimitException($"Archive allows at most {_options.MaxFileCount} file(s); requested {unique.Count}.");

        var resolved = new List<(Guid Id, string ZipPath, FileStoreResult Metadata)>(unique.Count);
        long totalBytes = 0;
        var usedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in unique) {
            ct.ThrowIfCancellationRequested();
            FileStoreResult metadata;
            try {
                metadata = await _fileStorage.GetMetadataAsync(entry.Id, ct).ConfigureAwait(false);
            }
            catch (FileNotFoundException) {
                _logger.LogInformation("Skipping missing or deleted file {FileId} in archive", entry.Id);
                continue;
            }

            if (metadata.DeletedAt != null || metadata.Availability == FileAvailability.Deleted) {
                _logger.LogInformation("Skipping deleted file {FileId} in archive", entry.Id);
                continue;
            }

            totalBytes += metadata.OriginalFileSize;
            if (totalBytes > _options.MaxTotalUncompressedBytes)
                throw new FileStorageArchiveLimitException(
                    $"Archive uncompressed size {totalBytes:N0} bytes exceeds the limit of {_options.MaxTotalUncompressedBytes:N0} bytes.");

            var zipPath = FileStorageArchivePath.EnsureExtension(
                FileStorageArchivePath.NormalizeZipPath(entry.ZipPath, entry.Id, metadata.OriginalFileName), metadata);
            zipPath = EnsureUniquePath(zipPath, usedPaths, entry.Id);
            resolved.Add((entry.Id, zipPath, metadata));
        }

        if (resolved.Count == 0)
            throw new FileNotFoundException("None of the requested files are available to archive.");

        var downloadName = FileStorageArchivePath.SanitizeZipFileName(fileName);
        var session = _temp.CreateSession(
            new() {
                MaxTotalSizeBytes = checked(_options.MaxTotalUncompressedBytes * 2),
                OverflowStrategy = TempOverflowStrategy.ThrowException
            });

        try {
            var payloadDir = session.CreateDirectory(PayloadDirectoryName);
            foreach (var (id, zipPath, _) in resolved) {
                ct.ThrowIfCancellationRequested();
                await SpoolFileAsync(payloadDir, id, zipPath, ct).ConfigureAwait(false);
            }

            var zipPathOnDisk = session.GetFilePath(downloadName);
            await CreateZipAsync(payloadDir, zipPathOnDisk, ct).ConfigureAwait(false);
            var zipLength = new FileInfo(zipPathOnDisk).Length;
            var zipStream = new FileStream(zipPathOnDisk, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
            var owned = new TempSessionReadStream(zipStream, session);
            session = null;
            _logger.LogInformation("Created file archive {FileName} with {Count} file(s), {ZipBytes} bytes", downloadName, resolved.Count, zipLength);
            return new(owned, downloadName, zipLength);
        }
        finally {
            session?.Dispose();
        }
    }

    private static List<FileStorageArchiveEntry> Deduplicate(IReadOnlyList<FileStorageArchiveEntry> entries)
    {
        var seen = new HashSet<Guid>();
        var list = new List<FileStorageArchiveEntry>(entries.Count);
        foreach (var entry in entries) {
            if (seen.Add(entry.Id))
                list.Add(entry);
        }

        return list;
    }

    private static string EnsureUniquePath(string zipPath, HashSet<string> used, Guid fileId)
    {
        if (used.Add(zipPath))
            return zipPath;

        var slash = zipPath.LastIndexOf('/');
        var dir = slash >= 0 ? zipPath[..(slash + 1)] : "";
        var leaf = slash >= 0 ? zipPath[(slash + 1)..] : zipPath;
        var ext = Path.GetExtension(leaf);
        var stem = string.IsNullOrEmpty(ext) ? leaf : leaf[..^ext.Length];
        var candidate = $"{dir}{stem}_{fileId:N}{ext}";
        used.Add(candidate);
        return candidate;
    }

    private async Task SpoolFileAsync(string payloadDir, Guid fileId, string zipPath, CancellationToken ct)
    {
        var source = await _fileStorage.GetFileStreamAsync(fileId, ct: ct).ConfigureAwait(false);
        if (source is null)
            throw new FileNotFoundException($"File with ID {fileId} not found", fileId.ToString());

        using (source) {
            var dest = Path.GetFullPath(Path.Combine(payloadDir, zipPath.Replace('/', Path.DirectorySeparatorChar)));
            PathHelpers.ThrowIfEscapesRoot(PathStyle.Host, payloadDir, dest);
            var parent = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);

            using var output = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
#if NETSTANDARD2_0
            await source.CopyToAsync(output, 81920, ct).ConfigureAwait(false);
#else
            await source.CopyToAsync(output, ct).ConfigureAwait(false);
#endif
        }
    }

    private static async Task CreateZipAsync(string payloadDir, string zipPath, CancellationToken ct)
    {
        using var zipStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: false);
        foreach (var file in Directory.EnumerateFiles(payloadDir, "*", SearchOption.AllDirectories)) {
            ct.ThrowIfCancellationRequested();
            var relative = file.Substring(payloadDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var entryName = relative.Replace('\\', '/');
            var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
            using var entryStream = entry.Open();
            using var input = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
#if NETSTANDARD2_0
            await input.CopyToAsync(entryStream, 81920, ct).ConfigureAwait(false);
#else
            await input.CopyToAsync(entryStream, ct).ConfigureAwait(false);
#endif
        }
    }
}
