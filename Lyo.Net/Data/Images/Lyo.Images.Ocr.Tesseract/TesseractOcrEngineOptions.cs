using Lyo.Images.Ocr.Models;

namespace Lyo.Images.Ocr.Tesseract;

/// <summary>Tesseract-specific configuration (tessdata directory).</summary>
public sealed class TesseractOcrEngineOptions
{
    /// <summary>Configuration subsection under <see cref="OcrEngineOptions.SectionName"/>.</summary>
    public const string ConfigurationKey = "Tesseract";

    /// <summary>Absolute or relative path to the tessdata folder containing <c>*.traineddata</c> files.</summary>
    public string TessdataDirectory { get; set; } = "";
}
