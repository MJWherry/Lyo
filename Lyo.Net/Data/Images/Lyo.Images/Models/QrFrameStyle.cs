namespace Lyo.Images.Models;

/// <summary>Decorative layout around a square QR raster (PNG).</summary>
public enum QrFrameStyle
{
    /// <summary>No frame; output is unchanged.</summary>
    None = 0,

    /// <summary>Card with a header band, optional tab/notch, caption text, and drop shadow (similar to a “scan me” badge).</summary>
    BadgeWithHeader = 1,

    /// <summary>Single filled panel behind the QR with rounded corners and optional shadow (no header).</summary>
    SimpleRoundedPanel = 2,

    /// <summary>Stroked border around the QR; optional caption below the code inside the margin.</summary>
    BorderOnly = 3
}
