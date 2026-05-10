using Lyo.Common.Records;

namespace Lyo.Images.Ocr.Models;

/// <summary>A single OCR word with bounds in pixel space (Y-up: origin bottom-left of the image).</summary>
/// <param name="Text">Recognized text.</param>
/// <param name="BoundingBoxPixels">Axis-aligned box; <see cref="BoundingBox2D.Top"/> is the upper Y (larger value).</param>
/// <param name="ConfidencePercent">Engine confidence 0–100 when available.</param>
public sealed record OcrWord(string Text, BoundingBox2D BoundingBoxPixels, float? ConfidencePercent);
