using Lyo.Common.Records;

namespace Lyo.Images.Ocr.Models;

/// <summary>An OCR text line aggregated from words (pixel space, Y-up).</summary>
/// <param name="Text">Combined line text.</param>
/// <param name="BoundingBoxPixels">Union box for the line.</param>
/// <param name="Words">Words belonging to this line.</param>
public sealed record OcrLine(string Text, BoundingBox2D BoundingBoxPixels, IReadOnlyList<OcrWord> Words);
