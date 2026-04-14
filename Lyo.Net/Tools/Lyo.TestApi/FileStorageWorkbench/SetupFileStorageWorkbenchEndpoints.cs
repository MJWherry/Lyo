using Lyo.Cache;
using Lyo.Common.Records;
using Lyo.FileMetadataStore.Models;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.FileStorage;
using Lyo.FileStorage.Multipart;
using Lyo.Keystore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lyo.TestApi.FileStorageWorkbench;

public static class SetupFileStorageWorkbenchEndpoints
{
    private static IFileStorageService GetFileStorage(IServiceProvider services)
        => services.GetRequiredKeyedService<IFileStorageService>(Constants.FileStorageWorkbench.ServiceKey);

    private static IMultipartUploadService GetMultipartUploadService(IServiceProvider services)
        => services.GetRequiredKeyedService<IMultipartUploadService>(Constants.FileStorageWorkbench.ServiceKey);

    private static IKeyStore GetKeyStore(IServiceProvider services) => services.GetRequiredKeyedService<IKeyStore>(Constants.FileStorageWorkbench.ServiceKey);

    private static IKeyInventoryStore? GetKeyInventoryStore(IServiceProvider services) => GetKeyStore(services) as IKeyInventoryStore;

    /// <summary>Shared by <c>Workbench/FileStorage/files/save-stream</c> and <see cref="Constants.DirectFileUpload.FilePath" />.</summary>
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
        var result = await fileStorage.SaveFromStreamAsync(
            stream, file.Length, originalFileName ?? file.FileName, compress, encrypt, keyId, pathPrefix, chunkSize, contentType, tenantId, ct: ct);

