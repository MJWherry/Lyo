using Lyo.Common.Records;

namespace Lyo.Pdf.Models;

/// <summary>A single word extracted from a PDF page.</summary>
/// <param name="Text">The word text.</param>
/// <param name="BoundingBox">Bounding box in PDF coordinates (points).</param>
/// <param name="Format">Optional formatting (font, color, bold, italic) from the PDF.</param>
public sealed record PdfWord(string Text, BoundingBox2D BoundingBox, PdfWordFormat? Format = null);