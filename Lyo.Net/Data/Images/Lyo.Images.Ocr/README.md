# Lyo.Images.Ocr

Lyo OCR contracts: `IOcrEngine`, request/response models, Y-up pixel boxes (same as [`BoundingBox2D`](../../../Core/Common/Lyo.Common.Core/Records/BoundingBox2D.cs)), coordinate helpers, and shared `OcrEngineOptions`.

Concrete engines (e.g. `Lyo.Images.Ocr.Tesseract`) register `IOcrEngine`. This package only registers cross-provider options through `AddOcrEngineOptions` / `AddOcrEngineOptionsFromConfiguration`.

## Examples

### appsettings sample

```json
{
  "OcrEngine": {
    "EnableMetrics": false,
    "DefaultLanguages": "eng",
    "DefaultPageSegmentationMode": "SparseTextOsd"
  }
}
```

### Bind options from configuration

```csharp
using Lyo.Images.Ocr;

services.AddOcrEngineOptionsFromConfiguration(builder.Configuration);
// then register a concrete IOcrEngine (e.g. AddTesseractOcrEngine).
```

## Types

| Type | Description |
| -------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `IOcrEngine` | `ReadAsync(Stream imageStream, OcrReadRequest?, CancellationToken) → Task<Result<OcrPageResult>>`. OCR on an encoded raster stream (PNG/JPEG/etc.). |
| `OcrReadRequest` | Optional overrides for one call: `Languages`, `PageSegmentationMode`, `MinimumConfidencePercent`. |
| `OcrPageResult` | `FullText`, `Words` (pixel Y-up bounding boxes), `Lines` (grouped), `ImageWidth`, `ImageHeight`. |
| `OcrWord` / `OcrLine` | Word and line records in `OcrPageResult`. `OcrWord.BoundingBoxPixels` is a `BoundingBox2D` (Y-up). |
| `OcrEngineOptions` | `EnableMetrics`, `DefaultLanguages` (default `"eng"`), `DefaultPageSegmentationMode` (default `SparseTextOsd`); `SectionName = "OcrEngine"`. |
| `OcrPageSegmentationMode` | Layout modes that do not depend on a provider (`AutoOsd`, `Auto`, `SingleColumn`, `SingleBlock`, `SingleLine`, `SingleWord`, `CircleWord`, `SingleChar`, `SparseTextOsd`, …). |
| `OcrCoordinateTransforms` | `FromTopLeftDownwardRect`, `MapPixelBoxToPdfPoints`, and other helpers that convert among raster, Y-up pixel, and PDF point coordinates. |
| `OcrLineGrouper` | Turns `OcrWord` results into `OcrLine` rows. |
| `OcrMetrics` / `OcrErrorCodes` | Shared metric names and error codes for engines. |
| `OcrServiceCollectionExtensions` | Container helpers: `AddOcrEngineOptions(Action<OcrEngineOptions>?)`, `AddOcrEngineOptionsFromConfiguration(IConfiguration, sectionName?)`. |

## How boxes are oriented

- `OcrWord.BoundingBoxPixels` is Y-up: origin at the image bottom-left, so `Top` &gt; `Bottom` and `Height = Top - Bottom` matches [`BoundingBox2D`](../../../Core/Common/Lyo.Common.Core/Records/BoundingBox2D.cs).
- `OcrCoordinateTransforms.FromTopLeftDownwardRect` converts typical top-left raster rects (e.g. Tesseract) into this form.
- For a PDF overlay on a rendered page, call `OcrCoordinateTransforms.MapPixelBoxToPdfPoints` (see `Lyo.Pdf.Ocr`).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Configuration` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)