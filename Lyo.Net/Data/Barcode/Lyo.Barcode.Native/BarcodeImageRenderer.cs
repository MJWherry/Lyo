using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using Lyo.Barcode.Models;

namespace Lyo.Barcode.Native;

internal static class BarcodeImageRenderer
{
    /// <summary>ISO/IEC 15417 quiet zone: at least 10× the width of the narrowest bar (here, one module).</summary>
    internal const int IsoMinimumQuietZoneModules = 10;

    internal static byte[] Render(bool[] modules, BarcodeOptions options)
        => options.Format switch {
            BarcodeFormat.Bmp => RenderBmp(modules, options),
            BarcodeFormat.Svg => Encoding.UTF8.GetBytes(RenderSvg(modules, options)),
            var _ => RenderBmp(modules, options)
        };

    internal static string RenderSvg(bool[] modules, BarcodeOptions options)
    {
        var quiet = ResolveQuietZoneModules(options.QuietZoneModules);
        var fullModules = modules.Length + 2 * quiet;
        var moduleW = options.ModuleWidthPixels;
        var barH = options.BarHeightPixels;
        var quietPx = quiet * moduleW;
        var width = fullModules * moduleW;
        var height = barH + 2 * quietPx;
        ParseRgb(options.DarkColor, out var dr, out var dg, out var db);
        ParseRgb(options.LightColor, out var lr, out var lg, out var lb);
        var dark = FormattableString.Invariant($"#{dr:X2}{dg:X2}{db:X2}");
        var light = FormattableString.Invariant($"#{lr:X2}{lg:X2}{lb:X2}");
        var sb = new StringBuilder(256 + modules.Length * 8);
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" ");
        sb.Append("width=\"").Append(width.ToString(CultureInfo.InvariantCulture)).Append("\" ");
        sb.Append("height=\"").Append(height.ToString(CultureInfo.InvariantCulture)).Append("\" ");
        sb.Append("viewBox=\"0 0 ").Append(width.ToString(CultureInfo.InvariantCulture)).Append(' ').Append(height.ToString(CultureInfo.InvariantCulture)).Append("\">");
        sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"").Append(light).Append("\"/>");
        for (var mx = 0; mx < fullModules; mx++) {
            var srcX = mx - quiet;
            var on = srcX >= 0 && srcX < modules.Length && modules[srcX];
            if (!on)
                continue;

            var x = mx * moduleW;
            sb.Append("<rect x=\"")
                .Append(x.ToString(CultureInfo.InvariantCulture))
                .Append("\" y=\"")
                .Append(quietPx.ToString(CultureInfo.InvariantCulture))
                .Append("\" width=\"")
                .Append(moduleW.ToString(CultureInfo.InvariantCulture))
                .Append("\" height=\"")
                .Append(barH.ToString(CultureInfo.InvariantCulture))
                .Append("\" fill=\"")
                .Append(dark)
                .Append("\"/>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    /// <summary>Standard bottom-up BMP (positive <c>biHeight</c>) for broad decoder and viewer compatibility; enforces minimum quiet zone.</summary>
    private static byte[] RenderBmp(bool[] modules, BarcodeOptions options)
    {
        var quiet = ResolveQuietZoneModules(options.QuietZoneModules);
        var fullModules = modules.Length + 2 * quiet;
        var moduleW = options.ModuleWidthPixels;
        var barH = options.BarHeightPixels;
        var quietPx = quiet * moduleW;
        var widthPx = fullModules * moduleW;
        var heightPx = barH + 2 * quietPx;
        ParseRgb(options.DarkColor, out var dr, out var dg, out var db);
        ParseRgb(options.LightColor, out var lr, out var lg, out var lb);
        var rowStride = (widthPx * 3 + 3) & ~3;
        var pixelSize = rowStride * heightPx;
        var fileSize = 14 + 40 + pixelSize;
        using var ms = new MemoryStream(fileSize);

        // BITMAPFILEHEADER
        Span<byte> fileHeader = stackalloc byte[14];
        fileHeader[0] = (byte)'B';
        fileHeader[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(fileHeader.Slice(2), fileSize);
        BinaryPrimitives.WriteInt32LittleEndian(fileHeader.Slice(6), 0);
        BinaryPrimitives.WriteInt32LittleEndian(fileHeader.Slice(10), 14 + 40);
        ms.Write(fileHeader);

        // Bottom-up DIB: positive height, first scanline in file = bottom row of the image.
        Span<byte> infoHeader = stackalloc byte[40];
        BinaryPrimitives.WriteInt32LittleEndian(infoHeader.Slice(0), 40);
        BinaryPrimitives.WriteInt32LittleEndian(infoHeader.Slice(4), widthPx);
        BinaryPrimitives.WriteInt32LittleEndian(infoHeader.Slice(8), heightPx);
        BinaryPrimitives.WriteInt16LittleEndian(infoHeader.Slice(12), 1);
        BinaryPrimitives.WriteInt16LittleEndian(infoHeader.Slice(14), 24);
        BinaryPrimitives.WriteInt32LittleEndian(infoHeader.Slice(16), 0);
        BinaryPrimitives.WriteInt32LittleEndian(infoHeader.Slice(20), pixelSize);
        BinaryPrimitives.WriteInt32LittleEndian(infoHeader.Slice(24), 0);
        BinaryPrimitives.WriteInt32LittleEndian(infoHeader.Slice(28), 0);
        BinaryPrimitives.WriteInt32LittleEndian(infoHeader.Slice(32), 0);
        BinaryPrimitives.WriteInt32LittleEndian(infoHeader.Slice(36), 0);
        ms.Write(infoHeader);
        var rowBuffer = new byte[rowStride];
        var padStart = widthPx * 3;
        var padLen = rowStride - padStart;
        for (var fileRow = 0; fileRow < heightPx; fileRow++) {
            var y = heightPx - 1 - fileRow;
            var inBarBand = y >= quietPx && y < quietPx + barH;
            for (var x = 0; x < widthPx; x++) {
                byte b, g, r;
                if (inBarBand) {
                    var mx = x / moduleW;
                    var srcX = mx - quiet;
                    var on = srcX >= 0 && srcX < modules.Length && modules[srcX];
                    if (on)
                        (b, g, r) = (db, dg, dr);
                    else
                        (b, g, r) = (lb, lg, lr);
                }
                else
                    (b, g, r) = (lb, lg, lr);

                var o = x * 3;
                rowBuffer[o] = b;
                rowBuffer[o + 1] = g;
                rowBuffer[o + 2] = r;
            }

            if (padLen > 0)
                rowBuffer.AsSpan(padStart, padLen).Clear();

            ms.Write(rowBuffer);
        }

        return ms.ToArray();
    }

    internal static int ResolveQuietZoneModules(int requested) => Math.Max(IsoMinimumQuietZoneModules, Math.Max(0, requested));

    private static void ParseRgb(string hex, out byte r, out byte g, out byte b)
    {
        var s = hex.Trim();
        if (s.StartsWith('#'))
            s = s.Substring(1);

        if (s.Length == 3) {
            r = (byte)(Convert.ToByte(s.Substring(0, 1), 16) * 17);
            g = (byte)(Convert.ToByte(s.Substring(1, 1), 16) * 17);
            b = (byte)(Convert.ToByte(s.Substring(2, 1), 16) * 17);
            return;
        }

        if (s.Length == 6) {
            r = Convert.ToByte(s.Substring(0, 2), 16);
            g = Convert.ToByte(s.Substring(2, 2), 16);
            b = Convert.ToByte(s.Substring(4, 2), 16);
            return;
        }

        r = g = b = 0;
    }
}