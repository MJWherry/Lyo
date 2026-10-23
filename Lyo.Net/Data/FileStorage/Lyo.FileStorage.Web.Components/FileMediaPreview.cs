using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;

namespace Lyo.FileStorage.Web.Components;

/// <summary>Which viewer a catalog file opens in.</summary>
public enum FileMediaPreviewKind
{
    /// <summary>Raster or SVG image.</summary>
    Image,

    /// <summary>Audio, including WebM, which the file catalog classifies as audio.</summary>
    Audio,

    /// <summary>Video. A plain video element does not play HLS playlists.</summary>
    Video
}

/// <summary>Routes image, audio, and video files to a viewer and builds the inline download path.</summary>
public static class FileMediaPreview
{
    /// <summary>Viewer for <paramref name="type" />, or null when the file is not image, audio, or video.</summary>
    public static FileMediaPreviewKind? PreviewKind(FileTypeInfo type)
    {
        ArgumentHelpers.ThrowIfNull(type);
        return type.Category switch {
            FileTypeCategory.Images => FileMediaPreviewKind.Image,
            FileTypeCategory.Audio => FileMediaPreviewKind.Audio,
            FileTypeCategory.Video => FileMediaPreviewKind.Video,
            var _ => null
        };
    }

    /// <summary>API-relative download path that asks the host to send the file inline so a media element can play it.</summary>
    public static string InlineDownloadPath(Guid fileId) => $"files/{fileId:D}/download?inline=true";
}