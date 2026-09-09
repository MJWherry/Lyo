using System.Diagnostics;

namespace Lyo.Images.Models;

/// <summary>Caption band (text strip above or below the image) drawn by <see cref="IImageDecorationService.AddCaptionAsync" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class CaptionOptions
{
    /// <summary>Caption text. Required; <see cref="IImageDecorationService.AddCaptionAsync" /> rejects empty strings.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Where the caption band sits relative to the image. Starts as <see cref="CaptionPlacement.HeaderAbove" />.</summary>
    public CaptionPlacement Placement { get; set; } = CaptionPlacement.HeaderAbove;

    /// <summary>Caption band background color (hex). Starts as <c>#1e293b</c>.</summary>
    public string BackgroundColorHex { get; set; } = "#1e293b";

    /// <summary>Caption text color (hex). Starts as <c>#FFFFFF</c>.</summary>
    public string TextColorHex { get; set; } = "#FFFFFF";

    /// <summary>Caption font size in output pixels. <c>0</c> picks a size from the image side length so text stays readable on large rasters.</summary>
    public int FontSizePx { get; set; }

    /// <summary>Preferred font family. Falls back to <c>DejaVu Sans</c> / <c>Liberation Sans</c> / <c>Arial</c> / <c>Helvetica</c> when missing.</summary>
    public string FontFamily { get; set; } = "DejaVu Sans";

    /// <summary>Minimum caption band height in pixels (may grow when <see cref="AutoSizeToCaption" /> is true). Starts as 52.</summary>
    public int BandHeightPx { get; set; } = 52;

    /// <summary>If true (default), the caption band grows to fit the measured or wrapped caption.</summary>
    public bool AutoSizeToCaption { get; set; } = true;

    /// <summary>If true, draws a downward tab/notch on the inside edge of the caption band (badge headers). Starts as false.</summary>
    public bool DrawNotch { get; set; }

    /// <summary>Notch width in pixels. Starts as 36.</summary>
    public int NotchWidthPx { get; set; } = 36;

    /// <summary>Notch depth in pixels. Starts as 10.</summary>
    public int NotchDepthPx { get; set; } = 10;

    /// <summary>Rounded corners on the outer caption-band edge (header: top; footer: bottom). Starts as 0 (square).</summary>
    public int CornerRadiusPx { get; set; }

    public override string ToString() => $"CaptionOptions: text={Text}, placement={Placement}, bandHeight={BandHeightPx}px";
}