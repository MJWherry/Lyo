namespace Lyo.Images.Ocr.Models;

/// <summary>Per-call OCR parameters; null properties fall back to <see cref="OcrEngineOptions"/> defaults.</summary>
public sealed class OcrReadRequest
{
    /// <summary>Override languages (e.g. <c>eng+jpn</c>).</summary>
    public string? Languages { get; set; }

    /// <summary>Override page segmentation mode.</summary>
    public OcrPageSegmentationMode? PageSegmentationMode { get; set; }

    /// <summary>Minimum confidence 0–100 for words to include; null keeps all words.</summary>
    public int? MinimumConfidencePercent { get; set; }
}
