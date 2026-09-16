using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Lyo.Api.FileStorage.Models;
using Lyo.Common.Core.Net;
using Lyo.Exceptions;
using Lyo.FileMetadataStore.Models;
using Lyo.Http.Client;
using Lyo.Json;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;

namespace Lyo.FileManager;

/// <summary>FileStorage HTTP API used by <see cref="FileManagerSession" />.</summary>
public sealed class FileStorageHttpApi : IFileManagerApi
{
    private readonly FileManagerServer _server;
    private readonly ILyoHttpClient _http;
    private readonly HttpClient _client;
    private readonly bool _ownsClient;
    private readonly JsonSerializerOptions _json = LyoJsonSerializerOptions.Create();

    /// <summary>Calls <paramref name="server" />. Pass <paramref name="http" /> from a test handler.</summary>
    public FileStorageHttpApi(FileManagerServer server, HttpClient? http = null)
    {
        ArgumentHelpers.ThrowIfNull(server);
        _server = server;
        _ownsClient = http == null;
        _client = http ?? new HttpClient();
        if (_client.BaseAddress == null)
            _client.BaseAddress = BaseAddress(server.BaseUrl);
        ApplyAuth(_client, server);
        _http = new LyoHttpClient(_client);
    }

    /// <inheritdoc />
    public async Task<FileManagerHealth> CheckHealthAsync(CancellationToken ct)
    {
        var health = await _http.GetAsAsync<FileStorageHealthResponse>(Url("health"), ct: ct).ConfigureAwait(false);
        return new(health?.IsHealthy == true, health?.Message);
    }

    /// <inheritdoc />
    public async Task<FileStorageCapabilitiesResponse> GetCapabilitiesAsync(CancellationToken ct)
        => await _http.GetAsAsync<FileStorageCapabilitiesResponse>(Url("capabilities"), ct: ct).ConfigureAwait(false) ?? FileStorageCapabilitiesResponse.None;

