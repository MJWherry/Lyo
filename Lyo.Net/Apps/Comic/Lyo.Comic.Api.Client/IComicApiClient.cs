using Lyo.Api.Client;
using Lyo.Comic.Api.Models.Response;
using Lyo.FileMetadataStore.Models;

namespace Lyo.Comic.Api.Client;

/// <summary>Typed HTTP client for the Comic API service.</summary>
public interface IComicApiClient : IApiClient
{
    /// <summary>Downloads the raw bytes of a file from the Comic API's file storage.</summary>
    Task<byte[]> GetFileAsync(Guid id, CancellationToken ct = default);

    /// <summary>Downloads multiple files by ID in a single request. Missing IDs are silently omitted from the result.</summary>
    Task<IReadOnlyList<FileBatchEntry>> GetFilesBatchAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default);

    /// <summary>
    /// Uploads a file to the Comic API's file storage and returns the stored file metadata. Optional <paramref name="seriesId" />, <paramref name="volumeId" />, and
    /// <paramref name="chapterId" /> (non-empty GUIDs) select the storage path prefix; the most specific id wins (chapter, then volume, then series). Omit all three to use the
    /// default sharded layout.
    /// </summary>
    Task<FileStoreResult?> UploadFileAsync(
        Stream data,
        string fileName,
        Guid? seriesId = null,
        Guid? volumeId = null,
        Guid? chapterId = null,
        CancellationToken ct = default);

    /// <summary>Deletes a file from the Comic API's file storage. Returns true if deleted, false if not found.</summary>
    Task<bool> DeleteFileAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns the absolute URL for a file served by the Comic API (for use as an img src).</summary>
    string GetFileUrl(Guid id);

    /// <summary>Returns distinct tag values applied to any series.</summary>
    Task<IReadOnlyList<string>> GetAllSeriesTagsAsync(CancellationToken ct = default);

    /// <summary>Returns tags applied to the series.</summary>
    Task<IReadOnlyList<string>> GetSeriesTagsAsync(Guid seriesId, CancellationToken ct = default);

    /// <summary>Adds a tag to the series.</summary>
    Task AddSeriesTagAsync(Guid seriesId, string tag, string tagType = "tag", string? slug = null, CancellationToken ct = default);

    /// <summary>Removes a tag from the series.</summary>
    Task RemoveSeriesTagAsync(Guid seriesId, string tag, string tagType = "tag", string? slug = null, CancellationToken ct = default);

    /// <summary>Returns tags applied to the volume.</summary>
    Task<IReadOnlyList<string>> GetVolumeTagsAsync(Guid volumeId, CancellationToken ct = default);

    /// <summary>Adds a tag to the volume.</summary>
    Task AddVolumeTagAsync(Guid volumeId, string tag, string tagType = "tag", string? slug = null, CancellationToken ct = default);

    /// <summary>Removes a tag from the volume.</summary>
    Task RemoveVolumeTagAsync(Guid volumeId, string tag, string tagType = "tag", string? slug = null, CancellationToken ct = default);

    /// <summary>Returns tags applied to the chapter.</summary>
    Task<IReadOnlyList<string>> GetChapterTagsAsync(Guid chapterId, CancellationToken ct = default);

    /// <summary>Adds a tag to the chapter.</summary>
    Task AddChapterTagAsync(Guid chapterId, string tag, string tagType = "tag", string? slug = null, CancellationToken ct = default);

    /// <summary>Removes a tag from the chapter.</summary>
    Task RemoveChapterTagAsync(Guid chapterId, string tag, string tagType = "tag", string? slug = null, CancellationToken ct = default);
}