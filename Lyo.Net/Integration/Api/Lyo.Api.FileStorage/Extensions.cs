using Lyo.Api.ApiEndpoint;
using Lyo.Api.FileStorage.Models;
using Lyo.Api.Models.Error;
using Lyo.Cache;
using Lyo.Common.Records;
using Lyo.Exceptions;
using Lyo.FileMetadataStore.Models;
using Lyo.FileMetadataStore.Postgres;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.FileStorage;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.Staged;
using Lyo.KeyStore;
using Domain = Lyo.FileStorage.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ApiErrorCodes = Lyo.Api.Models.Constants.ApiErrorCodes;

namespace Lyo.Api.FileStorage;

/// <summary>Maps file-storage workbench HTTP endpoints and the FileMetadata QueryProject surface.</summary>
public static class Extensions
{
    /// <summary>
    /// Maps <c>{Route}</c> file/stage/multipart/archive routes, optional <c>POST {DirectUploadPath}</c>, and read-only FileMetadata Query/QueryProject.
    /// Resolve keyed <c>IFileStorageService</c>, <c>IMultipartUploadService</c>, <c>IStagedFileUploadService</c>, and <c>IFileStorageArchiveService</c> under
    /// <see cref="FileStorageApiOptions.ServiceKey" />.
    /// </summary>
    public static WebApplication BuildFileStorageApi(this WebApplication app, FileStorageApiOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(app);
        options ??= new();
        options.Validate();
        MapWorkbenchGroup(app, options);
        MapDirectUpload(app, options);
        MapFileMetadataQuery(app, options);
        return app;
    }

    private static void MapFileMetadataQuery(WebApplication app, FileStorageApiOptions options)
    {
        app.CreateReadOnlyBuilder<FileMetadataStoreDbContext, FileMetadataEntity, FileMetadataEntity, string>(options.FileMetadataRoute, "FileMetadata")
            .AllowAnonymous()
            .WithReadOnlyEndpoints()
            .Build();
    }

