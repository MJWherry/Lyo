namespace Lyo.Images.Models;

/// <summary>Options for <see cref="IImageService.CompositeCenterOverlayPngAsync" /> (e.g. logo on a QR code).</summary>
public sealed class ImageCenterOverlayOptions
{
    /// <summary>Overlay width/height as a percent of the background square side (1–50). Default: 15. Values outside that range are clamped when compositing.</summary>
    public int OverlaySizePercent { get; set; } = 15;

    /// <summary>Draw a stroke around the overlay. When true, the pad behind the logo uses <see cref="BorderColorHex"/>; stroke color uses <see cref="OverlayBorderStrokeHex"/> when set and parseable, otherwise a dark default so the edge is visible on the light pad.</summary>
    public bool DrawOverlayBorder { get; set; } = true;

    /// <summary>Fill color in <c>#RRGGBB</c> or <c>#RGB</c> form for the pad cleared behind the overlay (typically QR light modules).</summary>
    public string BorderColorHex { get; set; } = "#FFFFFF";

    /// <summary>Stroke color when <see cref="DrawOverlayBorder"/> is true. Defaults to a dark slate when unset so the frame contrasts the pad.</summary>
    public string? OverlayBorderStrokeHex { get; set; }

    /// <summary>
    /// If set (positive), the background image is resized to this square (pixels) before compositing. Must be the actual target canvas size—do not pass unrelated scales (e.g. QR
    /// “pixels per module”).
    /// </summary>
    public int? BackgroundSquareSize { get; set; }
}