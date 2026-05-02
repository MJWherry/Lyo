using Lyo.Comic.Api.Models.Response;
using Lyo.FileStorage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Comic.Api.Endpoints;

public static class FilesEndpoints
{
    private const string FileStorageKey = "comic-files";
    private const string DefaultContentType = "application/octet-stream";

    public static IEndpointRouteBuilder MapFilesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/files").WithTags("Files").WithOpenApi();

        group.MapGet("/{id:guid}", GetFile);
        group.MapPost("/batch", GetFilesBatch);
        group.MapPost("/upload", UploadFile).DisableAntiforgery();
        group.MapDelete("/{id:guid}", DeleteFile);

        return app;
    }

    private static async Task<IResult> GetFile(
        Guid id,
        [FromKeyedServices(FileStorageKey)] IFileStorageService fileStorage,
        CancellationToken ct = default)
    {
        try {
            var metadata = await fileStorage.GetMetadataAsync(id, ct);
            var bytes    = await fileStorage.GetFileAsync(id, ct);
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
        [FromKeyedServices(FileStorageKey)] IFileStorageService fileStorage,
        [FromKeyedServices(FileStorageKey)] ComicFileUploadOptions uploadOptions,
        CancellationToken ct = default)
    {
        await using var stream = file.OpenReadStream();
        var result = await fileStorage.SaveFromStreamAsync(
            stream, file.Length, file.FileName,
            uploadOptions.Compress,
            uploadOptions.Encrypt,
            uploadOptions.KeyId,
            ct: ct);
        return Results.Ok(new { result.Id });
    }

    private static async Task<IResult> DeleteFile(
        Guid id,
        [FromKeyedServices(FileStorageKey)] IFileStorageService fileStorage,
        CancellationToken ct = default)
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
            var bytes    = await fileStorage.GetFileAsync(id, ct);
            return new FileBatchEntry(id, metadata.ContentType ?? DefaultContentType, Convert.ToBase64String(bytes));
        }
        catch (FileNotFoundException) {
            return null;
        }
    }
}
