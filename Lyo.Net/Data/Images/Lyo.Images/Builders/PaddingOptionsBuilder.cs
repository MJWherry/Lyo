using Lyo.Images.Models;

namespace Lyo.Images.Builders;

/// <summary>Fluent builder for <see cref="PaddingOptions" />. Use <see cref="New" /> to start a builder.</summary>
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

    /// <summary>Creates a new builder seeded with <see cref="PaddingOptions" /> defaults.</summary>
    public static PaddingOptionsBuilder New() => new();

    /// <summary>Sets the padding (pixels) between the image and the card edge.</summary>
    public PaddingOptionsBuilder WithPadding(int px)
    {
        _paddingPx = px;
        return this;
    }

    /// <summary>Sets the outer margin (pixels) from the canvas edge to the card.</summary>
    public PaddingOptionsBuilder WithMargin(int px)
    {
        _marginPx = px;
        return this;
    }

    /// <summary>Sets the card fill color (hex).</summary>
    public PaddingOptionsBuilder WithPanelColor(string hex)
    {
        _panelColorHex = hex;
        return this;
    }

    /// <summary>Sets the outer canvas color (hex).</summary>
    public PaddingOptionsBuilder WithCanvasColor(string hex)
    {
        _canvasColorHex = hex;
        return this;
    }

    /// <summary>Enables a drop shadow with the given color and optional offset (set color to <c>null</c> to disable).</summary>
    public PaddingOptionsBuilder WithShadow(string? color, int? offsetPx = null)
    {
        _shadowColorHex = color;
        if (offsetPx.HasValue)
            _shadowOffsetPx = offsetPx.Value;

        return this;
    }

    /// <summary>Sets the card corner radius (pixels).</summary>
    public PaddingOptionsBuilder WithCornerRadius(int px)
    {
        _cornerRadiusPx = px;
        return this;
    }

    /// <summary>Materializes a <see cref="PaddingOptions" /> instance.</summary>
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