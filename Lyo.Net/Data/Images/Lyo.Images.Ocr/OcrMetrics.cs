namespace Lyo.Images.Ocr;

/// <summary>Metric names for OCR instrumentation.</summary>
public static class OcrMetrics
{
    /// <summary>Histogram of OCR read duration in milliseconds.</summary>
    public const string ReadDurationMs = "ocr.read.duration_ms";

    /// <summary>Counter bumped on successful OCR reads.</summary>
    public const string ReadSuccess = "ocr.read.success";

    /// <summary>Counter bumped on failed OCR reads.</summary>
    public const string ReadFailure = "ocr.read.failure";
}