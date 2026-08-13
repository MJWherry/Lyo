using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using Lyo.Api.Models;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Error;
using Lyo.Comic.Api.Storage;
using Lyo.Common.Pathing;
using Lyo.FileStorage;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Models;
using ApiErrorCodes = Lyo.Api.Models.Constants.ApiErrorCodes;

namespace Lyo.Comic.Api.Endpoints;

internal static class ArchiveEndpointHelpers
{
    internal const string HttpClientName = "comic-archive";

    internal static async Task<IResult> StreamArchiveAsync(Func<CancellationToken, Task<FileStorageArchive>> create, CancellationToken ct)
    {
        try {
            var archive = await create(ct).ConfigureAwait(false);
            return Results.File(archive.Stream, archive.ContentType, archive.FileName);
        }
        catch (FileStorageArchiveLimitException ex) {
            throw ApiErrorException.From(LyoProblemDetailsBuilder.CreateWithActivity().WithErrorCode(ApiErrorCodes.InvalidRequest).WithMessage(ex.Message).Build());
        }
        catch (ArgumentException ex) {
            throw ApiErrorException.From(LyoProblemDetailsBuilder.CreateWithActivity().WithErrorCode(ApiErrorCodes.InvalidRequest).WithMessage(ex.Message).Build());
        }
        catch (FileNotFoundException) {
            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, "Resource was not found."));
        }
    }

    internal static async Task<IResult> StreamComicArchiveAsync(
        IReadOnlyList<ComicArchiveSource> sources,
        string zipName,
        IFileStorageArchiveService archive,
        IFileStorageService files,
        HttpClient http,
        FileStorageArchiveOptions options,
        string emptyMessage,
        CancellationToken ct)
    {
        if (sources.Count == 0)
            ThrowBadRequest(emptyMessage);

        if (sources.All(s => s.FileId is not null)) {
            var stored = new List<FileStorageArchiveEntry>(sources.Count);
            foreach (var source in sources) {
                var id = source.FileId!.Value;
                try {
                    await files.GetMetadataAsync(id, ct).ConfigureAwait(false);
                    stored.Add(new(id, source.ZipPath));
                }
                catch (FileNotFoundException) {
                }
            }

            if (stored.Count == 0)
                ThrowBadRequest(emptyMessage);

            return await StreamArchiveAsync(token => archive.CreateArchiveAsync(stored, zipName, token), ct).ConfigureAwait(false);
        }

        try {
            return StreamMixedArchive(sources, zipName, files, http, options, emptyMessage, ct);
        }
        catch (FileStorageArchiveLimitException ex) {
            throw ApiErrorException.From(LyoProblemDetailsBuilder.CreateWithActivity().WithErrorCode(ApiErrorCodes.InvalidRequest).WithMessage(ex.Message).Build());
        }
        catch (ArgumentException ex) {
            throw ApiErrorException.From(LyoProblemDetailsBuilder.CreateWithActivity().WithErrorCode(ApiErrorCodes.InvalidRequest).WithMessage(ex.Message).Build());
        }
    }

    [DoesNotReturn]
    internal static void ThrowBadRequest(string message)
        => throw ApiErrorException.From(LyoProblemDetailsBuilder.CreateWithActivity().WithErrorCode(ApiErrorCodes.InvalidRequest).WithMessage(message).Build());

    [DoesNotReturn]
    internal static void ThrowNotFound()
        => throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, "Resource was not found."));

    private static IResult StreamMixedArchive(
        IReadOnlyList<ComicArchiveSource> sources,
        string zipName,
        IFileStorageService files,
        HttpClient http,
        FileStorageArchiveOptions options,
        string emptyMessage,
        CancellationToken ct)
    {
        options.Validate();
        var downloadName = FileStorageArchivePath.SanitizeZipFileName(zipName);
        return Results.Stream(async output =>
        {
            string? tempPath = Path.GetTempFileName();
            try {
                var added = 0;
                long totalBytes = 0;
                var usedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                await using (var zipStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
                using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: false)) {
                    foreach (var source in sources) {
                        ct.ThrowIfCancellationRequested();
                        if (added >= options.MaxFileCount)
                            throw new FileStorageArchiveLimitException($"Archive allows at most {options.MaxFileCount} file(s); requested more.");

                        var opened = await TryOpenSourceAsync(source, files, http, ct).ConfigureAwait(false);
                        if (opened is null)
                            continue;

                        await using (var content = opened.Value.Stream) {
                            totalBytes += opened.Value.Length;
                            if (totalBytes > options.MaxTotalUncompressedBytes)
                                throw new FileStorageArchiveLimitException(
                                    $"Archive uncompressed size {totalBytes:N0} bytes exceeds the limit of {options.MaxTotalUncompressedBytes:N0} bytes.");

                            var zipPath = UniquePath(
                                FileStorageArchivePath.NormalizeZipPath(source.ZipPath + opened.Value.Extension, opened.Value.Id, null),
                                usedPaths, opened.Value.Id);
                            var entry = zip.CreateEntry(zipPath, CompressionLevel.Fastest);
                            await using var entryStream = entry.Open();
                            await content.CopyToAsync(entryStream, ct).ConfigureAwait(false);
                        }

                        added++;
                    }
                }

                if (added == 0)
                    ThrowBadRequest(emptyMessage);

                await using var read = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.DeleteOnClose);
                tempPath = null;
                await read.CopyToAsync(output, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception) when (ct.IsCancellationRequested) {
                throw new OperationCanceledException(ct);
            }
            finally {
                if (tempPath is not null) {
                    try {
                        File.Delete(tempPath);
                    }
                    catch (IOException) {
                    }
                }
            }
        }, "application/zip", downloadName);
    }

    private static async Task<(Stream Stream, string Extension, Guid Id, long Length)?> TryOpenSourceAsync(
        ComicArchiveSource source,
        IFileStorageService files,
        HttpClient http,
        CancellationToken ct)
    {
        if (source.FileId is { } id) {
            try {
                var metadata = await files.GetMetadataAsync(id, ct).ConfigureAwait(false);
                var stream = await files.GetFileStreamAsync(id, ct: ct).ConfigureAwait(false);
                if (stream is null)
                    return null;

                return (stream, FileStorageArchivePath.ExtensionFromMetadata(metadata), id, metadata.OriginalFileSize);
            }
            catch (FileNotFoundException) {
                return null;
            }
        }

        if (source.RemoteUrl is null)
            return null;

        try {
            using var response = await http.GetAsync(source.RemoteUrl, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return null;

            var bytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
            if (bytes.Length == 0)
                return null;

            var contentType = response.Content.Headers.ContentType?.MediaType;
            return (new MemoryStream(bytes, writable: false), RemoteExtension(source.RemoteUrl, contentType), Guid.NewGuid(), bytes.Length);
        }
        catch (HttpRequestException) when (!ct.IsCancellationRequested) {
            return null;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) {
            return null;
        }
    }

    private static string RemoteExtension(Uri url, string? contentType)
    {
        var ext = PathHelpers.GetExtension(PathStyle.Posix, url.AbsolutePath);
        if (!string.IsNullOrEmpty(ext) && ext.Length <= 8)
            return ext;

        return contentType?.ToLowerInvariant() switch {
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            "image/avif" => ".avif",
            _ => ".jpg"
        };
    }

    private static string UniquePath(string zipPath, HashSet<string> used, Guid id)
    {
        if (used.Add(zipPath))
            return zipPath;

        var slash = zipPath.LastIndexOf('/');
        var dir = slash >= 0 ? zipPath[..(slash + 1)] : "";
        var leaf = slash >= 0 ? zipPath[(slash + 1)..] : zipPath;
        var ext = Path.GetExtension(leaf);
        var stem = string.IsNullOrEmpty(ext) ? leaf : leaf[..^ext.Length];
        var candidate = $"{dir}{stem}_{id:N}{ext}";
        used.Add(candidate);
        return candidate;
    }
}
