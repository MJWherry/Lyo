using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Lyo.Comic;
using Lyo.Common.Pathing;
using Lyo.Exceptions;
using Lyo.FileStorage;

namespace Lyo.Comic.Api.Storage;

/// <summary>One page or cover to put in a chapter/series zip. <see cref="FileId" /> is file storage; <see cref="RemoteUrl" /> is a seeded http(s) image ref.</summary>
public readonly record struct ComicArchiveSource(string ZipPath, Guid? FileId, Uri? RemoteUrl);

/// <summary>Builds zip file names and nested entry paths for chapter and series downloads.</summary>
public static class ComicArchiveLayout
{
    /// <summary>
    /// Parses a file-storage GUID from a stored image ref. Accepts a raw GUID, a relative file path whose last segment is a GUID (<c>/files/{id}</c>, <c>/api/files/{id}</c>), or
    /// an http(s) URL whose last path segment is a GUID.
    /// </summary>
    public static bool TryParseFileId(string? imageRef, out Guid id)
    {
        id = default;
        if (string.IsNullOrWhiteSpace(imageRef))
            return false;

        var trimmed = imageRef.Trim();
        string path;
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            path = uri.AbsolutePath;
        else
            path = trimmed;

        var leaf = PathHelpers.GetFileName(PathStyle.Posix, PathHelpers.NormalizeSeparators(path, PathStyle.Posix));
        var query = leaf.IndexOf('?');
        if (query >= 0)
            leaf = leaf[..query];

        return Guid.TryParse(leaf, out id);
    }

    /// <summary>True when <paramref name="imageRef" /> is an absolute http(s) URL that is not a file-storage id.</summary>
    public static bool TryParseRemoteUrl(string? imageRef, [NotNullWhen(true)] out Uri? url)
    {
        url = null;
        if (TryParseFileId(imageRef, out _))
            return false;
        if (!Uri.TryCreate(imageRef?.Trim(), UriKind.Absolute, out var uri))
            return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        url = uri;
        return true;
    }

    /// <summary>Default chapter zip name: <c>{series} - Ch. {n}[ - {title}].zip</c>.</summary>
    public static string ChapterZipFileName(string seriesTitle, ComicChapter chapter)
    {
        ArgumentHelpers.ThrowIfNull(chapter);
        var series = FileStorageArchivePath.SanitizePathSegment(seriesTitle);
        var number = FormatChapterNumber(chapter.ChapterNumber);
        if (string.IsNullOrWhiteSpace(chapter.Title))
            return FileStorageArchivePath.SanitizeZipFileName($"{series} - Ch. {number}");

        var title = FileStorageArchivePath.SanitizePathSegment(chapter.Title);
        return FileStorageArchivePath.SanitizeZipFileName($"{series} - Ch. {number} - {title}");
    }

    /// <summary>Default series zip name: <c>{seriesTitle}.zip</c>.</summary>
    public static string SeriesZipFileName(string seriesTitle)
        => FileStorageArchivePath.SanitizeZipFileName(FileStorageArchivePath.SanitizePathSegment(seriesTitle));

    /// <summary>Flat chapter entries: optional <c>cover</c> plus zero-padded page stems.</summary>
    public static IReadOnlyList<ComicArchiveSource> ChapterEntries(ComicChapter chapter, IReadOnlyList<ComicPage> pages)
    {
        ArgumentHelpers.ThrowIfNull(chapter);
        ArgumentHelpers.ThrowIfNull(pages);
        var list = new List<ComicArchiveSource>();
        TryAdd(list, chapter.CoverImageRef, "cover");
        foreach (var page in pages.OrderBy(p => p.PageNumber))
            TryAdd(list, page.ImageRef, page.PageNumber.ToString("000", CultureInfo.InvariantCulture));

        return list;
    }

