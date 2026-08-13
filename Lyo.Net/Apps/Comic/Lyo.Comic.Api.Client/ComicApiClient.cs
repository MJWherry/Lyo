using System.Text;
using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Comic.Api.Models.Request;
using Lyo.Comic.Api.Models.Response;
using Lyo.FileMetadataStore.Models;

namespace Lyo.Comic.Api.Client;

/// <summary>HTTP client implementation for the Comic API service.</summary>
public sealed class ComicApiClient(HttpClient httpClient, ComicApiClientOptions options, JsonSerializerOptions serializerOptions)
    : ApiClient(httpClient: httpClient, options: options, serializerOptions: serializerOptions), IComicApiClient
{
    private const string ComicApiPrefix = "api/comic";

    public Task<byte[]> GetFileAsync(Guid id, CancellationToken ct = default)
#if NETSTANDARD2_0
        => HttpClient.GetByteArrayAsync($"files/{id}");
#else
        => HttpClient.GetByteArrayAsync($"files/{id}", ct);
#endif

    public async Task<IReadOnlyList<FileBatchEntry>> GetFilesBatchAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default)
    {
        var result = await PostAsAsync<FilesBatchReq, FileBatchEntry[]>("files/batch", new(ids), ct: ct);
        return result;
    }

    public Task<(Stream Content, string? FileName, long? ContentLength)> GetFilesArchiveAsync(
        IReadOnlyList<FilesArchiveEntryReq> entries,
        string? fileName = null,
        CancellationToken ct = default)
        => PostArchiveStreamAsync("files/archive", new FilesArchiveReq(entries, fileName), ct);

    public Task<(Stream Content, string? FileName, long? ContentLength)> GetChapterArchiveAsync(Guid chapterId, string? fileName = null, CancellationToken ct = default)
        => GetFileStreamAsync(ArchiveUri($"{ComicApiPrefix}/chapters/{chapterId:D}/archive", fileName), ct: ct);

    public Task<(Stream Content, string? FileName, long? ContentLength)> GetSeriesArchiveAsync(Guid seriesId, string? fileName = null, CancellationToken ct = default)
        => GetFileStreamAsync(ArchiveUri($"{ComicApiPrefix}/series/{seriesId:D}/archive", fileName), ct: ct);

    public async Task<FileStoreResult?> UploadFileAsync(
        Stream data,
        string fileName,
        Guid? seriesId = null,
        Guid? volumeId = null,
        Guid? chapterId = null,
        CancellationToken ct = default)
    {
        var parts = new List<string>(3);
        if (seriesId is { } sId && sId != Guid.Empty)
            parts.Add($"seriesId={sId:D}");

        if (volumeId is { } vId && vId != Guid.Empty)
            parts.Add($"volumeId={vId:D}");

        if (chapterId is { } cId && cId != Guid.Empty)
            parts.Add($"chapterId={cId:D}");

        var query = parts.Count > 0 ? "?" + string.Join("&", parts) : "";
        return await PostFileAsAsync<FileStoreResult>($"files/upload{query}", data, fileName, ct: ct);
    }

    public async Task<bool> DeleteFileAsync(Guid id, CancellationToken ct = default)
    {
        var result = await DeleteAsAsync<bool>($"files/{id}", ct: ct);
        return result;
    }

    public string GetFileUrl(Guid id) => $"{BaseOptions.BaseUrl?.TrimEnd('/')}/files/{id}";

    public async Task<IReadOnlyList<string>> GetAllSeriesTagsAsync(CancellationToken ct = default)
    {
        var list = await GetAsAsync<List<string>>($"{ComicApiPrefix}/series/tags", ct: ct);
        return list ?? [];
    }

    public async Task<IReadOnlyList<string>> GetSeriesTagsAsync(Guid seriesId, CancellationToken ct = default)
    {
        var list = await GetAsAsync<List<string>>($"{ComicApiPrefix}/series/{seriesId}/tags", ct: ct);
        return list ?? [];
    }

    public Task AddSeriesTagAsync(Guid seriesId, string tag, string tagType = "tag", string? slug = null, CancellationToken ct = default)
        => PostExpectingNoContentAsync($"{ComicApiPrefix}/series/{seriesId}/tags", new AddTagReq { Name = tag, TagType = tagType, Slug = slug }, ct: ct);

    public Task RemoveSeriesTagAsync(Guid seriesId, string tag, string tagType = "tag", string? slug = null, CancellationToken ct = default)
        => DeleteAsAsync<object?>(ComicTagDeleteUri("series", seriesId, tag, tagType, slug), ct: ct);

    public async Task<IReadOnlyList<string>> GetVolumeTagsAsync(Guid volumeId, CancellationToken ct = default)
    {
        var list = await GetAsAsync<List<string>>($"{ComicApiPrefix}/volumes/{volumeId}/tags", ct: ct);
        return list ?? [];
    }

    public Task AddVolumeTagAsync(Guid volumeId, string tag, string tagType = "tag", string? slug = null, CancellationToken ct = default)
        => PostExpectingNoContentAsync($"{ComicApiPrefix}/volumes/{volumeId}/tags", new AddTagReq { Name = tag, TagType = tagType, Slug = slug }, ct: ct);

    public Task RemoveVolumeTagAsync(Guid volumeId, string tag, string tagType = "tag", string? slug = null, CancellationToken ct = default)
        => DeleteAsAsync<object?>(ComicTagDeleteUri("volumes", volumeId, tag, tagType, slug), ct: ct);

    public async Task<IReadOnlyList<string>> GetChapterTagsAsync(Guid chapterId, CancellationToken ct = default)
    {
        var list = await GetAsAsync<List<string>>($"{ComicApiPrefix}/chapters/{chapterId}/tags", ct: ct);
        return list ?? [];
    }

    public Task AddChapterTagAsync(Guid chapterId, string tag, string tagType = "tag", string? slug = null, CancellationToken ct = default)
        => PostExpectingNoContentAsync($"{ComicApiPrefix}/chapters/{chapterId}/tags", new AddTagReq { Name = tag, TagType = tagType, Slug = slug }, ct: ct);

    public Task RemoveChapterTagAsync(Guid chapterId, string tag, string tagType = "tag", string? slug = null, CancellationToken ct = default)
        => DeleteAsAsync<object?>(ComicTagDeleteUri("chapters", chapterId, tag, tagType, slug), ct: ct);

    private static string ComicTagDeleteUri(string segmentPrefix, Guid entityId, string tag, string tagType, string? slug)
    {
        var qs = $"tagType={Uri.EscapeDataString(tagType)}&slug={Uri.EscapeDataString(slug ?? string.Empty)}";
        return $"{ComicApiPrefix}/{segmentPrefix}/{entityId}/tags/{Uri.EscapeDataString(tag)}?{qs}";
    }

    private static string ArchiveUri(string path, string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return path;

        return $"{path}?fileName={Uri.EscapeDataString(fileName)}";
    }

    private async Task<(Stream Content, string? FileName, long? ContentLength)> PostArchiveStreamAsync<TRequest>(string uri, TRequest body, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(body, SerializerOptions);
        var request = new HttpRequestMessage(HttpMethod.Post, uri) {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        try {
            await EnsureSuccessStatusCodeOrThrowApiExceptionAsync(response, ct).ConfigureAwait(false);
#if NET5_0_OR_GREATER
            var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
#else
            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
            var downloadName = response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"');
            return (new HttpResponseStream(stream, response, request), downloadName, response.Content.Headers.ContentLength);
        }
        catch {
            response.Dispose();
            request.Dispose();
            throw;
        }
    }
}