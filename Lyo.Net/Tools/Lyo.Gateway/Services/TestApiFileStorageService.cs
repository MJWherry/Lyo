using System.Globalization;
using Lyo.Api.Client;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Models;
using Lyo.Health;

namespace Lyo.Gateway.Services;

public sealed class TestApiFileStorageService : IFileStorageService
{
    private readonly IApiClient _apiClient;
    private readonly string _routePrefix;
    private readonly string? _streamUploadRelativePath;

    public TestApiFileStorageService(IApiClient apiClient, string routePrefix, string? streamUploadRelativePath = "upload/file")
    {
        _apiClient = apiClient;
        _routePrefix = routePrefix.Trim('/');
        _streamUploadRelativePath = string.IsNullOrWhiteSpace(streamUploadRelativePath) ? null : streamUploadRelativePath.Trim().Trim('/');
    }

    public string HealthCheckName => "filestorage";

    public event EventHandler<FileSavedResult>? FileSaved;

    public event EventHandler<FileRetrievedResult>? FileRetrieved;

    public event EventHandler<FileDeletedResult>? FileDeleted;

    public event EventHandler<FileMetadataRetrievedResult>? FileMetadataRetrieved;

    /// <summary>Not raised by this HTTP proxy; audit is recorded by TestApi storage.</summary>
#pragma warning disable CS0067 // Event required by IFileStorageService
    public event EventHandler<FileAuditEventArgs>? FileAuditOccurred;
#pragma warning restore CS0067

    public async Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
        => await _apiClient.GetAsAsync<HealthResult>(BuildUri("health"), ct: ct).ConfigureAwait(false) ??
            HealthResult.Unhealthy(TimeSpan.Zero, "Health endpoint returned no payload.");

    public async Task<byte[]> GetFileAsync(Guid fileId, CancellationToken ct = default)
    {
        await using var stream = await GetFileStreamAsync(fileId, ct).ConfigureAwait(false);
        if (stream == null)
            return [];

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
        return ms.ToArray();
    }

    public async Task<Stream?> GetFileStreamAsync(Guid fileId, CancellationToken ct = default)
    {
        var (stream, _, contentLength) = await _apiClient.GetFileStreamAsync(BuildUri($"files/{fileId:D}/download"), ct: ct).ConfigureAwait(false);
        FileRetrieved?.Invoke(this, new(fileId, contentLength ?? 0, false, false));
        return stream;
    }

    public async Task<bool> DeleteFileAsync(Guid fileId, FileDeletionMode mode = FileDeletionMode.RemoveObjectAndTombstoneMetadata, CancellationToken ct = default)
    {
        if (mode != FileDeletionMode.RemoveObjectAndTombstoneMetadata)
            throw new NotSupportedException("The Test API HTTP-backed file storage proxy only supports default deletion (object removed, metadata tombstoned).");

        var deleted = await _apiClient.DeleteAsAsync<bool>(BuildUri($"files/{fileId:D}"), ct: ct).ConfigureAwait(false);
        FileDeleted?.Invoke(this, new(fileId, deleted));
        return deleted;
    }

    public async Task<FileStoreResult> GetMetadataAsync(Guid fileId, CancellationToken ct = default)
    {
        var result = await _apiClient.GetAsAsync<FileStoreResult>(BuildUri($"files/{fileId:D}/metadata"), ct: ct).ConfigureAwait(false) ??
            throw new InvalidOperationException($"Metadata endpoint returned no payload for file '{fileId}'.");

        FileMetadataRetrieved?.Invoke(this, new(result.Id, FileStoreSnapshot.From(result)));
        return result;
    }

    public async Task<DekMigrationResult> MigrateDeksAsync(
        string sourceKeyId,
        string? sourceKeyVersion = null,
        string? targetKeyId = null,
        string? targetKeyVersion = null,
        int batchSize = 100,
        CancellationToken ct = default)
        => await _apiClient.PostAsAsync<FileStorageMigrateDeksRequest, DekMigrationResult>(
                BuildUri("files/migrate-deks"), new(sourceKeyId, sourceKeyVersion, targetKeyId, targetKeyVersion, batchSize), ct: ct)
            .ConfigureAwait(false);

    public async Task<DekMigrationResult> RotateDeksAsync(
        IReadOnlyCollection<Guid> fileIds,
        string? targetKeyId = null,
        string? targetKeyVersion = null,
        int batchSize = 100,
        CancellationToken ct = default)
        => await _apiClient.PostAsAsync<FileStorageRotateDeksRequest, DekMigrationResult>(
                BuildUri("files/rotate-deks"), new(fileIds.ToList(), targetKeyId, targetKeyVersion, batchSize), ct: ct)
            .ConfigureAwait(false);

    public async Task<FileStoreResult> SaveFileAsync(
        byte[] data,
        string? originalFileName = null,
        bool compress = false,
        bool encrypt = false,
        string? keyId = null,
        string? pathPrefix = null,
        int? chunkSize = null,
        string? contentType = null,
        string? tenantId = null,
        CancellationToken ct = default)
    {
        using var stream = new MemoryStream(data, false);
        return await SaveFromStreamAsync(stream, data.LongLength, originalFileName, compress, encrypt, keyId, pathPrefix, chunkSize, contentType, tenantId, ct: ct)
            .ConfigureAwait(false);
    }

    public async Task<FileStoreResult> SaveFileAsync(
        string filePath,
        string? originalFileName = null,
        bool compress = false,
        bool encrypt = false,
        string? keyId = null,
        string? pathPrefix = null,
        int? chunkSize = null,
        string? contentType = null,
        string? tenantId = null,
        CancellationToken ct = default)
    {
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        return await SaveFromStreamAsync(
                stream, stream.Length, originalFileName ?? Path.GetFileName(filePath), compress, encrypt, keyId, pathPrefix, chunkSize, contentType, tenantId, ct: ct)
            .ConfigureAwait(false);
    }

