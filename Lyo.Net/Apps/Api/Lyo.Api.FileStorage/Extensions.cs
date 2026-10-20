using Lyo.Api.ApiEndpoint;
using Lyo.Api.FileStorage.Models;
using Lyo.Api.Models.Error;
using Lyo.Cache;
using Lyo.Common.Core.Net;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.DownloadAccess;
using Lyo.FileMetadataStore.Models;
using Lyo.FileMetadataStore.Postgres;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.FileStorage;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Multipart;
using Lyo.KeyStore;
using Domain = Lyo.FileStorage.Models;
using IFileDownloadAccessService = Lyo.FileMetadataStore.DownloadAccess.IFileDownloadAccessService;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ApiErrorCodes = Lyo.Api.Models.Constants.ApiErrorCodes;

namespace Lyo.Api.FileStorage;

/// <summary>Maps file-storage HTTP routes and the FileMetadata QueryProject surface.</summary>
public static class Extensions
{
    /// <summary>
    /// Maps <c>{Route}</c> file, multipart, and archive routes, optional <c>POST {DirectUploadPath}</c>, and read-only FileMetadata Query/QueryProject.
    /// Without <see cref="IFileStorageFactory" />, storage, multipart, and archive resolve with <see cref="FileStorageApiOptions.ServiceKey" />.
    /// With a factory, <c>GET {Route}/stores</c> lists each name and <c>{Route}/{name}</c> serves that store. <c>FileStorage:Default</c> serves the bare route and direct upload.
    /// </summary>
    public static WebApplication BuildFileStorageApi(this WebApplication app, FileStorageApiOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(app);
        options ??= new();
        options.Validate();
        var route = options.Route.Trim().Trim('/');
        using (var scope = app.Services.CreateScope()) {
            var factory = scope.ServiceProvider.GetService<IFileStorageFactory>();
            if (factory == null)
                MapKeyedFileStorage(app, options, route);
            else
                MapFactoryFileStorage(app, options, route, factory, scope.ServiceProvider);
        }

        MapFileMetadataQuery(app, options);
        return app;
    }

    private static void MapKeyedFileStorage(WebApplication app, FileStorageApiOptions options, string route)
    {
        var key = options.ServiceKey;
        MapFileStorageGroup(app, route, services => GetFileStorage(services, key), key, new(key, Required: false), mapAccessLinks: true);
        MapDirectUpload(app, options, services => GetFileStorage(services, key));
    }

    private static void MapFactoryFileStorage(WebApplication app, FileStorageApiOptions options, string route, IFileStorageFactory factory, IServiceProvider services)
    {
        var stores = factory.Names.Select(name => (Name: name, EncryptionKey: factory.GetEncryptionServiceKey(name))).ToArray();
        foreach (var store in stores) {
            if (ReservedStoreNames.Contains(store.Name))
                throw new InvalidOperationException($"File storage '{store.Name}' uses a reserved route name.");
        }

        var defaultName = factory.DefaultName;
        string? canonicalDefault = null;
        if (!string.IsNullOrWhiteSpace(defaultName)) {
            canonicalDefault = stores.FirstOrDefault(store => string.Equals(store.Name, defaultName, StringComparison.OrdinalIgnoreCase)).Name;
            if (canonicalDefault == null)
                throw new InvalidOperationException($"File storage default '{defaultName}' is not registered.");
        }

        app.MapGet(route + "/stores", (IFileStorageFactory current) => Results.Ok(current.Names.Select(name => new FileStorageStoreResponse(name, route + "/" + name, null)).ToArray()))
            .WithTags("FileStorage");

        var hasKeyed = services is IServiceProviderIsKeyedService keyedServices && keyedServices.IsKeyedService(typeof(IFileStorageService), options.ServiceKey);
        if (canonicalDefault != null) {
            var name = canonicalDefault;
            var encryptionKey = stores.First(store => store.Name == name).EncryptionKey;
            MapFileStorageGroup(app, route, current => current.GetRequiredService<IFileStorageFactory>().Get(name), name, new(encryptionKey, Required: true), mapAccessLinks: true);
            MapDirectUpload(app, options, current => current.GetRequiredService<IFileStorageFactory>().Get(name));
        }
        else if (hasKeyed) {
            MapKeyedFileStorage(app, options, route);
        }

        foreach (var store in stores)
            MapFileStorageGroup(
                app, route + "/" + store.Name, current => current.GetRequiredService<IFileStorageFactory>().Get(store.Name), store.Name,
                new(store.EncryptionKey, Required: true), mapAccessLinks: false);
    }

