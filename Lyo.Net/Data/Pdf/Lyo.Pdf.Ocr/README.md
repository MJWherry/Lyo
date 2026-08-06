# Lyo.Pdf.Ocr

Glues [`Lyo.Pdf.Rendering`](../Lyo.Pdf.Rendering/README.md) (PDFium → PNG) to an `IOcrEngine` from `Lyo.Images.Ocr` and projects OCR pixel boxes back into PDF coordinate space.

Targets `net10.0`.

## Examples

### Register with DI

```csharp
services.AddTesseractOcrEngineFromConfiguration(configuration); // or any IOcrEngine
services.AddPdfOcr();
```

### Example

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

`AddPdfOcr` registers `PdfOcrService` as a singleton and calls `AddPdfPageRasterizer` if `IPdfPageRasterizer` is not already registered. `IOcrEngine` must be registered separately.

## Example

For selectable-text PDFs prefer `IPdfReader.Text` (PdfPig) directly — OCR only helps when the PDF lacks an embedded text layer.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Images.Ocr` — (direct, lyo)
- `Lyo.Pdf.Models` — (direct, lyo)
- `Lyo.Pdf.Rendering` — (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (direct, microsoft)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.DataTable.Models` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `PDFtoImage` `5.2.1` — (transitive, third-party)
- `SixLabors.ImageSharp` `3.1.12` — (transitive, third-party)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)