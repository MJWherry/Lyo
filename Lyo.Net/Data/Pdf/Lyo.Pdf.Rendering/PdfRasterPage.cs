namespace Lyo.Pdf.Rendering;

/// <summary>A single PDF page rendered to a PNG bitmap.</summary>
/// <param name="PngBytes">PNG-encoded raster.</param>
/// <param name="WidthPx">Bitmap width in pixels.</param>
/// <param name="HeightPx">Bitmap height in pixels.</param>
public sealed record PdfRasterPage(byte[] PngBytes, int WidthPx, int HeightPx);
