using Lyo.Comic.Api.Models.Response;
using Lyo.Comic.Api.Storage;
using Lyo.FileStorage;
using Microsoft.AspNetCore.Mvc;

namespace Lyo.Comic.Api.Endpoints;

public static class FilesEndpoints
{
    private const string FileStorageKey = "comic-files";
    private const string DefaultContentType = "application/octet-stream";

    public static IEndpointRouteBuilder MapFilesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/files").WithTags("Files");
        group.MapGet("/{id:guid}", GetFile);
        group.MapPost("/batch", GetFilesBatch);
        group.MapPost("/upload", UploadFile).DisableAntiforgery();
        group.MapDelete("/{id:guid}", DeleteFile);
        return app;
    }

    private static async Task<IResult> GetFile(Guid id, [FromKeyedServices(FileStorageKey)] IFileStorageService fileStorage, CancellationToken ct = default)
    {
        try {
            var metadata = await fileStorage.GetMetadataAsync(id, ct);
            var bytes = await fileStorage.GetFileAsync(id, ct);
            return Results.Bytes(bytes, metadata.ContentType ?? DefaultContentType);
        }
        catch (FileNotFoundException) {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetFilesBatch(
        [FromBody] FilesBatchReq req,
        [FromKeyedServices(FileStorageKey)] IFileStorageService fileStorage,
        CancellationToken ct = default)
    {
        if (req.Ids is not { Count: > 0 })
            return Results.BadRequest("At least one file ID is required.");

        var entries = new List<FileBatchEntry>(req.Ids.Count);
        foreach (var id in req.Ids) {
            var entry = await FetchEntryAsync(fileStorage, id, ct);
            if (entry != null)
                entries.Add(entry);
        }

        return Results.Ok(entries);
    }

    private static async Task<IResult> UploadFile(
        IFormFile file,
        Guid? seriesId,
        Guid? volumeId,
        Guid? chapterId,
        IComicStore comicStore,
        [FromKeyedServices(FileStorageKey)] IFileStorageService fileStorage,
        [FromKeyedServices(FileStorageKey)] ComicFileUploadOptions uploadOptions,
        CancellationToken ct = default)
    {
        var prefixResult = await ResolveUploadPathPrefixAsync(comicStore, seriesId, volumeId, chapterId, ct);
        if (prefixResult.Error is { } err)
            return err;

        await using var stream = file.OpenReadStream();
        var result = await fileStorage.SaveFromStreamAsync(
            stream, file.Length, file.FileName, uploadOptions.Compress, uploadOptions.Encrypt, uploadOptions.KeyId, prefixResult.PathPrefix, ct: ct);

        return Results.Ok(new { result.Id });
    }

    private static bool HasScope(Guid? id) => id is { } g && g != Guid.Empty;

    private static async Task<(string? PathPrefix, IResult? Error)> ResolveUploadPathPrefixAsync(
        IComicStore comicStore,
        Guid? seriesId,
        Guid? volumeId,
        Guid? chapterId,
        CancellationToken ct)
    {
        if (!HasScope(seriesId) && !HasScope(volumeId) && !HasScope(chapterId))
            return (null, null);

        if (HasScope(chapterId)) {
            var chapter = await comicStore.GetChapterByIdAsync(chapterId!.Value, ct);
            if (chapter is null)
                return (null, Results.NotFound());

            if (HasScope(seriesId) && chapter.SeriesId != seriesId!.Value)
                return (null, Results.BadRequest("seriesId does not match the chapter's series."));

            if (HasScope(volumeId)) {
                var expectedVolume = chapter.VolumeId ?? Guid.Empty;
                if (expectedVolume != volumeId!.Value)
                    return (null, Results.BadRequest("volumeId does not match the chapter's volume."));
            }

            return (ComicFileStoragePath.BuildPathPrefix(chapter), null);
        }

        if (HasScope(volumeId)) {
            var volume = await comicStore.GetVolumeByIdAsync(volumeId!.Value, ct);
            if (volume is null)
                return (null, Results.NotFound());

            if (HasScope(seriesId) && volume.SeriesId != seriesId!.Value)
                return (null, Results.BadRequest("seriesId does not match the volume's series."));

            return (ComicFileStoragePath.BuildVolumePrefix(volume.SeriesId, volume.Id), null);
        }

        if (HasScope(seriesId)) {
            var series = await comicStore.GetSeriesByIdAsync(seriesId!.Value, ct);
            if (series is null)
                return (null, Results.NotFound());

            return (ComicFileStoragePath.BuildSeriesPrefix(series.Id), null);
        }

        return (null, Results.BadRequest("Invalid upload scope query parameters."));
    }

    private static async Task<IResult> DeleteFile(Guid id, [FromKeyedServices(FileStorageKey)] IFileStorageService fileStorage, CancellationToken ct = default)
    {
        try {
            var deleted = await fileStorage.DeleteFileAsync(id, ct);
            return deleted ? Results.Ok() : Results.NotFound();
        }
        catch (FileNotFoundException) {
            return Results.NotFound();
        }
    }

    private static async Task<FileBatchEntry?> FetchEntryAsync(IFileStorageService fileStorage, Guid id, CancellationToken ct)
    {
        try {
            var metadata = await fileStorage.GetMetadataAsync(id, ct);
            var bytes = await fileStorage.GetFileAsync(id, ct);
            return new(id, metadata.ContentType ?? DefaultContentType, Convert.ToBase64String(bytes));
        }
        catch (FileNotFoundException) {
            return null;
        }
    }
}