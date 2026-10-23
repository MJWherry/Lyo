using System.Diagnostics;
using Lyo.Common.Metadata.Records;

namespace Lyo.Images.Ocr.Models;

/// <summary>One OCR word with bounds in pixel space (Y-up: origin at the image bottom-left).</summary>
/// <param name="Text">Recognized text.</param>
/// <param name="BoundingBoxPixels">Axis-aligned box; <see cref="BoundingBox2D.Top" /> is the upper Y (larger value).</param>
/// <param name="ConfidencePercent">Engine confidence 0–100 when available.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record OcrWord(string Text, BoundingBox2D BoundingBoxPixels, float? ConfidencePercent);