namespace Lyo.Images.Ocr;

/// <summary>Metric names for OCR instrumentation.</summary>
public static class OcrMetrics
{
    /// <summary>Histogram of OCR read duration (ms).</summary>
    public const string ReadDurationMs = "ocr.read.duration_ms";

    /// <summary>Counter incremented on successful OCR reads.</summary>
    public const string ReadSuccess = "ocr.read.success";

    /// <summary>Counter incremented on failed OCR reads.</summary>
    public const string ReadFailure = "ocr.read.failure";
}