    public async Task<FileStoreResult> SaveFromStreamAsync(
        Stream input,
        long declaredLength,
        string? originalFileName = null,
        bool compress = false,
        bool encrypt = false,
        string? keyId = null,
        string? pathPrefix = null,
        int? chunkSize = null,
        string? contentType = null,
        string? tenantId = null,
        FileAvailability? availabilityOverride = null,
        Guid? fileId = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        _ = availabilityOverride;
        _ = fileId;
        var uri = BuildSaveStreamUri(originalFileName, compress, encrypt, keyId, pathPrefix, chunkSize, contentType, tenantId);
        var result = await _apiClient.PostFileAsAsync<FileStoreResult>(uri, input, originalFileName ?? "upload", ct: ct).ConfigureAwait(false);
        FileSaved?.Invoke(this, new(result.Id, FileStoreSnapshot.From(result), result.OriginalFileSize, result.SourceFileSize, result.IsCompressed, result.IsEncrypted));
        return result;
    }

    public async Task<string> GetPreSignedReadUrlAsync(Guid fileId, TimeSpan? expiration = null, string? pathPrefix = null, CancellationToken ct = default)
        => await GetPreSignedReadUrlAsync(fileId, expiration, pathPrefix, null, ct).ConfigureAwait(false);

    public async Task<string> GetPreSignedReadUrlAsync(
        Guid fileId,
        TimeSpan? expiration,
        string? pathPrefix,
        PreSignedReadUrlOptions? urlResponseOptions,
        CancellationToken ct = default)
    {
        var path = $"files/{fileId:D}/presigned-read";
        var parts = new List<string>();
        if (expiration.HasValue)
            parts.Add($"expiresHours={Uri.EscapeDataString(expiration.Value.TotalHours.ToString(CultureInfo.InvariantCulture))}");

        if (!string.IsNullOrEmpty(pathPrefix))
            parts.Add($"pathPrefix={Uri.EscapeDataString(pathPrefix)}");

        AppendPresignedReadQueryParams(parts, urlResponseOptions);
        var qs = parts.Count > 0 ? "?" + string.Join("&", parts) : "";
        var response = await _apiClient.GetAsAsync<PresignedReadResponse>(BuildUri($"{path}{qs}"), ct: ct).ConfigureAwait(false);
        return response?.Url ?? throw new InvalidOperationException("Pre-signed read endpoint returned no payload.");
    }

    public async Task<DirectUploadBeginResult> BeginDirectUploadAsync(DirectUploadBeginRequest request, CancellationToken ct = default)
        => await _apiClient.PostAsAsync<DirectUploadBeginRequest, DirectUploadBeginResult>(BuildUri("direct-upload/begin"), request, ct: ct).ConfigureAwait(false);

    public async Task<FileStoreResult> CompleteDirectUploadAsync(Guid fileId, DirectUploadCompleteRequest? completeRequest = null, CancellationToken ct = default)
        => await _apiClient.PostAsAsync<DirectUploadCompleteRequest?, FileStoreResult>(BuildUri($"direct-upload/{fileId:D}/complete"), completeRequest, ct: ct)
            .ConfigureAwait(false);

    public async Task<FileStoreResult> CopyFileAsync(Guid sourceFileId, CopyFileRequest? request = null, CancellationToken ct = default)
        => await _apiClient.PostAsAsync<FileStorageCopyWorkbenchRequest, FileStoreResult>(BuildUri("files/copy"), new(sourceFileId, request), ct: ct).ConfigureAwait(false);

    private static void AppendPresignedReadQueryParams(List<string> parts, PreSignedReadUrlOptions? opts)
    {
        if (opts == null)
            return;

        if (!string.IsNullOrWhiteSpace(opts.ContentDisposition))
            parts.Add($"contentDisposition={Uri.EscapeDataString(opts.ContentDisposition.Trim())}");

        if (!string.IsNullOrWhiteSpace(opts.ContentType))
            parts.Add($"contentType={Uri.EscapeDataString(opts.ContentType.Trim())}");
    }

    private string BuildUri(string relativePath) => $"{_routePrefix}/{relativePath}";

    private string BuildSaveStreamUri(
        string? originalFileName,
        bool compress,
        bool encrypt,
        string? keyId,
        string? pathPrefix,
        int? chunkSize,
        string? contentType,
        string? tenantId)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(originalFileName))
            parts.Add($"originalFileName={Uri.EscapeDataString(originalFileName)}");

        parts.Add($"compress={compress.ToString().ToLowerInvariant()}");
        parts.Add($"encrypt={encrypt.ToString().ToLowerInvariant()}");
        if (!string.IsNullOrEmpty(keyId))
            parts.Add($"keyId={Uri.EscapeDataString(keyId)}");

        if (!string.IsNullOrEmpty(pathPrefix))
            parts.Add($"pathPrefix={Uri.EscapeDataString(pathPrefix)}");

        if (chunkSize.HasValue)
            parts.Add($"chunkSize={chunkSize.Value.ToString(CultureInfo.InvariantCulture)}");

        if (!string.IsNullOrEmpty(contentType))
            parts.Add($"contentType={Uri.EscapeDataString(contentType)}");

        if (!string.IsNullOrEmpty(tenantId))
            parts.Add($"tenantId={Uri.EscapeDataString(tenantId)}");

        var qs = parts.Count > 0 ? "?" + string.Join("&", parts) : "";
        var basePath = _streamUploadRelativePath != null ? _streamUploadRelativePath : $"{_routePrefix}/files/save-stream";
        return $"{basePath}{qs}";
    }
}