using Lyo.Images.Models;

namespace Lyo.Images.Builders;

/// <summary>Fluent builder for <see cref="OverlayOptions" />. Start with <see cref="New" />.</summary>
public sealed class OverlayOptionsBuilder
{
    private int? _backgroundSquareSize;
    private string? _borderColorHex;
    private int? _borderStrokeWidthPx;
    private bool? _drawBorder;
    private int? _overlaySizePercent;
    private string? _padColorHex;
    private OverlayPosition? _position;

    private OverlayOptionsBuilder() { }

    /// <summary>Builds a new builder seeded with <see cref="OverlayOptions" /> defaults.</summary>
    public static OverlayOptionsBuilder New() => new();

    /// <summary>Overlay placement (starts as <see cref="OverlayPosition.Center" />). Only <see cref="OverlayPosition.Center" /> is implemented in v1.</summary>
    public OverlayOptionsBuilder WithPosition(OverlayPosition position)
    {
        _position = position;
        return this;
    }

    /// <summary>Overlay size as a percent of the background side (1–50; clamped at composite time).</summary>
    public OverlayOptionsBuilder WithOverlaySizePercent(int percent)
    {
        _overlaySizePercent = percent;
        return this;
    }

    /// <summary>Pad fill color (hex) behind the overlay. Pass <c>null</c> for no pad.</summary>
    public OverlayOptionsBuilder WithPadColor(string? hex)
    {
        _padColorHex = hex;
        return this;
    }

    /// <summary>Turns on a stroke around the overlay, using <paramref name="strokeColorHex" /> when given (starts as dark slate).</summary>
    public OverlayOptionsBuilder WithBorder(string? strokeColorHex = null, int? strokeWidthPx = null)
    {
        _drawBorder = true;
        if (strokeColorHex != null)
            _borderColorHex = strokeColorHex;

        if (strokeWidthPx.HasValue)
            _borderStrokeWidthPx = strokeWidthPx.Value;

        return this;
    }

    /// <summary>Clears a border set earlier.</summary>
    public OverlayOptionsBuilder WithoutBorder()
    {
        _drawBorder = false;
        return this;
    }

    /// <summary>Target square size for the background; the background is resized to this before compositing.</summary>
    public OverlayOptionsBuilder WithBackgroundSquareSize(int? size)
    {
        _backgroundSquareSize = size;
        return this;
    }

    /// <summary>Builds an <see cref="OverlayOptions" />.</summary>
    public OverlayOptions Build()
    {
        var o = new OverlayOptions();
        if (_position.HasValue)
            o.Position = _position.Value;

        if (_overlaySizePercent.HasValue)
            o.OverlaySizePercent = _overlaySizePercent.Value;

        if (_padColorHex != null)
            o.PadColorHex = _padColorHex;

        if (_drawBorder.HasValue)
            o.DrawBorder = _drawBorder.Value;

        if (_borderColorHex != null)
            o.BorderColorHex = _borderColorHex;

        if (_borderStrokeWidthPx.HasValue)
            o.BorderStrokeWidthPx = _borderStrokeWidthPx.Value;

        if (_backgroundSquareSize.HasValue)
            o.BackgroundSquareSize = _backgroundSquareSize.Value;

        return o;
    }
}