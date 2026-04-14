using Lyo.Images.Models;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Lyo.Images;

/// <summary>Composites decorative frames around square QR PNG output.</summary>
internal static class QrFrameLayoutCompositor
{
    public static async Task<byte[]> ApplyAsync(byte[] qrPng, QrFrameLayoutOptions o, CancellationToken ct)
    {
        if (o.Style == QrFrameStyle.None)
            return qrPng;

        await using var inMs = new MemoryStream(qrPng, false);
        using var qr = await Image.LoadAsync<Rgba32>(inMs, ct).ConfigureAwait(false);
        var s = Math.Min(qr.Width, qr.Height);
        if (qr.Width != qr.Height) {
            qr.Mutate(x => x.Crop(new Rectangle((qr.Width - s) / 2, (qr.Height - s) / 2, s, s)));
        }

        // Large module scales (e.g. 256 px/module) produce huge bitmaps. Fixed pixel strokes (4px) and ~52px headers
        // become invisible when the browser scales the PNG to a few hundred pixels — scale chrome with QR side s.
        var layout = ScaledChromeLayout.FromOptions(s, o);

        using var canvas = o.Style switch {
            QrFrameStyle.BadgeWithHeader => ComposeBadge(qr, o, s, layout),
            QrFrameStyle.SimpleRoundedPanel => ComposeSimplePanel(qr, o, s, layout),
            QrFrameStyle.BorderOnly => ComposeBorderOnly(qr, o, s, layout),
            _ => throw new ArgumentOutOfRangeException(nameof(o.Style), o.Style, null)
        };

        await using var outMs = new MemoryStream();
        await canvas.SaveAsync(outMs, ImagePngEncoding.Truecolor, ct).ConfigureAwait(false);
        return outMs.ToArray();
    }

    private static Image<Rgba32> ComposeBadge(Image<Rgba32> qr, QrFrameLayoutOptions o, int s, ScaledChromeLayout L)
    {
        var headerH = L.HeaderHeightPx;
        var notchD = L.NotchDepthPx;
        var pad = L.PaddingPx;
        var margin = L.MarginPx;
        var r = L.CornerRadiusPx;
        var shadowOff = L.ShadowOffsetPx;

        var cardW = s + 2 * pad;
        var bodyTopOffset = headerH + notchD;
        var totalCardH = bodyTopOffset + pad + s + pad;

        var canvasW = cardW + 2 * margin + shadowOff;
        var canvasH = totalCardH + 2 * margin + shadowOff;
        var img = new Image<Rgba32>(canvasW, canvasH);
        if (!TryParseColor(o.CanvasBackgroundHex, out var canvasBg))
            canvasBg = Color.White;
        img.Mutate(x => x.Fill(canvasBg));

        var cardX = margin;
        var cardY = margin;

        if (shadowOff > 0 && TryParseColor(o.ShadowHex, out var sh)) {
            var shadowPath = RoundedRectPath(cardX + shadowOff, cardY + shadowOff, cardW, totalCardH, r);
            img.Mutate(x => x.Fill(sh, shadowPath));
        }

        if (!TryParseColor(o.PanelBackgroundHex, out var panel))
            panel = Color.White;
        var cardOutline = RoundedRectPath(cardX, cardY, cardW, totalCardH, r);
        img.Mutate(x => x.Fill(panel, cardOutline));
        if (!TryParseColor(o.CardOutlineHex, out var edge))
            TryParseColor("#64748B", out edge);
        img.Mutate(x => x.Draw(Pens.Solid(edge, L.CardOutlineWidthPx), cardOutline));

        if (!TryParseColor(o.HeaderBackgroundHex, out var headBg))
            headBg = Color.Parse("#1e293b");
        var headerPath = TopRoundedRectPath(cardX, cardY, cardW, headerH, r);
        img.Mutate(x => x.Fill(headBg, headerPath));

        if (o.DrawHeaderNotch && notchD > 0) {
            var cx = cardX + cardW / 2f;
            var nw = Math.Clamp(L.NotchWidthPx, 8, cardW);
            var hb = cardY + headerH;
            var nPb = new PathBuilder();
            nPb.MoveTo(new PointF(cx - nw / 2f, hb));
            nPb.LineTo(new PointF(cx + nw / 2f, hb));
            nPb.LineTo(new PointF(cx, hb + notchD));
            nPb.CloseFigure();
            img.Mutate(x => x.Fill(headBg, nPb.Build()));
        }

        var caption = o.CaptionText?.Trim();
        if (!string.IsNullOrEmpty(caption)) {
            if (!TryParseColor(o.HeaderCaptionTextHex, out var capCol))
                capCol = Color.White;
            var font = CreateFont(L.CaptionFontSizePx, o.FontFamily);
            var textOpts = new RichTextOptions(font) {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Origin = new PointF(cardX + cardW / 2f, cardY + headerH / 2f)
            };
            img.Mutate(x => x.DrawText(textOpts, caption, Brushes.Solid(capCol), null));
        }

        var qx = (int)(cardX + pad);
        var qy = (int)(cardY + bodyTopOffset + pad);
        img.Mutate(x => x.DrawImage(qr, new Point(qx, qy), 1f));
        return img;
    }

