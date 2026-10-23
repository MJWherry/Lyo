using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.Media;

/// <summary>Image viewer. Uses <see cref="Src" /> when it is set; otherwise <see cref="Poster" />. Both blank renders “No image.”</summary>
public partial class LyoImage
{
    /// <summary>Image URL. Blank falls through to <see cref="Poster" />.</summary>
    [Parameter]
    public string? Src { get; set; }

    /// <summary>Fallback URL used only when <see cref="Src" /> is blank.</summary>
    [Parameter]
    public string? Poster { get; set; }

    /// <summary>Accessible name. Empty marks a decorative image.</summary>
    [Parameter]
    public string Alt { get; set; } = "";

    /// <summary>Optional DOM id. Defaults to <c>lyo-image</c>.</summary>
    [Parameter]
    public string? ElementId { get; set; }
}