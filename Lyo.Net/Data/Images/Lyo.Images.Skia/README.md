# Lyo.Images.Skia

**SkiaSharp** implementation of **`IImageService`** from [`Lyo.Images`](../Lyo.Images/README.md): resize, crop, rotate, watermark, format conversion, thumbnails, compression, metadata (with optional **MetadataExtractor**-based EXIF in the Skia pipeline), palette extraction, and batch processing.

## When to use Skia vs ImageSharp

| | **Lyo.Images.Skia** | **Lyo.Images (ImageSharp)** |
|--|---------------------|-----------------------------|
| **Platforms** | Strong on Linux/mobile; native Skia assets. | Pure managed; broad format support. |
| **EXIF** | Extended EXIF via MetadataExtractor where wired. | Rich EXIF via ImageSharp metadata APIs. |
| **QR frames** | `AddSkiaImageService` does **not** auto-register **`IQrFrameLayoutService`**. Register **`QrFrameLayoutService`** (or ImageSharp + `AddImageSharpImageService`) if you need **`CompositeQrFramePngAsync`**. | `AddImageSharpImageService` registers **`IQrFrameLayoutService`** when absent. |

## Public API

| Type | Description |
|------|-------------|
| **`SkiaImageService`** | `IImageService` implementation using SkiaSharp bitmap decode/encode. |
| **`Extensions`** | **`AddSkiaImageService`**, **`AddSkiaImageServiceFromConfiguration`** — same **`ImageServiceOptions`** / `"ImageService"` section as ImageSharp. |
| **`Constants.Metrics`** | Metric name strings for Skia operation timings. |

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

### Optional: QR frame compositing with Skia-only DI

```csharp
using Lyo.Images;
using Microsoft.Extensions.DependencyInjection;

services.AddSingleton<IQrFrameLayoutService>(_ => new QrFrameLayoutService());
services.AddSkiaImageService();
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
