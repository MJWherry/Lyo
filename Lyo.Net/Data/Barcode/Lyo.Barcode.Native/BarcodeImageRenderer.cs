using System.Buffers.Binary;
using System.Globalization;
using System.Security;
using System.Text;
using Lyo.Barcode.Models;

namespace Lyo.Barcode.Native;

internal static class BarcodeImageRenderer
{
    /// <summary>ISO/IEC 15417 quiet zone: at least 10× the width of the narrowest bar (here, one module).</summary>
    internal const int IsoMinimumQuietZoneModules = 10;

    internal static byte[] Render(bool[] modules, string encodedData, BarcodeOptions options)
    {
        var caption = ResolveCaptionText(options, encodedData);
        var hasCaption = options.ShowHumanReadableTextBelow && !string.IsNullOrEmpty(caption);
        return options.Format switch {
            BarcodeFormat.Bmp => hasCaption ? BarcodeBmpCaptionRenderer.Render(modules, caption!, options) : RenderBmp(modules, options),
            BarcodeFormat.Svg => Encoding.UTF8.GetBytes(RenderSvg(modules, caption, hasCaption, options)),
            var _ => hasCaption ? BarcodeBmpCaptionRenderer.Render(modules, caption!, options) : RenderBmp(modules, options)
        };
    }

    internal static int GetBorderPixels(BarcodeOptions options)
        => options.ShowBorder ? Math.Max(1, options.BorderWidthPixels) : 0;

    internal static (int WidthPx, int HeightPx) MeasureDimensions(int moduleCount, string encodedData, BarcodeOptions options)
    {
        var caption = ResolveCaptionText(options, encodedData);
        var hasCaption = options.ShowHumanReadableTextBelow && !string.IsNullOrEmpty(caption);
        var quiet = ResolveQuietZoneModules(options.QuietZoneModules);
        var fullModules = moduleCount + 2 * quiet;
        var innerW = fullModules * options.ModuleWidthPixels;
        var quietPx = quiet * options.ModuleWidthPixels;
        var captionBand = hasCaption
            ? options.HumanReadableMarginTopPixels + options.HumanReadableFontSizePixels + options.HumanReadableMarginBottomPixels
            : 0;

        var innerH = options.BarHeightPixels + 2 * quietPx + captionBand;
        var b = GetBorderPixels(options);
        return (innerW + 2 * b, innerH + 2 * b);
    }

    internal static string? ResolveCaptionText(BarcodeOptions options, string encodedData)
    {
        if (!options.ShowHumanReadableTextBelow)
            return null;

        var t = options.HumanReadableText?.Trim();
        return string.IsNullOrEmpty(t) ? encodedData : t;
    }

