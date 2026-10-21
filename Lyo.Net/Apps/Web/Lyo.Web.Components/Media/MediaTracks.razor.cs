using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.Media;

/// <summary>Renders resolved caption tracks as HTML <c>&lt;track&gt;</c> children of <c>audio</c> or <c>video</c>.</summary>
public partial class MediaTracks
{
    /// <summary>Tracks to resolve. Null is no tracks.</summary>
    [Parameter]
    public IReadOnlyList<MediaCaptionTrack>? Captions { get; set; }
}