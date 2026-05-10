using Lyo.Exceptions;
using Lyo.Images.Ocr;
using Lyo.Images.Ocr.Models;
using Lyo.Pdf.Models;
using Lyo.Pdf.Ocr.Models;
using Lyo.Pdf.Rendering;
using Lyo.Result;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Pdf.Ocr;

/// <summary>Renders a PDF page and runs <see cref="IOcrEngine"/>; maps word boxes to PDF coordinates.</summary>
public sealed class PdfOcrService(
    IOcrEngine ocrEngine,
    IPdfPageRasterizer rasterizer,
    ILogger<PdfOcrService>? logger = null)
{
    private readonly ILogger _logger = logger ?? NullLogger<PdfOcrService>.Instance;

    /// <summary>
    /// Rasterizes <paramref name="pageNumber1Based"/> at <paramref name="dpi"/>, runs OCR, and maps each <see cref="OcrWord"/> to <see cref="PdfWord"/> using
    /// <see cref="OcrCoordinateTransforms.MapPixelBoxToPdfPoints"/>.
    /// </summary>
    public async Task<Result<PdfOcrDocumentPage>> ReadPageAsync(
        IPdfReader pdfReader,
        int pageNumber1Based,
        int dpi,
        OcrReadRequest? ocrRequest = null,
        string? pdfPassword = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentHelpers.ThrowIfNull(pdfReader);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber1Based, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(dpi, 1);

        try {
            var (pageWidthPts, pageHeightPts) = pdfReader.GetPageSizePoints(pageNumber1Based);
            var rasterResult = await rasterizer
                .RenderPageToPngAsync(pdfReader.SourceBytes, pageNumber1Based, dpi, pdfPassword, cancellationToken)
                .ConfigureAwait(false);

            if (!rasterResult.IsSuccess || rasterResult.Data is null)
                return Result<PdfOcrDocumentPage>.Failure(rasterResult.Errors ?? []);

            var png = rasterResult.Data;
            await using var pngStream = new MemoryStream(png.PngBytes, writable: false);
            var ocrResult = await ocrEngine.ReadAsync(pngStream, ocrRequest, cancellationToken).ConfigureAwait(false);
            if (!ocrResult.IsSuccess || ocrResult.Data is null)
                return Result<PdfOcrDocumentPage>.Failure(ocrResult.Errors ?? []);

            var wordsPdf = new List<PdfWord>(ocrResult.Data.Words.Count);
            foreach (var w in ocrResult.Data.Words) {
                var pdfBox = OcrCoordinateTransforms.MapPixelBoxToPdfPoints(
                    w.BoundingBoxPixels,
                    pageWidthPts,
                    pageHeightPts,
                    png.WidthPx,
                    png.HeightPx);

                wordsPdf.Add(new PdfWord(w.Text, pdfBox, null));
            }

            return Result<PdfOcrDocumentPage>.Success(new PdfOcrDocumentPage(ocrResult.Data, wordsPdf, pageWidthPts, pageHeightPts));
        }
        catch (Exception ex) {
            _logger.LogError(ex, "PDF OCR failed for page {Page}.", pageNumber1Based);
            return Result<PdfOcrDocumentPage>.Failure(ex, PdfOcrErrorCodes.ReadFailed);
        }
    }
}