    private static void MapFileMetadataQuery(WebApplication app, FileStorageApiOptions options)
    {
        app.CreateReadOnlyBuilder<FileMetadataStoreDbContext, FileMetadataEntity, FileMetadataEntity, string>(options.FileMetadataRoute, "FileMetadata")
            .AllowAnonymous()
            .WithReadOnlyEndpoints()
            .Build();
    }

    private static void MapDirectUpload(WebApplication app, FileStorageApiOptions options, Func<IServiceProvider, IFileStorageService> resolveStorage)
    {
        if (string.IsNullOrWhiteSpace(options.DirectUploadPath))
            return;

        app.MapPost(
                options.DirectUploadPath, async (
                    IFormFile file, [FromQuery] string? originalFileName, [FromQuery] bool compress, [FromQuery] bool encrypt, [FromQuery] string? keyId,
                    [FromQuery] string? pathPrefix, [FromQuery] int? chunkSize, [FromQuery] string? contentType, [FromQuery] string? charset, [FromQuery] string? tenantId,
                    IServiceProvider services,
                    ICacheService cache, CancellationToken ct) => {
                    var fileStorage = resolveStorage(services);
                    return await SaveStreamFromFormAsync(
                        file, originalFileName, compress, encrypt, keyId, pathPrefix, chunkSize, contentType, charset, tenantId, fileStorage, cache, ct);
                })
            .DisableAntiforgery()
            .WithTags("DirectFileUpload");
    }

