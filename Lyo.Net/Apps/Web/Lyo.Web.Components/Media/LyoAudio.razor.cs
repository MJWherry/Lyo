using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.Media;

/// <summary>Audio viewer. A blank <see cref="Src" /> renders “No audio.” Controls are on unless turned off.</summary>
public partial class LyoAudio
{
    /// <summary>Media URL. Blank renders the empty state.</summary>
    [Parameter]
    public string? Src { get; set; }

    /// <summary>Shows the browser controls. Default is true.</summary>
    [Parameter]
    public bool Controls { get; set; } = true;

    /// <summary>Timed-text tracks. Blank sources are dropped and only the first default stays default.</summary>
    [Parameter]
    public IReadOnlyList<MediaCaptionTrack>? Captions { get; set; }

    /// <summary>Optional DOM id. Defaults to <c>lyo-audio</c>.</summary>
    [Parameter]
    public string? ElementId { get; set; }
}