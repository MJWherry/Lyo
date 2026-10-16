using Microsoft.AspNetCore.Components;

namespace Lyo.Podcast.Web.Components;

/// <summary>Show detail with seasons and episodes. Transcripts stay off this view. A show with no seasons is a valid catalog row.</summary>
public partial class PodcastShowDetail
{
    /// <summary>Show the host already loaded, including seasons and episodes.</summary>
    [Parameter]
    public PodcastShow? Show { get; set; }

    /// <summary>Display URL for <see cref="PodcastShow.CoverImageRef" />. The host resolves it.</summary>
    [Parameter]
    public string? CoverImageUrl { get; set; }

    private static string FormatRuntime(int? seconds)
    {
        if (seconds is not int value || value < 0)
            return "";
        return $"{value / 60} min";
    }
}
