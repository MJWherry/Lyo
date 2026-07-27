# Lyo.Images

Production-ready **raster image processing** for .NET using **SixLabors.ImageSharp**. Implements **`IImageService`** (resize, crop, rotate, watermark, format conversion,
thumbnails, compression, metadata, palette extraction, batch processing) plus a generic **image-decoration** surface (`IImageDecorationService`): centered/positioned **overlay**
compositing (raster + SVG), stroked **frame** outlines, **caption** bands, and **outer padding/shadow** — all stream-based and chainable through
**`IImageDecorationPipeline`**.

## Public API overview

| Type                                                                                                                | Description                                                                                                                                                       |
|---------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`IImageService`** / **`ImageSharpImageService`** / **`ImageServiceBase`**                                         | Primary façade for stream-based image operations and file helpers; ImageSharp backend with full EXIF.                                                             |
| **`IImageDecorationService`** / **`ImageDecorationService`**                                                        | Generic decoration primitives: `OverlayAsync`, `AddFrameAsync`, `AddCaptionAsync`, `AddOuterPaddingAsync`. `IImageService` inherits this interface.               |
| **`IImageDecorationPipeline`** (via `IImageDecorationService.Pipeline(byte[]/Stream)`)                              | Fluent chain of primitives that keeps a single in-memory image between stages.                                                                                    |
| **`OverlayOptionsBuilder`** / **`FrameOptionsBuilder`** / **`CaptionOptionsBuilder`** / **`PaddingOptionsBuilder`** | Fluent option builders under `Lyo.Images.Builders`; also exposed as configurator overloads on the pipeline (e.g. `pipeline.AddFrame(b => b.WithStrokeWidth(2))`). |
| **`ISpriteSheetExportService`** / **`SpriteSheetExportService`**                                                    | Spritesheet export, frame crops, animated GIF helpers (`Lyo.Images.Sprite`).                                                                                      |
| **`Extensions`**                                                                                                    | DI registration: **`AddImageSharpImageService`** (options/action/`IConfiguration` overloads), **`AddSpriteSheetExportService`**.                                  |

`AddImageSharpImageService` also registers **`IImageDecorationService`** if not already present, so consumers that resolve only the decoration interface still work.

### Namespaces

- **`Lyo.Images`** — services, pipeline, DI extensions, error codes.
- **`Lyo.Images.Models`** — **`ImageServiceOptions`**, **`ImageProcessRequest`**, **`WatermarkOptions`**, **`OverlayOptions`** / **`OverlayPosition`**, **`FrameOptions`**,
  **`CaptionOptions`** / **`CaptionPlacement`**, **`PaddingOptions`**, **`ImageMetadata`**, enums such as **`ResizeMode`** and **`WatermarkPosition`**.
- **`Lyo.Images.Builders`** — fluent builders for the option types plus pipeline-builder extensions.
- **`Lyo.Images.Decoration`** — internal per-primitive drawers (`OverlayDrawer`, `FrameDrawer`, `CaptionDrawer`, `OuterPaddingDrawer`) backing `ImageDecorationService`.
- **`Lyo.Images.Sprite`** / **`Lyo.Images.Sprite.Models`** — spritesheet pipeline types.

## Features

- **Resize** — Max, Crop, Pad, BoxPad, Stretch (`ResizeMode`).
- **Crop**, **Rotate**, **Watermark**, **Convert format**, **Thumbnail**, **Compress**.
- **Metadata** — Dimensions, format, optional **EXIF** (device, GPS, date taken) via ImageSharp.
- **Palette** — Dominant colors (`GetPaletteAsync`); optional ignore of transparent pixels (`ImageServiceOptions`).
- **Batch** — `ProcessBatchAsync` with `ImageProcessRequest` / `ImageOperation` subclasses.
- **Decoration primitives** — `OverlayAsync` (raster center/positioned pad + stroke; SVG documents get a base64 PNG `<image>` spliced before `</svg>`), `AddFrameAsync` (stroked
  outline with optional rounded corners + fill), `AddCaptionAsync` (header/footer band, optional notch + rounded outside corners), `AddOuterPaddingAsync` (rounded card + canvas
  margin + optional drop shadow). Each accepts a `Stream` in/out and an `ImageFormat?`. The pipeline (`IImageDecorationPipeline`) chains them without intermediate streams.
- **Thread-safe**, **async**, **logging/metrics**, **cancellation**.

## Quick start

```csharp
using Lyo.Images;
using Lyo.Images.Builders;
using Lyo.Common.Enums;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddImageSharpImageService(o => o.DefaultQuality = 90);

var sp = services.BuildServiceProvider();
var images = sp.GetRequiredService<IImageService>();

await using var input = File.OpenRead("photo.jpg");
await using var output = File.Create("photo-800.jpg");
await images.ResizeAsync(input, output, 800, 600, ResizeMode.Max, ImageFormat.Jpeg, quality: 90);
```

### Build a QR badge by chaining decoration primitives

```csharp
using Lyo.Images;
using Lyo.Images.Builders;
using Lyo.Common.Enums;

var decoration = sp.GetRequiredService<IImageDecorationService>();
var qrBytes = File.ReadAllBytes("qr.png");
var logoBytes = File.ReadAllBytes("logo.png");

var result = await decoration.Pipeline(qrBytes)
    .Overlay(logoBytes, b => b
        .WithOverlaySizePercent(18)
        .WithPadColor("#FFFFFF")
        .WithBorder("#000000"))
    .AddCaption(b => b
        .WithText("Scan Me")
        .WithBackgroundColor("#1e293b")
        .WithTextColor("#FFFFFF")
        .WithNotch())
    .AddOuterPadding(b => b
        .WithPanelColor("#FFFFFF")
        .WithCornerRadius(16)
        .WithShadow("#33000000", offsetPx: 6))
    .AddFrame(b => b
        .WithStrokeColor("#1e293b")
        .WithStrokeWidth(2)
        .WithCornerRadius(16))
    .ToByteArrayAsync(ImageFormat.Png);

File.WriteAllBytes("qr-badge.png", result.Data!);
```

Stages run in the queued order. SVG input is supported by `Overlay`; the other primitives require raster input and throw `NotSupportedException` if the pipeline state is SVG.

## Production readiness

- Thread-safe service usage; validate streams and options per implementation.
- Streaming-friendly APIs; size limits enforced via **`ImageServiceOptions`**.
- Optional **metrics** histograms (see `Lyo.Images.Constants.Metrics`).

## Dependencies

*(Synchronized from `Lyo.Images.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                                 | Version |
|---------------------------------------------------------|---------|
| `Microsoft.Extensions.Configuration.Binder`             | `[10,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions`             | `[10,)` |
| `SixLabors.Fonts`                                       | `2.*`   |
| `SixLabors.ImageSharp`                                  | `3.*`   |
| `SixLabors.ImageSharp.Drawing`                          | `2.*`   |

### Project references

- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Exceptions`](../../../Core/Exceptions/Lyo.Exceptions/README.md)
- [`Lyo.Metrics`](../../../Core/Metrics/Lyo.Metrics/README.md)
- [`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md)

## Related image libraries

- [`Lyo.Images.Ocr`](../Lyo.Images.Ocr/README.md) — OCR abstractions and models.
- [`Lyo.Images.OpenCv`](../Lyo.Images.OpenCv/README.md) — OpenCvSharp Telea inpaint on PNG ROI buffers (optional native runtime).
