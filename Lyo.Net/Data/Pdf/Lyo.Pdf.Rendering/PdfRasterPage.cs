namespace Lyo.Pdf.Rendering;

/// <summary>One PDF page rasterized as a PNG bitmap.</summary>
/// <param name="PngBytes">PNG-encoded pixels.</param>
/// <param name="WidthPx">Bitmap width in pixels.</param>
/// <param name="HeightPx">Bitmap height in pixels.</param>
public sealed record PdfRasterPage(byte[] PngBytes, int WidthPx, int HeightPx);