# Lyo.Pdf.Ocr

Glues [`Lyo.Pdf.Rendering`](../Lyo.Pdf.Rendering/README.md) (PDFium → PNG) to an
`IOcrEngine` from `Lyo.Images.Ocr` and projects OCR pixel boxes back into PDF
coordinate space.

Targets `net10.0`.

## API

`PdfOcrService` exposes a single method:

```csharp
Task<Result<PdfOcrDocumentPage>> ReadPageAsync(
    IPdfReader pdfReader,
    int pageNumber1Based,
    int dpi,
    OcrReadRequest? ocrRequest = null,
    string? pdfPassword = null,
    CancellationToken cancellationToken = default);
```

The pipeline:

1. `pdfReader.GetPageSizePoints(pageNumber1Based)` for the PDF page dimensions.
2. `IPdfPageRasterizer.RenderPageToPngAsync(pdfReader.SourceBytes, …)` for the
   pixel raster (and bitmap width/height).
3. `IOcrEngine.ReadAsync(pngStream, ocrRequest, …)` for the per-word text and
   pixel-space bounding boxes (Y-up).
4. `OcrCoordinateTransforms.MapPixelBoxToPdfPoints(box, pageWidthPts, pageHeightPts,
   widthPx, heightPx)` to lift each `OcrWord` into a `PdfWord`.

`PdfOcrDocumentPage` carries the original `OcrPageResult` plus the projected
`IReadOnlyList<PdfWord> WordsInPdfPoints` and the source page size.

Failures from either stage propagate as `Result<PdfOcrDocumentPage>.Failure`;
unexpected exceptions are tagged with `PdfOcrErrorCodes.ReadFailed`
(`"PDF_OCR_READ_FAILED"`).

## Dependency injection

```csharp
services.AddTesseractOcrEngineFromConfiguration(configuration); // or any IOcrEngine
services.AddPdfOcr();
```

`AddPdfOcr` registers `PdfOcrService` as a singleton and calls
`AddPdfPageRasterizer` if `IPdfPageRasterizer` is not already registered.
`IOcrEngine` must be registered separately.

## Example

```csharp
public sealed class Worker(PdfOcrService pdfOcr, IPdfService pdfService)
{
    public async Task<PdfOcrDocumentPage> RunAsync(byte[] pdfBytes, CancellationToken ct)
    {
        await using var reader = await pdfService.OpenFromBytesAsync(pdfBytes, ct);
        var result = await pdfOcr.ReadPageAsync(reader, pageNumber1Based: 1, dpi: 200, cancellationToken: ct);
        return result.ValueOrThrow();
    }
}
```

For selectable-text PDFs prefer `IPdfReader.Text` (PdfPig) directly — OCR only
helps when the PDF lacks an embedded text layer.

## Dependencies

| Package / project                                                    | Role                                     |
|----------------------------------------------------------------------|------------------------------------------|
| `Lyo.Pdf.Rendering`                                                  | PDF → PNG raster                         |
| `Lyo.Images.Ocr`                                                     | `IOcrEngine` + `OcrCoordinateTransforms` |
| `Lyo.Pdf.Models`                                                     | `IPdfReader`, `PdfWord`                  |
| `Lyo.Exceptions`                                                     | Argument validation                      |
| `Microsoft.Extensions.DependencyInjection.Abstractions`, `…Logging…` | DI + logging                             |

## Related projects

- [`Lyo.Pdf`](../Lyo.Pdf/README.md), [`Lyo.Pdf.Models`](../Lyo.Pdf.Models/README.md),
  [`Lyo.Pdf.Rendering`](../Lyo.Pdf.Rendering/README.md).
