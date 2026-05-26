using System.Globalization;
using System.Text.RegularExpressions;
using Lyo.Common.Records;
using Lyo.Images.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ImageSharpResizeMode = SixLabors.ImageSharp.Processing.ResizeMode;

namespace Lyo.Images.Decoration;

/// <summary>Composites an overlay image on top of a background (raster) or splices an embedded base64 PNG into an SVG document.</summary>
internal static class OverlayDrawer
{
    /// <summary>Mutates <paramref name="background" /> in place: optional resize, optional pad fill, overlay paint, and optional stroke.</summary>
    public static async Task ApplyToRasterAsync(Image<Rgba32> background, byte[] overlayImageBytes, OverlayOptions options, CancellationToken ct)
    {
        var overlayPct = Math.Clamp(options.OverlaySizePercent, 1, 50);
        if (options.BackgroundSquareSize is > 0) {
            var s = options.BackgroundSquareSize.Value;
            if (background.Width != s || background.Height != s)
                background.Mutate(x => x.Resize(s, s));
        }

        var w = background.Width;
        var h = background.Height;
        var side = Math.Min(w, h);
        var iconSize = Math.Max(1, (int)(side * (overlayPct / 100.0)));
        var (ix, iy) = ComputePosition(options.Position, w, h, iconSize);
        await using var iconStream = new MemoryStream(overlayImageBytes, false);
        using var iconImg = await Image.LoadAsync<Rgba32>(iconStream, ct).ConfigureAwait(false);
        iconImg.Mutate(x => x.Resize(new ResizeOptions { Size = new(iconSize, iconSize), Mode = ImageSharpResizeMode.Pad, PadColor = Color.Transparent }));
        background.Mutate(ctx => {
            if (!string.IsNullOrWhiteSpace(options.PadColorHex) && DecorationGeometry.TryParseColor(options.PadColorHex, out var pad)) {
                // Clamp pad to canvas so tiny backgrounds don't get negative rectangles.
                var padL = Math.Max(0, ix - 2);
                var padT = Math.Max(0, iy - 2);
                var padR = Math.Min(w, ix + iconSize + 2);
                var padB = Math.Min(h, iy + iconSize + 2);
                if (padR > padL && padB > padT)
                    ctx.Fill(pad, new RectangularPolygon(padL, padT, padR - padL, padB - padT));
            }

            ctx.DrawImage(iconImg, new Point(ix, iy), 1f);
            if (options.DrawBorder) {
                var stroke = DecorationGeometry.ParseColorOr(options.BorderColorHex, Color.Parse("#334155"));
                var strokeWidth = Math.Max(1, options.BorderStrokeWidthPx);
                var bL = Math.Max(0, ix - 1);
                var bT = Math.Max(0, iy - 1);
                var bR = Math.Min(w, ix + iconSize + 1);
                var bB = Math.Min(h, iy + iconSize + 1);
                if (bR > bL && bB > bT)
                    ctx.Draw(Pens.Solid(stroke, strokeWidth), new RectangularPolygon(bL, bT, bR - bL, bB - bT));
            }
        });
    }

    /// <summary>Embeds a resized PNG copy of the overlay inside the SVG document just before <c>&lt;/svg&gt;</c>; returns the modified SVG string (or the original on failure).</summary>
    public static async Task<string> ApplyToSvgAsync(string svg, byte[] overlayImageBytes, OverlayOptions options, CancellationToken ct)
    {
        var overlayPct = Math.Clamp(options.OverlaySizePercent, 1, 50);
        if (!TryGetSvgCanvasSize(svg, out var canvas) || canvas <= 0)
            return svg;

        var iconSize = Math.Max(1, (int)(canvas * (overlayPct / 100.0)));
        var (ix, iy) = ComputePosition(options.Position, canvas, canvas, iconSize);
        await using var iconStream = new MemoryStream(overlayImageBytes, false);
        using var iconImg = await Image.LoadAsync<Rgba32>(iconStream, ct).ConfigureAwait(false);
        iconImg.Mutate(x => x.Resize(new ResizeOptions { Size = new(iconSize, iconSize), Mode = ImageSharpResizeMode.Pad, PadColor = Color.Transparent }));
        await using var encoded = new MemoryStream();
        await iconImg.SaveAsync(encoded, ImagePngEncoding.Truecolor, ct).ConfigureAwait(false);
        var b64 = Convert.ToBase64String(encoded.ToArray());
        var idx = svg.LastIndexOf("</svg>", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return svg;

        var border = "";
        if (options.DrawBorder) {
            var strokeHex = string.IsNullOrWhiteSpace(options.BorderColorHex) ? "#334155" : options.BorderColorHex!;
            var strokeWidth = Math.Max(1, options.BorderStrokeWidthPx);
            border = $"  <rect x=\"{ix - 1}\" y=\"{iy - 1}\" width=\"{iconSize + 2}\" height=\"{iconSize + 2}\" fill=\"none\" stroke=\"{strokeHex}\" stroke-width=\"{strokeWidth}\"/>\n";
        }

        var img =
            $"  <image href=\"data:{FileTypeInfo.Png.MimeType};base64,{b64}\" x=\"{ix}\" y=\"{iy}\" width=\"{iconSize}\" height=\"{iconSize}\" preserveAspectRatio=\"xMidYMid meet\"/>\n";

        return string.Concat(svg.AsSpan(0, idx), border, img, svg.AsSpan(idx));
    }

    private static (int X, int Y) ComputePosition(OverlayPosition position, int w, int h, int iconSize)
        => position switch {
            OverlayPosition.Center => ((w - iconSize) / 2, (h - iconSize) / 2),
            OverlayPosition.TopLeft => (0, 0),
            OverlayPosition.TopRight => (w - iconSize, 0),
            OverlayPosition.BottomLeft => (0, h - iconSize),
            OverlayPosition.BottomRight => (w - iconSize, h - iconSize),
            var _ => ((w - iconSize) / 2, (h - iconSize) / 2)
        };

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
