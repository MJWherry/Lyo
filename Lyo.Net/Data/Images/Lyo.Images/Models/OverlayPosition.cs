namespace Lyo.Images.Models;

/// <summary>Where an overlay sits on its background. Only <see cref="Center" /> is implemented in v1; the rest are reserved.</summary>
public enum OverlayPosition
{
    /// <summary>Centered on the background (default).</summary>
    Center = 0,

    /// <summary>Anchored to the background top-left.</summary>
    TopLeft = 1,

    /// <summary>Anchored to the background top-right.</summary>
    TopRight = 2,

    /// <summary>Anchored to the background bottom-left.</summary>
    BottomLeft = 3,

    /// <summary>Anchored to the background bottom-right.</summary>
    BottomRight = 4
}