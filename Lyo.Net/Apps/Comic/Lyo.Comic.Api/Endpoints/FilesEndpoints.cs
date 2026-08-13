using Lyo.Api.Models;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Error;
using Lyo.Comic.Api.Models.Response;
using Lyo.Comic.Api.Storage;
using Lyo.FileStorage.Abstractions;
using Microsoft.AspNetCore.Mvc;
using ApiErrorCodes = Lyo.Api.Models.Constants.ApiErrorCodes;

namespace Lyo.Comic.Api.Endpoints;

public static class FilesEndpoints
{
    private const string FileStorageKey = "comic-files";
    private const string DefaultContentType = "application/octet-stream";

    public static IEndpointRouteBuilder MapFilesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/files").WithTags("Files").RequireAuthorization();
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
            var bytes = await fileStorage.GetFileAsync(id, ct: ct);
            return Results.Bytes(bytes, metadata.ContentType ?? DefaultContentType);
        }
        catch (FileNotFoundException) {
            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, "Resource was not found."));
        }
    }

    private static async Task<IResult> GetFilesBatch(
        [FromBody] FilesBatchReq req,
        [FromKeyedServices(FileStorageKey)] IFileStorageService fileStorage,
        CancellationToken ct = default)
    {
        if (req.Ids is not { Count: > 0 })
            ThrowBadRequest("At least one file ID is required.");

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
        var pathPrefix = await ResolveUploadPathPrefixAsync(comicStore, seriesId, volumeId, chapterId, ct).ConfigureAwait(false);
        await using var stream = file.OpenReadStream();
        var result = await fileStorage.SaveFromStreamAsync(
            stream, file.Length, file.FileName, uploadOptions.Compress, uploadOptions.Encrypt, uploadOptions.KeyId, pathPrefix, ct: ct);

        return Results.Ok(new { result.Id });
    }

    private static bool HasScope(Guid? id) => id is { } g && g != Guid.Empty;

    private static async Task<string?> ResolveUploadPathPrefixAsync(
        IComicStore comicStore,
        Guid? seriesId,
        Guid? volumeId,
        Guid? chapterId,
        CancellationToken ct)
    {
        if (!HasScope(seriesId) && !HasScope(volumeId) && !HasScope(chapterId))
            return null;

        if (HasScope(chapterId)) {
            var chapter = await comicStore.GetChapterByIdAsync(chapterId!.Value, ct);
            if (chapter is null)
                throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, "Resource was not found."));

            if (HasScope(seriesId) && chapter.SeriesId != seriesId!.Value)
                ThrowBadRequest("seriesId does not match the chapter's series.");

            if (HasScope(volumeId)) {
                var expectedVolume = chapter.VolumeId ?? Guid.Empty;
                if (expectedVolume != volumeId!.Value)
                    ThrowBadRequest("volumeId does not match the chapter's volume.");
            }

            return ComicFileStoragePath.BuildPathPrefix(chapter);
        }

        if (HasScope(volumeId)) {
            var volume = await comicStore.GetVolumeByIdAsync(volumeId!.Value, ct);
            if (volume is null)
                throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, "Resource was not found."));

            if (HasScope(seriesId) && volume.SeriesId != seriesId!.Value)
                ThrowBadRequest("seriesId does not match the volume's series.");

            return ComicFileStoragePath.BuildVolumePrefix(volume.SeriesId, volume.Id);
        }

        if (HasScope(seriesId)) {
            var series = await comicStore.GetSeriesByIdAsync(seriesId!.Value, ct);
            if (series is null)
                throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, "Resource was not found."));

            return ComicFileStoragePath.BuildSeriesPrefix(series.Id);
        }

        ThrowBadRequest("Invalid upload scope query parameters.");
        return null;
    }

    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void ThrowBadRequest(string message)
        => throw ApiErrorException.From(LyoProblemDetailsBuilder.CreateWithActivity().WithErrorCode(ApiErrorCodes.InvalidRequest).WithMessage(message).Build());

    private static async Task<IResult> DeleteFile(Guid id, [FromKeyedServices(FileStorageKey)] IFileStorageService fileStorage, CancellationToken ct = default)
    {
        try {
            var deleted = await fileStorage.DeleteFileAsync(id, ct: ct);
            if (!deleted)
                throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, "Resource was not found."));

            return Results.Ok();
        }
        catch (FileNotFoundException) {
            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, "Resource was not found."));
        }
    }

    private static async Task<FileBatchEntry?> FetchEntryAsync(IFileStorageService fileStorage, Guid id, CancellationToken ct)
    {
        try {
            var metadata = await fileStorage.GetMetadataAsync(id, ct);
            var bytes = await fileStorage.GetFileAsync(id, ct: ct);
            return new(id, metadata.ContentType ?? DefaultContentType, Convert.ToBase64String(bytes));
        }
        catch (FileNotFoundException) {
            return null;
        }
    }
}
