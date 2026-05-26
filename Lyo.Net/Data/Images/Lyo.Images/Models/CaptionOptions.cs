namespace Lyo.Images.Models;

/// <summary>Caption band (text strip above or below the input image) drawn by <see cref="IImageDecorationService.AddCaptionAsync" />.</summary>
public sealed class CaptionOptions
{
    /// <summary>Caption text. Required; <see cref="IImageDecorationService.AddCaptionAsync" /> rejects empty values.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Where the caption band sits relative to the input image. Default: <see cref="CaptionPlacement.HeaderAbove" />.</summary>
    public CaptionPlacement Placement { get; set; } = CaptionPlacement.HeaderAbove;

    /// <summary>Caption band background color (hex). Default: <c>#1e293b</c>.</summary>
    public string BackgroundColorHex { get; set; } = "#1e293b";

    /// <summary>Caption text color (hex). Default: <c>#FFFFFF</c>.</summary>
    public string TextColorHex { get; set; } = "#FFFFFF";

    /// <summary>Caption font size in output pixels. <c>0</c> selects an automatic size from the input image side length so text remains readable on huge rasters.</summary>
    public int FontSizePx { get; set; }

    /// <summary>Preferred font family name. Falls back to <c>DejaVu Sans</c> / <c>Liberation Sans</c> / <c>Arial</c> / <c>Helvetica</c> when missing.</summary>
    public string FontFamily { get; set; } = "DejaVu Sans";

    /// <summary>Minimum caption band height in pixels (caption may grow when <see cref="AutoSizeToCaption" /> is true). Default: 52.</summary>
    public int BandHeightPx { get; set; } = 52;

    /// <summary>When true (default), the caption band grows to fit the measured/wrapped caption.</summary>
    public bool AutoSizeToCaption { get; set; } = true;

    /// <summary>Draws a downward tab/notch at the inside edge of the caption band (typical for badge headers). Default: false.</summary>
    public bool DrawNotch { get; set; }

    /// <summary>Notch width in pixels. Default: 36.</summary>
    public int NotchWidthPx { get; set; } = 36;

    /// <summary>Notch depth in pixels. Default: 10.</summary>
    public int NotchDepthPx { get; set; } = 10;

    /// <summary>Rounded corners on the outer edge of the caption band (header: top corners; footer: bottom corners). Default: 0 (square).</summary>
    public int CornerRadiusPx { get; set; }
}
