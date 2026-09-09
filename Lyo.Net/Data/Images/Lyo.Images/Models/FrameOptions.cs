using System.Diagnostics;

namespace Lyo.Images.Models;

/// <summary>Stroked outline (optional rounded corners and inner fill) drawn by <see cref="IImageDecorationService.AddFrameAsync" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class FrameOptions
{
    /// <summary>Stroke color (hex). Starts as <c>#000000</c>.</summary>
    public string StrokeColorHex { get; set; } = "#000000";

    /// <summary>Stroke width in pixels. Starts as 4.</summary>
    public int StrokeWidthPx { get; set; } = 4;

    /// <summary>Stroke corner radius in pixels. Starts as 0 (square corners).</summary>
    public int CornerRadiusPx { get; set; }

    /// <summary>Optional fill color (hex) between the stroke and the image. Null leaves the gap transparent.</summary>
    public string? FillColorHex { get; set; }

    /// <summary>Padding in pixels between the image and the stroke. Starts as 24.</summary>
    public int PaddingPx { get; set; } = 24;

    public override string ToString() => $"FrameOptions: stroke={StrokeWidthPx}px {StrokeColorHex}, padding={PaddingPx}px, radius={CornerRadiusPx}px";
}