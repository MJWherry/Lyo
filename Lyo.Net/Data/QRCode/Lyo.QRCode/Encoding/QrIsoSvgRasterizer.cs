using Lyo.QRCode.Encoding.Iso;

namespace Lyo.QRCode.Encoding;

internal static class QrIsoSvgRasterizer
{
    public static string ToSvg(QrIsoMatrix qr, int pixelsPerModule, string darkHex, string lightHex, bool drawQuietZones)
    {
        var matrix = qr.ModuleMatrix;
        var matrixSize = matrix.Count - (drawQuietZones ? 0 : 8);
        var qz = drawQuietZones ? 0 : 4;
        var w = matrixSize * pixelsPerModule;
        var sb = new StringBuilder(256 + matrixSize * matrixSize / 2);
        sb.Append(@"<svg xmlns=""http://www.w3.org/2000/svg"" ");
        sb.Append("width=\"").Append(w).Append("\" height=\"").Append(w).Append("\" ");
        sb.Append("viewBox=\"0 0 ").Append(w).Append(' ').Append(w).Append("\">\n");
        sb.Append("  <rect width=\"100%\" height=\"100%\" fill=\"").Append(lightHex).Append("\"/>\n");
        for (var y = 0; y < matrixSize; y++) {
            var row = matrix[y + qz];
            for (var x = 0; x < matrixSize; x++) {
                if (!row[x + qz])
                    continue;

                sb.Append("  <rect x=\"")
                    .Append(x * pixelsPerModule)
                    .Append("\" y=\"")
                    .Append(y * pixelsPerModule)
                    .Append("\" width=\"")
                    .Append(pixelsPerModule)
                    .Append("\" height=\"")
                    .Append(pixelsPerModule)
                    .Append("\" fill=\"")
                    .Append(darkHex)
                    .Append("\"/>\n");
            }
        }

        sb.Append("</svg>");
        return sb.ToString();
    }
}