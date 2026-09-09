using Lyo.Images.Models;

namespace Lyo.Images.Builders;

/// <summary>Fluent builder for <see cref="CaptionOptions" />. Start with <see cref="New" />.</summary>
public sealed class CaptionOptionsBuilder
{
    private bool? _autoSizeToCaption;
    private string? _backgroundColorHex;
    private int? _bandHeightPx;
    private int? _cornerRadiusPx;
    private bool? _drawNotch;
    private string? _fontFamily;
    private int? _fontSizePx;
    private int? _notchDepthPx;
    private int? _notchWidthPx;
    private CaptionPlacement? _placement;
    private string? _text;
    private string? _textColorHex;

    private CaptionOptionsBuilder() { }

    /// <summary>Builds a new builder seeded with <see cref="CaptionOptions" /> defaults.</summary>
    public static CaptionOptionsBuilder New() => new();

    /// <summary>Caption text (required).</summary>
    public CaptionOptionsBuilder WithText(string text)
    {
        _text = text;
        return this;
    }

    /// <summary>Caption placement (header above or footer below).</summary>
    public CaptionOptionsBuilder WithPlacement(CaptionPlacement placement)
    {
        _placement = placement;
        return this;
    }

    /// <summary>Caption band background color (hex).</summary>
    public CaptionOptionsBuilder WithBackgroundColor(string hex)
    {
        _backgroundColorHex = hex;
        return this;
    }

    /// <summary>Caption text color (hex).</summary>
    public CaptionOptionsBuilder WithTextColor(string hex)
    {
        _textColorHex = hex;
        return this;
    }

    /// <summary>Caption font (pixel size; 0 picks an automatic size). Optional font family.</summary>
    public CaptionOptionsBuilder WithFont(int sizePx, string? family = null)
    {
        _fontSizePx = sizePx;
        if (family != null)
            _fontFamily = family;

        return this;
    }

    /// <summary>Minimum caption band height in pixels.</summary>
    public CaptionOptionsBuilder WithBandHeight(int px)
    {
        _bandHeightPx = px;
        return this;
    }

    /// <summary>If true, grow the band height to fit the wrapped caption (starts as true).</summary>
    public CaptionOptionsBuilder WithAutoSize(bool autoSize)
    {
        _autoSizeToCaption = autoSize;
        return this;
    }

    /// <summary>Turns on a downward notch (badge header) with optional width/depth overrides.</summary>
    public CaptionOptionsBuilder WithNotch(int? widthPx = null, int? depthPx = null)
    {
        _drawNotch = true;
        if (widthPx.HasValue)
            _notchWidthPx = widthPx.Value;

        if (depthPx.HasValue)
            _notchDepthPx = depthPx.Value;

        return this;
    }

    /// <summary>Clears a notch set earlier.</summary>
    public CaptionOptionsBuilder WithoutNotch()
    {
        _drawNotch = false;
        return this;
    }

    /// <summary>Rounded corners on the outer caption-band edge.</summary>
    public CaptionOptionsBuilder WithCornerRadius(int px)
    {
        _cornerRadiusPx = px;
        return this;
    }

    /// <summary>Builds a <see cref="CaptionOptions" />.</summary>
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