        await InvalidateFileMetadataQueryCacheAsync(cache).ConfigureAwait(false);
        return Results.Ok(result);
    }

    /// <summary>API QueryProject results for file metadata are cached; invalidate after any mutating file operation so grids see new rows.</summary>
    private static Task InvalidateFileMetadataQueryCacheAsync(ICacheService cache)
        => cache.InvalidateQueryCacheAsync<FileMetadataEntity>();

    extension(WebApplication app)
    {
        public WebApplication BuildFileStorageWorkbenchGroup()
        {
            var group = app.MapGroup(Constants.FileStorageWorkbench.Route).WithTags("FileStorageWorkbench");
            group.MapGet(
                "health", async (IServiceProvider services, CancellationToken ct) => {
                    var fileStorage = GetFileStorage(services);
                    return Results.Ok(await fileStorage.CheckHealthAsync(ct));
                });

            group.MapPost(
                "files/save", async ([FromBody] SaveFileRequest request, IServiceProvider services, ICacheService cache, CancellationToken ct) => {
                    var fileStorage = GetFileStorage(services);
                    var result = await fileStorage.SaveFileAsync(
                        request.Data, request.OriginalFileName, request.Compress, request.Encrypt, request.KeyId, request.PathPrefix, request.ChunkSize, request.ContentType,
                        request.TenantId, ct);

                    await InvalidateFileMetadataQueryCacheAsync(cache).ConfigureAwait(false);
                    return Results.Ok(result);
                });

            group.MapPost(
                "files/save-stream", async (
                    IFormFile file,
                    [FromQuery] string? originalFileName,
                    [FromQuery] bool compress,
                    [FromQuery] bool encrypt,
                    [FromQuery] string? keyId,
                    [FromQuery] string? pathPrefix,
                    [FromQuery] int? chunkSize,
                    [FromQuery] string? contentType,
                    [FromQuery] string? tenantId,
                    IServiceProvider services,
                    ICacheService cache,
                    CancellationToken ct) => {
                    var fileStorage = GetFileStorage(services);
                    return await SaveStreamFromFormAsync(
                        file, originalFileName, compress, encrypt, keyId, pathPrefix, chunkSize, contentType, tenantId, fileStorage, cache, ct);
                }).DisableAntiforgery();

            group.MapGet(
                "files/{fileId:guid}/presigned-read", async (Guid fileId, double? expiresHours, string? pathPrefix, IServiceProvider services, CancellationToken ct) => {
                    var fileStorage = GetFileStorage(services);
                    var expiration = expiresHours.HasValue ? TimeSpan.FromHours(expiresHours.Value) : (TimeSpan?)null;
                    var url = await fileStorage.GetPreSignedReadUrlAsync(fileId, expiration, pathPrefix, ct);
                    return Results.Ok(new PresignedReadResponse(url));
                });

            group.MapPost(
                "multipart/begin", async ([FromBody] BeginMultipartWorkbenchRequest request, IServiceProvider services, CancellationToken ct) => {
                    var multipart = GetMultipartUploadService(services);
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
                    return Results.Ok(result);
                });

            group.MapGet(
                "multipart/{sessionId:guid}/part-url", async (Guid sessionId, int partNumber, IServiceProvider services, CancellationToken ct) => {
                    var multipart = GetMultipartUploadService(services);
                    var descriptor = await multipart.GetPresignedPartUploadAsync(sessionId, partNumber, ct);
                    return Results.Ok(descriptor);
                });

            group.MapPost(
                "multipart/complete", async ([FromBody] CompleteMultipartWorkbenchRequest request, IServiceProvider services, ICacheService cache, CancellationToken ct) => {
                    var multipart = GetMultipartUploadService(services);
                    var parts = request.Parts.Select(p => new CompletedPart { PartNumber = p.PartNumber, ETagOrBlockId = p.ETagOrBlockId }).ToList();
                    var complete = new CompleteMultipartUploadRequest { SessionId = request.SessionId, Parts = parts };
                    var result = await multipart.CompleteAsync(complete, ct);
                    await InvalidateFileMetadataQueryCacheAsync(cache).ConfigureAwait(false);
                    return Results.Ok(result);
                });

            group.MapPost(
                "multipart/{sessionId:guid}/abort", async (Guid sessionId, IServiceProvider services, CancellationToken ct) => {
                    var multipart = GetMultipartUploadService(services);
                    await multipart.AbortAsync(sessionId, ct);
                    return Results.Ok();
                });

            group.MapGet(
                "files/{fileId:guid}/metadata", async (Guid fileId, IServiceProvider services, CancellationToken ct) => {
                    var fileStorage = GetFileStorage(services);
                    return Results.Ok(await fileStorage.GetMetadataAsync(fileId, ct));
                });

            group.MapGet(
                "files/{fileId:guid}/download", async (HttpContext http, Guid fileId, IServiceProvider services, CancellationToken ct) => {
                    var fileStorage = GetFileStorage(services);
                    var metadata = await fileStorage.GetMetadataAsync(fileId, ct);
                    var fileName = metadata.OriginalFileName ?? metadata.SourceFileName;

                    if (!metadata.IsEncrypted && !metadata.IsCompressed) {
                        // Plain files: try presigned URL redirect to avoid any server-side buffering.
                        try {
                            var url = await fileStorage.GetPreSignedReadUrlAsync(fileId, pathPrefix: metadata.PathPrefix, ct: ct);
                            return Results.Redirect(url);
                        }
                        catch (NotSupportedException) {
                            // Storage backend doesn't support presigned URLs (e.g. local disk) — fall through to streaming.
                        }
                    }

                    // Encrypted/compressed or no presigned support: stream decrypted bytes from storage.
                    var stream = await fileStorage.GetFileStreamAsync(fileId, ct);
                    if (stream == null)
                        return Results.NotFound();

                    if (metadata.OriginalFileSize > 0)
                        http.Response.ContentLength = metadata.OriginalFileSize;

                    return Results.Stream(stream, FileTypeInfo.Unknown.MimeType, fileName, enableRangeProcessing: true);
                });

            group.MapDelete(
                "files/{fileId:guid}", async (Guid fileId, IServiceProvider services, ICacheService cache, CancellationToken ct) => {
                    var fileStorage = GetFileStorage(services);
                    var deleted = await fileStorage.DeleteFileAsync(fileId, ct).ConfigureAwait(false);
                    if (deleted)
                        await InvalidateFileMetadataQueryCacheAsync(cache).ConfigureAwait(false);

                    return Results.Ok(deleted);
                });

            group.MapPost(
                "files/migrate-deks", async ([FromBody] MigrateDeksRequest request, IServiceProvider services, CancellationToken ct) => {
                    var fileStorage = GetFileStorage(services);
                    var result = await fileStorage.MigrateDeksAsync(
                        request.SourceKeyId, request.SourceKeyVersion, request.TargetKeyId, request.TargetKeyVersion, request.BatchSize, ct);

                    return Results.Ok(result);
                });

            group.MapPost(
                "files/rotate-deks", async ([FromBody] RotateDeksRequest request, IServiceProvider services, CancellationToken ct) => {
                    var fileStorage = GetFileStorage(services);
                    var result = await fileStorage.RotateDeksAsync(request.FileIds, request.TargetKeyId, request.TargetKeyVersion, request.BatchSize, ct);
                    return Results.Ok(result);
                });

            group.MapGet(
                "files/search", async (
                    string? searchText,
                    string? keyId,
                    string? keyVersion,
                    int? take,
                    IDbContextFactory<FileMetadataStoreDbContext> dbFactory,
                    CancellationToken ct) => {
                    await using var db = await dbFactory.CreateDbContextAsync(ct);
                    var query = db.FileMetadata.AsNoTracking().AsQueryable();
                    if (!string.IsNullOrWhiteSpace(searchText)) {
                        var term = searchText.Trim();
                        query = query.Where(e
                            => (e.OriginalFileName != null && EF.Functions.ILike(e.OriginalFileName, $"%{term}%")) || EF.Functions.ILike(e.SourceFileName, $"%{term}%") ||
                            (e.PathPrefix != null && EF.Functions.ILike(e.PathPrefix, $"%{term}%")));
                    }

                    if (!string.IsNullOrWhiteSpace(keyId))
                        query = query.Where(e => e.DataEncryptionKeyId == keyId);

                    if (!string.IsNullOrWhiteSpace(keyVersion))
                        query = query.Where(e => e.DataEncryptionKeyVersion == keyVersion);

                    var items = await query.OrderByDescending(e => e.Timestamp).Take(Math.Clamp(take ?? 25, 1, 250)).ToListAsync(ct);
                    return Results.Ok(items.Select(e => e.ToFileStoreResult()).ToList());
                });

            group.MapGet(
                "keys/search", async (string? searchText, int? take, IDbContextFactory<FileMetadataStoreDbContext> dbFactory, IServiceProvider services, CancellationToken ct) => {
                    await using var db = await dbFactory.CreateDbContextAsync(ct);
                    var keyStore = GetKeyStore(services);
                    var query = db.FileMetadata.AsNoTracking().Where(e => e.IsEncrypted && e.DataEncryptionKeyId != null && e.DataEncryptionKeyVersion != null);
                    if (!string.IsNullOrWhiteSpace(searchText)) {
                        var term = searchText.Trim();
                        query = query.Where(e => e.DataEncryptionKeyId != null && EF.Functions.ILike(e.DataEncryptionKeyId, $"%{term}%"));
                    }

                    var items = await query.GroupBy(e => new { KeyId = e.DataEncryptionKeyId!, Version = e.DataEncryptionKeyVersion! })
                        .Select(g => new { g.Key.KeyId, g.Key.Version, FileCount = g.Count() })
                        .OrderBy(g => g.KeyId)
                        .ThenByDescending(g => g.Version)
                        .Take(Math.Clamp(take ?? 25, 1, 250))
                        .ToListAsync(ct);

                    var currentVersions = new Dictionary<string, string?>();
                    foreach (var keyIdValue in items.Select(i => i.KeyId).Distinct())
                        currentVersions[keyIdValue] = await keyStore.GetCurrentVersionAsync(keyIdValue, ct);

                    var results = new List<KeySearchResult>(items.Count);
                    foreach (var item in items) {
                        var metadata = await keyStore.GetKeyMetadataAsync(item.KeyId, item.Version, ct);
                        results.Add(new(item.KeyId, item.Version, currentVersions[item.KeyId] == item.Version, metadata, item.FileCount));
                    }

                    return Results.Ok(results);
                });

            group.MapGet(
                "keys/available", async (IServiceProvider services, CancellationToken ct) => {
                    var inventoryStore = GetKeyInventoryStore(services);
                    return Results.Ok(inventoryStore == null ? [] : await inventoryStore.GetAvailableKeyIdsAsync(ct));
                });

            group.MapGet(
                "keys/{keyId}/versions", async (string keyId, IServiceProvider services, CancellationToken ct) => {
                    var inventoryStore = GetKeyInventoryStore(services);
                    return Results.Ok(inventoryStore == null ? [] : await inventoryStore.GetAvailableVersionsAsync(keyId, ct));
                });

            group.MapGet(
                "keys/{keyId}/raw", async (string keyId, string? version, IServiceProvider services, CancellationToken ct) => {
                    var keyStore = GetKeyStore(services);
                    var key = await keyStore.GetKeyAsync(keyId, version, ct);
                    return key == null ? Results.NotFound() : Results.Ok(key);
                });

            group.MapGet(
                "keys/{keyId}/exists", async (string keyId, string? version, IServiceProvider services, CancellationToken ct) => {
                    var keyStore = GetKeyStore(services);
                    return Results.Ok(await keyStore.HasKeyAsync(keyId, version, ct));
                });

            group.MapGet(
                "keys/{keyId}/current-version", async (string keyId, IServiceProvider services, CancellationToken ct) => {
                    var keyStore = GetKeyStore(services);
                    return Results.Ok(await keyStore.GetCurrentVersionAsync(keyId, ct));
                });

            group.MapGet(
                "keys/{keyId}/metadata/{version}", async (string keyId, string version, IServiceProvider services, CancellationToken ct) => {
                    var keyStore = GetKeyStore(services);
                    var metadata = await keyStore.GetKeyMetadataAsync(keyId, version, ct);
                    return metadata == null ? Results.NotFound() : Results.Ok(metadata);
                });

            group.MapPut(
                "keys/{keyId}/metadata/{version}", async (string keyId, string version, [FromBody] KeyMetadata metadata, IServiceProvider services, CancellationToken ct) => {
                    var keyStore = GetKeyStore(services);
                    await keyStore.SetKeyMetadataAsync(keyId, version, metadata, ct);
                    return Results.Ok(true);
                });

            group.MapGet(
                "keys/{keyId}/salt/{version}", async (string keyId, string version, IServiceProvider services, CancellationToken ct) => {
                    var keyStore = GetKeyStore(services);
                    var salt = await keyStore.GetSaltForVersionAsync(keyId, version, ct);
                    return salt == null ? Results.NotFound() : Results.Ok(salt);
                });

            group.MapPost(
                "keys/add", async ([FromBody] AddKeyRequest request, IServiceProvider services, CancellationToken ct) => {
                    var keyStore = GetKeyStore(services);
                    await keyStore.AddKeyAsync(request.KeyId, request.Version, request.Key, ct);
                    return Results.Ok(true);
                });

            group.MapPost(
                "keys/add-string", async ([FromBody] AddKeyStringRequest request, IServiceProvider services, CancellationToken ct) => {
                    var keyStore = GetKeyStore(services);
                    await keyStore.AddKeyFromStringAsync(request.KeyId, request.Version, request.KeyString, ct);
                    return Results.Ok(true);
                });

            group.MapPost(
                "keys/update", async ([FromBody] UpdateKeyRequest request, IServiceProvider services, CancellationToken ct) => {
                    var keyStore = GetKeyStore(services);
                    return Results.Ok(await keyStore.UpdateKeyAsync(request.KeyId, request.Key, ct));
                });

            group.MapPost(
                "keys/update-string", async ([FromBody] UpdateKeyStringRequest request, IServiceProvider services, CancellationToken ct) => {
                    var keyStore = GetKeyStore(services);
                    return Results.Ok(await keyStore.UpdateKeyFromStringAsync(request.KeyId, request.KeyString, ct));
                });

            group.MapPost(
                "keys/set-current", async ([FromBody] SetCurrentVersionRequest request, IServiceProvider services, CancellationToken ct) => {
                    var keyStore = GetKeyStore(services);
                    await keyStore.SetCurrentVersionAsync(request.KeyId, request.Version, ct);
                    return Results.Ok(true);
                });

            return app;
        }

        /// <summary>POST /upload/file — same contract as Workbench/FileStorage/files/save-stream for callers that hit Test API directly (e.g. Gateway without nesting under the workbench prefix).</summary>
        public WebApplication BuildDirectFileUploadEndpoint()
        {
            app.MapPost(
                Constants.DirectFileUpload.FilePath,
                async (
                    IFormFile file,
                    [FromQuery] string? originalFileName,
                    [FromQuery] bool compress,
                    [FromQuery] bool encrypt,
                    [FromQuery] string? keyId,
                    [FromQuery] string? pathPrefix,
                    [FromQuery] int? chunkSize,
                    [FromQuery] string? contentType,
                    [FromQuery] string? tenantId,
                    IServiceProvider services,
                    ICacheService cache,
                    CancellationToken ct) => {
                    var fileStorage = GetFileStorage(services);
                    return await SaveStreamFromFormAsync(
                        file, originalFileName, compress, encrypt, keyId, pathPrefix, chunkSize, contentType, tenantId, fileStorage, cache, ct);
                })
                .DisableAntiforgery()
                .WithTags("DirectFileUpload");

            return app;
        }
    }
}

