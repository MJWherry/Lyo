using System.Diagnostics;
using Lyo.Barcode.Models;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Barcode;

/// <summary>Chains settings that become a <see cref="BarcodeRequest" /> plus <see cref="BarcodeOptions" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class BarcodeBuilder
{
    private int? _barHeightPixels;
    private string? _borderColorHex;
    private int? _borderWidthPixels;
    private string? _darkColor;
    private string? _data;
    private BarcodeFormat? _format;
    private bool _hasBorderColorHex;
    private bool _hasHumanReadableColorHex;
    private bool _hasHumanReadableText;
    private string? _humanReadableColorHex;
    private int? _humanReadableFontSizePixels;
    private int? _humanReadableMarginBottomPixels;
    private int? _humanReadableMarginTopPixels;
    private string? _humanReadableText;
    private string? _lightColor;
    private int? _moduleWidthPixels;
    private int? _quietZoneModules;
    private bool? _showBorder;
    private bool? _showHumanReadableTextBelow;
    private BarcodeSymbology _symbology = BarcodeSymbology.Code128;

    /// <summary>Stores the text that will be encoded.</summary>
    /// <returns>The same builder so calls can be chained.</returns>
    public BarcodeBuilder WithData(string data)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(data);
        _data = data;
        return this;
    }

    /// <summary>Picks which barcode family to draw.</summary>
    public BarcodeBuilder WithSymbology(BarcodeSymbology symbology)
    {
        _symbology = symbology;
        return this;
    }

    /// <summary>Picks BMP or SVG as the output image type.</summary>
    public BarcodeBuilder WithFormat(BarcodeFormat format)
    {
        _format = format;
        return this;
    }

    /// <summary>Sets how many pixels wide each module (bar pitch) is.</summary>
    public BarcodeBuilder WithModuleWidthPixels(int pixels)
    {
        ArgumentHelpers.ThrowIfNotInRange(pixels, 1, int.MaxValue);
        _moduleWidthPixels = pixels;
        return this;
    }

    /// <summary>Sets how tall the bars are in pixels. Quiet zone is not included.</summary>
    public BarcodeBuilder WithBarHeightPixels(int pixels)
    {
        ArgumentHelpers.ThrowIfNotInRange(pixels, 1, int.MaxValue);
        _barHeightPixels = pixels;
        return this;
    }

    /// <summary>Sets the quiet-zone width on each side, measured in modules.</summary>
    public BarcodeBuilder WithQuietZoneModules(int modules)
    {
        ArgumentHelpers.ThrowIfNotInRange(modules, 0, int.MaxValue);
        _quietZoneModules = modules;
        return this;
    }

    /// <summary>Sets bar (foreground) color using <c>#RRGGBB</c> or <c>#RGB</c>.</summary>
    public BarcodeBuilder WithDarkColor(string color)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(color);
        if (!IsValidHexColor(color))
            throw new InvalidFormatException("Color must be in hex format (e.g., '#000000' or '#FF0000').", nameof(color), color, "Hex color format");

        _darkColor = color;
        return this;
    }

    /// <summary>Sets the background color using <c>#RRGGBB</c> or <c>#RGB</c>.</summary>
    public BarcodeBuilder WithLightColor(string color)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(color);
        if (!IsValidHexColor(color))
            throw new InvalidFormatException("Color must be in hex format (e.g., '#FFFFFF' or '#FF0000').", nameof(color), color, "Hex color format");

        _lightColor = color;
        return this;
    }

    /// <summary>If true, the human-readable interpretation is drawn under the bars.</summary>
    public BarcodeBuilder WithShowHumanReadableTextBelow(bool show = true)
    {
        _showHumanReadableTextBelow = show;
        return this;
    }

    /// <summary>Text shown under the bars. Whitespace-only drops the override and the encoded payload is used instead.</summary>
    public BarcodeBuilder WithHumanReadableText(string? text)
    {
        _hasHumanReadableText = true;
        _humanReadableText = text;
        return this;
    }

    /// <summary>Sets the caption font size in pixels.</summary>
    public BarcodeBuilder WithHumanReadableFontSizePixels(int pixels)
    {
        ArgumentHelpers.ThrowIfNotInRange(pixels, 1, int.MaxValue);
        _humanReadableFontSizePixels = pixels;
        return this;
    }

    /// <summary>Sets the gap between the bar bottoms and the caption.</summary>
    public BarcodeBuilder WithHumanReadableMarginTopPixels(int pixels)
    {
        ArgumentHelpers.ThrowIfNotInRange(pixels, 0, int.MaxValue);
        _humanReadableMarginTopPixels = pixels;
        return this;
    }

    /// <summary>Sets extra space under the caption.</summary>
    public BarcodeBuilder WithHumanReadableMarginBottomPixels(int pixels)
    {
        ArgumentHelpers.ThrowIfNotInRange(pixels, 0, int.MaxValue);
        _humanReadableMarginBottomPixels = pixels;
        return this;
    }

    /// <summary>Sets caption ink color. Null or whitespace drops the override and the dark color is used.</summary>
    public BarcodeBuilder WithHumanReadableColorHex(string? color)
    {
        _hasHumanReadableColorHex = true;
        if (!string.IsNullOrWhiteSpace(color) && !IsValidHexColor(color))
            throw new InvalidFormatException("Color must be in hex format (e.g., '#000000' or '#FF0000').", nameof(color), color, "Hex color format");

        _humanReadableColorHex = color;
        return this;
    }

    /// <summary>If true, a frame is drawn around the BMP or SVG image.</summary>
    public BarcodeBuilder WithShowBorder(bool show = true)
    {
        _showBorder = show;
        return this;
    }

    /// <summary>How thick each side of the frame is, in pixels, when the border is on.</summary>
    public BarcodeBuilder WithBorderWidthPixels(int pixels)
    {
        ArgumentHelpers.ThrowIfNotInRange(pixels, 1, int.MaxValue);
        _borderWidthPixels = pixels;
        return this;
    }

    /// <summary>Frame color as <c>#RRGGBB</c> or <c>#RGB</c>.</summary>
    public BarcodeBuilder WithBorderColorHex(string color)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(color);
        if (!IsValidHexColor(color))
            throw new InvalidFormatException("Color must be in hex format (e.g., '#000000' or '#FF0000').", nameof(color), color, "Hex color format");

        _hasBorderColorHex = true;
        _borderColorHex = color;
        return this;
    }

    /// <summary>Restores every field to its default value.</summary>
    public BarcodeBuilder Clear()
    {
        _data = null;
        _symbology = BarcodeSymbology.Code128;
        _format = null;
        _moduleWidthPixels = null;
        _barHeightPixels = null;
        _quietZoneModules = null;
        _darkColor = null;
        _lightColor = null;
        _showHumanReadableTextBelow = null;
        _hasHumanReadableText = false;
        _humanReadableText = null;
        _humanReadableFontSizePixels = null;
        _humanReadableMarginTopPixels = null;
        _humanReadableMarginBottomPixels = null;
        _hasHumanReadableColorHex = false;
        _humanReadableColorHex = null;
        _showBorder = null;
        _borderWidthPixels = null;
        _hasBorderColorHex = false;
        _borderColorHex = null;
        return this;
    }

    /// <summary>Produces the tuple that <see cref="IBarcodeService.GenerateAsync(string, BarcodeSymbology, BarcodeOptions?, CancellationToken)" /> consumes.</summary>
    /// <exception cref="ArgumentException">Raised when no payload has been stored.</exception>
    public (string Data, BarcodeSymbology Symbology, BarcodeOptions Options) Build()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_data);
        var options = new BarcodeOptions();
        if (_format.HasValue)
            options.Format = _format.Value;

        if (_moduleWidthPixels.HasValue)
            options.ModuleWidthPixels = _moduleWidthPixels.Value;

        if (_barHeightPixels.HasValue)
            options.BarHeightPixels = _barHeightPixels.Value;

        if (_quietZoneModules.HasValue)
            options.QuietZoneModules = _quietZoneModules.Value;

        if (!string.IsNullOrWhiteSpace(_darkColor))
            options.DarkColor = _darkColor;

        if (!string.IsNullOrWhiteSpace(_lightColor))
            options.LightColor = _lightColor;

        if (_showHumanReadableTextBelow.HasValue)
            options.ShowHumanReadableTextBelow = _showHumanReadableTextBelow.Value;

        if (_hasHumanReadableText)
            options.HumanReadableText = string.IsNullOrWhiteSpace(_humanReadableText) ? null : _humanReadableText;

        if (_humanReadableFontSizePixels.HasValue)
            options.HumanReadableFontSizePixels = _humanReadableFontSizePixels.Value;

        if (_humanReadableMarginTopPixels.HasValue)
            options.HumanReadableMarginTopPixels = _humanReadableMarginTopPixels.Value;

        if (_humanReadableMarginBottomPixels.HasValue)
            options.HumanReadableMarginBottomPixels = _humanReadableMarginBottomPixels.Value;

        if (_hasHumanReadableColorHex)
            options.HumanReadableColorHex = string.IsNullOrWhiteSpace(_humanReadableColorHex) ? null : _humanReadableColorHex;

        if (_showBorder.HasValue)
            options.ShowBorder = _showBorder.Value;

        if (_borderWidthPixels.HasValue)
            options.BorderWidthPixels = _borderWidthPixels.Value;

        if (_hasBorderColorHex && !string.IsNullOrWhiteSpace(_borderColorHex))
            options.BorderColorHex = _borderColorHex;

        return (_data, _symbology, options);
    }

    /// <summary>Allocates a fresh builder.</summary>
    public static BarcodeBuilder New() => new();

    private static bool IsValidHexColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return false;

        var hex = color.StartsWith("#") ? color.Substring(1) : color;
        if (hex.Length != 3 && hex.Length != 6)
            return false;

        foreach (var c in hex) {
            if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(_data))
            parts.Add($"Data: {_data.Substring(0, Math.Min(_data.Length, 50))}{(_data.Length > 50 ? "..." : "")}");

        parts.Add($"Symbology: {_symbology}");
        if (_format.HasValue)
            parts.Add($"Format: {_format}");

        if (_moduleWidthPixels.HasValue)
            parts.Add($"ModuleWidth: {_moduleWidthPixels}px");

        if (_barHeightPixels.HasValue)
            parts.Add($"BarHeight: {_barHeightPixels}px");

        if (_quietZoneModules.HasValue)
            parts.Add($"QuietZone: {_quietZoneModules} modules");

        if (!string.IsNullOrWhiteSpace(_darkColor))
            parts.Add($"Dark: {_darkColor}");

        if (!string.IsNullOrWhiteSpace(_lightColor))
            parts.Add($"Light: {_lightColor}");

        if (_showBorder == true)
            parts.Add($"Border: {_borderWidthPixels}px {_borderColorHex ?? "(default)"}");

        return $"Barcode: {string.Join(", ", parts)}";
    }
}