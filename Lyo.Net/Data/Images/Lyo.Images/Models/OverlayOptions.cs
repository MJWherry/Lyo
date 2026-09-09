using System.Diagnostics;

namespace Lyo.Images.Models;

/// <summary>
/// Layout and style for <see cref="IImageDecorationService.OverlayAsync" />. Replaces the old "center overlay" knobs with a position-aware shape. Defaults match the QR-logo
/// case (centered, light pad, optional stroke).
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class OverlayOptions
{
    /// <summary>Where the overlay sits on the background. Starts as <see cref="OverlayPosition.Center" />.</summary>
    public OverlayPosition Position { get; set; } = OverlayPosition.Center;

    /// <summary>Overlay width/height as a percent of the background side (1–50). Starts as 15. Values outside that range are clamped at composite time.</summary>
    public int OverlaySizePercent { get; set; } = 15;

    /// <summary>Pad fill color (hex) behind the overlay (e.g. QR light modules). Null draws no pad.</summary>
    public string? PadColorHex { get; set; }

    /// <summary>If true, stroke the overlay using <see cref="BorderColorHex" /> (dark slate when unset or invalid).</summary>
    public bool DrawBorder { get; set; }

    /// <summary>Stroke color (hex) when <see cref="DrawBorder" /> is true. Starts as dark slate so the edge contrasts a light pad.</summary>
    public string? BorderColorHex { get; set; }

    /// <summary>Stroke width in pixels. Starts as 2.</summary>
    public int BorderStrokeWidthPx { get; set; } = 2;

    /// <summary>If set (positive), the background is resized to this square pixel size before compositing. Must match the target canvas size.</summary>
    public int? BackgroundSquareSize { get; set; }

    public override string ToString() => $"OverlayOptions: position={Position}, size={OverlaySizePercent}%, border={DrawBorder}";
}