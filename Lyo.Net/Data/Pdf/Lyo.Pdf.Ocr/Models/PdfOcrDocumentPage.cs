using Lyo.Images.Ocr.Models;
using Lyo.Pdf.Models;

namespace Lyo.Pdf.Ocr.Models;

/// <summary>OCR output for one PDF page: pixel-space OCR plus words mapped to PDF points.</summary>
/// <param name="Ocr"><see cref="OcrPageResult"/> in image pixel space (Y-up).</param>
/// <param name="WordsInPdfPoints"><see cref="PdfWord"/> instances using PDF coordinate space.</param>
/// <param name="PageWidthPoints">Page width from <see cref="IPdfReader.GetPageSizePoints"/>.</param>
/// <param name="PageHeightPoints">Page height from <see cref="IPdfReader.GetPageSizePoints"/>.</param>
public sealed record PdfOcrDocumentPage(
    OcrPageResult Ocr,
    IReadOnlyList<PdfWord> WordsInPdfPoints,
    double PageWidthPoints,
    double PageHeightPoints);