    private static Image<Rgba32> ComposeSimplePanel(Image<Rgba32> qr, QrFrameLayoutOptions o, int s, ScaledChromeLayout L)
    {
        var pad = L.PaddingPx;
        var margin = L.MarginPx;
        var r = L.CornerRadiusPx;
        var shadowOff = L.ShadowOffsetPx;

        var cardW = s + 2 * pad;
        var cardH = s + 2 * pad;
        var canvasW = cardW + 2 * margin + shadowOff;
        var canvasH = cardH + 2 * margin + shadowOff;
        var img = new Image<Rgba32>(canvasW, canvasH);
        if (!TryParseColor(o.CanvasBackgroundHex, out var canvasBg))
            canvasBg = Color.White;
        img.Mutate(x => x.Fill(canvasBg));

        var cardX = margin;
        var cardY = margin;
        if (shadowOff > 0 && TryParseColor(o.ShadowHex, out var sh)) {
            var shadowPath = RoundedRectPath(cardX + shadowOff, cardY + shadowOff, cardW, cardH, r);
            img.Mutate(x => x.Fill(sh, shadowPath));
        }

        if (!TryParseColor(o.PanelBackgroundHex, out var panel))
            panel = Color.White;
        var cardPath = RoundedRectPath(cardX, cardY, cardW, cardH, r);
        img.Mutate(x => x.Fill(panel, cardPath));
        if (!TryParseColor(o.CardOutlineHex, out var edge))
            TryParseColor("#64748B", out edge);
        img.Mutate(x => x.Draw(Pens.Solid(edge, L.CardOutlineWidthPx), cardPath));

        var qx = (int)(cardX + pad);
        var qy = (int)(cardY + pad);
        img.Mutate(x => x.DrawImage(qr, new Point(qx, qy), 1f));
        return img;
    }

    private static Image<Rgba32> ComposeBorderOnly(Image<Rgba32> qr, QrFrameLayoutOptions o, int s, ScaledChromeLayout L)
    {
        var pad = L.PaddingPx;
        var margin = L.MarginPx;
        var r = L.CornerRadiusPx;
        var footer = L.CaptionFooterPaddingPx;
        var caption = o.CaptionText?.Trim();
        var extraFooter = string.IsNullOrEmpty(caption) ? 0 : footer + (int)L.CaptionFontSizePx + L.FooterCaptionGapPx;
        var cardW = s + 2 * pad;
        var cardH = s + 2 * pad + extraFooter;
        var canvasW = cardW + 2 * margin;
        var canvasH = cardH + 2 * margin;
        var img = new Image<Rgba32>(canvasW, canvasH);
        if (!TryParseColor(o.CanvasBackgroundHex, out var canvasBg))
            canvasBg = Color.White;
        img.Mutate(x => x.Fill(canvasBg));

        var cardX = margin;
        var cardY = margin;
        if (!TryParseColor(o.PanelBackgroundHex, out var panel))
            panel = Color.White;
        var cardPath = RoundedRectPath(cardX, cardY, cardW, cardH, r);
        img.Mutate(x => x.Fill(panel, cardPath));

        var stroke = L.BorderStrokeWidthPx;
        if (!TryParseColor(o.BorderStrokeHex, out var borderCol))
            borderCol = Color.Parse("#334155");
        var innerR = Math.Max(0, r - stroke / 2f);
        var inner = RoundedRectPath(cardX + stroke / 2f, cardY + stroke / 2f, cardW - stroke, cardH - stroke, innerR);
        var pen = Pens.Solid(borderCol, stroke);
        img.Mutate(x => x.Draw(pen, inner));

        var qx = (int)(cardX + pad);
        var qy = (int)(cardY + pad);
        img.Mutate(x => x.DrawImage(qr, new Point(qx, qy), 1f));

        if (!string.IsNullOrEmpty(caption)) {
            if (!TryParseColor(o.HeaderCaptionTextHex, out var capCol))
                capCol = Color.Black;
            var font = CreateFont(L.CaptionFontSizePx, o.FontFamily);
            var textOpts = new RichTextOptions(font) {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Origin = new PointF(cardX + cardW / 2f, cardY + pad + s + footer + L.CaptionFontSizePx / 2f)
            };
            img.Mutate(x => x.DrawText(textOpts, caption, Brushes.Solid(capCol), null));
        }

        return img;
    }

