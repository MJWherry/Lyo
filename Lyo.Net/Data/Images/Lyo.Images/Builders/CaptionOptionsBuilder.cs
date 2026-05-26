using Lyo.Images.Models;

namespace Lyo.Images.Builders;

/// <summary>Fluent builder for <see cref="CaptionOptions" />. Use <see cref="New" /> to start a builder.</summary>
public sealed class CaptionOptionsBuilder
{
    private string? _text;
    private CaptionPlacement? _placement;
    private string? _backgroundColorHex;
    private string? _textColorHex;
    private int? _fontSizePx;
    private string? _fontFamily;
    private int? _bandHeightPx;
    private bool? _autoSizeToCaption;
    private bool? _drawNotch;
    private int? _notchWidthPx;
    private int? _notchDepthPx;
    private int? _cornerRadiusPx;

    private CaptionOptionsBuilder() { }

    /// <summary>Creates a new builder seeded with <see cref="CaptionOptions" /> defaults.</summary>
    public static CaptionOptionsBuilder New() => new();

    /// <summary>Sets the caption text (required).</summary>
    public CaptionOptionsBuilder WithText(string text)
    {
        _text = text;
        return this;
    }

    /// <summary>Sets the caption placement (header above or footer below).</summary>
    public CaptionOptionsBuilder WithPlacement(CaptionPlacement placement)
    {
        _placement = placement;
        return this;
    }

    /// <summary>Sets the caption band background color (hex).</summary>
    public CaptionOptionsBuilder WithBackgroundColor(string hex)
    {
        _backgroundColorHex = hex;
        return this;
    }

    /// <summary>Sets the caption text color (hex).</summary>
    public CaptionOptionsBuilder WithTextColor(string hex)
    {
        _textColorHex = hex;
        return this;
    }

    /// <summary>Sets the caption font (pixel size; 0 selects an automatic size). Optionally sets the font family.</summary>
    public CaptionOptionsBuilder WithFont(int sizePx, string? family = null)
    {
        _fontSizePx = sizePx;
        if (family != null)
            _fontFamily = family;

        return this;
    }

    /// <summary>Sets the minimum caption band height in pixels.</summary>
    public CaptionOptionsBuilder WithBandHeight(int px)
    {
        _bandHeightPx = px;
        return this;
    }

    /// <summary>Toggles auto-growing the band height to fit the wrapped caption (default true).</summary>
    public CaptionOptionsBuilder WithAutoSize(bool autoSize)
    {
        _autoSizeToCaption = autoSize;
        return this;
    }

    /// <summary>Enables a downward notch (badge-style header) with optional width/depth overrides.</summary>
    public CaptionOptionsBuilder WithNotch(int? widthPx = null, int? depthPx = null)
    {
        _drawNotch = true;
        if (widthPx.HasValue)
            _notchWidthPx = widthPx.Value;

        if (depthPx.HasValue)
            _notchDepthPx = depthPx.Value;

        return this;
    }

    /// <summary>Removes any notch setting previously applied.</summary>
    public CaptionOptionsBuilder WithoutNotch()
    {
        _drawNotch = false;
        return this;
    }

    /// <summary>Sets rounded corners on the outer edge of the caption band.</summary>
    public CaptionOptionsBuilder WithCornerRadius(int px)
    {
        _cornerRadiusPx = px;
        return this;
    }

    /// <summary>Materializes a <see cref="CaptionOptions" /> instance.</summary>
    public CaptionOptions Build()
    {
        var o = new CaptionOptions();
        if (_text != null)
            o.Text = _text;

        if (_placement.HasValue)
            o.Placement = _placement.Value;

        if (_backgroundColorHex != null)
            o.BackgroundColorHex = _backgroundColorHex;

        if (_textColorHex != null)
            o.TextColorHex = _textColorHex;

        if (_fontSizePx.HasValue)
            o.FontSizePx = _fontSizePx.Value;

        if (_fontFamily != null)
            o.FontFamily = _fontFamily;

        if (_bandHeightPx.HasValue)
            o.BandHeightPx = _bandHeightPx.Value;

        if (_autoSizeToCaption.HasValue)
            o.AutoSizeToCaption = _autoSizeToCaption.Value;

        if (_drawNotch.HasValue)
            o.DrawNotch = _drawNotch.Value;

        if (_notchWidthPx.HasValue)
            o.NotchWidthPx = _notchWidthPx.Value;

        if (_notchDepthPx.HasValue)
            o.NotchDepthPx = _notchDepthPx.Value;

        if (_cornerRadiusPx.HasValue)
            o.CornerRadiusPx = _cornerRadiusPx.Value;

        return o;
    }
}
