using Lyo.Images.Models;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Lyo.Images.Decoration;

/// <summary>Stacks a caption band above or below the input image, optionally with a downward notch and rounded outside corners.</summary>
internal static class CaptionDrawer
{
    /// <summary>
    /// Returns a new image whose canvas is taller than the input by the resolved band height. The caller is responsible for disposing the input and the returned image
    /// independently.
    /// </summary>
    public static Image<Rgba32> Apply(Image<Rgba32> input, CaptionOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Text))
            throw new ArgumentException("Caption text is required.", nameof(options));

        var w = input.Width;
        var h = input.Height;
        var side = Math.Min(w, h);
        var captionTrim = options.Text.Trim();
        var fontSizePx = ResolveFontSizePx(options, side);
        var maxTextWidth = Math.Max(40f, w - 2f * Math.Max((int)(side * 0.012), 6));
        var captionBlockH = EstimateCaptionBlockHeight(captionTrim, fontSizePx, maxTextWidth, options.FontFamily);
        var headerMin = options.BandHeightPx > 0 ? options.BandHeightPx : 52;
        var headerScaled = Math.Clamp(Math.Max(headerMin, (int)(side * 0.16)), 32, Math.Max(64, Math.Min((int)(side * 0.42), 3200)));
        var bandH = headerScaled;
        if (options.AutoSizeToCaption) {
            var innerPad = Math.Max((int)(side * 0.012), 6);
            var bandForText = captionBlockH + innerPad * 2;
            bandH = Math.Clamp(Math.Max(headerScaled, bandForText), 32, Math.Max(64, Math.Min((int)(side * 0.42), 3200)));
        }

        var notchDepth = options.DrawNotch ? Math.Clamp(Math.Max(options.NotchDepthPx, (int)(side * 0.014)), 6, Math.Min(80, (int)(side * 0.04))) : 0;
        var notchWidth = options.DrawNotch ? Math.Clamp(Math.Max(options.NotchWidthPx, (int)(side * 0.08)), 16, (int)(side * 0.35)) : 0;
        var canvasW = w;
        var canvasH = h + bandH + notchDepth;
        var imageY = options.Placement == CaptionPlacement.HeaderAbove ? bandH + notchDepth : 0;
        var bandY = options.Placement == CaptionPlacement.HeaderAbove ? 0 : h;
        var notchY = options.Placement == CaptionPlacement.HeaderAbove ? bandH : bandY - notchDepth;
        var bg = DecorationGeometry.ParseColorOr(options.BackgroundColorHex, Color.Parse("#1e293b"));
        var fg = DecorationGeometry.ParseColorOr(options.TextColorHex, Color.White);
        var image = new Image<Rgba32>(canvasW, canvasH);
        image.Mutate(ctx => {
            var bandPath = options.CornerRadiusPx > 0
                ? options.Placement == CaptionPlacement.HeaderAbove
                    ? DecorationGeometry.TopRoundedRectPath(0, bandY, canvasW, bandH, options.CornerRadiusPx)
                    : DecorationGeometry.BottomRoundedRectPath(0, bandY, canvasW, bandH, options.CornerRadiusPx)
                : new RectangularPolygon(0, bandY, canvasW, bandH);

            ctx.Fill(bg, bandPath);
            if (options.DrawNotch && notchDepth > 0) {
                var cx = canvasW / 2f;
                var nw = Math.Clamp(notchWidth, 8, canvasW);
                var nb = new PathBuilder();
                if (options.Placement == CaptionPlacement.HeaderAbove) {
                    nb.MoveTo(new(cx - nw / 2f, notchY));
                    nb.LineTo(new(cx + nw / 2f, notchY));
                    nb.LineTo(new(cx, notchY + notchDepth));
                }
                else {
                    nb.MoveTo(new(cx - nw / 2f, notchY + notchDepth));
                    nb.LineTo(new(cx + nw / 2f, notchY + notchDepth));
                    nb.LineTo(new(cx, notchY));
                }

                nb.CloseFigure();
                ctx.Fill(bg, nb.Build());
            }

            var font = FontCache.GetOrCreate(fontSizePx, options.FontFamily);
            var textOpts = new RichTextOptions(font) {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Origin = new(canvasW / 2f, bandY + bandH / 2f),
                WrappingLength = maxTextWidth
            };

            ctx.DrawText(textOpts, captionTrim, Brushes.Solid(fg), null);
            ctx.DrawImage(input, new Point(0, imageY), 1f);
        });

        return image;
    }

    private static float ResolveFontSizePx(CaptionOptions options, int side)
    {
        var captionMax = Math.Min(side * 0.14f, 2048f);
        var autoCaption = Math.Clamp(Math.Max(22f, side * 0.048f), 20f, captionMax);
        return options.FontSizePx > 0 ? Math.Clamp(options.FontSizePx, 8f, captionMax) : autoCaption;
    }

    private static int EstimateCaptionBlockHeight(string text, float fontSizePx, float maxWidthPx, string? fontFamily)
    {
        if (string.IsNullOrEmpty(text) || fontSizePx <= 0 || maxWidthPx < 8f)
            return (int)Math.Ceiling(fontSizePx * 1.35);

        try {
            var font = FontCache.GetOrCreate(fontSizePx, fontFamily);
            var opts = new TextOptions(font) { WrappingLength = maxWidthPx };
            return Math.Max(1, (int)Math.Ceiling(TextMeasurer.MeasureSize(text, opts).Height));
        }
        catch {
            return (int)Math.Ceiling(fontSizePx * 1.35);
        }
    }
}