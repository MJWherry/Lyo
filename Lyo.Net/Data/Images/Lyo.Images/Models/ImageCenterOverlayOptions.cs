namespace Lyo.Images.Models;

/// <summary>Options for <see cref="IImageService.CompositeCenterOverlayPngAsync" /> (e.g. logo on a QR code).</summary>
public sealed class ImageCenterOverlayOptions
{
    /// <summary>Overlay width/height as a percent of the background square side (1–50). Default: 15. Values outside that range are clamped when compositing.</summary>
    public int OverlaySizePercent { get; set; } = 15;

    /// <summary>Draw a border around the overlay using <see cref="BorderColorHex" />. Default: true.</summary>
    public bool DrawOverlayBorder { get; set; } = true;

    /// <summary>Border/fill color in <c>#RRGGBB</c> or <c>#RGB</c> form (used for pad behind overlay and stroke).</summary>
    public string BorderColorHex { get; set; } = "#FFFFFF";

    /// <summary>If set (positive), the background image is resized to this square (pixels) before compositing. Must be the actual target canvas size—do not pass unrelated scales (e.g. QR “pixels per module”).</summary>
    public int? BackgroundSquareSize { get; set; }
}