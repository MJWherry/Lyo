# Lyo.Pdf.Rendering

Turns PDF pages into PNG through [PDFtoImage](https://www.nuget.org/packages/PDFtoImage) (PDFium + Skia; `bblanchon.PDFium` native packages). Targets `net10.0`.

## Examples

### Render a cover page

```csharp
services.AddPdfPageRasterizer();

public sealed class CoverRenderer(IPdfPageRasterizer rasterizer)
{
    public async Task<byte[]> RenderAsync(ReadOnlyMemory<byte> pdf, CancellationToken ct)
        => (await rasterizer.RenderPageToPngAsync(pdf, pageNumber1Based: 1, dpi: 144, cancellationToken: ct))
            .ValueOrThrow()
            .PngBytes;
}
```

### Rasterizer contract

```csharp
public interface IPdfPageRasterizer
{
    Task<Result<PdfRasterPage>> RenderPageToPngAsync(
        ReadOnlyMemory<byte> pdfBytes,
        int pageNumber1Based,
        int dpi,
        string? password = null,
        CancellationToken cancellationToken = default);
}

public sealed record PdfRasterPage(byte[] PngBytes, int WidthPx, int HeightPx);
```

## How the default rasterizer works

The default implementation is `PdfToImagePageRasterizer`. It sends the CPU-bound
PDFium call through `Task.Run`, reads bitmap size from the rendered bytes with `ImageSharp`,
and writes an elapsed-ms trace on success. Failures come back as
`Result<PdfRasterPage>.Failure` tagged with:

- `PdfRenderErrorCodes.PageOutOfRange` (`"PDF_RENDER_PAGE_OUT_OF_RANGE"`) when
  `pageNumber1Based > pageCount`.
- `PdfRenderErrorCodes.RenderFailed` (`"PDF_RENDER_FAILED"`) for invalid PDFs,
  missing/incorrect passwords, or any PDFium error.

## Passwords and OCR

Protected PDFs need the document `password` argument. Pages are 1-based. Use [`Lyo.Pdf.Ocr`](../Lyo.Pdf.Ocr/README.md) when rasterization should run with `IOcrEngine` and bounding boxes should be projected back into PDF coordinates.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `PDFtoImage` `5.2.1` (direct, third-party)
- `SixLabors.ImageSharp` `3.1.12` (direct, third-party)