    private static void MapDirectUpload(WebApplication app, FileStorageApiOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.DirectUploadPath))
            return;

        app.MapPost(
                options.DirectUploadPath, async (
                    IFormFile file, [FromQuery] string? originalFileName, [FromQuery] bool compress, [FromQuery] bool encrypt, [FromQuery] string? keyId,
                    [FromQuery] string? pathPrefix, [FromQuery] int? chunkSize, [FromQuery] string? contentType, [FromQuery] string? tenantId, IServiceProvider services,
                    ICacheService cache, CancellationToken ct) => {
                    var fileStorage = GetFileStorage(services, options.ServiceKey);
                    return await SaveStreamFromFormAsync(
                        file, originalFileName, compress, encrypt, keyId, pathPrefix, chunkSize, contentType, tenantId, fileStorage, cache, ct);
                })
            .DisableAntiforgery()
            .WithTags("DirectFileUpload");
    }

    private static void MapWorkbenchGroup(WebApplication app, FileStorageApiOptions options)
    {
        var route = options.Route.Trim().Trim('/');
        var serviceKey = options.ServiceKey;
        var group = app.MapGroup(route).WithTags("FileStorageWorkbench");

        group.MapGet(
            "health", async (IServiceProvider services, CancellationToken ct) => {
                var fileStorage = GetFileStorage(services, serviceKey);
                var health = await fileStorage.CheckHealthAsync(ct);
                var message = health.Message;
                if (!health.IsHealthy && string.IsNullOrWhiteSpace(message) && health.Exception != null)
                    message = health.Exception.Message;

                return Results.Ok(new FileStorageHealthResponse(health.IsHealthy, message));
            });

        group.MapPost(
            "files/save", async ([FromBody] SaveFileRequest request, IServiceProvider services, ICacheService cache, CancellationToken ct) => {
                var fileStorage = GetFileStorage(services, serviceKey);
                var result = await fileStorage.SaveFileAsync(
                    request.Data, request.OriginalFileName, request.Compress, request.Encrypt, request.KeyId, request.PathPrefix, request.ChunkSize, request.ContentType,
                    request.TenantId, ct);

                await InvalidateFileMetadataQueryCacheAsync(cache).ConfigureAwait(false);
                return Results.Ok(result);
            });

        group.MapPost(
            "files/{fileId:guid}/access-links", async (
                Guid fileId, [FromBody] CreateDownloadAccessLinkRequest request, HttpContext http, IServiceProvider services, CancellationToken ct) => {
                var accessService = services.GetRequiredService<IFileDownloadAccessService>();
                var tenantId = request.TenantId ?? http.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                var result = await accessService.CreateLinkAsync(
                    new(fileId, request.NotBeforeUtc, request.ExpiresAtUtc, request.WindowStartUtc, request.WindowEndUtc, request.MaxDownloads, tenantId), ct);

                return Results.Ok(
                    new DownloadAccessLinkResponse(
                        result.LinkId, result.Token, $"{route}/files/access/{result.Token}/download",
                        $"{route}/files/access/{result.Token}/presigned-read", result.CreatedUtc, result.ExpiresAtUtc));
            });

        group.MapPost(
                "files/save-stream", async (
                    IFormFile file, [FromQuery] string? originalFileName, [FromQuery] bool compress, [FromQuery] bool encrypt, [FromQuery] string? keyId,
                    [FromQuery] string? pathPrefix, [FromQuery] int? chunkSize, [FromQuery] string? contentType, [FromQuery] string? tenantId, IServiceProvider services,
                    ICacheService cache, CancellationToken ct) => {
                    var fileStorage = GetFileStorage(services, serviceKey);
                    return await SaveStreamFromFormAsync(
                        file, originalFileName, compress, encrypt, keyId, pathPrefix, chunkSize, contentType, tenantId, fileStorage, cache, ct);
                })
            .DisableAntiforgery();

        group.MapGet(
            "files/{fileId:guid}/presigned-read", async (
                Guid fileId, double? expiresHours, string? pathPrefix, string? contentDisposition, string? contentType, IServiceProvider services, CancellationToken ct) => {
                var fileStorage = GetFileStorage(services, serviceKey);
                var expiration = expiresHours.HasValue ? TimeSpan.FromHours(expiresHours.Value) : (TimeSpan?)null;
                var opts = BuildPreSignedReadUrlOptions(contentDisposition, contentType);
                var url = opts == null
                    ? await fileStorage.GetPreSignedReadUrlAsync(fileId, expiration, pathPrefix, ct)
                    : await fileStorage.GetPreSignedReadUrlAsync(fileId, expiration, pathPrefix, opts, ct);

                return Results.Ok(new PresignedReadResponse(url));
            });

        group.MapPost(
            "direct-upload/begin", async ([FromBody] DirectUploadBeginRequest request, IServiceProvider services, CancellationToken ct) => {
                var fileStorage = GetFileStorage(services, serviceKey);
                var result = await fileStorage.BeginDirectUploadAsync(ToDomain(request), ct);
                return Results.Ok(result);
            });

        group.MapPost(
            "direct-upload/{fileId:guid}/complete", async (
                Guid fileId, [FromBody] DirectUploadCompleteRequest? request, IServiceProvider services, ICacheService cache, CancellationToken ct) => {
                var fileStorage = GetFileStorage(services, serviceKey);
                var result = await fileStorage.CompleteDirectUploadAsync(fileId, ToDomain(request), ct);
                await InvalidateFileMetadataQueryCacheAsync(cache).ConfigureAwait(false);
                return Results.Ok(result);
            });

        group.MapPut(
                "direct-upload/{fileId:guid}/put", async (Guid fileId, HttpContext http, IServiceProvider services, CancellationToken ct) => {
                    var fileStorage = GetFileStorage(services, serviceKey);
                    if (fileStorage is not LocalFileStorageService local)
                        throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidOperation, "Not implemented."));

                    await local.ReceiveWorkbenchDirectPutAsync(fileId, http.Request.Body, ct).ConfigureAwait(false);
                    return Results.NoContent();
                })
            .DisableAntiforgery();

        group.MapPost(
            "stage/begin", async ([FromBody] StagedUploadBeginRequest request, IServiceProvider services, CancellationToken ct) => {
                var staged = GetStaged(services, serviceKey);
                var result = await staged.BeginAsync(ToDomain(request), ct);
                return Results.Ok(ToHttp(result));
            });

        group.MapPut(
                "stage/{stageId:guid}/put", async (Guid stageId, HttpContext http, IServiceProvider services, CancellationToken ct) => {
                    var staged = GetStaged(services, serviceKey);
                    if (staged is not LocalStagedFileUploadService local)
                        throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidOperation, "Not implemented."));

                    await local.ReceiveWorkbenchStagePutAsync(stageId, http.Request.Body, ct).ConfigureAwait(false);
                    return Results.NoContent();
                })
            .DisableAntiforgery();

        group.MapPost(
            "stage/{stageId:guid}/complete", async (Guid stageId, [FromBody] StagedUploadCompleteRequest? request, IServiceProvider services, CancellationToken ct) => {
                var staged = GetStaged(services, serviceKey);
                var result = await staged.CompleteAsync(stageId, ToDomain(request), ct);
                return Results.Ok(result);
            });

        group.MapPost(
            "stage/{stageId:guid}/commit", async (
                Guid stageId, [FromBody] StagedUploadCommitRequest request, IServiceProvider services, ICacheService cache, CancellationToken ct) => {
                var staged = GetStaged(services, serviceKey);
                try {
                    var result = await staged.CommitAsync(stageId, ToDomain(request), ct);
                    await InvalidateFileMetadataQueryCacheAsync(cache).ConfigureAwait(false);
                    return Results.Ok(result);
                }
                catch (InvalidOperationException ex) {
                    throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidRequest, ex.Message));
                }
            });

        group.MapPost(
            "stage/{stageId:guid}/abort", async (Guid stageId, IServiceProvider services, CancellationToken ct) => {
                var staged = GetStaged(services, serviceKey);
                await staged.AbortAsync(stageId, ct);
                return Results.Ok();
            });

        group.MapGet(
            "stage/{stageId:guid}", async (Guid stageId, IServiceProvider services, CancellationToken ct) => {
                var staged = GetStaged(services, serviceKey);
                var result = await staged.GetAsync(stageId, ct);
                return Results.Ok(result);
            });

        group.MapPost(
            "files/copy", async ([FromBody] CopyFileRequest request, IServiceProvider services, ICacheService cache, CancellationToken ct) => {
                var fileStorage = GetFileStorage(services, serviceKey);
                var result = await fileStorage.CopyFileAsync(request.SourceFileId, new Domain.CopyFileRequest { PathPrefix = request.PathPrefix }, ct);
                await InvalidateFileMetadataQueryCacheAsync(cache).ConfigureAwait(false);
                return Results.Ok(result);
            });

        group.MapPost(
            "files/move", async ([FromBody] MoveFileRequest request, IServiceProvider services, ICacheService cache, CancellationToken ct) => {
                var fileStorage = GetFileStorage(services, serviceKey);
                var result = await fileStorage.MoveFileAsync(request.FileId, new Domain.MoveFileRequest { PathPrefix = request.PathPrefix }, ct);
                await InvalidateFileMetadataQueryCacheAsync(cache).ConfigureAwait(false);
                return Results.Ok(result);
            });

        group.MapPost(
            "files/rename", async ([FromBody] RenameFileRequest request, IServiceProvider services, ICacheService cache, CancellationToken ct) => {
                var fileStorage = GetFileStorage(services, serviceKey);
                var result = await fileStorage.RenameFileAsync(request.FileId, new Domain.RenameFileRequest { OriginalFileName = request.OriginalFileName }, ct);
                await InvalidateFileMetadataQueryCacheAsync(cache).ConfigureAwait(false);
                return Results.Ok(result);
            });

        group.MapGet(
            "diagnostics/storage-keys", async (string? prefix, int? maxKeys, IServiceProvider services, CancellationToken ct) => {
                var fileStorage = GetFileStorage(services, serviceKey);
                if (fileStorage is not IFileStorageDiagnosticsService dx)
                    throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidOperation, "Not implemented."));

                var cap = Math.Clamp(maxKeys ?? 1000, 1, 10_000);
                var keys = await dx.ListStorageKeysAsync(prefix, cap, ct);
                return Results.Ok(keys);
            });

        group.MapGet(
            "key-ids", async (IServiceProvider services, CancellationToken ct) => {
                var ids = await ListEncryptionKeyIdsAsync(services, serviceKey, ct).ConfigureAwait(false);
                return Results.Ok(ids);
            });

        group.MapPost(
            "multipart/begin", async ([FromBody] BeginMultipartRequest request, IServiceProvider services, CancellationToken ct) => {
                var multipart = GetMultipart(services, serviceKey);
                var begin = new MultipartBeginRequest {
                    DeclaredContentLength = request.DeclaredContentLength,
                    PartSizeBytes = request.PartSizeBytes,
                    Compress = request.Compress,
                    Encrypt = request.Encrypt,
                    KeyId = request.KeyId,
                    PathPrefix = request.PathPrefix,
                    ContentType = request.ContentType,
                    OriginalFileName = request.OriginalFileName,
                    TenantId = request.TenantId,
                    SessionTtl = request.SessionTtlHours.HasValue ? TimeSpan.FromHours(request.SessionTtlHours.Value) : null
                };

                var result = await multipart.BeginAsync(begin, ct);
                return Results.Ok(ToHttp(result));
            });

        group.MapGet(
            "multipart/{sessionId:guid}/part-url", async (Guid sessionId, int partNumber, IServiceProvider services, CancellationToken ct) => {
                var multipart = GetMultipart(services, serviceKey);
                var descriptor = await multipart.GetPresignedPartUploadAsync(sessionId, partNumber, ct);
                return Results.Ok(ToHttp(descriptor));
            });

        group.MapPost(
            "multipart/complete", async ([FromBody] CompleteMultipartRequest request, IServiceProvider services, ICacheService cache, CancellationToken ct) => {
                var multipart = GetMultipart(services, serviceKey);
                var parts = request.Parts.Select(p => new Lyo.FileStorage.Multipart.CompletedPart { PartNumber = p.PartNumber, ETagOrBlockId = p.ETagOrBlockId }).ToList();
                var complete = new CompleteMultipartUploadRequest { SessionId = request.SessionId, Parts = parts };
                try {
                    var result = await multipart.CompleteAsync(complete, ct);
                    await InvalidateFileMetadataQueryCacheAsync(cache).ConfigureAwait(false);
                    return Results.Ok(result);
                }
                catch (InvalidOperationException ex) {
                    throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidRequest, ex.Message));
                }
            });

        group.MapPost(
            "multipart/{sessionId:guid}/abort", async (Guid sessionId, IServiceProvider services, CancellationToken ct) => {
                var multipart = GetMultipart(services, serviceKey);
                await multipart.AbortAsync(sessionId, ct);
                return Results.Ok();
            });

        group.MapGet(
            "files/{fileId:guid}/metadata", async (
                Guid fileId, IServiceProvider services, IDbContextFactory<FileMetadataStoreDbContext> dbFactory, CancellationToken ct, bool? includeDeleted) => {
                if (includeDeleted == true) {
                    await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
                    var entity = await db.FileMetadata.AsNoTracking().FirstOrDefaultAsync(e => e.Id == fileId.ToString(), ct).ConfigureAwait(false);
                    if (entity == null)
                        throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, "Resource was not found."));

                    return Results.Ok(entity.ToFileStoreResult());
                }

                var fileStorage = GetFileStorage(services, serviceKey);
                return Results.Ok(await fileStorage.GetMetadataAsync(fileId, ct).ConfigureAwait(false));
            });

        group.MapGet(
            "files/archive", async (HttpContext http, [FromQuery] Guid[]? id, [FromQuery] string? fileName, IServiceProvider services, CancellationToken ct) => {
                var ids = id ?? [];
                if (ids.Length == 0)
                    throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidRequest, "At least one file id is required."));

                try {
                    var entries = ids.Select(fileId => new Domain.FileStorageArchiveEntry(fileId)).ToList();
                    var archive = await GetArchive(services, serviceKey).CreateArchiveAsync(entries, fileName, ct).ConfigureAwait(false);
                    if (archive.Length > 0)
                        http.Response.ContentLength = archive.Length;

                    return Results.Stream(archive.Stream, archive.ContentType, archive.FileName, enableRangeProcessing: true);
                }
                catch (FileStorageArchiveLimitException ex) {
                    throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidRequest, ex.Message));
                }
                catch (FileNotFoundException ex) {
                    throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, ex.Message));
                }
                catch (ArgumentException ex) {
                    throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidRequest, ex.Message));
                }
            });

        group.MapGet(
            "files/{fileId:guid}/download", async (HttpContext http, Guid fileId, bool? inline, IServiceProvider services, CancellationToken ct) => {
                var fileStorage = GetFileStorage(services, serviceKey);
                var metadata = await fileStorage.GetMetadataAsync(fileId, ct);
                return await StreamStoredFileAsync(http, fileStorage, fileId, metadata, inline == true, ct);
            });

        group.MapGet(
            "files/access/{token}/download", async (HttpContext http, string token, IServiceProvider services, CancellationToken ct) => {
                var accessService = services.GetRequiredService<IFileDownloadAccessService>();
                var access = await accessService.ValidateAndConsumeDownloadAsync(token, http.User.Identity?.Name, http.Connection.RemoteIpAddress?.ToString(), ct: ct);
                if (!access.IsAllowed || access.FileId == null)
                    return Results.StatusCode(MapFailureStatusCode(access.FailureReason));

                var fileStorage = GetFileStorage(services, serviceKey);
                var fileId = access.FileId.Value;
                var metadata = await fileStorage.GetMetadataAsync(fileId, ct);
                return await StreamStoredFileAsync(http, fileStorage, fileId, metadata, inline: false, ct);
            });

        group.MapGet(
            "files/access/{token}/presigned-read", async (
                string token, double? expiresHours, string? contentDisposition, string? contentType, IServiceProvider services, HttpContext http, CancellationToken ct) => {
                var accessService = services.GetRequiredService<IFileDownloadAccessService>();
                var access = await accessService.ValidateAndConsumeDownloadAsync(token, http.User.Identity?.Name, http.Connection.RemoteIpAddress?.ToString(), ct: ct);
                if (!access.IsAllowed || access.FileId == null)
                    return Results.StatusCode(MapFailureStatusCode(access.FailureReason));

                var fileStorage = GetFileStorage(services, serviceKey);
                var metadata = await fileStorage.GetMetadataAsync(access.FileId.Value, ct);
                var expiration = expiresHours.HasValue ? TimeSpan.FromHours(expiresHours.Value) : (TimeSpan?)null;
                var opts = BuildPreSignedReadUrlOptions(contentDisposition, contentType);
                var url = opts == null
                    ? await fileStorage.GetPreSignedReadUrlAsync(access.FileId.Value, expiration, metadata.PathPrefix, ct)
                    : await fileStorage.GetPreSignedReadUrlAsync(access.FileId.Value, expiration, metadata.PathPrefix, opts, ct);

                return Results.Ok(new PresignedReadResponse(url));
            });

        group.MapDelete(
            "files/{fileId:guid}", async (Guid fileId, IServiceProvider services, ICacheService cache, CancellationToken ct) => {
                var fileStorage = GetFileStorage(services, serviceKey);
                try {
                    var deleted = await fileStorage.DeleteFileAsync(fileId, ct: ct).ConfigureAwait(false);
                    if (deleted)
                        await InvalidateFileMetadataQueryCacheAsync(cache).ConfigureAwait(false);

                    return Results.Ok(deleted);
                }
                catch (FileNotFoundException) {
                    await InvalidateFileMetadataQueryCacheAsync(cache).ConfigureAwait(false);
                    return Results.Ok(true);
                }
            });

        group.MapPost(
            "files/migrate-deks", async ([FromBody] MigrateDeksRequest request, IServiceProvider services, ICacheService cache, CancellationToken ct) => {
                var fileStorage = GetFileStorage(services, serviceKey);
                var result = await fileStorage.MigrateDeksAsync(
                    request.SourceKeyId, request.SourceKeyVersion, request.TargetKeyId, request.TargetKeyVersion, request.BatchSize, ct);

                await InvalidateFileMetadataQueryCacheAsync(cache).ConfigureAwait(false);
                return Results.Ok(result);
            });

        group.MapPost(
            "files/rotate-deks", async ([FromBody] RotateDeksRequest request, IServiceProvider services, ICacheService cache, CancellationToken ct) => {
                var fileStorage = GetFileStorage(services, serviceKey);
                var result = await fileStorage.RotateDeksAsync(request.FileIds, request.TargetKeyId, request.TargetKeyVersion, request.BatchSize, ct);
                await InvalidateFileMetadataQueryCacheAsync(cache).ConfigureAwait(false);
                return Results.Ok(result);
            });
    }

    private static IFileStorageService GetFileStorage(IServiceProvider services, string serviceKey)
        => services.GetRequiredKeyedService<IFileStorageService>(serviceKey);

    private static IMultipartUploadService GetMultipart(IServiceProvider services, string serviceKey)
        => services.GetRequiredKeyedService<IMultipartUploadService>(serviceKey);

    private static IStagedFileUploadService GetStaged(IServiceProvider services, string serviceKey)
        => services.GetRequiredKeyedService<IStagedFileUploadService>(serviceKey);

    private static IFileStorageArchiveService GetArchive(IServiceProvider services, string serviceKey)
        => services.GetRequiredKeyedService<IFileStorageArchiveService>(serviceKey);

    private static async Task<IResult> SaveStreamFromFormAsync(
        IFormFile file,
        string? originalFileName,
        bool compress,
        bool encrypt,
        string? keyId,
        string? pathPrefix,
        int? chunkSize,
        string? contentType,
        string? tenantId,
        IFileStorageService fileStorage,
        ICacheService cache,
        CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        try {
            var result = await fileStorage.SaveFromStreamAsync(
                stream, file.Length, originalFileName ?? file.FileName, compress, encrypt, keyId, pathPrefix, chunkSize, contentType, tenantId, ct: ct);

            await InvalidateFileMetadataQueryCacheAsync(cache).ConfigureAwait(false);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex) {
            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidRequest, ex.Message));
        }
    }

    private static Task InvalidateFileMetadataQueryCacheAsync(ICacheService cache) => cache.InvalidateQueryCacheAsync<FileMetadataEntity>();

    private static Domain.PreSignedReadUrlOptions? BuildPreSignedReadUrlOptions(string? contentDisposition, string? contentType)
    {
        var cd = string.IsNullOrWhiteSpace(contentDisposition) ? null : contentDisposition.Trim();
        var mime = string.IsNullOrWhiteSpace(contentType) ? null : contentType.Trim();
        if (cd == null && mime == null)
            return null;

        return new() { ContentDisposition = cd, ContentType = mime };
    }

    private static string BuildContentDisposition(string? fileName, bool inline)
    {
        var leaf = Path.GetFileName((fileName ?? "download").Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(leaf))
            leaf = "download";

        var ascii = leaf.Replace("\"", "'", StringComparison.Ordinal);
        var kind = inline ? "inline" : "attachment";
        return $"{kind}; filename=\"{ascii}\"; filename*=UTF-8''{Uri.EscapeDataString(leaf)}";
    }

    /// <summary>
    /// Streams stored bytes through the API (decrypt/decompress on the host). Direct-to-bucket URLs are only <c>presigned-read</c> and access-link <c>presigned-read</c>.
    /// </summary>
    private static async Task<IResult> StreamStoredFileAsync(
        HttpContext http,
        IFileStorageService fileStorage,
        Guid fileId,
        FileStoreResult metadata,
        bool inline,
        CancellationToken ct)
    {
        var stream = await fileStorage.GetFileStreamAsync(fileId, ct: ct);
        if (stream == null)
            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, "Resource was not found."));

        if (metadata.OriginalFileSize > 0)
            http.Response.ContentLength = metadata.OriginalFileSize;

        var fileName = metadata.OriginalFileName ?? metadata.SourceFileName;
        var mime = string.IsNullOrWhiteSpace(metadata.ContentType) ? FileTypeInfo.Unknown.MimeType : metadata.ContentType;
        if (inline) {
            http.Response.Headers.ContentDisposition = BuildContentDisposition(fileName, inline: true);
            return Results.Stream(stream, mime, enableRangeProcessing: true);
        }

        return Results.Stream(stream, mime, fileName, enableRangeProcessing: true);
    }

    private static Domain.DirectUploadBeginRequest ToDomain(DirectUploadBeginRequest request)
        => new() {
            OriginalFileName = request.OriginalFileName,
            PathPrefix = request.PathPrefix,
            DeclaredMaxSizeBytes = request.DeclaredMaxSizeBytes,
            ContentType = request.ContentType,
            TenantId = request.TenantId,
            UrlExpiration = Hours(request.UrlExpirationHours)
        };

    private static Domain.DirectUploadCompleteRequest? ToDomain(DirectUploadCompleteRequest? request)
        => request is null
            ? null
            : new() { ExpectedByteLength = request.ExpectedByteLength, OriginalFileName = request.OriginalFileName };

    private static Domain.StagedUploadBeginRequest ToDomain(StagedUploadBeginRequest request)
        => new() {
            OriginalFileName = request.OriginalFileName,
            PathPrefix = request.PathPrefix,
            DeclaredMaxSizeBytes = request.DeclaredMaxSizeBytes,
            ContentType = request.ContentType,
            TenantId = request.TenantId,
            UrlExpiration = Hours(request.UrlExpirationHours),
            SessionTtl = Hours(request.SessionTtlHours)
        };

    private static Domain.StagedUploadCompleteRequest? ToDomain(StagedUploadCompleteRequest? request)
        => request is null
            ? null
            : new() { ExpectedByteLength = request.ExpectedByteLength, OriginalFileName = request.OriginalFileName };

    private static Domain.StagedUploadCommitRequest ToDomain(StagedUploadCommitRequest request)
        => new() {
            Compress = request.Compress,
            Encrypt = request.Encrypt,
            KeyId = request.KeyId,
            PathPrefix = request.PathPrefix,
            ChunkSize = request.ChunkSize
        };

    private static MultipartBeginResponse ToHttp(MultipartBeginResult result)
        => new(result.SessionId, result.TargetFileId, result.PartSizeBytes, result.ExpiresUtc, result.ProviderKind.ToString());

    private static MultipartPartUrlResponse ToHttp(MultipartPartDescriptor descriptor)
        => new(descriptor.PartNumber, descriptor.PresignedPutUrl, descriptor.HttpMethod);

    private static StagedUploadBeginResult ToHttp(Domain.StagedUploadBeginResult result)
        => new(result.StageId, result.PresignedPutUrl, result.UrlExpiresUtc, result.StorageLocation, result.ProviderKind.ToString(), result.RequiredPutHeaders);

    private static async Task<IReadOnlyList<string>> ListEncryptionKeyIdsAsync(IServiceProvider services, string serviceKey, CancellationToken ct)
    {
        var keyed = services.GetKeyedService<IKeyStore>(serviceKey);
        if (keyed is IKeyInventoryStore keyedInventory)
            return await keyedInventory.GetAvailableKeyIdsAsync(ct).ConfigureAwait(false);

        if (keyed != null)
            return [];

        var unkeyed = services.GetService<IKeyStore>() as IKeyInventoryStore ?? services.GetService<IKeyInventoryStore>();
        if (unkeyed == null)
            return [];

        return await unkeyed.GetAvailableKeyIdsAsync(ct).ConfigureAwait(false);
    }

    private static TimeSpan? Hours(double? hours) => hours is null ? null : TimeSpan.FromHours(hours.Value);

    private static int MapFailureStatusCode(FileDownloadAccessConsumeFailureReason? reason)
        => reason switch {
            FileDownloadAccessConsumeFailureReason.InvalidToken => StatusCodes.Status400BadRequest,
            FileDownloadAccessConsumeFailureReason.LockUnavailable => StatusCodes.Status429TooManyRequests,
            FileDownloadAccessConsumeFailureReason.NotFound => StatusCodes.Status404NotFound,
            FileDownloadAccessConsumeFailureReason.Revoked => StatusCodes.Status403Forbidden,
            FileDownloadAccessConsumeFailureReason.NotYetValid => StatusCodes.Status403Forbidden,
            FileDownloadAccessConsumeFailureReason.Expired => StatusCodes.Status410Gone,
            FileDownloadAccessConsumeFailureReason.OutsideWindow => StatusCodes.Status403Forbidden,
            FileDownloadAccessConsumeFailureReason.MaxDownloadsReached => StatusCodes.Status429TooManyRequests,
            var _ => StatusCodes.Status403Forbidden
        };
}
