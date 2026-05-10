# Lyo.Pdf.Ocr

Combines **`Lyo.Pdf.Rendering`** (PDFium → PNG) with **`Lyo.Images.Ocr`** (`IOcrEngine`) and maps **Y-up pixel** boxes to **`PdfWord`** PDF coordinates via **`OcrCoordinateTransforms.MapPixelBoxToPdfPoints`**.

Use **`IPdfReader.GetPageSizePoints`** (from **`Lyo.Pdf`**) together with the raster dimensions returned internally so scaling matches the rendered bitmap.

## Dependency injection

```csharp
// AddPdfOcr() registers IPdfPageRasterizer when missing
services.AddTesseractOcrEngineFromConfiguration(configuration);
services.AddPdfOcr();
```

## Example

```csharp
public class Worker(PdfOcrService pdfOcr, IPdfService pdfService)
{
    public async Task<PdfOcrDocumentPage> Run(byte[] pdfBytes, CancellationToken ct)
    {
        await using var reader = await pdfService.OpenFromBytesAsync(pdfBytes, ct).ConfigureAwait(false);
        var result = await pdfOcr.ReadPageAsync(reader, pageNumber1Based: 1, dpi: 200, cancellationToken: ct).ConfigureAwait(false);
        return result.ValueOrThrow();
    }
}
```

For selectable text PDFs, prefer **`IPdfReader.Text`** / PdfPig extraction instead of OCR.
