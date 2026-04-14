# Lyo.Images.Skia

Image processing library for .NET using SkiaSharp. This library provides a SkiaSharp-based implementation of the `IImageService` interface.

## Features

- **Resize** - Resize images with multiple resize modes (Max, Crop, Pad, BoxPad, Stretch)
- **Crop** - Crop images to specified rectangles
- **Rotate** - Rotate images by degrees
- **Watermark** - Add text watermarks with customizable fonts, colors, positions, and opacity
- **Format Conversion** - Convert between JPEG, PNG, WebP, GIF, BMP, ICO formats
- **Thumbnail Generation** - Generate thumbnails with aspect ratio preservation
- **Compression** - Compress images with quality control
- **Metadata** - Extract image metadata (dimensions, format, etc.)
- **Batch Processing** - Process multiple images in batch operations

## Usage

```csharp
using Lyo.Images;
using Lyo.Images.Models;
using Lyo.Images.Skia;

// Create service with options
var options = new ImageServiceOptions
{
    DefaultQuality = 90,
    MaxWidth = 10000,
    MaxHeight = 10000,
    MaxFileSizeBytes = 100 * 1024 * 1024
};

IImageService imageService = new SkiaImageService(options, logger);

// Resize an image
await imageService.ResizeAsync(
    inputStream,
    outputStream,
    width: 800,
    height: 600,
    resizeMode: ResizeMode.Max,
    format: ImageFormat.Jpeg,
    quality: 90
);

// Add watermark
var watermarkOptions = new WatermarkOptions
{
    FontSize = 24,
    FontFamily = "Arial",
    TextColor = "#FFFFFF",
    Position = WatermarkPosition.BottomRight,
    Opacity = 0.7f,
    Padding = 10
};

await imageService.WatermarkAsync(
    inputStream,
    outputStream,
    "Copyright 2024",
    watermarkOptions
);

// Generate thumbnail
var thumbnailBytes = await imageService.GenerateThumbnailAsync(
    inputStream,
    maxWidth: 200,
    maxHeight: 200,
    format: ImageFormat.Jpeg
);
```

## Advantages of SkiaSharp

- **Cross-platform** - Works on Linux, Windows, macOS, iOS, Android
- **High Performance** - Optimized native rendering engine
- **Advanced Text Rendering** - Excellent watermark and text capabilities
- **Wide Format Support** - Supports JPEG, PNG, WebP, GIF, BMP, ICO, and more
- **Professional Graphics** - Based on Google's Skia graphics library

## Requirements

- .NET 10.0 or later
- SkiaSharp 2.88.x or later




<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Images.Skia.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `MetadataExtractor` | `2.9.0` |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions` | `[10,)` |
| `SkiaSharp` | `3.*` |
| `SkiaSharp.NativeAssets.Linux.NoDependencies` | `3.*` |

### Project references

- `Lyo.Common`
- `Lyo.Exceptions`
- `Lyo.Images`
- `Lyo.Metrics`

## Public API (generated)

Top-level `public` types in `*.cs` (*4*). Nested types and file-scoped namespaces may omit some entries.

- `Constants`
- `Extensions`
- `Metrics`
- `SkiaImageService`

<!-- LYO_README_SYNC:END -->

## License

Copyright © Lyo