    /// <inheritdoc />
    public async Task<FileManagerListing> ListAsync(string? pathPrefix, CancellationToken ct)
    {
        var path = Url("files/folder?maxKeys=10000");
        if (!string.IsNullOrWhiteSpace(pathPrefix))
            path += "&pathPrefix=" + Uri.EscapeDataString(pathPrefix.Trim().Trim('/'));
        var listed = await _http.GetAsAsync<FileStorageFolderListResponse>(path, ct: ct).ConfigureAwait(false);
        List<FileManagerEntry> entries = [];
        foreach (var row in listed?.Entries ?? [])
            entries.Add(new(row.Name, row.IsDirectory, row.PathPrefix, row.FileId, row.Presence, row.OriginalFileSize, row.PhysicalKey, false, row.IsCompressed, row.Compression, row.IsEncrypted, row.Encryption));
        return new(entries, listed?.Truncated == true);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileManagerQueryRow>> QueryAsync(string? nameContains, string? pathPrefix, CancellationToken ct)
    {
        var request = new ProjectionQueryReq {
            Select = ["Id", "OriginalFileName", "PathPrefix", "OriginalFileSize"],
            WhereClause = Where(nameContains, pathPrefix)
        };
        var json = await PostJsonAsync(_server.MetadataRouteOrDefault() + "/QueryProject", request, ct).ConfigureAwait(false);
        return ReadRows(json);
    }

    /// <inheritdoc />
    public async Task UploadAsync(string filePath, string? pathPrefix, FileManagerUploadOptions options, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentHelpers.ThrowIfNull(options);
        var length = new FileInfo(filePath).Length;
        var threshold = options.MultipartThresholdBytes ?? FileManagerLimits.MultipartThresholdBytes;
        if (length > threshold && (await GetCapabilitiesAsync(ct).ConfigureAwait(false)).PresignedMultipartParts)
            await UploadMultipartAsync(filePath, pathPrefix, options, length, ct).ConfigureAwait(false);
        else
            await _http.PostFileAsAsync<JsonElement>(SaveStreamUrl(filePath, pathPrefix, options), filePath, ct: ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task DownloadAsync(Guid fileId, string destinationPath, CancellationToken ct)
        => _http.DownloadToFileAsync(Url($"files/{fileId:D}/download"), destinationPath, ct: ct);

    /// <inheritdoc />
    public async Task DownloadArchiveAsync(IReadOnlyList<Guid> fileIds, string destinationPath, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(fileIds);
        var query = string.Join("&", fileIds.Select(id => "id=" + id.ToString("D")));
        await _http.DownloadToFileAsync(Url("files/archive?" + query), destinationPath, ct: ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FileStoreResult> GetMetadataAsync(Guid fileId, bool includeDeleted, CancellationToken ct)
    {
        var path = Url($"files/{fileId:D}/metadata");
        if (includeDeleted)
            path += "?includeDeleted=true";
        return await _http.GetAsAsync<FileStoreResult>(path, ct: ct).ConfigureAwait(false) ??
            throw new InvalidOperationException($"Metadata for {fileId} was not returned.");
    }

    /// <inheritdoc />
    public Task RenameAsync(Guid fileId, string originalFileName, CancellationToken ct)
        => PostAckAsync(Url("files/rename"), new RenameFileRequest(fileId, originalFileName), ct);

    /// <inheritdoc />
    public Task MoveAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
        => PostAckAsync(Url("files/move"), new MoveFileRequest(fileId, pathPrefix), ct);

    /// <inheritdoc />
    public Task CopyAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
        => PostAckAsync(Url("files/copy"), new CopyFileRequest(fileId, pathPrefix), ct);

    /// <inheritdoc />
    public Task DeleteAsync(Guid fileId, CancellationToken ct)
        => _http.DeleteAsAsync<JsonElement>(Url($"files/{fileId:D}"), ct: ct);

    /// <inheritdoc />
    public async Task<string> CreateAccessLinkAsync(Guid fileId, DateTime? expiresAtUtc, int? maxDownloads, CancellationToken ct)
    {
        var json = await PostJsonAsync(Url($"files/{fileId:D}/access-links"), new CreateDownloadAccessLinkRequest(ExpiresAtUtc: expiresAtUtc, MaxDownloads: maxDownloads), ct)
            .ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var relative = doc.RootElement.GetProperty("downloadUrl").GetString() ?? "";
        return new Uri(_client.BaseAddress!, relative).ToString();
    }

    /// <inheritdoc />
    public async Task<string> PresignedReadAsync(Guid fileId, CancellationToken ct)
    {
        var read = await _http.GetAsAsync<PresignedReadResponse>(Url($"files/{fileId:D}/presigned-read"), ct: ct).ConfigureAwait(false);
        return read?.Url ?? "";
    }

    /// <inheritdoc />
    public Task DownloadPhysicalAsync(string physicalKey, string destinationPath, CancellationToken ct)
        => _http.DownloadToFileAsync(Url("files/physical/download?physicalKey=" + Uri.EscapeDataString(physicalKey)), destinationPath, ct: ct);

    /// <inheritdoc />
    public Task RenamePhysicalAsync(string physicalKey, string newFileName, CancellationToken ct)
        => PostAckAsync(Url("files/physical/rename"), new RenamePhysicalObjectRequest(physicalKey, newFileName), ct);

    /// <inheritdoc />
    public Task MovePhysicalAsync(string physicalKey, string? destinationPrefix, CancellationToken ct)
        => PostAckAsync(Url("files/physical/move"), new MovePhysicalObjectRequest(physicalKey, destinationPrefix), ct);

    /// <inheritdoc />
    public Task CopyPhysicalAsync(string physicalKey, string? destinationPrefix, CancellationToken ct)
        => PostAckAsync(Url("files/physical/copy"), new CopyPhysicalObjectRequest(physicalKey, destinationPrefix), ct);

    /// <inheritdoc />
    public Task DeletePhysicalAsync(string physicalKey, CancellationToken ct)
        => _http.DeleteAsAsync<JsonElement>(Url("files/physical?physicalKey=" + Uri.EscapeDataString(physicalKey)), ct: ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListKeyIdsAsync(CancellationToken ct)
        => await _http.GetAsAsync<string[]>(Url("key-ids"), ct: ct).ConfigureAwait(false) ?? [];

    /// <inheritdoc />
    public Task<DekMigrationResult> RotateAsync(IReadOnlyList<Guid> fileIds, string? targetKeyId, string? targetKeyVersion, CancellationToken ct)
        => PostMigrationAsync(Url("files/rotate-deks"), new RotateDeksRequest(fileIds.ToArray(), targetKeyId, targetKeyVersion), ct);

    /// <inheritdoc />
    public Task<DekMigrationResult> MigrateAsync(
        string sourceKeyId,
        string? sourceKeyVersion,
        string? targetKeyId,
        string? targetKeyVersion,
        CancellationToken ct)
        => PostMigrationAsync(Url("files/migrate-deks"), new MigrateDeksRequest(sourceKeyId, sourceKeyVersion, targetKeyId, targetKeyVersion), ct);

    /// <inheritdoc />
    public void Dispose()
    {
        _http.Dispose();
        if (_ownsClient)
            _client.Dispose();
    }

    private async Task UploadMultipartAsync(string filePath, string? pathPrefix, FileManagerUploadOptions options, long length, CancellationToken ct)
    {
        var beginJson = await PostJsonAsync(
                Url("multipart/begin"), new BeginMultipartRequest(
                    OriginalFileName: Path.GetFileName(filePath), Compress: options.Compress, Encrypt: options.Encrypt, KeyId: options.KeyId, PathPrefix: pathPrefix,
                    ContentType: options.ContentType, Charset: options.Charset, DeclaredContentLength: length), ct)
            .ConfigureAwait(false);
        var begin = JsonSerializer.Deserialize<MultipartBeginResponse>(beginJson, _json) ??
            throw new InvalidOperationException("Multipart begin returned no session.");
        try {
            using var partResponse = await _client.GetAsync(Url($"multipart/{begin.SessionId:D}/part-url?partNumber=1"), ct).ConfigureAwait(false);
            partResponse.EnsureSuccessStatusCode();
            var partJson = await partResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var part = JsonSerializer.Deserialize<MultipartPartUrlResponse>(partJson, _json);
            var putUrl = part?.PresignedPutUrl ?? throw new InvalidOperationException("Multipart part URL was missing.");
            using var file = File.OpenRead(filePath);
            using var put = new HttpRequestMessage(HttpMethod.Put, putUrl) { Content = new StreamContent(file) };
            using var putResponse = await _client.SendAsync(put, ct).ConfigureAwait(false);
            putResponse.EnsureSuccessStatusCode();
            var etag = putResponse.Headers.ETag?.Tag?.Trim('"') ?? "local";
            await PostAckAsync(Url("multipart/complete"), new CompleteMultipartRequest(begin.SessionId, [new(1, etag)]), ct).ConfigureAwait(false);
        }
        catch {
            await PostAckAsync(Url($"multipart/{begin.SessionId:D}/abort"), new { }, ct).ConfigureAwait(false);
            throw;
        }
    }

    private string SaveStreamUrl(string filePath, string? pathPrefix, FileManagerUploadOptions options)
    {
        var query = new List<string> {
            "originalFileName=" + Uri.EscapeDataString(Path.GetFileName(filePath)),
            "compress=" + (options.Compress ? "true" : "false"),
            "encrypt=" + (options.Encrypt ? "true" : "false")
        };
        if (!string.IsNullOrWhiteSpace(options.KeyId))
            query.Add("keyId=" + Uri.EscapeDataString(options.KeyId));
        if (!string.IsNullOrWhiteSpace(pathPrefix))
            query.Add("pathPrefix=" + Uri.EscapeDataString(pathPrefix));
        if (!string.IsNullOrWhiteSpace(options.ContentType))
            query.Add("contentType=" + Uri.EscapeDataString(options.ContentType));
        if (!string.IsNullOrWhiteSpace(options.Charset))
            query.Add("charset=" + Uri.EscapeDataString(options.Charset));
        return Url("files/save-stream?" + string.Join("&", query));
    }

    private async Task<DekMigrationResult> PostMigrationAsync<T>(string url, T body, CancellationToken ct)
    {
        var json = await PostJsonAsync(url, body, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<DekMigrationResult>(json, _json) ?? new(0, 0, 0, [], []);
    }

    private async Task PostAckAsync<T>(string url, T body, CancellationToken ct) => await PostJsonAsync(url, body, ct).ConfigureAwait(false);

    private async Task<string> PostJsonAsync<T>(string url, T body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) {
            Content = new StringContent(JsonSerializer.Serialize(body, _json), Encoding.UTF8, "application/json")
        };
        using var response = await _client.SendAsync(request, ct).ConfigureAwait(false);
        var text = response.Content == null ? "" : await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return text;
    }

    private string Url(string relative)
    {
        var route = _server.Route.Trim().Trim('/');
        return string.IsNullOrEmpty(route) ? relative.TrimStart('/') : route + "/" + relative.TrimStart('/');
    }

    private static IReadOnlyList<FileManagerQueryRow> ReadRows(string json)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
        if (!doc.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            return [];
        List<FileManagerQueryRow> rows = [];
        foreach (var item in items.EnumerateArray()) {
            var id = item.TryGetProperty("id", out var idValue) && Guid.TryParse(idValue.GetString(), out var parsed) ? parsed : Guid.Empty;
            rows.Add(
                new(
                    id, item.TryGetProperty("originalFileName", out var name) ? name.GetString() : null,
                    item.TryGetProperty("pathPrefix", out var prefix) ? prefix.GetString() : null,
                    item.TryGetProperty("originalFileSize", out var size) ? size.GetInt64() : 0));
        }

        return rows;
    }

    private static WhereClause? Where(string? nameContains, string? pathPrefix)
    {
        List<WhereClause> children = [];
        if (!string.IsNullOrWhiteSpace(nameContains))
            children.Add(new ConditionClause("OriginalFileName", ComparisonOperatorEnum.Contains, nameContains.Trim()));
        if (!string.IsNullOrWhiteSpace(pathPrefix))
            children.Add(new ConditionClause("PathPrefix", ComparisonOperatorEnum.Equals, pathPrefix.Trim().Trim('/')));
        return children.Count switch {
            0 => null,
            1 => children[0],
            _ => new GroupClause(GroupOperatorEnum.And, children)
        };
    }

    private static void ApplyAuth(HttpClient client, FileManagerServer server)
    {
        client.DefaultRequestHeaders.Authorization = null;
        client.DefaultRequestHeaders.Remove(LyoHttpHeaders.ApiKey);
        if (string.IsNullOrWhiteSpace(server.Secret) || server.AuthKind == FileManagerAuthKind.None)
            return;
        if (server.AuthKind == FileManagerAuthKind.Bearer)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", server.Secret);
        else
            client.DefaultRequestHeaders.TryAddWithoutValidation(LyoHttpHeaders.ApiKey, server.Secret);
    }

    private static Uri BaseAddress(string baseUrl)
    {
        var text = baseUrl.Trim();
        if (!text.EndsWith('/'))
            text += "/";
        return new Uri(text, UriKind.Absolute);
    }
}
