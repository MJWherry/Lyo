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

    public BarcodeBuilder WithData(string data)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(data, nameof(data));
        _data = data;
        return this;
    }

    public BarcodeBuilder WithSymbology(BarcodeSymbology symbology)
    {
        _symbology = symbology;
        return this;
    }

    public BarcodeBuilder WithFormat(BarcodeFormat format)
    {
        _format = format;
        return this;
    }

    public BarcodeBuilder WithModuleWidthPixels(int pixels)
    {
        ArgumentHelpers.ThrowIfNotInRange(pixels, 1, int.MaxValue, nameof(pixels));
        _moduleWidthPixels = pixels;
        return this;
    }

    public BarcodeBuilder WithBarHeightPixels(int pixels)
    {
        ArgumentHelpers.ThrowIfNotInRange(pixels, 1, int.MaxValue, nameof(pixels));
        _barHeightPixels = pixels;
        return this;
    }

    public BarcodeBuilder WithQuietZoneModules(int modules)
    {
        ArgumentHelpers.ThrowIfNotInRange(modules, 0, int.MaxValue, nameof(modules));
        _quietZoneModules = modules;
        return this;
    }

    public BarcodeBuilder WithDarkColor(string color)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(color, nameof(color));
        if (!IsValidHexColor(color))
            throw new InvalidFormatException("Color must be in hex format (e.g., '#000000' or '#FF0000').", nameof(color), color, "Hex color format");

        _darkColor = color;
        return this;
    }

    public BarcodeBuilder WithLightColor(string color)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(color, nameof(color));
        if (!IsValidHexColor(color))
            throw new InvalidFormatException("Color must be in hex format (e.g., '#FFFFFF' or '#FF0000').", nameof(color), color, "Hex color format");

        _lightColor = color;
        return this;
    }

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
        return this;
    }

    public (string Data, BarcodeSymbology Symbology, BarcodeOptions Options) Build()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_data, nameof(_data));
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

        return (_data, _symbology, options);
    }

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

        return $"Barcode: {string.Join(", ", parts)}";
    }
}