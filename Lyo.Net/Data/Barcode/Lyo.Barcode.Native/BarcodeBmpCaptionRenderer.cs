using Lyo.Barcode.Models;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Lyo.Barcode.Native;

/// <summary>BMP export with human-readable caption uses ImageSharp (vector BMP path is bar rows only).</summary>
internal static class BarcodeBmpCaptionRenderer
{
    public static byte[] Render(bool[] modules, string caption, BarcodeOptions options)
    {
        var quiet = BarcodeImageRenderer.ResolveQuietZoneModules(options.QuietZoneModules);
        var fullModules = modules.Length + 2 * quiet;
        var moduleW = options.ModuleWidthPixels;
        var barH = options.BarHeightPixels;
        var quietPx = quiet * moduleW;
        var innerW = fullModules * moduleW;
        var marginTop = options.HumanReadableMarginTopPixels;
        var marginBot = options.HumanReadableMarginBottomPixels;
        var fontPx = options.HumanReadableFontSizePixels;
        var captionBand = marginTop + fontPx + marginBot;
        var innerH = 2 * quietPx + barH + captionBand;
        var b = BarcodeImageRenderer.GetBorderPixels(options);
        var width = innerW + 2 * b;
        var height = innerH + 2 * b;

        BarcodeImageRenderer.ParseRgb(options.LightColor, out var lr, out var lg, out var lb);
        BarcodeImageRenderer.ParseRgb(options.DarkColor, out var dr, out var dg, out var db);
        BarcodeImageRenderer.ParseRgb(options.BorderColorHex, out var borR, out var borG, out var borB);
        var light = Color.FromRgb(lr, lg, lb);
        var dark = Color.FromRgb(dr, dg, db);
        var borderCol = Color.FromRgb(borR, borG, borB);
        var capHex = string.IsNullOrWhiteSpace(options.HumanReadableColorHex) ? options.DarkColor : options.HumanReadableColorHex!;
        BarcodeImageRenderer.ParseRgb(capHex, out var cr, out var cg, out var cb);
        var ink = Color.FromRgb(cr, cg, cb);

        using var img = new Image<Rgba32>(width, height);
        img.Mutate(ctx => {
            ctx.Fill(borderCol);
            ctx.Fill(light, new Rectangle(b, b, innerW, innerH));
            var ox = b;
            var oy = b;
            var mx = 0;
            while (mx < fullModules) {
                var srcX = mx - quiet;
                var on = srcX >= 0 && srcX < modules.Length && modules[srcX];
                if (!on) {
                    mx++;
                    continue;
                }

                var runStart = mx;
                while (mx < fullModules) {
                    var sx = mx - quiet;
                    var o = sx >= 0 && sx < modules.Length && modules[sx];
                    if (!o)
                        break;

                    mx++;
                }

                var runLen = mx - runStart;
                var x = ox + runStart * moduleW;
                var rw = runLen * moduleW;
                ctx.Fill(dark, new Rectangle(x, oy + quietPx, rw, barH));
            }

            var font = CreateFont(fontPx);
            var textOpts = new RichTextOptions(font) {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Origin = new PointF(width / 2f, oy + quietPx + barH + marginTop)
            };

            ctx.DrawText(textOpts, caption, Brushes.Solid(ink), null);
        });

        using var ms = new MemoryStream();
        img.Save(ms, new BmpEncoder());
        return ms.ToArray();
    }

    private static Font CreateFont(float sizePx)
    {
        foreach (var n in new[] { "DejaVu Sans", "Liberation Sans", "Arial", "Helvetica" }) {
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
