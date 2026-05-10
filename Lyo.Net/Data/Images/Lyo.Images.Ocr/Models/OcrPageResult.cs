namespace Lyo.Images.Ocr.Models;

/// <summary>OCR output for one raster page.</summary>
/// <param name="FullText">Full plain text for the page.</param>
/// <param name="Words">Word-level results with pixel bounding boxes (Y-up).</param>
/// <param name="Lines">Line-level grouping derived from words.</param>
/// <param name="ImageWidth">Width of the analyzed bitmap in pixels.</param>
/// <param name="ImageHeight">Height of the analyzed bitmap in pixels.</param>
public sealed record OcrPageResult(
    string FullText,
    IReadOnlyList<OcrWord> Words,
    IReadOnlyList<OcrLine> Lines,
    int ImageWidth,
    int ImageHeight);
