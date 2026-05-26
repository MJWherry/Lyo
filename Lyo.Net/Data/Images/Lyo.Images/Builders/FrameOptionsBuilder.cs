using Lyo.Images.Models;

namespace Lyo.Images.Builders;

/// <summary>Fluent builder for <see cref="FrameOptions" />. Use <see cref="New" /> to start a builder.</summary>
public sealed class FrameOptionsBuilder
{
    private string? _strokeColorHex;
    private int? _strokeWidthPx;
    private int? _cornerRadiusPx;
    private string? _fillColorHex;
    private int? _paddingPx;

    private FrameOptionsBuilder() { }

    /// <summary>Creates a new builder seeded with <see cref="FrameOptions" /> defaults.</summary>
    public static FrameOptionsBuilder New() => new();

    /// <summary>Sets the stroke color (hex).</summary>
    public FrameOptionsBuilder WithStrokeColor(string hex)
    {
        _strokeColorHex = hex;
        return this;
    }

    /// <summary>Sets the stroke width in pixels.</summary>
    public FrameOptionsBuilder WithStrokeWidth(int px)
    {
        _strokeWidthPx = px;
        return this;
    }

    /// <summary>Sets the corner radius for the stroke (pixels).</summary>
    public FrameOptionsBuilder WithCornerRadius(int px)
    {
        _cornerRadiusPx = px;
        return this;
    }

    /// <summary>Sets the fill color (hex) between the stroke and the image. Pass <c>null</c> to keep the area transparent.</summary>
    public FrameOptionsBuilder WithFillColor(string? hex)
    {
        _fillColorHex = hex;
        return this;
    }

    /// <summary>Sets the padding (pixels) between the image and the stroke.</summary>
    public FrameOptionsBuilder WithPadding(int px)
    {
        _paddingPx = px;
        return this;
    }

    /// <summary>Materializes a <see cref="FrameOptions" /> instance.</summary>
    public FrameOptions Build()
    {
        var o = new FrameOptions();
        if (_strokeColorHex != null)
            o.StrokeColorHex = _strokeColorHex;

        if (_strokeWidthPx.HasValue)
            o.StrokeWidthPx = _strokeWidthPx.Value;

        if (_cornerRadiusPx.HasValue)
            o.CornerRadiusPx = _cornerRadiusPx.Value;

        if (_fillColorHex != null)
            o.FillColorHex = _fillColorHex;

        if (_paddingPx.HasValue)
            o.PaddingPx = _paddingPx.Value;

        return o;
    }
}
