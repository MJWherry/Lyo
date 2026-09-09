# Lyo.Pdf.Ocr

PNG-renders a PDF page with [`Lyo.Pdf.Rendering`](../Lyo.Pdf.Rendering/README.md) (PDFium), runs an `IOcrEngine` from `Lyo.Images.Ocr`, then lifts OCR pixel boxes into PDF points.

Targets `net10.0`.

## Examples

### Add Pdf OCR in DI

```csharp
services.AddTesseractOcrEngineFromConfiguration(configuration); // or any IOcrEngine
services.AddPdfOcr();
```

### Read one page

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

## ReadPageAsync pipeline

`PdfOcrService` has one method:

```csharp
Task<Result<PdfOcrDocumentPage>> ReadPageAsync(
    IPdfReader pdfReader,
    int pageNumber1Based,
    int dpi,
    OcrReadRequest? ocrRequest = null,
    string? pdfPassword = null,
    CancellationToken cancellationToken = default);
```

Steps:

1. `pdfReader.GetPageSizePoints(pageNumber1Based)` for the PDF page dimensions.
2. `IPdfPageRasterizer.RenderPageToPngAsync(pdfReader.SourceBytes, …)` for the
   pixel raster (and bitmap width/height).
3. `IOcrEngine.ReadAsync(pngStream, ocrRequest, …)` for the per-word text and
   pixel-space bounding boxes (Y-up).
4. `OcrCoordinateTransforms.MapPixelBoxToPdfPoints(box, pageWidthPts, pageHeightPts,
   widthPx, heightPx)` to lift each `OcrWord` into a `PdfWord`.

`PdfOcrDocumentPage` holds the original `OcrPageResult`, the projected
`IReadOnlyList<PdfWord> WordsInPdfPoints`, and the source page size.

Either stage can fail as `Result<PdfOcrDocumentPage>.Failure`;
unexpected exceptions are tagged with `PdfOcrErrorCodes.ReadFailed`
(`"PDF_OCR_READ_FAILED"`).

## DI registration

`AddPdfOcr` adds `PdfOcrService` as a singleton and, if `IPdfPageRasterizer` is missing, calls `AddPdfPageRasterizer`. Register `IOcrEngine` yourself.

## When OCR is worth it

Selectable-text PDFs should use `IPdfReader.Text` (PdfPig) instead. OCR is useful only when the PDF has no embedded text layer.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Images.Ocr` (direct, lyo)
- `Lyo.Pdf.Models` (direct, lyo)
- `Lyo.Pdf.Rendering` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Configuration` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `PDFtoImage` `5.2.1` (transitive, third-party)
- `SixLabors.ImageSharp` `3.1.12` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)