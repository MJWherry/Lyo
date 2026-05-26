# Lyo.Images.Skia

**SkiaSharp** implementation of **`IImageService`** from [`Lyo.Images`](../Lyo.Images/README.md): resize, crop, rotate, watermark, format conversion, thumbnails, compression,
metadata (with optional **MetadataExtractor**-based EXIF in the Skia pipeline), palette extraction, and batch processing.

## When to use Skia vs ImageSharp

|                | **Lyo.Images.Skia**                                                                                                                                                                                                                                                | **Lyo.Images (ImageSharp)**                                              |
|----------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------|
| **Platforms**  | Strong on Linux/mobile; native Skia assets.                                                                                                                                                                                                                        | Pure managed; broad format support.                                      |
| **EXIF**       | Extended EXIF via MetadataExtractor where wired.                                                                                                                                                                                                                   | Rich EXIF via ImageSharp metadata APIs.                                  |
| **Decoration** | Inherits the ImageSharp-backed `IImageDecorationService` primitives via `ImageServiceBase`; `AddSkiaImageService` does **not** auto-register `IImageDecorationService` separately. Add `AddImageSharpImageService` first if you want it resolvable as its own type. | `AddImageSharpImageService` registers `IImageDecorationService` as well. |

## Public API

| Type                    | Description                                                                                                                                      |
|-------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------|
| **`SkiaImageService`**  | `IImageService` implementation using SkiaSharp bitmap decode/encode.                                                                             |
| **`Extensions`**        | **`AddSkiaImageService`**, **`AddSkiaImageServiceFromConfiguration`** — same **`ImageServiceOptions`** / `"ImageService"` section as ImageSharp. |
| **`Constants.Metrics`** | Metric name strings for Skia operation timings.                                                                                                  |

Internal helpers (**`SkiaExifExtractor`**, etc.) are not part of the supported public contract.

## Usage

```csharp
using Lyo.Images;
using Lyo.Images.Models;
using Lyo.Images.Skia;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddSkiaImageService(o => {
    o.DefaultQuality = 90;
    o.MaxWidth = 10_000;
    o.MaxHeight = 10_000;
});

var imageService = services.BuildServiceProvider().GetRequiredService<IImageService>();

await imageService.ResizeAsync(inputStream, outputStream, 800, 600, ResizeMode.Max, ImageFormat.Jpeg, 90);
```

### Decoration primitives

`SkiaImageService` inherits the ImageSharp-backed `IImageDecorationService` primitives (`OverlayAsync`, `AddFrameAsync`, `AddCaptionAsync`, `AddOuterPaddingAsync`,
plus the `Pipeline(...)` fluent API) through `ImageServiceBase`. To resolve `IImageDecorationService` as its own DI registration alongside Skia, add the
ImageSharp registration first:

```csharp
using Lyo.Images;
using Microsoft.Extensions.DependencyInjection;

services.AddImageSharpImageService(); // registers IImageDecorationService
services.AddSkiaImageService();       // overrides IImageService with Skia
```

## Advantages of SkiaSharp

- Cross-platform native rendering performance.
- Solid text rendering for watermarks.
- Broad raster format support (JPEG, PNG, WebP, GIF, BMP, ICO, etc.—subject to Skia build).

## Dependencies

*(Synchronized from `Lyo.Images.Skia.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                                 | Version |
|---------------------------------------------------------|---------|
| `MetadataExtractor`                                     | `2.9.0` |
| `Microsoft.Extensions.Configuration.Abstractions`       | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder`             | `[10,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions`             | `[10,)` |
| `SkiaSharp`                                             | `3.*`   |
| `SkiaSharp.NativeAssets.Linux.NoDependencies`           | `3.*`   |

### Project references

- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Images`](../Lyo.Images/README.md)
- [`Lyo.Metrics`](../../../Core/Metrics/Lyo.Metrics/README.md)
- [`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md)
