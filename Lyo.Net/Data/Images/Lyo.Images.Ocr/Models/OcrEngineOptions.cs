namespace Lyo.Images.Ocr.Models;

/// <summary>Cross-provider OCR options bound from configuration or set in DI.</summary>
public sealed class OcrEngineOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "OcrEngine";

    /// <summary>When true, implementations should emit timing/success metrics.</summary>
    public bool EnableMetrics { get; set; }

    /// <summary>Default languages passed to engines that support multi-language (e.g. <c>eng</c>, <c>eng+jpn</c>).</summary>
    public string DefaultLanguages { get; set; } = "eng";

    /// <summary>Default page segmentation mode when a request does not override it.</summary>
    public OcrPageSegmentationMode DefaultPageSegmentationMode { get; set; } = OcrPageSegmentationMode.Auto;
}
