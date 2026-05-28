namespace Lyo.Images.Models;

/// <summary>Placement of an overlay image relative to its background. Only <see cref="Center" /> is wired in v1; other values are reserved.</summary>
public enum OverlayPosition
{
    /// <summary>Overlay is centered on the background (default).</summary>
    Center = 0,

    /// <summary>Overlay anchored to the top-left of the background.</summary>
    TopLeft = 1,

    /// <summary>Overlay anchored to the top-right of the background.</summary>
    TopRight = 2,

    /// <summary>Overlay anchored to the bottom-left of the background.</summary>
    BottomLeft = 3,

    /// <summary>Overlay anchored to the bottom-right of the background.</summary>
    BottomRight = 4
}