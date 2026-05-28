# Lyo.Pdf.Rendering

Rasterizes PDF pages to PNG via [PDFtoImage](https://www.nuget.org/packages/PDFtoImage)
(PDFium + Skia under the hood; `bblanchon.PDFium` native packages). Targets
`net10.0`.

## API

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

`PdfToImagePageRasterizer` is the default implementation: it dispatches the CPU-bound
PDFium call through `Task.Run`, probes the rendered bytes with `ImageSharp` to read
the bitmap dimensions, and logs an elapsed-ms trace on success. Failures surface as
`Result<PdfRasterPage>.Failure` tagged with:

- `PdfRenderErrorCodes.PageOutOfRange` (`"PDF_RENDER_PAGE_OUT_OF_RANGE"`) when
  `pageNumber1Based > pageCount`.
- `PdfRenderErrorCodes.RenderFailed` (`"PDF_RENDER_FAILED"`) for invalid PDFs,
  missing/incorrect passwords, or any PDFium error.

## Usage

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

Pass the document `password` argument when the PDF is protected. Pages are 1-based.

Prefer [`Lyo.Pdf.Ocr`](../Lyo.Pdf.Ocr/README.md) when you need to combine
rasterization with `IOcrEngine` and project bounding boxes back into PDF coordinates.

## Dependencies

| Package                                                 | Role                         |
|---------------------------------------------------------|------------------------------|
| `PDFtoImage`                                            | PDFium-backed rasterization  |
| `SixLabors.ImageSharp`                                  | Bitmap probe (width/height)  |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | DI registration              |
| `Microsoft.Extensions.Logging.Abstractions`             | Optional logger              |
| `Lyo.Common`, `Lyo.Exceptions`, `Lyo.Result`            | Shared helpers + `Result<T>` |

## Related projects

- [`Lyo.Pdf.Ocr`](../Lyo.Pdf.Ocr/README.md), [`Lyo.Pdf`](../Lyo.Pdf/README.md),
  [`Lyo.Pdf.Models`](../Lyo.Pdf.Models/README.md).
