using Lyo.Exceptions;

namespace Lyo.Images.Ocr.Models;

/// <summary>Cross-provider OCR options bound from configuration or set in DI.</summary>
public sealed class OcrEngineOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "OcrEngine";

    /// <summary>When set, implementations should emit timing and success metrics.</summary>
    public bool EnableMetrics { get; set; }

    /// <summary>Starting languages passed to engines that support multi-language (such as <c>eng</c>, <c>eng+jpn</c>).</summary>
    public string DefaultLanguages { get; set; } = "eng";

    /// <summary>Starting page-segmentation mode when a request does not override it.</summary>
    public OcrPageSegmentationMode DefaultPageSegmentationMode { get; set; } = OcrPageSegmentationMode.SparseTextOsd;

    /// <summary>Throws when language or page-segmentation defaults are invalid.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(DefaultLanguages);
        ArgumentHelpers.ThrowIfNotDefined(DefaultPageSegmentationMode);
    }
}