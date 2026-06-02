using System.Diagnostics;

namespace Lyo.Images.Models;

/// <summary>Outer canvas margin around an input image with optional rounded card fill and drop shadow, applied by <see cref="IImageDecorationService.AddOuterPaddingAsync" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class PaddingOptions
{
    /// <summary>Padding (pixels) between the input image and the inner edge of the card fill. Default: 24.</summary>
    public int PaddingPx { get; set; } = 24;

    /// <summary>Outer margin (pixels) from the canvas edge to the card. Default: 20.</summary>
    public int MarginPx { get; set; } = 20;

    /// <summary>Card fill color (hex) painted behind the image. Default: <c>#FFFFFF</c>.</summary>
    public string PanelColorHex { get; set; } = "#FFFFFF";

    /// <summary>Outer canvas color (hex) painted around the card. Default light gray so a white card stays visible on white web backgrounds.</summary>
    public string CanvasColorHex { get; set; } = "#FFF3F4F6";

    /// <summary>Drop shadow color (hex, typically semi-transparent). When null, no shadow is drawn.</summary>
    public string? ShadowColorHex { get; set; }

    /// <summary>Shadow offset down/right in pixels. Default: 6.</summary>
    public int ShadowOffsetPx { get; set; } = 6;

    /// <summary>Card corner radius in pixels. Default: 0 (square card).</summary>
    public int CornerRadiusPx { get; set; }

    public override string ToString()
        => $"PaddingOptions: padding={PaddingPx}px, margin={MarginPx}px, radius={CornerRadiusPx}px";
}