    /// <summary>
    /// Nested series entries under a folder named after the series: volume folders, chapter folders, then page stems. Chapters without a volume sit next to volume folders.
    /// </summary>
    public static IReadOnlyList<ComicArchiveSource> SeriesEntries(
        ComicSeries series,
        IReadOnlyList<ComicVolume> volumes,
        IReadOnlyList<ComicChapter> chapters,
        IReadOnlyDictionary<Guid, IReadOnlyList<ComicPage>> pagesByChapter)
    {
        ArgumentHelpers.ThrowIfNull(series);
        ArgumentHelpers.ThrowIfNull(volumes);
        ArgumentHelpers.ThrowIfNull(chapters);
        ArgumentHelpers.ThrowIfNull(pagesByChapter);
        var root = FileStorageArchivePath.SanitizePathSegment(series.Title);
        var list = new List<ComicArchiveSource>();
        TryAdd(list, series.CoverImageRef, $"{root}/cover");

        var volumeById = volumes.ToDictionary(v => v.Id);
        foreach (var volume in volumes.OrderBy(v => v.VolumeNumber ?? decimal.MaxValue).ThenBy(v => v.Title, StringComparer.OrdinalIgnoreCase)) {
            var volumeFolder = $"{root}/{VolumeFolderName(volume)}";
            TryAdd(list, volume.CoverImageRef, $"{volumeFolder}/cover");
        }

        foreach (var chapter in chapters.OrderBy(c => c.ChapterNumber).ThenBy(c => c.Language, StringComparer.OrdinalIgnoreCase)) {
            string chapterDir;
            if (chapter.VolumeId is { } volumeId && volumeById.TryGetValue(volumeId, out var volume))
                chapterDir = $"{root}/{VolumeFolderName(volume)}/{ChapterFolderName(chapter)}";
            else
                chapterDir = $"{root}/{ChapterFolderName(chapter)}";

            TryAdd(list, chapter.CoverImageRef, $"{chapterDir}/cover");
            if (!pagesByChapter.TryGetValue(chapter.Id, out var pages))
                continue;

            foreach (var page in pages.OrderBy(p => p.PageNumber))
                TryAdd(list, page.ImageRef, $"{chapterDir}/{page.PageNumber.ToString("000", CultureInfo.InvariantCulture)}");
        }

        return list;
    }

    /// <summary>Volume folder: <c>Vol. {nn}</c> plus optional title.</summary>
    public static string VolumeFolderName(ComicVolume volume)
    {
        ArgumentHelpers.ThrowIfNull(volume);
        string stem;
        if (volume.VolumeNumber is { } n)
            stem = $"Vol. {FormatVolumeNumber(n)}";
        else
            stem = "Vol.";

        if (string.IsNullOrWhiteSpace(volume.Title))
            return FileStorageArchivePath.SanitizePathSegment(stem);

        return FileStorageArchivePath.SanitizePathSegment($"{stem} {volume.Title}");
    }

    /// <summary>Chapter folder: <c>Ch. {nnn}</c> plus optional title.</summary>
    public static string ChapterFolderName(ComicChapter chapter)
    {
        ArgumentHelpers.ThrowIfNull(chapter);
        var stem = $"Ch. {FormatChapterNumber(chapter.ChapterNumber)}";
        if (string.IsNullOrWhiteSpace(chapter.Title))
            return FileStorageArchivePath.SanitizePathSegment(stem);

        return FileStorageArchivePath.SanitizePathSegment($"{stem} {chapter.Title}");
    }

    /// <summary>Pads whole chapter numbers to three digits; keeps fractional values in invariant form.</summary>
    public static string FormatChapterNumber(decimal chapterNumber) => FormatPadded(chapterNumber, 3);

    /// <summary>Pads whole volume numbers to two digits; keeps fractional values in invariant form.</summary>
    public static string FormatVolumeNumber(decimal volumeNumber) => FormatPadded(volumeNumber, 2);

    private static string FormatPadded(decimal value, int integerWidth)
    {
        if (value == decimal.Truncate(value) && value >= 0 && value <= int.MaxValue)
            return ((int)value).ToString(new string('0', integerWidth), CultureInfo.InvariantCulture);

        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static void TryAdd(List<ComicArchiveSource> list, string? imageRef, string zipPathWithoutExtension)
    {
        if (TryParseFileId(imageRef, out var id)) {
            list.Add(new(zipPathWithoutExtension, id, null));
            return;
        }

        if (TryParseRemoteUrl(imageRef, out var url))
            list.Add(new(zipPathWithoutExtension, null, url));
    }
}
