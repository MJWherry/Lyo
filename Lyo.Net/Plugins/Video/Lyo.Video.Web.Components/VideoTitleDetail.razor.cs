using Microsoft.AspNetCore.Components;

namespace Lyo.Video.Web.Components;

/// <summary>Movie detail or a series season and episode list. Transcripts stay off this view.</summary>
public partial class VideoTitleDetail
{
    /// <summary>Title the host already loaded, including seasons when it is a series.</summary>
    [Parameter]
    public VideoTitle? Title { get; set; }

    /// <summary>Display URL for <see cref="VideoTitle.CoverImageRef" />. The host resolves it.</summary>
    [Parameter]
    public string? CoverImageUrl { get; set; }

    private static string FormatRuntime(int? seconds)
    {
        if (seconds is not int value || value < 0)
            return "";

        return $"{value / 60} min";
    }
}