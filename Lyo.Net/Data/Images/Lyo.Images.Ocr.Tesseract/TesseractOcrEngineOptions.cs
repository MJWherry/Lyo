using Lyo.Images.Ocr.Models;

namespace Lyo.Images.Ocr.Tesseract;

/// <summary>Tesseract-only options (tessdata directory).</summary>
public sealed class TesseractOcrEngineOptions
{
    /// <summary>Configuration key nested under <see cref="OcrEngineOptions.SectionName" />.</summary>
    public const string ConfigurationKey = "Tesseract";

    /// <summary>Absolute or relative path to the tessdata folder that holds <c>*.traineddata</c> files.</summary>
    public string TessdataDirectory { get; set; } = "";
}