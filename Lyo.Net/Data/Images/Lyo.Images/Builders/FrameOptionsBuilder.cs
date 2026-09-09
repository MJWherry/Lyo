using Lyo.Images.Models;

namespace Lyo.Images.Builders;

/// <summary>Fluent builder for <see cref="FrameOptions" />. Start with <see cref="New" />.</summary>
public sealed class FrameOptionsBuilder
{
    private int? _cornerRadiusPx;
    private string? _fillColorHex;
    private int? _paddingPx;
    private string? _strokeColorHex;
    private int? _strokeWidthPx;

    private FrameOptionsBuilder() { }

    /// <summary>Builds a new builder seeded with <see cref="FrameOptions" /> defaults.</summary>
    public static FrameOptionsBuilder New() => new();

    /// <summary>Stroke color (hex).</summary>
    public FrameOptionsBuilder WithStrokeColor(string hex)
    {
        _strokeColorHex = hex;
        return this;
    }

    /// <summary>Stroke width in pixels.</summary>
    public FrameOptionsBuilder WithStrokeWidth(int px)
    {
        _strokeWidthPx = px;
        return this;
    }

    /// <summary>Stroke corner radius in pixels.</summary>
    public FrameOptionsBuilder WithCornerRadius(int px)
    {
        _cornerRadiusPx = px;
        return this;
    }

    /// <summary>Fill color (hex) between the stroke and the image. Pass <c>null</c> to leave the gap transparent.</summary>
    public FrameOptionsBuilder WithFillColor(string? hex)
    {
        _fillColorHex = hex;
        return this;
    }

    /// <summary>Padding in pixels between the image and the stroke.</summary>
    public FrameOptionsBuilder WithPadding(int px)
    {
        _paddingPx = px;
        return this;
    }

    /// <summary>Builds a <see cref="FrameOptions" />.</summary>
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