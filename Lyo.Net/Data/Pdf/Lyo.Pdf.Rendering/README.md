# Lyo.Pdf.Rendering

Rasterizes PDF pages to **PNG** using **[PDFtoImage](https://www.nuget.org/packages/PDFtoImage)** (PDFium via **bblanchon.PDFium** native packages).

## Usage

```csharp
services.AddPdfPageRasterizer();

public class Consumer(IPdfPageRasterizer rasterizer)
{
    public async Task<byte[]> RenderCover(ReadOnlyMemory<byte> pdf)
        => (await rasterizer.RenderPageToPngAsync(pdf, pageNumber1Based: 1, dpi: 144).ConfigureAwait(false)).ValueOrThrow().PngBytes;
}
```

## Notes

- Prefer **`Lyo.Pdf.Ocr`** when combining rasterization with **`IOcrEngine`** and PDF-space bounding boxes.
- Password-protected PDFs: pass the **`password`** argument to **`RenderPageToPngAsync`**.
