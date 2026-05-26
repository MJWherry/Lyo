# Lyo.Images.Ocr

Engine-agnostic **OCR contracts** for Lyo: **`IOcrEngine`**, request/response models, **Y-up pixel bounding boxes** (aligned with
[`BoundingBox2D`](../../../Core/Common/Lyo.Common/Records/BoundingBox2D.cs)), coordinate helpers, and shared **`OcrEngineOptions`**.

Implementations (e.g. **`Lyo.Images.Ocr.Tesseract`**) register **`IOcrEngine`**. This package only registers cross-provider options via **`AddOcrEngineOptions`** /
**`AddOcrEngineOptionsFromConfiguration`**.

## Public API

| Type                                  | Description                                                                                                                                                    |
|---------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`IOcrEngine`**                      | `ReadAsync(Stream imageStream, OcrReadRequest?, CancellationToken) → Task<Result<OcrPageResult>>` — runs OCR on an encoded raster stream (PNG/JPEG/etc.).      |
| **`OcrReadRequest`**                  | Optional per-call overrides: `Languages`, `PageSegmentationMode`, `MinimumConfidencePercent`.                                                                  |
| **`OcrPageResult`**                   | `FullText`, `Words` (with pixel Y-up bounding boxes), `Lines` (grouped), `ImageWidth`, `ImageHeight`.                                                          |
| **`OcrWord`** / **`OcrLine`**         | Word + line records used in `OcrPageResult`. `OcrWord.BoundingBoxPixels` is a `BoundingBox2D` (Y-up).                                                          |
| **`OcrEngineOptions`**                | `EnableMetrics`, `DefaultLanguages` (default `"eng"`), `DefaultPageSegmentationMode` (default `SparseTextOsd`); `SectionName = "OcrEngine"`.                   |
| **`OcrPageSegmentationMode`**         | Provider-neutral layout modes (`AutoOsd`, `Auto`, `SingleColumn`, `SingleBlock`, `SingleLine`, `SingleWord`, `CircleWord`, `SingleChar`, `SparseTextOsd`, …). |
| **`OcrCoordinateTransforms`**         | `FromTopLeftDownwardRect`, `MapPixelBoxToPdfPoints`, and other helpers to convert between raster, Y-up pixel, and PDF point coordinates.                       |
| **`OcrLineGrouper`**                  | Groups `OcrWord` results into `OcrLine` rows.                                                                                                                  |
| **`OcrMetrics`** / **`OcrErrorCodes`** | Metric name and error code constants for engines to use consistently.                                                                                          |
| **`OcrServiceCollectionExtensions`**  | DI: `AddOcrEngineOptions(Action<OcrEngineOptions>?)`, `AddOcrEngineOptionsFromConfiguration(IConfiguration, sectionName?)`.                                    |

## Bounding boxes

- **`OcrWord.BoundingBoxPixels`** uses **Y-up** coordinates: origin at the **bottom-left** of the image, so **`Top` &gt; `Bottom`** and **`Height = Top - Bottom`** matches
  [`BoundingBox2D`](../../../Core/Common/Lyo.Common/Records/BoundingBox2D.cs).
- Use **`OcrCoordinateTransforms.FromTopLeftDownwardRect`** to convert typical top-left raster rects (e.g. Tesseract) into this form.
- For PDF overlay with a rendered page, use **`OcrCoordinateTransforms.MapPixelBoxToPdfPoints`** (see **`Lyo.Pdf.Ocr`**).

## Configuration

```json
{
  "OcrEngine": {
    "EnableMetrics": false,
    "DefaultLanguages": "eng",
    "DefaultPageSegmentationMode": "SparseTextOsd"
  }
}
```

```csharp
using Lyo.Images.Ocr;

services.AddOcrEngineOptionsFromConfiguration(builder.Configuration);
// then register a concrete IOcrEngine (e.g. AddTesseractOcrEngine).
```

## Dependencies

*(Synchronized from `Lyo.Images.Ocr.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                                 | Version |
|---------------------------------------------------------|---------|
| `Microsoft.Extensions.Configuration.Abstractions`       | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder`             | `[10,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions`             | `[10,)` |

### Project references

- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Metrics`](../../../Core/Metrics/Lyo.Metrics/README.md)
- [`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md)

## Related packages

| Package                        | Role                                                 |
|--------------------------------|------------------------------------------------------|
| **`Lyo.Images.Ocr.Tesseract`** | Local OCR via Tesseract                              |
| **`Lyo.Pdf.Rendering`**        | Rasterize PDF pages for OCR                          |
| **`Lyo.Pdf.Ocr`**              | Combine rasterization + OCR + PDF coordinate mapping |
