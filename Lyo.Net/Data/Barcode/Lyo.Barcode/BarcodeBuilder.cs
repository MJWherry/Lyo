using System.Diagnostics;
using Lyo.Barcode.Models;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Barcode;

/// <summary>Fluent builder for <see cref="BarcodeRequest" /> and <see cref="BarcodeOptions" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class BarcodeBuilder
{
    private int? _barHeightPixels;
    private string? _darkColor;
    private string? _data;
    private BarcodeFormat? _format;
    private string? _lightColor;
    private int? _moduleWidthPixels;
    private int? _quietZoneModules;
    private BarcodeSymbology _symbology = BarcodeSymbology.Code128;
    private bool? _showHumanReadableTextBelow;
    private bool _hasHumanReadableText;
    private string? _humanReadableText;
    private int? _humanReadableFontSizePixels;
    private int? _humanReadableMarginTopPixels;
    private int? _humanReadableMarginBottomPixels;
    private bool _hasHumanReadableColorHex;
    private string? _humanReadableColorHex;
    private bool? _showBorder;
    private int? _borderWidthPixels;
    private bool _hasBorderColorHex;
    private string? _borderColorHex;

    /// <summary>Sets the payload string to encode.</summary>
    /// <returns>This builder for chaining.</returns>
    public BarcodeBuilder WithData(string data)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(data);
        _data = data;
        return this;
    }

    /// <summary>Sets the barcode symbology.</summary>
    public BarcodeBuilder WithSymbology(BarcodeSymbology symbology)
    {
        _symbology = symbology;
        return this;
    }

    /// <summary>Sets the output image format (BMP or SVG).</summary>
    public BarcodeBuilder WithFormat(BarcodeFormat format)
    {
        _format = format;
        return this;
    }

    /// <summary>Sets horizontal module width in pixels (bar pitch).</summary>
    public BarcodeBuilder WithModuleWidthPixels(int pixels)
    {
        ArgumentHelpers.ThrowIfNotInRange(pixels, 1, int.MaxValue);
        _moduleWidthPixels = pixels;
        return this;
    }

    /// <summary>Sets bar height in pixels (excluding quiet zone).</summary>
    public BarcodeBuilder WithBarHeightPixels(int pixels)
    {
        ArgumentHelpers.ThrowIfNotInRange(pixels, 1, int.MaxValue);
        _barHeightPixels = pixels;
        return this;
    }

    /// <summary>Sets quiet zone width on each side in modules.</summary>
    public BarcodeBuilder WithQuietZoneModules(int modules)
    {
        ArgumentHelpers.ThrowIfNotInRange(modules, 0, int.MaxValue);
        _quietZoneModules = modules;
        return this;
    }

    /// <summary>Sets foreground (bar) color as <c>#RRGGBB</c> or <c>#RGB</c>.</summary>
    public BarcodeBuilder WithDarkColor(string color)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(color);
        if (!IsValidHexColor(color))
            throw new InvalidFormatException("Color must be in hex format (e.g., '#000000' or '#FF0000').", nameof(color), color, "Hex color format");

        _darkColor = color;
        return this;
    }

    /// <summary>Sets background color as <c>#RRGGBB</c> or <c>#RGB</c>.</summary>
    public BarcodeBuilder WithLightColor(string color)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(color);
        if (!IsValidHexColor(color))
            throw new InvalidFormatException("Color must be in hex format (e.g., '#FFFFFF' or '#FF0000').", nameof(color), color, "Hex color format");

        _lightColor = color;
        return this;
    }

    /// <summary>When true, draws human-readable interpretation under the bars.</summary>
    public BarcodeBuilder WithShowHumanReadableTextBelow(bool show = true)
    {
        _showHumanReadableTextBelow = show;
        return this;
    }

    /// <summary>Caption under the bars. Whitespace-only clears the override so the encoded payload is shown.</summary>
    public BarcodeBuilder WithHumanReadableText(string? text)
    {
        _hasHumanReadableText = true;
        _humanReadableText = text;
        return this;
    }

    /// <summary>Sets caption font size in pixels under the bars.</summary>
    public BarcodeBuilder WithHumanReadableFontSizePixels(int pixels)
    {
        ArgumentHelpers.ThrowIfNotInRange(pixels, 1, int.MaxValue);
        _humanReadableFontSizePixels = pixels;
        return this;
    }

    /// <summary>Sets vertical gap between bars and caption.</summary>
    public BarcodeBuilder WithHumanReadableMarginTopPixels(int pixels)
    {
        ArgumentHelpers.ThrowIfNotInRange(pixels, 0, int.MaxValue);
        _humanReadableMarginTopPixels = pixels;
        return this;
    }

    /// <summary>Sets padding below the caption.</summary>
    public BarcodeBuilder WithHumanReadableMarginBottomPixels(int pixels)
    {
        ArgumentHelpers.ThrowIfNotInRange(pixels, 0, int.MaxValue);
        _humanReadableMarginBottomPixels = pixels;
        return this;
    }

    /// <summary>Sets caption ink color; null/whitespace clears override (uses dark color).</summary>
    public BarcodeBuilder WithHumanReadableColorHex(string? color)
    {
        _hasHumanReadableColorHex = true;
        if (!string.IsNullOrWhiteSpace(color) && !IsValidHexColor(color))
            throw new InvalidFormatException("Color must be in hex format (e.g., '#000000' or '#FF0000').", nameof(color), color, "Hex color format");

        _humanReadableColorHex = color;
        return this;
    }

    /// <summary>When true, draws a border frame around the barcode image (BMP/SVG).</summary>
    public BarcodeBuilder WithShowBorder(bool show = true)
    {
        _showBorder = show;
        return this;
    }

    /// <summary>Border thickness in pixels per side when border is enabled.</summary>
    public BarcodeBuilder WithBorderWidthPixels(int pixels)
    {
        ArgumentHelpers.ThrowIfNotInRange(pixels, 1, int.MaxValue);
        _borderWidthPixels = pixels;
        return this;
    }

    /// <summary>Border color as <c>#RRGGBB</c> or <c>#RGB</c>.</summary>
    public BarcodeBuilder WithBorderColorHex(string color)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(color);
        if (!IsValidHexColor(color))
            throw new InvalidFormatException("Color must be in hex format (e.g., '#000000' or '#FF0000').", nameof(color), color, "Hex color format");

        _hasBorderColorHex = true;
        _borderColorHex = color;
        return this;
    }

    /// <summary>Resets all builder fields to defaults.</summary>
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

    /// <summary>Builds the tuple consumed by <see cref="IBarcodeService.GenerateAsync(string, BarcodeSymbology, BarcodeOptions?, CancellationToken)" />.</summary>
    /// <exception cref="ArgumentException">Thrown when data has not been set.</exception>
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

    /// <summary>Creates a new builder instance.</summary>
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