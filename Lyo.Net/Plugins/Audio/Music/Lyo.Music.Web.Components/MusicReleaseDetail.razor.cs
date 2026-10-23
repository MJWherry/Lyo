using Microsoft.AspNetCore.Components;

namespace Lyo.Music.Web.Components;

/// <summary>Release detail: cover, credits, and the track list. Lyrics stay off this view.</summary>
public partial class MusicReleaseDetail
{
    /// <summary>Release the host already loaded.</summary>
    [Parameter]
    public MusicRelease? Release { get; set; }

    /// <summary>Display URL for <see cref="MusicRelease.CoverImageRef" />. The host resolves it.</summary>
    [Parameter]
    public string? CoverImageUrl { get; set; }

    private static string FormatDuration(int? seconds)
    {
        if (seconds is not int value || value < 0)
            return "";

        return $"{value / 60}:{value % 60:00}";
    }
}