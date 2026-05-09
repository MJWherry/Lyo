using System.Numerics;
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
    private const int MaxFontCacheEntries = 64;

    private static readonly object s_fontCacheLock = new();

    private static readonly Dictionary<string, Font> s_fontCache = new(StringComparer.Ordinal);

    private static readonly Queue<string> s_fontCacheOrder = new();

    public static async Task<byte[]> ApplyAsync(byte[] qrPng, QrFrameLayoutOptions o, CancellationToken ct, bool useFastPng = false)
    {
        if (o.Style == QrFrameStyle.None)
            return qrPng;

        await using var inMs = new MemoryStream(qrPng, false);
        using var qr = await Image.LoadAsync<Rgba32>(inMs, ct).ConfigureAwait(false);
        var s = Math.Min(qr.Width, qr.Height);
        if (qr.Width != qr.Height)
            qr.Mutate(x => x.Crop(new((qr.Width - s) / 2, (qr.Height - s) / 2, s, s)));

        // Large module scales (e.g. 256 px/module) produce huge bitmaps. Fixed pixel strokes (4px) and ~52px headers
        // become invisible when the browser scales the PNG to a few hundred pixels — scale chrome with QR side s.
        var layout = ScaledChromeLayout.FromOptions(s, o);
        using var canvas = o.Style switch {
            QrFrameStyle.BadgeWithHeader => ComposeBadge(qr, o, s, layout),
            QrFrameStyle.SimpleRoundedPanel => ComposeSimplePanel(qr, o, s, layout),
            QrFrameStyle.BorderOnly => ComposeBorderOnly(qr, o, s, layout),
            var _ => throw new ArgumentOutOfRangeException(nameof(o.Style), o.Style, null)
        };

        var encoder = ImagePngEncoding.TruecolorForComposites(useFastPng);
        await using var outMs = new MemoryStream();
        await canvas.SaveAsync(outMs, encoder, ct).ConfigureAwait(false);
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

        if (!TryParseColor(o.PanelBackgroundHex, out var panel))
            panel = Color.White;

        if (!TryParseColor(o.CardOutlineHex, out var edge))
            TryParseColor("#64748B", out edge);

        if (!TryParseColor(o.HeaderBackgroundHex, out var headBg))
            headBg = Color.Parse("#1e293b");

        var caption = o.CaptionText?.Trim();
        Color capCol = default;
        var hasCaption = !string.IsNullOrEmpty(caption);
        if (hasCaption && !TryParseColor(o.HeaderCaptionTextHex, out capCol))
            capCol = Color.White;

        var cardX = margin;
        var cardY = margin;
        var qx = cardX + pad;
        var qy = cardY + bodyTopOffset + pad;

        img.Mutate(ctx => {
            ctx.Fill(canvasBg);
            if (shadowOff > 0 && TryParseColor(o.ShadowHex, out var sh)) {
                var shadowPath = RoundedRectPath(cardX + shadowOff, cardY + shadowOff, cardW, totalCardH, r);
                ctx.Fill(sh, shadowPath);
            }

            var cardOutline = RoundedRectPath(cardX, cardY, cardW, totalCardH, r);
            ctx.Fill(panel, cardOutline);
            ctx.Draw(Pens.Solid(edge, L.CardOutlineWidthPx), cardOutline);
            var headerPath = TopRoundedRectPath(cardX, cardY, cardW, headerH, r);
            ctx.Fill(headBg, headerPath);
            if (o.DrawHeaderNotch && notchD > 0) {
                var cx = cardX + cardW / 2f;
                var nw = Math.Clamp(L.NotchWidthPx, 8, cardW);
                var hb = cardY + headerH;
                var nPb = new PathBuilder();
                nPb.MoveTo(new(cx - nw / 2f, hb));
                nPb.LineTo(new(cx + nw / 2f, hb));
                nPb.LineTo(new(cx, hb + notchD));
                nPb.CloseFigure();
                ctx.Fill(headBg, nPb.Build());
            }

            if (hasCaption) {
                var font = GetOrCreateFont(L.CaptionFontSizePx, o.FontFamily);
                var textOpts = new RichTextOptions(font) {
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Origin = new PointF(cardX + cardW / 2f, cardY + headerH / 2f)
                };

                ctx.DrawText(textOpts, caption!, Brushes.Solid(capCol), null);
            }

            ctx.DrawImage(qr, new Point(qx, qy), 1f);
        });

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

        if (!TryParseColor(o.PanelBackgroundHex, out var panel))
            panel = Color.White;

        if (!TryParseColor(o.CardOutlineHex, out var edge))
            TryParseColor("#64748B", out edge);

        var cardX = margin;
        var cardY = margin;
        var qx = cardX + pad;
        var qy = cardY + pad;

        img.Mutate(ctx => {
            ctx.Fill(canvasBg);
            if (shadowOff > 0 && TryParseColor(o.ShadowHex, out var sh)) {
                var shadowPath = RoundedRectPath(cardX + shadowOff, cardY + shadowOff, cardW, cardH, r);
                ctx.Fill(sh, shadowPath);
            }

            var cardPath = RoundedRectPath(cardX, cardY, cardW, cardH, r);
            ctx.Fill(panel, cardPath);
            ctx.Draw(Pens.Solid(edge, L.CardOutlineWidthPx), cardPath);
            ctx.DrawImage(qr, new Point(qx, qy), 1f);
        });

        return img;
    }

    private static Image<Rgba32> ComposeBorderOnly(Image<Rgba32> qr, QrFrameLayoutOptions o, int s, ScaledChromeLayout L)
    {
        var pad = L.PaddingPx;
        var margin = L.MarginPx;
        var r = L.CornerRadiusPx;
        var footer = L.CaptionFooterPaddingPx;
        var caption = o.CaptionText?.Trim();
        var extraFooter = string.IsNullOrEmpty(caption) ? 0 : footer + L.CaptionBlockHeightPx + L.FooterCaptionGapPx;
        var cardW = s + 2 * pad;
        var cardH = s + 2 * pad + extraFooter;
        var canvasW = cardW + 2 * margin;
        var canvasH = cardH + 2 * margin;
        var img = new Image<Rgba32>(canvasW, canvasH);
        if (!TryParseColor(o.CanvasBackgroundHex, out var canvasBg))
            canvasBg = Color.White;

        if (!TryParseColor(o.PanelBackgroundHex, out var panel))
            panel = Color.White;

        if (!TryParseColor(o.BorderStrokeHex, out var borderCol))
            borderCol = Color.Parse("#334155");

        var stroke = L.BorderStrokeWidthPx;
        var innerR = Math.Max(0, r - stroke / 2f);
        var cardX = margin;
        var cardY = margin;
        var qx = cardX + pad;
        var qy = cardY + pad;
        var hasCaption = !string.IsNullOrEmpty(caption);
        Color capCol = default;
        if (hasCaption && !TryParseColor(o.HeaderCaptionTextHex, out capCol))
            capCol = Color.Black;

        img.Mutate(ctx => {
            ctx.Fill(canvasBg);
            var cardPath = RoundedRectPath(cardX, cardY, cardW, cardH, r);
            ctx.Fill(panel, cardPath);
            var inner = RoundedRectPath(cardX + stroke / 2f, cardY + stroke / 2f, cardW - stroke, cardH - stroke, innerR);
            var pen = Pens.Solid(borderCol, stroke);
            ctx.Draw(pen, inner);
            ctx.DrawImage(qr, new Point(qx, qy), 1f);
            if (hasCaption) {
                var font = GetOrCreateFont(L.CaptionFontSizePx, o.FontFamily);
                var cy = cardY + pad + s + footer + L.CaptionBlockHeightPx / 2f;
                var textOpts = new RichTextOptions(font) {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Origin = new PointF(cardX + cardW / 2f, cy)
                };

                ctx.DrawText(textOpts, caption!, Brushes.Solid(capCol), null);
            }
        });

        return img;
    }

    /// <summary>Measured or estimated height of the wrapped caption block in pixels.</summary>
    private static int EstimateCaptionBlockHeight(string text, float fontSizePx, float maxWidthPx, string? fontFamily)
    {
        if (string.IsNullOrEmpty(text) || fontSizePx <= 0 || maxWidthPx < 8f)
            return (int)Math.Ceiling(fontSizePx * 1.35);

        try {
            var font = GetOrCreateFont(fontSizePx, fontFamily);
            var opts = new TextOptions(font) { WrappingLength = maxWidthPx };
            return Math.Max(1, (int)Math.Ceiling(TextMeasurer.MeasureSize(text, opts).Height));
        }
        catch {
            return (int)Math.Ceiling(fontSizePx * 1.35);
        }
    }

    /// <summary>Closed rounded rectangle using cubic Bézier quarter-circle approximations (stable with ImageSharp.Drawing 2.x; avoids degenerate <c>ArcTo</c> output).</summary>
    private static IPath RoundedRectPath(float x, float y, float w, float h, float radius)
    {
        if (w <= 0 || h <= 0)
            return new RectangularPolygon(x, y, Math.Max(0, w), Math.Max(0, h));

        var rr = Math.Min(radius, Math.Min(w, h) / 2f);
        if (rr <= 0.5f)
            return new RectangularPolygon(x, y, w, h);

        const float k = 0.5522847498f;
        var pb = new PathBuilder();
        pb.MoveTo(new PointF(x + rr, y));
        pb.LineTo(new PointF(x + w - rr, y));
        pb.CubicBezierTo(new Vector2(x + w - rr + k * rr, y), new Vector2(x + w, y + rr - k * rr), new Vector2(x + w, y + rr));
        pb.LineTo(new PointF(x + w, y + h - rr));
        pb.CubicBezierTo(new Vector2(x + w, y + h - rr + k * rr), new Vector2(x + w - rr + k * rr, y + h), new Vector2(x + w - rr, y + h));
        pb.LineTo(new PointF(x + rr, y + h));
        pb.CubicBezierTo(new Vector2(x + rr - k * rr, y + h), new Vector2(x, y + h - rr + k * rr), new Vector2(x, y + h - rr));
        pb.LineTo(new PointF(x, y + rr));
        pb.CubicBezierTo(new Vector2(x, y + rr - k * rr), new Vector2(x + rr - k * rr, y), new Vector2(x + rr, y));
        pb.CloseFigure();
        return pb.Build();
    }

    /// <summary>Rounded top corners only; bottom edge square (badge header band).</summary>
    private static IPath TopRoundedRectPath(float x, float y, float w, float h, float radius)
    {
        if (w <= 0 || h <= 0)
            return new RectangularPolygon(x, y, Math.Max(0, w), Math.Max(0, h));

        var rr = Math.Min(radius, Math.Min(w, h) / 2f);
        if (rr <= 0.5f)
            return new RectangularPolygon(x, y, w, h);

        const float k = 0.5522847498f;
        var pb = new PathBuilder();
        pb.MoveTo(new PointF(x + rr, y));
        pb.LineTo(new PointF(x + w - rr, y));
        pb.CubicBezierTo(new Vector2(x + w - rr + k * rr, y), new Vector2(x + w, y + rr - k * rr), new Vector2(x + w, y + rr));
        pb.LineTo(new PointF(x + w, y + h));
        pb.LineTo(new PointF(x, y + h));
        pb.LineTo(new PointF(x, y + rr));
        pb.CubicBezierTo(new Vector2(x, y + rr - k * rr), new Vector2(x + rr - k * rr, y), new Vector2(x + rr, y));
        pb.CloseFigure();
        return pb.Build();
    }

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

    private static string FontCacheKey(float sizePx, string? family)
    {
        var halfPoints = Math.Clamp((int)Math.Round(sizePx * 2), 1, 20000);
        var fam = string.IsNullOrWhiteSpace(family) ? "" : family.Trim();
        return $"{fam}|{halfPoints}";
    }

    private static Font GetOrCreateFont(float sizePx, string? family)
    {
        var key = FontCacheKey(sizePx, family);
        lock (s_fontCacheLock) {
            if (s_fontCache.TryGetValue(key, out var cached))
                return cached;

            while (s_fontCache.Count >= MaxFontCacheEntries && s_fontCacheOrder.Count > 0) {
                var evictKey = s_fontCacheOrder.Dequeue();
                s_fontCache.Remove(evictKey);
            }

            var font = CreateFont(sizePx, family);
            s_fontCache[key] = font;
            s_fontCacheOrder.Enqueue(key);
            return font;
        }
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

    /// <summary>Pixel sizes derived from QR side length so chrome stays visible when the PNG is huge (e.g. 256 px per module).</summary>
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

        /// <summary>Height reserved for wrapped footer caption (border style).</summary>
        public int CaptionBlockHeightPx { get; init; }

        public static ScaledChromeLayout FromOptions(int s, QrFrameLayoutOptions o)
        {
            // Fractions of QR side — keeps header, borders, and text a stable share of the image when s is 500 or 8000 px.
            var pad = Math.Max(o.PaddingAroundQrPx, (int)(s * 0.045));
            var margin = Math.Max(o.OuterMarginPx, (int)(s * 0.035));
            var r = Math.Clamp(o.CornerRadiusPx * (s / 400f), 6f, s * 0.12f);
            var shadowOff = o.ShadowOffsetPx <= 0 ? 0 : Math.Clamp(Math.Max(o.ShadowOffsetPx, (int)(s * 0.012)), 4, 80);
            var notchD = o.DrawHeaderNotch ? Math.Clamp(Math.Max(o.NotchDepthPx, (int)(s * 0.014)), 6, Math.Min(80, (int)(s * 0.04))) : 0;
            var notchW = Math.Clamp(Math.Max(o.NotchWidthPx, (int)(s * 0.08)), 16, (int)(s * 0.35));
            var outline = Math.Clamp(Math.Max(o.CardOutlineWidthPx, s * 0.0075f), 3f, 72f);
            var borderStroke = Math.Clamp(Math.Max(o.BorderStrokeWidthPx, s * 0.011f), 5f, 120f);
            var cardW = s + 2 * pad;

            // Scale-aware caption cap (replaces fixed 220px ceiling so huge rasters get readable type).
            var captionMaxBadge = Math.Min(s * 0.14f, 2048f);
            var captionMaxBorder = Math.Min(s * 0.12f, 2048f);
            var captionMax = o.Style == QrFrameStyle.BorderOnly ? captionMaxBorder : captionMaxBadge;
            // Readable header/footer text at typical web QR sizes (~400–900px side): prior 2.6% of s was often ~15px.
            var autoCaption = Math.Clamp(Math.Max(22f, s * 0.048f), 20f, captionMax);
            var caption = o.CaptionFontSizePx > 0 ? Math.Clamp(o.CaptionFontSizePx, 8f, captionMax) : autoCaption;

            var footerPad = Math.Max(o.CaptionFooterPaddingPx, (int)(s * 0.02));
            var footerGap = Math.Max(8, (int)(s * 0.01));
            var captionTrim = o.CaptionText?.Trim() ?? "";
            var hasCaption = captionTrim.Length > 0;
            var innerPadForMeasure = Math.Max((int)(s * 0.012), 6);
            var maxTextWidth = Math.Max(40f, cardW - 2f * innerPadForMeasure);

            var headerMax = Math.Max(64, Math.Min((int)(s * 0.42), 3200));
            var headerMinUser = o.HeaderHeightPx > 0 ? o.HeaderHeightPx : 52;
            var headerScaled = Math.Clamp(Math.Max(headerMinUser, (int)(s * 0.16)), 32, headerMax);

            var captionBlockH = hasCaption ? EstimateCaptionBlockHeight(captionTrim, caption, maxTextWidth, o.FontFamily) : 0;

            var headerH = headerScaled;
            if (o.Style == QrFrameStyle.BadgeWithHeader && o.AutoSizeHeaderToCaption && hasCaption) {
                var innerPad = Math.Max((int)(s * 0.012), 6);
                var headerForText = captionBlockH + innerPad * 2;
                headerH = Math.Clamp(Math.Max(headerScaled, headerForText), 32, headerMax);
            }

            return new() {
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
                FooterCaptionGapPx = footerGap,
                CaptionBlockHeightPx = captionBlockH
            };
        }
    }
}
