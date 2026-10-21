using Microsoft.AspNetCore.Components;

namespace Lyo.Audiobook.Web.Components;

/// <summary>Audiobook detail: overview, credits, and chapters. Transcripts stay off this view.</summary>
public partial class AudiobookDetail
{
    /// <summary>Work the host already loaded, including chapters.</summary>
    [Parameter]
    public Audiobook? Work { get; set; }

    /// <summary>Display URL for <see cref="Audiobook.CoverImageRef" />. The host resolves it.</summary>
    [Parameter]
    public string? CoverImageUrl { get; set; }

    private static string FormatRuntime(int? seconds)
    {
        if (seconds is not int value || value < 0)
            return "";

        return $"{value / 60} min";
    }
}