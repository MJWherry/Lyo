namespace Lyo.Images.Ocr.Models;

/// <summary>OCR result for a single raster page.</summary>
/// <param name="FullText">Entire page as plain text.</param>
/// <param name="Words">Per-word hits with pixel boxes (Y-up).</param>
/// <param name="Lines">Lines assembled from those words.</param>
/// <param name="ImageWidth">Analyzed bitmap width in pixels.</param>
/// <param name="ImageHeight">Analyzed bitmap height in pixels.</param>
public sealed record OcrPageResult(string FullText, IReadOnlyList<OcrWord> Words, IReadOnlyList<OcrLine> Lines, int ImageWidth, int ImageHeight);