using System.Diagnostics;

namespace Lyo.Images.Models;

/// <summary>Outer canvas margin with optional rounded card fill and drop shadow, applied by <see cref="IImageDecorationService.AddOuterPaddingAsync" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class PaddingOptions
{
    /// <summary>Padding in pixels between the image and the inner card edge. Starts as 24.</summary>
    public int PaddingPx { get; set; } = 24;

    /// <summary>Outer margin in pixels from canvas edge to the card. Starts as 20.</summary>
    public int MarginPx { get; set; } = 20;

    /// <summary>Card fill color (hex) behind the image. Starts as <c>#FFFFFF</c>.</summary>
    public string PanelColorHex { get; set; } = "#FFFFFF";

    /// <summary>Outer canvas color (hex) around the card. Starts as light gray so a white card stays visible on a white page.</summary>
    public string CanvasColorHex { get; set; } = "#FFF3F4F6";

    /// <summary>Drop-shadow color (hex, usually semi-transparent). Null draws no shadow.</summary>
    public string? ShadowColorHex { get; set; }

    /// <summary>Shadow offset down/right in pixels. Starts as 6.</summary>
    public int ShadowOffsetPx { get; set; } = 6;

    /// <summary>Card corner radius in pixels. Starts as 0 (square card).</summary>
    public int CornerRadiusPx { get; set; }

    public override string ToString() => $"PaddingOptions: padding={PaddingPx}px, margin={MarginPx}px, radius={CornerRadiusPx}px";
}