    private static string RenderSvg(bool[] modules, string? caption, bool hasCaption, BarcodeOptions options)
    {
        var quiet = ResolveQuietZoneModules(options.QuietZoneModules);
        var fullModules = modules.Length + 2 * quiet;
        var moduleW = options.ModuleWidthPixels;
        var barH = options.BarHeightPixels;
        var quietPx = quiet * moduleW;
        var innerW = fullModules * moduleW;
        var captionBand = hasCaption
            ? options.HumanReadableMarginTopPixels + options.HumanReadableFontSizePixels + options.HumanReadableMarginBottomPixels
            : 0;

        var innerH = barH + 2 * quietPx + captionBand;
        var b = GetBorderPixels(options);
        var outerW = innerW + 2 * b;
        var outerH = innerH + 2 * b;
        ParseRgb(options.DarkColor, out var dr, out var dg, out var db);
        ParseRgb(options.LightColor, out var lr, out var lg, out var lb);
        ParseRgb(options.BorderColorHex, out var borR, out var borG, out var borB);
        var dark = FormattableString.Invariant($"#{dr:X2}{dg:X2}{db:X2}");
        var light = FormattableString.Invariant($"#{lr:X2}{lg:X2}{lb:X2}");
        var borderHex = FormattableString.Invariant($"#{borR:X2}{borG:X2}{borB:X2}");
        var sb = new StringBuilder(256 + modules.Length * 8);
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" ");
        sb.Append("width=\"").Append(outerW.ToString(CultureInfo.InvariantCulture)).Append("\" ");
        sb.Append("height=\"").Append(outerH.ToString(CultureInfo.InvariantCulture)).Append("\" ");
        sb.Append("viewBox=\"0 0 ").Append(outerW.ToString(CultureInfo.InvariantCulture)).Append(' ').Append(outerH.ToString(CultureInfo.InvariantCulture)).Append("\">");
        sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"").Append(borderHex).Append("\"/>");
        sb.Append("<rect x=\"")
            .Append(b.ToString(CultureInfo.InvariantCulture))
            .Append("\" y=\"")
            .Append(b.ToString(CultureInfo.InvariantCulture))
            .Append("\" width=\"")
            .Append(innerW.ToString(CultureInfo.InvariantCulture))
            .Append("\" height=\"")
            .Append(innerH.ToString(CultureInfo.InvariantCulture))
            .Append("\" fill=\"")
            .Append(light)
            .Append("\"/>");
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
            sb.Append("<rect x=\"")
                .Append(x.ToString(CultureInfo.InvariantCulture))
                .Append("\" y=\"")
                .Append((oy + quietPx).ToString(CultureInfo.InvariantCulture))
                .Append("\" width=\"")
                .Append(rw.ToString(CultureInfo.InvariantCulture))
                .Append("\" height=\"")
                .Append(barH.ToString(CultureInfo.InvariantCulture))
                .Append("\" fill=\"")
                .Append(dark)
                .Append("\"/>");
        }

        if (hasCaption) {
            var capHex = string.IsNullOrWhiteSpace(options.HumanReadableColorHex) ? options.DarkColor : options.HumanReadableColorHex!;
            ParseRgb(capHex, out var cr, out var cg, out var cb);
            var cap = FormattableString.Invariant($"#{cr:X2}{cg:X2}{cb:X2}");
            var fs = options.HumanReadableFontSizePixels.ToString(CultureInfo.InvariantCulture);
            var ty = (oy + quietPx + barH + options.HumanReadableMarginTopPixels + options.HumanReadableFontSizePixels).ToString(CultureInfo.InvariantCulture);
            var tx = (ox + innerW / 2f).ToString(CultureInfo.InvariantCulture);
            sb.Append("<text xml:space=\"preserve\" x=\"")
                .Append(tx)
                .Append("\" y=\"")
                .Append(ty)
                .Append("\" text-anchor=\"middle\" font-family=\"DejaVu Sans, Liberation Sans, monospace\" font-size=\"")
                .Append(fs)
                .Append("\" fill=\"")
                .Append(cap)
                .Append("\">")
                .Append(SecurityElement.Escape(caption))
                .Append("</text>");
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
        var innerW = fullModules * moduleW;
        var innerH = barH + 2 * quietPx;
        var borderPx = GetBorderPixels(options);
        var widthPx = innerW + 2 * borderPx;
        var heightPx = innerH + 2 * borderPx;
        ParseRgb(options.DarkColor, out var dr, out var dg, out var db);
        ParseRgb(options.LightColor, out var lr, out var lg, out var lb);
        ParseRgb(options.BorderColorHex, out var brR, out var brG, out var brB);
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
        var padStart = widthPx * 3;
        var padLen = rowStride - padStart;

        var quietInner = new byte[innerW * 3];
        for (var x = 0; x < innerW; x++) {
            var o = x * 3;
            quietInner[o] = lb;
            quietInner[o + 1] = lg;
            quietInner[o + 2] = lr;
        }

        var barInner = new byte[innerW * 3];
        for (var modX = 0; modX < fullModules; modX++) {
            var srcX = modX - quiet;
            var on = srcX >= 0 && srcX < modules.Length && modules[srcX];
            var (bb, gg, rr) = on ? (db, dg, dr) : (lb, lg, lr);
            var x0 = modX * moduleW;
            for (var i = 0; i < moduleW && x0 + i < innerW; i++) {
                var o = (x0 + i) * 3;
                barInner[o] = bb;
                barInner[o + 1] = gg;
                barInner[o + 2] = rr;
            }
        }

        var compositeRow = new byte[rowStride];
        for (var fileRow = 0; fileRow < heightPx; fileRow++) {
            var y = heightPx - 1 - fileRow;
            if (y < borderPx || y >= innerH + borderPx) {
                FillBorderRow(compositeRow, widthPx, rowStride, padLen, padStart, brB, brG, brR);
                ms.Write(compositeRow);
                continue;
            }

            var innerY = y - borderPx;
            var inBarBand = innerY >= quietPx && innerY < quietPx + barH;
            var srcInner = inBarBand ? barInner : quietInner;
            CompositeInnerRowWithBorder(compositeRow, srcInner, innerW, borderPx, widthPx, rowStride, padLen, padStart, brB, brG, brR);
            ms.Write(compositeRow);
        }

        return ms.ToArray();
    }

    private static void FillBorderRow(byte[] row, int widthPx, int rowStride, int padLen, int padStart, byte b, byte g, byte r)
    {
        for (var x = 0; x < widthPx; x++) {
            var o = x * 3;
            row[o] = b;
            row[o + 1] = g;
            row[o + 2] = r;
        }

        if (padLen > 0)
            row.AsSpan(padStart, padLen).Clear();
    }

    private static void CompositeInnerRowWithBorder(
        byte[] row,
        byte[] innerRgb,
        int innerW,
        int borderPx,
        int widthPx,
        int rowStride,
        int padLen,
        int padStart,
        byte brB,
        byte brG,
        byte brR)
    {
        var o = 0;
        for (var i = 0; i < borderPx; i++) {
            row[o++] = brB;
            row[o++] = brG;
            row[o++] = brR;
        }

        Buffer.BlockCopy(innerRgb, 0, row, o, innerW * 3);
        o += innerW * 3;
        for (var i = 0; i < borderPx; i++) {
            row[o++] = brB;
            row[o++] = brG;
            row[o++] = brR;
        }

        if (padLen > 0)
            row.AsSpan(padStart, padLen).Clear();
    }

    internal static int ResolveQuietZoneModules(int requested) => Math.Max(IsoMinimumQuietZoneModules, Math.Max(0, requested));

    internal static void ParseRgb(string hex, out byte r, out byte g, out byte b)
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