public sealed record SaveFileRequest(
    byte[] Data,
    string? OriginalFileName = null,
    bool Compress = false,
    bool Encrypt = false,
    string? KeyId = null,
    string? PathPrefix = null,
    int? ChunkSize = null,
    string? ContentType = null,
    string? TenantId = null);

public sealed record PresignedReadResponse(string Url);

public sealed record BeginMultipartWorkbenchRequest(
    int PartSizeBytes = 8 * 1024 * 1024,
    bool Compress = false,
    bool Encrypt = false,
    string? KeyId = null,
    string? PathPrefix = null,
    string? ContentType = null,
    string? OriginalFileName = null,
    string? TenantId = null,
    long? DeclaredContentLength = null,
    double? SessionTtlHours = null);

public sealed record CompleteMultipartWorkbenchRequest(Guid SessionId, IReadOnlyList<CompletedPartWorkbench> Parts);

public sealed record CompletedPartWorkbench(int PartNumber, string ETagOrBlockId);

public sealed record MigrateDeksRequest(string SourceKeyId, string? SourceKeyVersion = null, string? TargetKeyId = null, string? TargetKeyVersion = null, int BatchSize = 100);

public sealed record RotateDeksRequest(IReadOnlyCollection<Guid> FileIds, string? TargetKeyId = null, string? TargetKeyVersion = null, int BatchSize = 100);

public sealed record AddKeyRequest(string KeyId, string Version, byte[] Key);

public sealed record AddKeyStringRequest(string KeyId, string Version, string KeyString);

public sealed record UpdateKeyRequest(string KeyId, byte[] Key);

public sealed record UpdateKeyStringRequest(string KeyId, string KeyString);

public sealed record SetCurrentVersionRequest(string KeyId, string Version);

public sealed record KeySearchResult(string KeyId, string Version, bool IsCurrent, KeyMetadata? Metadata, int FileCount);