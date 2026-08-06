# Lyo.Pdf.Rendering

Rasterizes PDF pages to PNG via [PDFtoImage](https://www.nuget.org/packages/PDFtoImage) (PDFium + Skia under the hood; `bblanchon.PDFium` native packages). Targets `net10.0`.

## Examples

### Usage

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

### API

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

## API

`PdfToImagePageRasterizer` is the default implementation: it dispatches the CPU-bound
PDFium call through `Task.Run`, probes the rendered bytes with `ImageSharp` to read
the bitmap dimensions, and logs an elapsed-ms trace on success. Failures surface as
`Result<PdfRasterPage>.Failure` tagged with:

- `PdfRenderErrorCodes.PageOutOfRange` (`"PDF_RENDER_PAGE_OUT_OF_RANGE"`) when
  `pageNumber1Based > pageCount`.
- `PdfRenderErrorCodes.RenderFailed` (`"PDF_RENDER_FAILED"`) for invalid PDFs,
  missing/incorrect passwords, or any PDFium error.

## Usage

Pass the document `password` argument when the PDF is protected. Pages are 1-based. Prefer [`Lyo.Pdf.Ocr`](../Lyo.Pdf.Ocr/README.md) when you need to combine rasterization with `IOcrEngine` and project bounding boxes back into PDF coordinates.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Result` — (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (direct, microsoft)
- `PDFtoImage` `5.2.1` — (direct, third-party)
- `SixLabors.ImageSharp` `3.1.12` — (direct, third-party)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)