    /// <summary>Pixel sizes derived from QR side <paramref name="s" /> so chrome stays visible when the PNG is huge (e.g. 256 px per module).</summary>
    private readonly struct ScaledChromeLayout
    {
        public int PaddingPx { get; init; }
        public int MarginPx { get; init; }
        public float CornerRadiusPx { get; init; }
        public int ShadowOffsetPx { get; init; }
        public int HeaderHeightPx { get; init; }
        public int NotchDepthPx { get; init; }
        public int NotchWidthPx { get; init; }
        public float CardOutlineWidthPx { get; init; }
        public float BorderStrokeWidthPx { get; init; }
        public float CaptionFontSizePx { get; init; }
        public int CaptionFooterPaddingPx { get; init; }
        public int FooterCaptionGapPx { get; init; }

        public static ScaledChromeLayout FromOptions(int s, QrFrameLayoutOptions o)
        {
            // Fractions of QR side — keeps header, borders, and text a stable share of the image when s is 500 or 8000 px.
            var pad = Math.Max(o.PaddingAroundQrPx, (int)(s * 0.045));
            var margin = Math.Max(o.OuterMarginPx, (int)(s * 0.035));
            var r = Math.Clamp(o.CornerRadiusPx * (s / 400f), 6f, s * 0.12f);
            var shadowOff = o.ShadowOffsetPx <= 0 ? 0 : Math.Clamp(Math.Max(o.ShadowOffsetPx, (int)(s * 0.012)), 4, 80);

            var headerH = Math.Clamp(Math.Max(o.HeaderHeightPx, (int)(s * 0.16)), 32, Math.Min(900, (int)(s * 0.22)));
            var notchD = o.DrawHeaderNotch ? Math.Clamp(Math.Max(o.NotchDepthPx, (int)(s * 0.014)), 6, Math.Min(80, (int)(s * 0.04))) : 0;
            var notchW = Math.Clamp(Math.Max(o.NotchWidthPx, (int)(s * 0.08)), 16, (int)(s * 0.35));

            var outline = Math.Clamp(Math.Max(o.CardOutlineWidthPx, s * 0.0075f), 3f, 72f);
            var borderStroke = Math.Clamp(Math.Max(o.BorderStrokeWidthPx, s * 0.011f), 5f, 120f);

            // Badge header text is bounded by the header band; border-only captions scale with QR side only.
            var captionMax = o.Style == QrFrameStyle.BorderOnly
                ? Math.Min(220f, s * 0.055f)
                : Math.Min(220f, Math.Max(headerH * 0.42f, s * 0.022f));
            var caption = Math.Clamp(Math.Max(o.CaptionFontSizePx, s * 0.026f), 14f, captionMax);
            var footerPad = Math.Max(o.CaptionFooterPaddingPx, (int)(s * 0.02));
            var footerGap = Math.Max(8, (int)(s * 0.01));

            return new ScaledChromeLayout {
                PaddingPx = pad,
                MarginPx = margin,
                CornerRadiusPx = r,
                ShadowOffsetPx = shadowOff,
                HeaderHeightPx = headerH,
                NotchDepthPx = notchD,
                NotchWidthPx = notchW,
                CardOutlineWidthPx = outline,
                BorderStrokeWidthPx = borderStroke,
                CaptionFontSizePx = caption,
                CaptionFooterPaddingPx = footerPad,
                FooterCaptionGapPx = footerGap
            };
        }
    }

    /// <summary>Axis-aligned rectangle for fills and strokes.</summary>
    /// <remarks>
    /// We previously built rounded corners with <see cref="PathBuilder.ArcTo" />. With SixLabors.ImageSharp.Drawing 2.x, that sequence can produce a degenerate polygon so
    /// <see cref="IImageProcessingContext.Fill" /> only rasterizes a hairline (often at the top edge). <see cref="RectangularPolygon" /> is reliable; corner radius is ignored for the path until we adopt center-based arcs (<c>AddArc(center, rx, ry, …)</c>) or a vetted rounded-rect helper.
    /// </remarks>
    private static IPath RoundedRectPath(float x, float y, float w, float h, float radius)
    {
        _ = radius;
        if (w <= 0 || h <= 0)
            return new RectangularPolygon(x, y, Math.Max(0, w), Math.Max(0, h));

        return new RectangularPolygon(x, y, w, h);
    }

    /// <summary>Header band shape (same as <see cref="RoundedRectPath" /> — full rectangle).</summary>
    private static IPath TopRoundedRectPath(float x, float y, float w, float h, float radius)
        => RoundedRectPath(x, y, w, h, radius);

    private static bool TryParseColor(string hex, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(hex))
            return false;

        var h = hex.Trim();
        if (Color.TryParse(h, out color))
            return true;

        // MudBlazor hex often includes alpha (#RRGGBBAA); retry opaque RGB (#RRGGBB).
        if (h.Length == 9 && h[0] == '#' && Color.TryParse(h[..7], out color))
            return true;

        return false;
    }

    private static Font CreateFont(float sizePx, string? family)
    {
        var names = string.IsNullOrWhiteSpace(family)
            ? new[] { "DejaVu Sans", "Liberation Sans", "Arial", "Helvetica" }
            : new[] { family!, "DejaVu Sans", "Liberation Sans", "Arial" };

        foreach (var n in names) {
            try {
                return SystemFonts.CreateFont(n, sizePx, FontStyle.Regular);
            }
            catch {
                /* try next */
            }
        }

        return SystemFonts.CreateFont(SystemFonts.Families.First().Name, sizePx, FontStyle.Regular);
    }
}
