using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.Media;

/// <summary>Video viewer. A blank <see cref="Src" /> renders “No video.” and ignores <see cref="Poster" />. Controls are on unless turned off.</summary>
public partial class LyoVideo
{
    /// <summary>Media URL. Blank renders the empty state.</summary>
    [Parameter]
    public string? Src { get; set; }

    /// <summary>Poster frame shown before playback. Applied only when <see cref="Src" /> is present.</summary>
    [Parameter]
    public string? Poster { get; set; }

    /// <summary>Shows the browser controls. Default is true.</summary>
    [Parameter]
    public bool Controls { get; set; } = true;

    /// <summary>Timed-text tracks. Blank sources are dropped and only the first default stays default.</summary>
    [Parameter]
    public IReadOnlyList<MediaCaptionTrack>? Captions { get; set; }

    /// <summary>Optional DOM id. Defaults to <c>lyo-video</c>.</summary>
    [Parameter]
    public string? ElementId { get; set; }
}