    private static void MapFileStorageGroup(
        WebApplication app,
        string route,
        Func<IServiceProvider, IFileStorageService> resolveStorage,
        string serviceKey,
        KeyStoreLookup keyStore,
        bool mapAccessLinks)
    {
        var group = app.MapGroup(route).WithTags("FileStorage");

        group.MapGet(
            "health", async (IServiceProvider services, CancellationToken ct) => {
                var fileStorage = resolveStorage(services);
                var health = await fileStorage.CheckHealthAsync(ct);
                var message = health.Message;
                if (!health.IsHealthy && string.IsNullOrWhiteSpace(message) && health.Exception != null)
                    message = health.Exception.Message;

                return Results.Ok(new FileStorageHealthResponse(health.IsHealthy, message));
            });

        group.MapGet(
            "capabilities", async (IServiceProvider services, CancellationToken ct) => {
                var fileStorage = resolveStorage(services);
                var multipart = services.GetKeyedService<IMultipartUploadService>(serviceKey);
                var (keyed, unkeyed, inventory) = ResolveKeyStores(services, keyStore);

                IReadOnlyList<FileStorageTagTypeResponse> tagTypes = [];
                if (fileStorage is IFileStorageTags tags)
                    tagTypes = (await tags.GetTagTypesAsync(ct).ConfigureAwait(false)).Select(ToTagType).ToArray();

                var composed = FileStorageCapabilitiesComposer.Compose(
                    fileStorage, multipart, FileStorageCapabilitiesComposer.HasEncryptionStore(keyed, unkeyed, inventory),
                    services.GetKeyedService<IFileStorageArchiveService>(serviceKey) != null, services.GetService<IFileDownloadAccessService>() != null, tagTypes,
                    FileTagValueKinds.All.Select(ToKind).ToArray());
                return Results.Ok(composed);
            });

        group.MapGet(
            "files/{fileId:guid}/tags", async (Guid fileId, IServiceProvider services, CancellationToken ct) => {
                var tags = Tags(resolveStorage(services));
                var rows = await tags.GetTagsAsync(fileId, ct).ConfigureAwait(false);
                return Results.Ok(rows.Select(static row => new FileTagResponse(row.Id, row.FileId, row.TagType, row.Value, row.Slug, row.Automatic)).ToArray());
            });

        group.MapPut(
            "files/{fileId:guid}/tags", async (Guid fileId, [FromBody] FileTagWriteRequest request, IServiceProvider services, CancellationToken ct) => {
                ArgumentHelpers.ThrowIfNull(request);
                var tags = Tags(resolveStorage(services));
                var row = await tags.AddTagAsync(fileId, request.TagType, request.Value, request.Slug, ct).ConfigureAwait(false);
                return Results.Ok(new FileTagResponse(row.Id, row.FileId, row.TagType, row.Value, row.Slug, row.Automatic));
            });

        group.MapDelete(
            "files/{fileId:guid}/tags", async (Guid fileId, string tagType, string value, IServiceProvider services, CancellationToken ct) => {
                var tags = Tags(resolveStorage(services));
                var removed = await tags.RemoveTagAsync(fileId, tagType, value, ct).ConfigureAwait(false);
                return Results.Ok(removed);
            });

        group.MapGet(
            "files/tags/group", async (string[] groupBy, string? path, int? maxKeys, IServiceProvider services, CancellationToken ct) => {
                var tags = Tags(resolveStorage(services));
                var fileStorage = resolveStorage(services);
                var keys = groupBy.Select(FileTagGroupKey.Parse).ToArray();
                var cap = Math.Clamp(maxKeys ?? 10_000, 1, 10_000);
                var (entries, truncated) = await tags.ListTagGroupAsync(keys, path, cap, ct).ConfigureAwait(false);
                var dtos = entries.Select(
                    static e => new FileStorageFolderEntryDto(
                        e.Name, e.IsDirectory, e.PathPrefix, e.FileId, (Lyo.Api.FileStorage.Models.FileStoragePresence)(int)e.Presence, e.OriginalFileName,
                        e.OriginalFileSize, e.PhysicalKey, e.IsCompressed, e.Compression, e.IsEncrypted, e.Encryption)).ToList();
                var backend = (Lyo.Api.FileStorage.Models.FileStorageBackendKind)(int)FileStorageBackend.Detect(fileStorage);
                return Results.Ok(new FileStorageFolderListResponse(dtos, truncated, backend));
            });

        group.MapPost(
            "files/save", async ([FromBody] SaveFileRequest request, IServiceProvider services, ICacheService cache, CancellationToken ct) => {
                var fileStorage = resolveStorage(services);
                var result = await fileStorage.SaveFileAsync(
                    request.Data, request.OriginalFileName, request.Compress, request.Encrypt, request.KeyId, request.PathPrefix, request.ChunkSize, request.ContentType,
                    request.Charset, request.TenantId, request.Metadata, ct);

                await InvalidateFileMetadataQueryCacheAsync(cache).ConfigureAwait(false);
                return Results.Ok(result);
            });

        if (mapAccessLinks)
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
                    [FromQuery] string? pathPrefix, [FromQuery] int? chunkSize, [FromQuery] string? contentType, [FromQuery] string? charset, [FromQuery] string? tenantId,
                    IServiceProvider services,
                    ICacheService cache, CancellationToken ct) => {
                    var fileStorage = resolveStorage(services);
                    return await SaveStreamFromFormAsync(
                        file, originalFileName, compress, encrypt, keyId, pathPrefix, chunkSize, contentType, charset, tenantId, fileStorage, cache, ct);
                })
            .DisableAntiforgery();

        group.MapGet(
            "files/{fileId:guid}/presigned-read", async (
                Guid fileId, double? expiresHours, string? pathPrefix, string? contentDisposition, string? contentType, IServiceProvider services, CancellationToken ct) => {
                var fileStorage = resolveStorage(services);
                var expiration = expiresHours.HasValue ? TimeSpan.FromHours(expiresHours.Value) : (TimeSpan?)null;
                var opts = BuildPreSignedReadUrlOptions(contentDisposition, contentType);
                var url = opts == null
                    ? await fileStorage.GetPreSignedReadUrlAsync(fileId, expiration, pathPrefix, ct)
                    : await fileStorage.GetPreSignedReadUrlAsync(fileId, expiration, pathPrefix, opts, ct);

                return Results.Ok(new PresignedReadResponse(url));
            });

        group.MapPost(
            "direct-upload/begin", async ([FromBody] DirectUploadBeginRequest request, IServiceProvider services, CancellationToken ct) => {
                var fileStorage = resolveStorage(services);
                var result = await fileStorage.BeginDirectUploadAsync(ToDomain(request), ct);
                return Results.Ok(result);
            });

        group.MapPost(
            "direct-upload/{fileId:guid}/complete", async (
                Guid fileId, [FromBody] DirectUploadCompleteRequest? request, IServiceProvider services, ICacheService cache, CancellationToken ct) => {
                var fileStorage = resolveStorage(services);
                var result = await fileStorage.CompleteDirectUploadAsync(fileId, ToDomain(request), ct);
                await InvalidateFileMetadataQueryCacheAsync(cache).ConfigureAwait(false);
                return Results.Ok(result);
            });

        group.MapPut(
                "direct-upload/{fileId:guid}/put", async (Guid fileId, HttpContext http, IServiceProvider services, CancellationToken ct) => {
                    var fileStorage = resolveStorage(services);
                    if (fileStorage is not LocalFileStorageService local)
                        throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidOperation, "Not implemented."));

                    await local.ReceiveDirectPutAsync(fileId, http.Request.Body, ct).ConfigureAwait(false);
                    return Results.NoContent();
                })
            .DisableAntiforgery();

        group.MapPost(
            "files/copy", async ([FromBody] CopyFileRequest request, IServiceProvider services, ICacheService cache, CancellationToken ct) => {
                var fileStorage = resolveStorage(services);
                var result = await fileStorage.CopyFileAsync(request.SourceFileId, new Domain.CopyFileRequest { PathPrefix = request.PathPrefix }, ct);
                await InvalidateFileMetadataQueryCacheAsync(cache).ConfigureAwait(false);
                return Results.Ok(result);
            });

        group.MapPost(
            "files/move", async ([FromBody] MoveFileRequest request, IServiceProvider services, ICacheService cache, CancellationToken ct) => {
                var fileStorage = resolveStorage(services);
                var result = await fileStorage.MoveFileAsync(request.FileId, new Domain.MoveFileRequest { PathPrefix = request.PathPrefix }, ct);
                await InvalidateFileMetadataQueryCacheAsync(cache).ConfigureAwait(false);
                return Results.Ok(result);
            });

        group.MapPost(
            "files/rename", async ([FromBody] RenameFileRequest request, IServiceProvider services, ICacheService cache, CancellationToken ct) => {
                var fileStorage = resolveStorage(services);
                var result = await fileStorage.RenameFileAsync(request.FileId, new Domain.RenameFileRequest { OriginalFileName = request.OriginalFileName }, ct);
                await InvalidateFileMetadataQueryCacheAsync(cache).ConfigureAwait(false);
                return Results.Ok(result);
            });

        group.MapGet(
            "files/physical/download", async (string physicalKey, IServiceProvider services, CancellationToken ct) => {
                var fileStorage = resolveStorage(services);
                return await RunPhysicalAsync(
                    async () => {
                        var (stream, fileName) = await FileStoragePhysicalRead.OpenAsync(fileStorage, physicalKey, ct).ConfigureAwait(false);
                        return Results.Stream(stream, FileTypeInfo.Unknown.MimeType, fileName, enableRangeProcessing: true);
                    }).ConfigureAwait(false);
            });

        group.MapDelete(
            "files/physical", async (string physicalKey, IServiceProvider services, CancellationToken ct) => {
                var fileStorage = resolveStorage(services);
                return await RunPhysicalAsync(
                    async () => {
                        await FileStoragePhysicalMutate.DeleteAsync(fileStorage, physicalKey, ct).ConfigureAwait(false);
                        return Results.Ok(true);
                    }).ConfigureAwait(false);
            });

        group.MapPost(
            "files/physical/move", async ([FromBody] MovePhysicalObjectRequest request, IServiceProvider services, CancellationToken ct) => {
                var fileStorage = resolveStorage(services);
                return await RunPhysicalAsync(
                    async () => {
                        var key = await FileStoragePhysicalMutate
                            .MoveToPrefixAsync(fileStorage, request.PhysicalKey, request.DestinationPathPrefix, ct)
                            .ConfigureAwait(false);
                        return Results.Ok(new PhysicalObjectResult(key));
                    }).ConfigureAwait(false);
            });

        group.MapPost(
            "files/physical/copy", async ([FromBody] CopyPhysicalObjectRequest request, IServiceProvider services, CancellationToken ct) => {
                var fileStorage = resolveStorage(services);
                return await RunPhysicalAsync(
                    async () => {
                        var key = await FileStoragePhysicalMutate
                            .CopyToPrefixAsync(fileStorage, request.PhysicalKey, request.DestinationPathPrefix, ct)
                            .ConfigureAwait(false);
                        return Results.Ok(new PhysicalObjectResult(key));
                    }).ConfigureAwait(false);
            });

        group.MapPost(
            "files/physical/rename", async ([FromBody] RenamePhysicalObjectRequest request, IServiceProvider services, CancellationToken ct) => {
                var fileStorage = resolveStorage(services);
                return await RunPhysicalAsync(
                    async () => {
                        var key = await FileStoragePhysicalMutate
                            .RenameAsync(fileStorage, request.PhysicalKey, request.NewFileName, ct)
                            .ConfigureAwait(false);
                        return Results.Ok(new PhysicalObjectResult(key));
                    }).ConfigureAwait(false);
            });

        group.MapGet(
            "files/folder", async (string? pathPrefix, int? maxKeys, IServiceProvider services, CancellationToken ct) => {
                var fileStorage = resolveStorage(services);
                var store = GetMetadataStore(fileStorage, services, serviceKey);
                var cap = Math.Clamp(maxKeys ?? 10_000, 1, 10_000);
                var (entries, truncated) = await FileStorageFolderList.ListAsync(store, fileStorage, pathPrefix, cap, ct).ConfigureAwait(false);
                var dtos = entries.Select(
                    static e => new FileStorageFolderEntryDto(
                        e.Name, e.IsDirectory, e.PathPrefix, e.FileId, (Lyo.Api.FileStorage.Models.FileStoragePresence)(int)e.Presence, e.OriginalFileName,
                        e.OriginalFileSize, e.PhysicalKey, e.IsCompressed, e.Compression, e.IsEncrypted, e.Encryption)).ToList();
                var backend = (Lyo.Api.FileStorage.Models.FileStorageBackendKind)(int)FileStorageBackend.Detect(fileStorage);
                return Results.Ok(new FileStorageFolderListResponse(dtos, truncated, backend));
            });

        group.MapGet(
            "diagnostics/storage-keys", async (string? prefix, int? maxKeys, IServiceProvider services, CancellationToken ct) => {
                var fileStorage = resolveStorage(services);
                if (fileStorage is not IFileStorageDiagnosticsService dx)
                    throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidOperation, "Not implemented."));

                var cap = Math.Clamp(maxKeys ?? 1000, 1, 10_000);
                var keys = await dx.ListStorageKeysAsync(prefix, cap, ct);
                return Results.Ok(keys);
            });

        group.MapGet(
            "key-ids", async (IServiceProvider services, CancellationToken ct) => {
                var ids = await ListEncryptionKeyIdsAsync(services, keyStore, ct).ConfigureAwait(false);
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
                    Charset = request.Charset,
                    OriginalFileName = request.OriginalFileName,
                    TenantId = request.TenantId,
                    SessionTtl = request.SessionTtlHours.HasValue ? TimeSpan.FromHours(request.SessionTtlHours.Value) : null,
                    Metadata = request.Metadata
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

                var fileStorage = resolveStorage(services);
                return Results.Ok(await fileStorage.GetMetadataAsync(fileId, ct).ConfigureAwait(false));
            });

        group.MapGet(
            "files/{fileId:guid}/integrity", async (Guid fileId, IServiceProvider services, CancellationToken ct) => {
                var fileStorage = resolveStorage(services);
                return Results.Ok(await fileStorage.GetContentIntegrityAsync(fileId, ct).ConfigureAwait(false));
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
                var fileStorage = resolveStorage(services);
                var metadata = await fileStorage.GetMetadataAsync(fileId, ct);
                return await StreamStoredFileAsync(http, fileStorage, fileId, metadata, inline == true, ct);
            });

        if (mapAccessLinks)
            group.MapGet(
            "files/access/{token}/download", async (HttpContext http, string token, IServiceProvider services, CancellationToken ct) => {
                var accessService = services.GetRequiredService<IFileDownloadAccessService>();
                var access = await accessService.ValidateAndConsumeDownloadAsync(token, http.User.Identity?.Name, http.Connection.RemoteIpAddress?.ToString(), ct: ct);
                if (!access.IsAllowed || access.FileId == null)
                    return Results.StatusCode(MapFailureStatusCode(access.FailureReason));

                var fileStorage = resolveStorage(services);
                var fileId = access.FileId.Value;
                var metadata = await fileStorage.GetMetadataAsync(fileId, ct);
                return await StreamStoredFileAsync(http, fileStorage, fileId, metadata, inline: false, ct);
            });

        if (mapAccessLinks)
            group.MapGet(
            "files/access/{token}/presigned-read", async (
                string token, double? expiresHours, string? contentDisposition, string? contentType, IServiceProvider services, HttpContext http, CancellationToken ct) => {
                var accessService = services.GetRequiredService<IFileDownloadAccessService>();
                var access = await accessService.ValidateAndConsumeDownloadAsync(token, http.User.Identity?.Name, http.Connection.RemoteIpAddress?.ToString(), ct: ct);
                if (!access.IsAllowed || access.FileId == null)
                    return Results.StatusCode(MapFailureStatusCode(access.FailureReason));

                var fileStorage = resolveStorage(services);
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
                var fileStorage = resolveStorage(services);
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
                catch {
                    // Metadata is tombstoned or purged before the blob is gone, so a failed delete still busts cached metadata queries.
                    await InvalidateFileMetadataQueryCacheAsync(cache).ConfigureAwait(false);
                    throw;
                }
            });

        group.MapPost(
            "files/migrate-deks", async ([FromBody] MigrateDeksRequest request, IServiceProvider services, ICacheService cache, CancellationToken ct) => {
                var fileStorage = resolveStorage(services);
                var result = await fileStorage.MigrateDeksAsync(
                    request.SourceKeyId, request.SourceKeyVersion, request.TargetKeyId, request.TargetKeyVersion, request.BatchSize, ct);

                await InvalidateFileMetadataQueryCacheAsync(cache).ConfigureAwait(false);
                return Results.Ok(result);
            });

        group.MapPost(
            "files/rotate-deks", async ([FromBody] RotateDeksRequest request, IServiceProvider services, ICacheService cache, CancellationToken ct) => {
                var fileStorage = resolveStorage(services);
                var result = await fileStorage.RotateDeksAsync(request.FileIds, request.TargetKeyId, request.TargetKeyVersion, request.BatchSize, ct);
                await InvalidateFileMetadataQueryCacheAsync(cache).ConfigureAwait(false);
                return Results.Ok(result);
            });
    }

    private static IFileStorageService GetFileStorage(IServiceProvider services, string serviceKey)
        => services.GetRequiredKeyedService<IFileStorageService>(serviceKey);

    private static IFileStorageTags Tags(IFileStorageService storage)
    {
        if (storage is IFileStorageTags tags)
            return tags;

        throw new InvalidOperationException("File tags are not configured for this storage service.");
    }

    private static FileStorageTagTypeResponse ToTagType(FileTagTypeInfo type)
        => new(type.TagType, type.Automatic, type.Sort.ToString(), type.Grains.Select(static grain => grain.ToString()).ToArray(), FileTagValueKinds.Name(type.ValueKind));

    private static FileStorageTagValueKindResponse ToKind(FileTagValueKind kind)
        => new(FileTagValueKinds.Name(kind), FileTagValueKinds.SortFor(kind).ToString(), FileTagValueKinds.GrainsFor(kind).Select(static grain => grain.ToString()).ToArray());

    private static IFileMetadataStore GetMetadataStore(IFileStorageService fileStorage, IServiceProvider services, string serviceKey)
    {
        if (fileStorage is FileStorageServiceBase storage)
            return storage.MetadataStore;

        var keyed = services.GetKeyedService<IFileMetadataStore>(serviceKey);
        return keyed ?? services.GetRequiredService<IFileMetadataStore>();
    }

    private static IMultipartUploadService GetMultipart(IServiceProvider services, string serviceKey)
        => services.GetRequiredKeyedService<IMultipartUploadService>(serviceKey);

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
        string? charset,
        string? tenantId,
        IFileStorageService fileStorage,
        ICacheService cache,
        CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        try {
            var result = await fileStorage.SaveFromStreamAsync(
                stream, file.Length, originalFileName ?? file.FileName, compress, encrypt, keyId, pathPrefix, chunkSize, contentType, charset, tenantId, ct: ct);

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

    private static async Task<IResult> RunPhysicalAsync(Func<Task<IResult>> action)
    {
        try {
            return await action().ConfigureAwait(false);
        }
        catch (ArgumentException ex) {
            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidRequest, ex.Message));
        }
        catch (FileNotFoundException ex) {
            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, ex.Message));
        }
        catch (InvalidOperationException ex) {
            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidOperation, ex.Message));
        }
    }

    /// <summary>
    /// Pipes stored bytes through the API (host decrypts and decompresses). A single <c>Range</c> on an uncompressed object, when the backend can slice, is
    /// <c>206 Partial Content</c>. Compressed objects ignore <c>Range</c>. Bucket-direct URLs exist only on <c>presigned-read</c> and access-link <c>presigned-read</c>.
    /// </summary>
    internal static async Task<IResult> StreamStoredFileAsync(
        HttpContext http,
        IFileStorageService fileStorage,
        Guid fileId,
        FileStoreResult metadata,
        bool inline,
        CancellationToken ct)
    {
        var ranged = fileStorage as FileStorageServiceBase;
        var canServe = StoredByteRange.CanServe(metadata.IsCompressed, ranged is { SupportsPlainByteRange: true });
        var decision = StoredByteRange.Decide(canServe, metadata.OriginalFileSize, http.Request.Headers.Range.ToString());
        var fileName = metadata.OriginalFileName ?? metadata.SourceFileName;
        var mime = string.IsNullOrWhiteSpace(metadata.ContentType) ? FileTypeInfo.Unknown.MimeType : metadata.ContentType;

        if (decision.Kind == StoredDownloadKind.Unsatisfiable)
            return new UnsatisfiableStoredFileResult(metadata.OriginalFileSize);

        if (decision.Kind == StoredDownloadKind.Partial) {
            var slice = await ranged!.OpenPlainByteRangeAsync(metadata, decision.Start, decision.EndInclusive, ct).ConfigureAwait(false);
            if (slice == null)
                throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, "Resource was not found."));

            return new PartialStoredFileResult(slice, mime, fileName, inline, decision.Start, decision.EndInclusive, metadata.OriginalFileSize);
        }

        var stream = await fileStorage.GetFileStreamAsync(fileId, ct: ct).ConfigureAwait(false);
        if (stream == null)
            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, "Resource was not found."));

        if (metadata.OriginalFileSize > 0)
            http.Response.ContentLength = metadata.OriginalFileSize;

        if (decision.AcceptRanges)
            http.Response.Headers.AcceptRanges = "bytes";

        var digest = FileContentDigest.ForDownload(http.Request.Headers.Range.Count > 0, metadata.HashAlgorithm, metadata.OriginalFileHash);
        if (digest is not null)
            http.Response.Headers.Append("Content-Digest", digest);

        if (inline) {
            http.Response.Headers.ContentDisposition = ContentDisposition.Build(fileName, inline: true);
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
            Charset = request.Charset,
            TenantId = request.TenantId,
            UrlExpiration = Hours(request.UrlExpirationHours),
            Metadata = request.Metadata,
            ExpectedSha256 = request.ExpectedSha256,
            ExpectedContentMd5 = request.ExpectedContentMd5
        };

    private static Domain.DirectUploadCompleteRequest? ToDomain(DirectUploadCompleteRequest? request)
        => request is null
            ? null
            : new() { ExpectedByteLength = request.ExpectedByteLength, OriginalFileName = request.OriginalFileName };

    private static MultipartBeginResponse ToHttp(MultipartBeginResult result)
        => new(result.SessionId, result.TargetFileId, result.PartSizeBytes, result.ExpiresUtc, result.ProviderKind.ToString());

    private static MultipartPartUrlResponse ToHttp(MultipartPartDescriptor descriptor)
        => new(descriptor.PartNumber, descriptor.PresignedPutUrl, descriptor.HttpMethod);

    private static async Task<IReadOnlyList<string>> ListEncryptionKeyIdsAsync(IServiceProvider services, KeyStoreLookup keyStore, CancellationToken ct)
    {
        var (keyed, unkeyedStore, inventory) = ResolveKeyStores(services, keyStore);
        if (keyed is IKeyInventoryStore keyedInventory)
            return await keyedInventory.GetAvailableKeyIdsAsync(ct).ConfigureAwait(false);

        if (keyed != null)
            return [];

        var unkeyed = unkeyedStore as IKeyInventoryStore ?? inventory;
        if (unkeyed == null)
            return [];

        return await unkeyed.GetAvailableKeyIdsAsync(ct).ConfigureAwait(false);
    }

    private static (IKeyStore? Keyed, IKeyStore? Unkeyed, IKeyInventoryStore? Inventory) ResolveKeyStores(IServiceProvider services, KeyStoreLookup keyStore)
    {
        if (keyStore.Required && keyStore.Key != null) {
            var required = services.GetRequiredKeyedService<IKeyStore>(keyStore.Key);
            return (required, null, required as IKeyInventoryStore);
        }

        if (!keyStore.Required && keyStore.Key != null) {
            var keyed = services.GetKeyedService<IKeyStore>(keyStore.Key);
            if (keyed != null)
                return (keyed, null, keyed as IKeyInventoryStore);
        }

        var unkeyed = services.GetService<IKeyStore>();
        return (null, unkeyed, services.GetService<IKeyInventoryStore>());
    }

    private readonly record struct KeyStoreLookup(string? Key, bool Required);

    private static readonly HashSet<string> ReservedStoreNames = new(StringComparer.OrdinalIgnoreCase) {
        "stores", "health", "capabilities", "files", "key-ids", "multipart", "direct-upload", "diagnostics"
    };

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
