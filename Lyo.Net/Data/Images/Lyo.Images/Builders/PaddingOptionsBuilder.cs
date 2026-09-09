using Lyo.Images.Models;

namespace Lyo.Images.Builders;

/// <summary>Fluent builder for <see cref="PaddingOptions" />. Start with <see cref="New" />.</summary>
public sealed class PaddingOptionsBuilder
{
    private string? _canvasColorHex;
    private int? _cornerRadiusPx;
    private int? _marginPx;
    private int? _paddingPx;
    private string? _panelColorHex;
    private string? _shadowColorHex;
    private int? _shadowOffsetPx;

    private PaddingOptionsBuilder() { }

    /// <summary>Builds a new builder seeded with <see cref="PaddingOptions" /> defaults.</summary>
    public static PaddingOptionsBuilder New() => new();

    /// <summary>Padding in pixels between the image and the card edge.</summary>
    public PaddingOptionsBuilder WithPadding(int px)
    {
        _paddingPx = px;
        return this;
    }

    /// <summary>Outer margin in pixels from canvas edge to the card.</summary>
    public PaddingOptionsBuilder WithMargin(int px)
    {
        _marginPx = px;
        return this;
    }

    /// <summary>Card fill color (hex).</summary>
    public PaddingOptionsBuilder WithPanelColor(string hex)
    {
        _panelColorHex = hex;
        return this;
    }

    /// <summary>Outer canvas color (hex).</summary>
    public PaddingOptionsBuilder WithCanvasColor(string hex)
    {
        _canvasColorHex = hex;
        return this;
    }

    /// <summary>Turns on a drop shadow with the given color and optional offset (pass <c>null</c> color to disable).</summary>
    public PaddingOptionsBuilder WithShadow(string? color, int? offsetPx = null)
    {
        _shadowColorHex = color;
        if (offsetPx.HasValue)
            _shadowOffsetPx = offsetPx.Value;

        return this;
    }

    /// <summary>Card corner radius in pixels.</summary>
    public PaddingOptionsBuilder WithCornerRadius(int px)
    {
        _cornerRadiusPx = px;
        return this;
    }

    /// <summary>Builds a <see cref="PaddingOptions" />.</summary>
    public PaddingOptions Build()
    {
        var o = new PaddingOptions();
        if (_paddingPx.HasValue)
            o.PaddingPx = _paddingPx.Value;

        if (_marginPx.HasValue)
            o.MarginPx = _marginPx.Value;

        if (_panelColorHex != null)
            o.PanelColorHex = _panelColorHex;

        if (_canvasColorHex != null)
            o.CanvasColorHex = _canvasColorHex;

        if (_shadowColorHex != null)
            o.ShadowColorHex = _shadowColorHex;

        if (_shadowOffsetPx.HasValue)
            o.ShadowOffsetPx = _shadowOffsetPx.Value;

        if (_cornerRadiusPx.HasValue)
            o.CornerRadiusPx = _cornerRadiusPx.Value;

        return o;
    }
}