using System.Diagnostics;
using System.Runtime.InteropServices;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Images.Models;
using Lyo.QRCode.Models;

namespace Lyo.QRCode;

/// <summary>Builder class for constructing QR code generation requests with validation.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class QRCodeBuilder
{
    private string? _darkColor;
    private string? _data;
    private bool? _drawQuietZones;
    private QRCodeErrorCorrectionLevel? _errorCorrectionLevel;
    private QRCodeFormat? _format;
    private QRCodeIconOptions? _icon;
    private QrFrameLayoutOptions? _frame;
    private string? _lightColor;
    private int? _size;

    /// <summary>Sets the data to encode in the QR code.</summary>
    /// <param name="data">The data to encode (text, URL, etc.).</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when data is null or empty.</exception>
    public QRCodeBuilder WithData(string data)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(data, nameof(data));
        _data = data;
        return this;
    }

    /// <summary>Sets the QR code format.</summary>
    /// <param name="format">The output format (PNG, SVG, JPEG, Bitmap).</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown when format requires Windows but running on non-Windows OS.</exception>
    public QRCodeBuilder WithFormat(QRCodeFormat format)
    {
        if ((format == QRCodeFormat.Jpeg || format == QRCodeFormat.Bitmap) && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException($"QR code format '{format}' requires Windows. Use PNG or SVG format on non-Windows platforms.");

        _format = format;
        return this;
    }

    /// <summary>Sets the pixel size of each module in the rendered image (not the total bitmap width).</summary>
    /// <param name="size">Pixels per module (must be positive).</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when size is not positive.</exception>
    public QRCodeBuilder WithSize(int size)
    {
        ArgumentHelpers.ThrowIfNotInRange(size, 1, int.MaxValue, nameof(size));
        _size = size;
        return this;
    }

    /// <summary>Sets the error correction level.</summary>
    /// <param name="level">The error correction level (Low, Medium, Quartile, High).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QRCodeBuilder WithErrorCorrectionLevel(QRCodeErrorCorrectionLevel level)
    {
        _errorCorrectionLevel = level;
        return this;
    }

    /// <summary>Sets the dark color (foreground) in hex format.</summary>
    /// <param name="color">The color in hex format (e.g., "#000000").</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when color format is invalid.</exception>
    public QRCodeBuilder WithDarkColor(string color)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(color, nameof(color));
        if (!IsValidHexColor(color))
            throw new InvalidFormatException("Color must be in hex format (e.g., '#000000' or '#FF0000').", nameof(color), color, "Hex color format");

        _darkColor = color;
        return this;
    }

    /// <summary>Sets the light color (background) in hex format.</summary>
    /// <param name="color">The color in hex format (e.g., "#FFFFFF").</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when color format is invalid.</exception>
    public QRCodeBuilder WithLightColor(string color)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(color, nameof(color));
        if (!IsValidHexColor(color))
            throw new InvalidFormatException("Color must be in hex format (e.g., '#FFFFFF' or '#FF0000').", nameof(color), color, "Hex color format");

        _lightColor = color;
        return this;
    }

    /// <summary>Sets whether to draw quiet zones (white border around QR code).</summary>
    /// <param name="drawQuietZones">True to draw quiet zones, false otherwise.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QRCodeBuilder WithQuietZones(bool drawQuietZones)
    {
        _drawQuietZones = drawQuietZones;
        return this;
    }

    /// <summary>Sets an icon/logo to embed in the center of the QR code.</summary>
    /// <param name="iconBytes">The icon image bytes.</param>
    /// <param name="iconSizePercent">The icon size as a percentage of the QR image side (1–30). Default: 15</param>
    /// <param name="drawIconBorder">Whether to draw a border around the icon. Default: true</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when iconBytes is null or empty, or iconSizePercent is out of range.</exception>
    public QRCodeBuilder WithIcon(byte[] iconBytes, int iconSizePercent = 15, bool drawIconBorder = true)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(iconBytes, nameof(iconBytes));
        ArgumentHelpers.ThrowIfNotInRange(iconSizePercent, 1, QRCodeIconOptions.MaxIconSizePercent, nameof(iconSizePercent));
        _icon = new() { IconBytes = iconBytes, IconSizePercent = iconSizePercent, DrawIconBorder = drawIconBorder };
        return this;
    }

    /// <summary>Sets an icon/logo using a file path.</summary>
    /// <param name="iconFilePath">The path to the icon image file.</param>
    /// <param name="iconSizePercent">The icon size as a percentage of the QR image side (1–30). Default: 15</param>
    /// <param name="drawIconBorder">Whether to draw a border around the icon. Default: true</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when file path is invalid or iconSizePercent is out of range.</exception>
    public QRCodeBuilder WithIconFromFile(string iconFilePath, int iconSizePercent = 15, bool drawIconBorder = true)
    {
        ArgumentHelpers.ThrowIfFileNotFound(iconFilePath, nameof(iconFilePath));
        var iconBytes = File.ReadAllBytes(iconFilePath);
        return WithIcon(iconBytes, iconSizePercent, drawIconBorder);
    }

    /// <summary>Removes the icon from the QR code.</summary>
    /// <returns>The builder instance for method chaining.</returns>
    public QRCodeBuilder WithoutIcon()
    {
        _icon = null;
        return this;
    }

    /// <summary>Sets a decorative PNG frame (badge, panel, or border). PNG output only; requires a registered image service at generation time.</summary>
    public QRCodeBuilder WithFrame(QrFrameLayoutOptions frame)
    {
        ArgumentHelpers.ThrowIfNull(frame, nameof(frame));
        _frame = frame;
        return this;
    }

    /// <summary>Removes the frame from the QR code.</summary>
    public QRCodeBuilder WithoutFrame()
    {
        _frame = null;
        return this;
    }

    /// <summary>Clears all builder properties.</summary>
    /// <returns>The builder instance for method chaining.</returns>
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

    /// <summary>Builds the QR code options.</summary>
    /// <returns>A tuple containing the data and QR code options.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required fields are missing.</exception>
    public (string Data, QRCodeOptions Options) Build()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_data, nameof(_data));
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

        if (_frame != null)
            options.Frame = _frame;

        return (_data, options);
    }

    /// <summary>Creates a new instance of QRCodeBuilder.</summary>
    /// <returns>A new QRCodeBuilder instance.</returns>
    public static QRCodeBuilder New() => new();

    /// <summary>Validates if a string is a valid hex color format.</summary>
    private static bool IsValidHexColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return false;

        // Remove # if present
        var hex = color.StartsWith("#") ? color.Substring(1) : color;

        // Check if it's 3 or 6 hex digits
        if (hex.Length != 3 && hex.Length != 6)
            return false;

        // Check if all characters are valid hex digits
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

        if (_frame is { Style: not QrFrameStyle.None })
            parts.Add($"Frame: {_frame.Style}");

        return $"QR Code: {string.Join(", ", parts)}";
    }
}