namespace Lyo.Images.Models;

/// <summary>
/// Layout and styling for <see cref="IImageDecorationService.OverlayAsync" />. Generalizes the legacy "center overlay" knobs into a position-aware shape. Defaults match the
/// QR-logo use case (centered, light pad, optional stroke).
/// </summary>
public sealed class OverlayOptions
{
    /// <summary>Placement of the overlay relative to the background. Default: <see cref="OverlayPosition.Center" />.</summary>
    public OverlayPosition Position { get; set; } = OverlayPosition.Center;

    /// <summary>Overlay width/height as a percent of the background side (1–50). Default: 15. Values outside that range are clamped at composite time.</summary>
    public int OverlaySizePercent { get; set; } = 15;

    /// <summary>Pad fill color (hex) painted behind the overlay (e.g. QR light modules). When null, no pad is drawn.</summary>
    public string? PadColorHex { get; set; }

    /// <summary>Draw a stroke around the overlay. When <c>true</c>, <see cref="BorderColorHex" /> is used (defaulting to a dark slate when unset/invalid).</summary>
    public bool DrawBorder { get; set; }

    /// <summary>Stroke color (hex) when <see cref="DrawBorder" /> is true. Defaults to a dark slate so the edge contrasts a light pad.</summary>
    public string? BorderColorHex { get; set; }

    /// <summary>Stroke width in pixels. Default: 2.</summary>
    public int BorderStrokeWidthPx { get; set; } = 2;

    /// <summary>If set (positive), the background is resized to this square pixel size before compositing. Must be the actual target canvas size.</summary>
    public int? BackgroundSquareSize { get; set; }
}
