# Lyo.Images.Skia

**SkiaSharp** implementation of **`IImageService`** from [`Lyo.Images`](../Lyo.Images/README.md): resize, crop, rotate, watermark, format conversion, thumbnails, compression,
metadata (with optional **MetadataExtractor**-based EXIF in the Skia pipeline), palette extraction, and batch processing.

## Examples

### Usage

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

```csharp
using Lyo.Images;
using Microsoft.Extensions.DependencyInjection;

services.AddImageSharpImageService(); // registers IImageDecorationService
services.AddSkiaImageService(); // overrides IImageService with Skia
```

## When to use Skia vs ImageSharp

|                | **Lyo.Images.Skia**                                                                                                                                                                                                                                                 | **Lyo.Images (ImageSharp)**                                              |
|----------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------|
| **Platforms**  | Strong on Linux/mobile; native Skia assets.                                                                                                                                                                                                                         | Pure managed; broad format support.                                      |
| **EXIF**       | Extended EXIF via MetadataExtractor where wired.                                                                                                                                                                                                                    | Rich EXIF via ImageSharp metadata APIs.                                  |
| **Decoration** | Inherits the ImageSharp-backed `IImageDecorationService` primitives via `ImageServiceBase`; `AddSkiaImageService` does **not** auto-register `IImageDecorationService` separately. Add `AddImageSharpImageService` first if you want it resolvable as its own type. | `AddImageSharpImageService` registers `IImageDecorationService` as well. |

## Public API

| Type                    | Description                                                                                                                                      |
|-------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------|
| **`SkiaImageService`**  | `IImageService` implementation using SkiaSharp bitmap decode/encode.                                                                             |
| **`Extensions`**        | **`AddSkiaImageService`**, **`AddSkiaImageServiceFromConfiguration`** — same **`ImageServiceOptions`** / `"ImageService"` section as ImageSharp. |
| **`Constants.Metrics`** | Metric name strings for Skia operation timings.                                                                                                  |

Internal helpers (**`SkiaExifExtractor`**, etc.) are not part of the supported public contract.

## Decoration primitives

`SkiaImageService` inherits the ImageSharp-backed `IImageDecorationService` primitives (`OverlayAsync`, `AddFrameAsync`, `AddCaptionAsync`, `AddOuterPaddingAsync`, plus the
`Pipeline(...)` fluent API) through `ImageServiceBase`. To resolve `IImageDecorationService` as its own DI registration alongside Skia, add the ImageSharp registration first:

## Advantages of SkiaSharp

- Cross-platform native rendering performance.
- Solid text rendering for watermarks.
- Broad raster format support (JPEG, PNG, WebP, GIF, BMP, ICO, etc.—subject to Skia build).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Images` — (direct, lyo)
- `Lyo.Metrics` — (direct, lyo)
- `Lyo.Result` — (direct, lyo)
- `MetadataExtractor` `2.9.3` — (direct, third-party)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (direct, microsoft)
- `SkiaSharp` `3.*` — (direct, third-party)
- `SkiaSharp.NativeAssets.Linux.NoDependencies` `3.*` — (direct, third-party)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `SixLabors.Fonts` `2.1.3` — (transitive, third-party)
- `SixLabors.ImageSharp` `3.1.12` — (transitive, third-party)
- `SixLabors.ImageSharp.Drawing` `2.1.7` — (transitive, third-party)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)