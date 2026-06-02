using System.Diagnostics;

namespace Lyo.Images.Models;

/// <summary>Stroked outline (with optional rounded corners and inner fill) drawn around an existing image by <see cref="IImageDecorationService.AddFrameAsync" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class FrameOptions
{
    /// <summary>Stroke color (hex). Default: <c>#000000</c>.</summary>
    public string StrokeColorHex { get; set; } = "#000000";

    /// <summary>Stroke width in pixels. Default: 4.</summary>
    public int StrokeWidthPx { get; set; } = 4;

    /// <summary>Corner radius for the stroke (pixels). Default: 0 (square corners).</summary>
    public int CornerRadiusPx { get; set; }

    /// <summary>Optional fill color (hex) for the area between the stroke and the input image. When null, the gap stays transparent.</summary>
    public string? FillColorHex { get; set; }

    /// <summary>Padding (pixels) between the input image and the stroke. Default: 24.</summary>
    public int PaddingPx { get; set; } = 24;

    public override string ToString()
        => $"FrameOptions: stroke={StrokeWidthPx}px {StrokeColorHex}, padding={PaddingPx}px, radius={CornerRadiusPx}px";
}
