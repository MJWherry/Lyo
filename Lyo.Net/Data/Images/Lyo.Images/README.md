# Lyo.Images

Raster image processing for .NET using SixLabors.ImageSharp. Implements `IImageService` (resize, crop, rotate, watermark, format conversion, thumbnails, compression, metadata, palette extraction, batch processing) plus `IImageDecorationService`: centered or positioned overlay compositing (raster + SVG), stroked frame outlines, caption bands, and outer padding/shadow. All stream-based and chainable through `IImageDecorationPipeline`.

## Features

- **Resize.** Max, Crop, Pad, BoxPad, Stretch (`ResizeMode`).
- **Crop, rotate, watermark, convert, thumbnail, compress.**
- **Metadata.** Dimensions, format, optional EXIF (device, GPS, date taken) via ImageSharp.
- **Palette.** Dominant colors (`GetPaletteAsync`). Optional ignore of transparent pixels (`ImageServiceOptions`).
- **Batch.** `ProcessBatchAsync` with `ImageProcessRequest` / `ImageOperation` subclasses.
- **Decoration primitives.** `OverlayAsync` (raster center/positioned pad + stroke; SVG documents get a base64 PNG `<image>` spliced before `</svg>`), `AddFrameAsync` (stroked outline with optional rounded corners + fill), `AddCaptionAsync` (header/footer band, optional notch + rounded outside corners), `AddOuterPaddingAsync` (rounded card + canvas margin + optional drop shadow). Each accepts a `Stream` in/out and an `ImageFormat?`. The pipeline (`IImageDecorationPipeline`) chains them without intermediate streams.
- Thread-safe, async, logging/metrics, `CancellationToken`.

## Examples

### Quick start

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

## Types

| Type | Description |
| --------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `IImageService` / `ImageSharpImageService` / `ImageServiceBase` | Stream-based image operations and file helpers. ImageSharp backend with EXIF. |
| `IImageDecorationService` / `ImageDecorationService` | Decoration primitives: `OverlayAsync`, `AddFrameAsync`, `AddCaptionAsync`, `AddOuterPaddingAsync`. `IImageService` inherits this interface. |
| `IImageDecorationPipeline` (via `IImageDecorationService.Pipeline(byte[]/Stream)`) | Chain of primitives that keeps a single in-memory image between stages. |
| `OverlayOptionsBuilder` / `FrameOptionsBuilder` / `CaptionOptionsBuilder` / `PaddingOptionsBuilder` | Option builders under `Lyo.Images.Builders`. Also exposed as configurator overloads on the pipeline (e.g. `pipeline.AddFrame(b => b.WithStrokeWidth(2))`). |
| `ISpriteSheetExportService` / `SpriteSheetExportService` | Spritesheet export, frame crops, animated GIF helpers (`Lyo.Images.Sprite`). |
| `Extensions` | DI: `AddImageSharpImageService` (options/action/`IConfiguration` overloads), `AddSpriteSheetExportService`. |

`AddImageSharpImageService` also registers `IImageDecorationService` if not already present, so consumers that resolve only the decoration interface still work.

## Namespaces

- `Lyo.Images`. Services, pipeline, DI extensions, error codes.
- `Lyo.Images.Models`. `ImageServiceOptions`, `ImageProcessRequest`, `WatermarkOptions`, `OverlayOptions` / `OverlayPosition`, `FrameOptions`, `CaptionOptions` / `CaptionPlacement`, `PaddingOptions`, `ImageMetadata`, enums such as `ResizeMode` and `WatermarkPosition`.
- `Lyo.Images.Builders`. Builders for the option types plus pipeline-builder extensions.
- `Lyo.Images.Decoration`. Internal per-primitive drawers (`OverlayDrawer`, `FrameDrawer`, `CaptionDrawer`, `OuterPaddingDrawer`) backing `ImageDecorationService`.
- `Lyo.Images.Sprite` / `Lyo.Images.Sprite.Models`. Spritesheet pipeline types.

## Build a QR badge by chaining decoration primitives

Stages run in the queued order. SVG input is supported by `Overlay`; the other primitives require raster input and throw `NotSupportedException` if the pipeline state is SVG.

## Runtime notes

- Services are safe to call from multiple threads. Each implementation validates streams and options.
- Methods take streams. Size limits come from `ImageServiceOptions`.
- Optional metrics histograms (see `Lyo.Images.Constants.Metrics`).

## Related image libraries

- [`Lyo.Images.Ocr`](../Lyo.Images.Ocr/README.md). OCR abstractions and models.
- [`Lyo.Images.OpenCv`](../Lyo.Images.OpenCv/README.md). OpenCvSharp Telea inpaint on PNG ROI buffers (optional native runtime).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `SixLabors.Fonts` `2.1.3` (direct, third-party)
- `SixLabors.ImageSharp` `3.1.12` (direct, third-party)
- `SixLabors.ImageSharp.Drawing` `2.1.7` (direct, third-party)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)