# Lyo.Images.Ocr

Engine-agnostic **OCR contracts** for Lyo: **`IOcrEngine`**, request/response models, **Y-up pixel bounding boxes** (aligned with [`BoundingBox2D`](../../../Core/Common/Lyo.Common/Records/BoundingBox2D.cs)), coordinate helpers, and shared **`OcrEngineOptions`**.

Implementations (e.g. **`Lyo.Images.Ocr.Tesseract`**) register **`IOcrEngine`**. This package only registers cross-provider options via **`AddOcrEngineOptions`** / **`AddOcrEngineOptionsFromConfiguration`**.

## Examples

### Configuration

```json
{
  "OcrEngine": {
    "EnableMetrics": false,
    "DefaultLanguages": "eng",
    "DefaultPageSegmentationMode": "SparseTextOsd"
  }
}
```

### Configuration (2)

```csharp
using Lyo.Images.Ocr;

services.AddOcrEngineOptionsFromConfiguration(builder.Configuration);
// then register a concrete IOcrEngine (e.g. AddTesseractOcrEngine).
```

## Public API

| Type | Description |
| -------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`IOcrEngine`** | `ReadAsync(Stream imageStream, OcrReadRequest?, CancellationToken) → Task<Result<OcrPageResult>>` — runs OCR on an encoded raster stream (PNG/JPEG/etc.). |
| **`OcrReadRequest`** | Optional per-call overrides: `Languages`, `PageSegmentationMode`, `MinimumConfidencePercent`. |
| **`OcrPageResult`** | `FullText`, `Words` (with pixel Y-up bounding boxes), `Lines` (grouped), `ImageWidth`, `ImageHeight`. |
| **`OcrWord`** / **`OcrLine`** | Word + line records used in `OcrPageResult`. `OcrWord.BoundingBoxPixels` is a `BoundingBox2D` (Y-up). |
| **`OcrEngineOptions`** | `EnableMetrics`, `DefaultLanguages` (default `"eng"`), `DefaultPageSegmentationMode` (default `SparseTextOsd`); `SectionName = "OcrEngine"`. |
| **`OcrPageSegmentationMode`** | Provider-neutral layout modes (`AutoOsd`, `Auto`, `SingleColumn`, `SingleBlock`, `SingleLine`, `SingleWord`, `CircleWord`, `SingleChar`, `SparseTextOsd`, …). |
| **`OcrCoordinateTransforms`** | `FromTopLeftDownwardRect`, `MapPixelBoxToPdfPoints`, and other helpers to convert between raster, Y-up pixel, and PDF point coordinates. |
| **`OcrLineGrouper`** | Groups `OcrWord` results into `OcrLine` rows. |
| **`OcrMetrics`** / **`OcrErrorCodes`** | Metric name and error code constants for engines to use consistently. |
| **`OcrServiceCollectionExtensions`** | DI: `AddOcrEngineOptions(Action<OcrEngineOptions>?)`, `AddOcrEngineOptionsFromConfiguration(IConfiguration, sectionName?)`. |

## Bounding boxes

- **`OcrWord.BoundingBoxPixels`** uses **Y-up** coordinates: origin at the **bottom-left** of the image, so **`Top` &gt; `Bottom`** and **`Height = Top - Bottom`** matches [`BoundingBox2D`](../../../Core/Common/Lyo.Common/Records/BoundingBox2D.cs).
- Use **`OcrCoordinateTransforms.FromTopLeftDownwardRect`** to convert typical top-left raster rects (e.g. Tesseract) into this form.
- For PDF overlay with a rendered page, use **`OcrCoordinateTransforms.MapPixelBoxToPdfPoints`** (see **`Lyo.Pdf.Ocr`**).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Metrics` — (direct, lyo)
- `Lyo.Result` — (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)