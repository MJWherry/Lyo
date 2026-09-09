using Lyo.Common.Metadata.Records;

namespace Lyo.Pdf.Models;

/// <summary>One word taken from a PDF page, with optional style metadata.</summary>
/// <param name="Text">Glyph run text.</param>
/// <param name="BoundingBox">Rectangle in PDF coordinates (points).</param>
/// <param name="Format">Optional font/color/style captured from the PDF.</param>
public sealed record PdfWord(string Text, BoundingBox2D BoundingBox, PdfWordFormat? Format = null);