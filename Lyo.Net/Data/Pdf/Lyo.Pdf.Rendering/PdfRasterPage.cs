using System.Diagnostics;

namespace Lyo.Pdf.Rendering;

/// <summary>One PDF page rasterized as a PNG bitmap.</summary>
/// <param name="PngBytes">PNG-encoded pixels.</param>
/// <param name="WidthPx">Bitmap width in pixels.</param>
/// <param name="HeightPx">Bitmap height in pixels.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record PdfRasterPage(byte[] PngBytes, int WidthPx, int HeightPx)
{
    /// <summary>Pixel size and PNG length. Image bytes are omitted.</summary>
    public override string ToString() => $"{WidthPx}x{HeightPx} PngBytes={PngBytes?.Length ?? 0}";
}