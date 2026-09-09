# Lyo.Images.Skia

SkiaSharp `IImageService` for [`Lyo.Images`](../Lyo.Images/README.md): resize, crop, rotate, watermark, format conversion, thumbnails, compression, metadata (optional MetadataExtractor EXIF in the Skia pipeline), palette extraction, and batch processing.

## Examples

### Resize with Skia

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

### Register decoration plus Skia

```csharp
using Lyo.Images;
using Microsoft.Extensions.DependencyInjection;

services.AddImageSharpImageService(); // registers IImageDecorationService
services.AddSkiaImageService(); // overrides IImageService with Skia
```

## Skia compared with ImageSharp

| | Lyo.Images.Skia | Lyo.Images (ImageSharp) |
| ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------- |
| Platforms | Favours Linux and mobile. Native Skia assets. | Fully managed. Wider format coverage. |
| EXIF | Extended EXIF through MetadataExtractor where wired. | EXIF through ImageSharp metadata APIs. |
| Decoration | Gets the ImageSharp-backed `IImageDecorationService` primitives through `ImageServiceBase`. `AddSkiaImageService` does not register `IImageDecorationService` on its own. Call `AddImageSharpImageService` first if you want that type resolvable separately. | `AddImageSharpImageService` also registers `IImageDecorationService`. |

## Types

| Type | Description |
| ------------------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| `SkiaImageService` | `IImageService` that decodes and encodes bitmaps with SkiaSharp. |
| `Extensions` | `AddSkiaImageService`, `AddSkiaImageServiceFromConfiguration`. Uses the same `ImageServiceOptions` / `"ImageService"` section as ImageSharp. |
| `Constants.Metrics` | Metric name strings for Skia timing. |

Internal helpers (`SkiaExifExtractor`, etc.) are not a supported public contract.

## Decoration primitives

`SkiaImageService` gets the ImageSharp-backed `IImageDecorationService` primitives (`OverlayAsync`, `AddFrameAsync`, `AddCaptionAsync`, `AddOuterPaddingAsync`, plus the `Pipeline(...)` fluent API) through `ImageServiceBase`. To resolve `IImageDecorationService` as its own DI registration next to Skia, register ImageSharp first:

## Why SkiaSharp

- Renders natively on Linux and mobile.
- Watermark text rendering is reliable.
- Depending on the Skia build, raster formats include JPEG, PNG, WebP, GIF, BMP, ICO.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Configuration` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Images` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `MetadataExtractor` `2.9.3` (direct, third-party)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `SkiaSharp` `3.*` (direct, third-party)
- `SkiaSharp.NativeAssets.Linux.NoDependencies` `3.*` (direct, third-party)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `SixLabors.Fonts` `2.1.3` (transitive, third-party)
- `SixLabors.ImageSharp` `3.1.12` (transitive, third-party)
- `SixLabors.ImageSharp.Drawing` `2.1.7` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)