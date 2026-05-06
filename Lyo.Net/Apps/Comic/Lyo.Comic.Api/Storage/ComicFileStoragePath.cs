using Lyo.Comic;

namespace Lyo.Comic.Api.Storage;

/// <summary>Builds file-storage <see cref="Lyo.FileMetadataStore.Models.FileStoreResult.PathPrefix" /> segments for comic binaries.</summary>
public static class ComicFileStoragePath
{
    /// <summary>Returns <c>{seriesId}</c> (hyphenated GUID form).</summary>
    public static string BuildSeriesPrefix(Guid seriesId) => $"{seriesId:D}";

    /// <summary>Returns <c>{seriesId}/{volumeId}</c>.</summary>
    public static string BuildVolumePrefix(Guid seriesId, Guid volumeId) => $"{seriesId:D}/{volumeId:D}";

    /// <summary>
    /// Returns <c>{seriesId}/{volumeId-or-empty-guid}/{chapterId}</c> using standard GUID representation (hyphenated), for use as <see cref="Lyo.FileStorage.IFileStorageService" /> path
    /// prefix.
    /// </summary>
    public static string BuildPathPrefix(ComicChapter chapter)
    {
        ArgumentNullException.ThrowIfNull(chapter);
        var volumeId = chapter.VolumeId ?? Guid.Empty;
        return $"{chapter.SeriesId:D}/{volumeId:D}/{chapter.Id:D}";
    }
}
