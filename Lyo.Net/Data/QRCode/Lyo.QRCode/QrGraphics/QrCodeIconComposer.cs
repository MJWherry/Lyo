using System.Text.RegularExpressions;
using Lyo.Common.Enums;
using Lyo.Common.Records;
using Lyo.Images;
using Lyo.Images.Models;
using Lyo.QRCode.Models;
using Microsoft.Extensions.Logging;

namespace Lyo.QRCode.QrGraphics;

/// <summary>Embeds a raster icon in the center of PNG or SVG QR output via <see cref="IImageService" />.</summary>
internal static class QrCodeIconComposer
{
    /// <summary>Loads icon bytes from <see cref="QRCodeIconOptions.IconBytes" /> or <see cref="QRCodeIconOptions.IconFilePath" />.</summary>
    public static byte[]? TryResolveIconBytes(QRCodeIconOptions icon)
    {
        if (icon.IconBytes is { Length: > 0 })
            return icon.IconBytes;

        if (!string.IsNullOrWhiteSpace(icon.IconFilePath)) {
            if (!File.Exists(icon.IconFilePath))
                throw new FileNotFoundException($"QR icon file not found: {icon.IconFilePath}", icon.IconFilePath);

            return File.ReadAllBytes(icon.IconFilePath);
        }

        return null;
    }

    public static async Task<byte[]> ApplyIconToPngAsync(
        IImageService images,
        byte[] qrPngBytes,
        QRCodeIconOptions icon,
        string lightColorHex,
        ILogger? logger,
        CancellationToken ct)
    {
        byte[]? rawIcon = null;
        try {
            rawIcon = TryResolveIconBytes(icon);
            if (rawIcon == null)
                return qrPngBytes;

            var pct = QRCodeIconOptions.ClampIconSizePercent(icon.IconSizePercent);
            // Do not set BackgroundSquareSize: options.Size is pixels-per-module, not the rendered PNG side length.
            // Resizing the QR to that value destroys module alignment and breaks scans / logo placement.
            var overlayOpts = new ImageCenterOverlayOptions {
                OverlaySizePercent = pct,
                DrawOverlayBorder = icon.DrawIconBorder,
                BorderColorHex = lightColorHex
            };

            var result = await images.CompositeCenterOverlayPngAsync(qrPngBytes, rawIcon, overlayOpts, ct).ConfigureAwait(false);
            if (!result.IsSuccess || result.Data == null) {
                logger?.LogWarning("Failed to apply QR icon: {Errors}", result.Errors);
                return qrPngBytes;
            }

            return result.Data;
        }
        catch (Exception ex) {
            logger?.LogWarning(ex, "Failed to apply QR icon; returning QR without icon.");
            return qrPngBytes;
        }
    }

    public static async Task<string> ApplyIconToSvgAsync(
        IImageService images,
        string svg,
        QRCodeIconOptions icon,
        int totalPixelSize,
        string lightColorHex,
        ILogger? logger,
        CancellationToken ct)
    {
        byte[]? rawIcon = null;
        try {
            rawIcon = TryResolveIconBytes(icon);
            if (rawIcon == null)
                return svg;

            var pct = QRCodeIconOptions.ClampIconSizePercent(icon.IconSizePercent);
            if (!TryGetSvgCanvasSize(svg, out var canvas) || canvas <= 0)
                canvas = totalPixelSize;

            var iconSize = Math.Max(1, (int)(canvas * (pct / 100.0)));
            var ix = (canvas - iconSize) / 2;
            var iy = (canvas - iconSize) / 2;
            await using var inS = new MemoryStream(rawIcon);
            await using var outS = new MemoryStream();
            var resizeResult = await images.ResizeAsync(inS, outS, iconSize, iconSize, ResizeMode.Pad, ImageFormat.Png, null, ct).ConfigureAwait(false);
            if (!resizeResult.IsSuccess) {
                logger?.LogWarning("Failed to resize QR icon for SVG: {Errors}", resizeResult.Errors);
                return svg;
            }

            var b64 = Convert.ToBase64String(outS.ToArray());
            var idx = svg.LastIndexOf("</svg>", StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return svg;

            var border = icon.DrawIconBorder
                ? $"  <rect x=\"{ix - 1}\" y=\"{iy - 1}\" width=\"{iconSize + 2}\" height=\"{iconSize + 2}\" fill=\"none\" stroke=\"{lightColorHex}\" stroke-width=\"2\"/>\n"
                : "";

            var img = $"  <image href=\"data:{FileTypeInfo.Png.MimeType};base64,{b64}\" x=\"{ix}\" y=\"{iy}\" width=\"{iconSize}\" height=\"{iconSize}\" preserveAspectRatio=\"xMidYMid meet\"/>\n";
            var sb = new StringBuilder(svg.Length + 256);
            sb.Append(svg.AsSpan(0, idx));
            sb.Append(border);
            sb.Append(img);
            sb.Append(svg.AsSpan(idx));
            return sb.ToString();
        }
        catch (Exception ex) {
            logger?.LogWarning(ex, "Failed to apply QR icon to SVG; returning SVG without icon.");
            return svg;
        }
    }

    private static bool TryGetSvgCanvasSize(string svg, out int size)
    {
        size = 0;
        var m = Regex.Match(svg, @"viewBox\s*=\s*[""']\s*0\s+0\s+(\d+(?:\.\d+)?)\s+(\d+(?:\.\d+)?)\s*[""']", RegexOptions.IgnoreCase);
        if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var w) &&
            double.TryParse(m.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var h) && Math.Abs(w - h) < 0.5) {
            size = (int)Math.Round(w);
            return size > 0;
        }

        m = Regex.Match(svg, @"\bwidth\s*=\s*[""'](\d+)[""']", RegexOptions.IgnoreCase);
        if (!m.Success)
            return false;

        if (!int.TryParse(m.Groups[1].Value, out var width) || width <= 0)
            return false;

        size = width;
        return true;
    }
}