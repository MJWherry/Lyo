using Lyo.Images.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Lyo.Images.Decoration;

/// <summary>Wraps the input image in a stroked outline (with optional rounded corners and inner fill) and returns the expanded canvas as a new image.</summary>
internal static class FrameDrawer
{
    /// <summary>Returns a newly-allocated image (caller disposes the original separately) larger than the input by stroke + padding on every side.</summary>
    public static Image<Rgba32> Apply(Image<Rgba32> input, FrameOptions options)
    {
        var w = input.Width;
        var h = input.Height;
        var stroke = Math.Max(0, options.StrokeWidthPx);
        var pad = Math.Max(0, options.PaddingPx);
        var radius = Math.Max(0, options.CornerRadiusPx);
        var canvasW = w + 2 * (stroke + pad);
        var canvasH = h + 2 * (stroke + pad);
        var image = new Image<Rgba32>(canvasW, canvasH);
        var imageX = stroke + pad;
        var imageY = stroke + pad;
        var strokeColor = DecorationGeometry.ParseColorOr(options.StrokeColorHex, Color.Black);
        var hasFill = !string.IsNullOrWhiteSpace(options.FillColorHex) && DecorationGeometry.TryParseColor(options.FillColorHex, out var fillColor);
        image.Mutate(ctx => {
            if (hasFill) {
                var fillPath = DecorationGeometry.RoundedRectPath(stroke / 2f, stroke / 2f, canvasW - stroke, canvasH - stroke, Math.Max(0, radius - stroke / 2f));
                DecorationGeometry.TryParseColor(options.FillColorHex, out var fc);
                ctx.Fill(fc, fillPath);
            }

            ctx.DrawImage(input, new Point(imageX, imageY), 1f);
            if (stroke > 0) {
                var strokePath = DecorationGeometry.RoundedRectPath(stroke / 2f, stroke / 2f, canvasW - stroke, canvasH - stroke, Math.Max(0, radius - stroke / 2f));
                ctx.Draw(Pens.Solid(strokeColor, stroke), strokePath);
            }
        });

        return image;
    }
}