using System.Diagnostics;
using System.Runtime.InteropServices;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.QRCode.Models;
using Lyo.QRCode.Payloads;

namespace Lyo.QRCode;

/// <summary>Fluent builder that assembles a QR generation request and validates fields.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class QRCodeBuilder
{
    private string? _darkColor;
    private string? _data;
    private bool? _drawQuietZones;
    private QRCodeErrorCorrectionLevel? _errorCorrectionLevel;
    private QRCodeFormat? _format;
    private QRCodeIconOptions? _icon;
    private string? _lightColor;
    private int? _size;

    /// <summary>Sets the payload to encode in the QR code.</summary>
    /// <param name="data">Payload to encode (text, URL, and similar).</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentException">Raised when data is null or empty.</exception>
    public QRCodeBuilder WithData(string data)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(data);
        _data = data;
        return this;
    }

    /// <summary>Sets payload from a typed QR content object. If both <see cref="WithData" /> and <see cref="WithPayload" /> are used, the last call wins.</summary>
    /// <param name="payload">Payload serialized with <see cref="IQrPayload.ToQrString" />.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Raised when <paramref name="payload" /> is null.</exception>
    public QRCodeBuilder WithPayload(IQrPayload payload)
    {
        ArgumentHelpers.ThrowIfNull(payload);
        var s = payload.ToQrString();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(s);
        _data = s;
        return this;
    }

    /// <summary>Sets the QR output format.</summary>
    /// <param name="format">Output format (PNG, SVG, JPEG, Bitmap).</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="PlatformNotSupportedException">Raised when the format needs Windows but the process is not on Windows.</exception>
    public QRCodeBuilder WithFormat(QRCodeFormat format)
    {
        if ((format == QRCodeFormat.Jpeg || format == QRCodeFormat.Bitmap) && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException($"QR code format '{format}' requires Windows. Use PNG or SVG format on non-Windows platforms.");

        _format = format;
        return this;
    }

    /// <summary>Sets the pixel size of each module in the rendered image (not the total bitmap width).</summary>
    /// <param name="size">Pixels per module. Must be positive.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentException">Raised when size is not positive.</exception>
    public QRCodeBuilder WithSize(int size)
    {
        ArgumentHelpers.ThrowIfNotInRange(size, 1, int.MaxValue);
        _size = size;
        return this;
    }

    /// <summary>Sets the error-correction level.</summary>
    /// <param name="level">Error-correction level (Low, Medium, Quartile, High).</param>
    /// <returns>This builder, for chaining.</returns>
    public QRCodeBuilder WithErrorCorrectionLevel(QRCodeErrorCorrectionLevel level)
    {
        _errorCorrectionLevel = level;
        return this;
    }

    /// <summary>Sets the dark (foreground) color in hex.</summary>
    /// <param name="color">Color in hex (for example <c>#000000</c>).</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentException">Raised when the color format is invalid.</exception>
    public QRCodeBuilder WithDarkColor(string color)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(color);
        if (!IsValidHexColor(color))
            throw new InvalidFormatException("Color must be in hex format (e.g., '#000000' or '#FF0000').", nameof(color), color, "Hex color format");

        _darkColor = color;
        return this;
    }

    /// <summary>Sets the light (background) color in hex.</summary>
    /// <param name="color">Color in hex (for example <c>#FFFFFF</c>).</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentException">Raised when the color format is invalid.</exception>
    public QRCodeBuilder WithLightColor(string color)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(color);
        if (!IsValidHexColor(color))
            throw new InvalidFormatException("Color must be in hex format (e.g., '#FFFFFF' or '#FF0000').", nameof(color), color, "Hex color format");

        _lightColor = color;
        return this;
    }

    /// <summary>Sets whether to draw quiet zones (the white border around the QR code).</summary>
    /// <param name="drawQuietZones">True to draw quiet zones; false to omit them.</param>
    /// <returns>This builder, for chaining.</returns>
    public QRCodeBuilder WithQuietZones(bool drawQuietZones)
    {
        _drawQuietZones = drawQuietZones;
        return this;
    }

    /// <summary>Sets an icon or logo to embed in the center of the QR code.</summary>
    /// <param name="iconBytes">Icon image bytes.</param>
    /// <param name="iconSizePercent">Icon size as a percent of the QR image side (1–30). Starts as 15.</param>
    /// <param name="drawIconBorder">If true, draw a border around the icon. Starts as true.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentException">Raised when iconBytes is null or empty, or iconSizePercent is out of range.</exception>
    public QRCodeBuilder WithIcon(byte[] iconBytes, int iconSizePercent = 15, bool drawIconBorder = true)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(iconBytes);
        ArgumentHelpers.ThrowIfNotInRange(iconSizePercent, 1, QRCodeIconOptions.MaxIconSizePercent);
        _icon = new() { IconBytes = iconBytes, IconSizePercent = iconSizePercent, DrawIconBorder = drawIconBorder };
        return this;
    }

    /// <summary>Sets an icon or logo from a file path.</summary>
    /// <param name="iconFilePath">Path to the icon image file.</param>
    /// <param name="iconSizePercent">Icon size as a percent of the QR image side (1–30). Starts as 15.</param>
    /// <param name="drawIconBorder">If true, draw a border around the icon. Starts as true.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentException">Raised when the file path is invalid or iconSizePercent is out of range.</exception>
    public QRCodeBuilder WithIconFromFile(string iconFilePath, int iconSizePercent = 15, bool drawIconBorder = true)
    {
        ArgumentHelpers.ThrowIfFileNotFound(iconFilePath);
        var iconBytes = File.ReadAllBytes(iconFilePath);
        return WithIcon(iconBytes, iconSizePercent, drawIconBorder);
    }

    /// <summary>Removes the icon from the QR code.</summary>
    /// <returns>This builder, for chaining.</returns>
    public QRCodeBuilder WithoutIcon()
    {
        _icon = null;
        return this;
    }

    /// <summary>Clears every builder property.</summary>
    /// <returns>This builder, for chaining.</returns>
    public QRCodeBuilder Clear()
    {
        _data = null;
        _format = null;
        _size = null;
        _errorCorrectionLevel = null;
        _darkColor = null;
        _lightColor = null;
        _drawQuietZones = null;
        _icon = null;
        return this;
    }

    /// <summary>Builds the QR options.</summary>
    /// <returns>A tuple of the data and QR options.</returns>
    /// <exception cref="InvalidOperationException">Raised when required fields are missing.</exception>
    public (string Data, QRCodeOptions Options) Build()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_data);
        var options = new QRCodeOptions();
        if (_format.HasValue)
            options.Format = _format.Value;

        if (_size.HasValue)
            options.Size = _size.Value;

        if (_errorCorrectionLevel.HasValue)
            options.ErrorCorrectionLevel = _errorCorrectionLevel.Value;

        if (!string.IsNullOrWhiteSpace(_darkColor))
            options.DarkColor = _darkColor;

        if (!string.IsNullOrWhiteSpace(_lightColor))
            options.LightColor = _lightColor;

        if (_drawQuietZones.HasValue)
            options.DrawQuietZones = _drawQuietZones.Value;

        if (_icon != null)
            options.Icon = _icon;

        return (_data, options);
    }

    /// <summary>Creates a new <see cref="QRCodeBuilder" />.</summary>
    /// <returns>A new builder.</returns>
    public static QRCodeBuilder New() => new();

    /// <summary>True when the string is a valid hex color.</summary>
    private static bool IsValidHexColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return false;

        // Drop a leading # if present
        var hex = color.StartsWith("#") ? color.Substring(1) : color;

        // Must be 3 or 6 hex digits
        if (hex.Length != 3 && hex.Length != 6)
            return false;

        // Every character must be a hex digit
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

        if (_format.HasValue)
            parts.Add($"Format: {_format}");

        if (_size.HasValue)
            parts.Add($"Size: {_size}px");

        if (_errorCorrectionLevel.HasValue)
            parts.Add($"ECC: {_errorCorrectionLevel}");

        if (!string.IsNullOrWhiteSpace(_darkColor))
            parts.Add($"Dark: {_darkColor}");

        if (!string.IsNullOrWhiteSpace(_lightColor))
            parts.Add($"Light: {_lightColor}");

        if (_drawQuietZones.HasValue)
            parts.Add($"QuietZones: {_drawQuietZones}");

        if (_icon != null)
            parts.Add("HasIcon: true");

        return $"QR Code: {string.Join(", ", parts)}";
    }
}