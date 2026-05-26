using Lyo.Images.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Lyo.Images.Decoration;

/// <summary>Wraps the input image in an outer canvas with optional rounded card fill and drop shadow.</summary>
internal static class OuterPaddingDrawer
{
    /// <summary>Returns a new image whose canvas is larger than the input by margin + padding + shadow offset on every side.</summary>
    public static Image<Rgba32> Apply(Image<Rgba32> input, PaddingOptions options)
    {
        var w = input.Width;
        var h = input.Height;
        var pad = Math.Max(0, options.PaddingPx);
        var margin = Math.Max(0, options.MarginPx);
        var radius = Math.Max(0, options.CornerRadiusPx);
        var shadowOff = string.IsNullOrWhiteSpace(options.ShadowColorHex) ? 0 : Math.Max(0, options.ShadowOffsetPx);
        var cardW = w + 2 * pad;
        var cardH = h + 2 * pad;
        var canvasW = cardW + 2 * margin + shadowOff;
        var canvasH = cardH + 2 * margin + shadowOff;
        var cardX = margin;
        var cardY = margin;
        var imageX = cardX + pad;
        var imageY = cardY + pad;
        var canvas = DecorationGeometry.ParseColorOr(options.CanvasColorHex, Color.White);
        var panel = DecorationGeometry.ParseColorOr(options.PanelColorHex, Color.White);
        var image = new Image<Rgba32>(canvasW, canvasH);
        image.Mutate(ctx => {
            ctx.Fill(canvas);
            if (shadowOff > 0 && DecorationGeometry.TryParseColor(options.ShadowColorHex, out var shadow)) {
                var shadowPath = DecorationGeometry.RoundedRectPath(cardX + shadowOff, cardY + shadowOff, cardW, cardH, radius);
                ctx.Fill(shadow, shadowPath);
            }

            var cardPath = DecorationGeometry.RoundedRectPath(cardX, cardY, cardW, cardH, radius);
            ctx.Fill(panel, cardPath);
            ctx.DrawImage(input, new Point(imageX, imageY), 1f);
        });

        return image;
    }
}
