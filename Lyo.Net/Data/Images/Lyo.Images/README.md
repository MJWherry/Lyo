# Lyo.Images

A production-ready image processing library for .NET using SixLabors.ImageSharp.

## Features

- ✅ **Resize** - Resize images with multiple modes (Max, Crop, Pad, BoxPad, Stretch)
- ✅ **Crop** - Crop images to specific rectangles
- ✅ **Rotate** - Rotate images by degrees
- ✅ **Watermark** - Add text watermarks to images
- ✅ **Format Conversion** - Convert between image formats (JPEG, PNG, WebP, etc.)
- ✅ **Thumbnail Generation** - Generate thumbnails from images
- ✅ **Compression** - Compress images with quality control
- ✅ **Metadata** - Extract image metadata (dimensions, format, EXIF: location, device, date taken, etc.)
- ✅ **Batch Processing** - Process multiple images efficiently
- ✅ **Thread-Safe** - Safe for concurrent operations
- ✅ **Streaming Support** - Works with streams for memory efficiency

## Quick Start

```csharp
using Lyo.Images;
using Lyo.Images.Models;

var service = new ImageSharpImageService(options, logger);

// Resize an image
await service.ResizeAsync(
    inputStream,
    outputStream,
    width: 800,
    height: 600,
    resizeMode: ResizeMode.Max);

// Generate thumbnail
var thumbnail = await service.GenerateThumbnailAsync(
    inputStream,
    maxWidth: 200,
    maxHeight: 200);

// Get metadata (including EXIF: location, device, date taken)
var result = await service.GetMetadataAsync(imageStream);
// Or: await service.GetMetadataFromFileAsync("/path/to/image.jpg");
if (result.IsSuccess && result.Data != null) {
    var m = result.Data;
    Console.WriteLine($"Image: {m.Width}x{m.Height} {m.Format}");
    if (m.ExifInfo != null) {
        Console.WriteLine($"Device: {m.ExifInfo.CameraMake} {m.ExifInfo.CameraModel}");
        if (m.ExifInfo.Latitude.HasValue && m.ExifInfo.Longitude.HasValue)
            Console.WriteLine($"Location: {m.ExifInfo.Latitude}, {m.ExifInfo.Longitude}");
        if (m.ExifInfo.DateTimeTaken.HasValue)
            Console.WriteLine($"Taken: {m.ExifInfo.DateTimeTaken}");
    }
}
```

## Production Ready

This library has been designed for production use and includes:

- ✅ Thread-safe operations
- ✅ Comprehensive error handling
- ✅ Input validation
- ✅ Streaming support for large images
- ✅ Logging and metrics support
- ✅ Cancellation token support
- ✅ Extensible architecture




<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Images.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions` | `[10,)` |
| `SixLabors.ImageSharp` | `3.*` |
| `SixLabors.ImageSharp.Drawing` | `2.*` |

### Project references

- `Lyo.Common`
- `Lyo.Exceptions`
- `Lyo.Metrics`

## Public API (generated)

Top-level `public` types in `*.cs` (*28*). Nested types and file-scoped namespaces may omit some entries.

- `CompressOperation`
- `Constants`
- `ConvertFormatOperation`
- `CropOperation`
- `ExifUserCommentDecoder`
- `Extensions`
- `IImageService`
- `ImageCenterOverlayOptions`
- `ImageErrorCodes`
- `ImageOperation`
- `ImageProcessRequest`
- `ImageServiceBase`
- `ImageServiceOptions`
- `ImageSharpImageService`
- `IsExternalInit`
- `ISpriteSheetExportService`
- `Metrics`
- `ResizeMode`
- `ResizeOperation`
- `RotateOperation`
- `SpriteGridPadMode`
- `SpriteSheetCalculation`
- `SpriteSheetCalculator`
- `SpriteSheetExportService`
- `SpriteSheetOptions`
- `WatermarkOperation`
- `WatermarkOptions`
- `WatermarkPosition`

<!-- LYO_README_SYNC:END -->

## License

[Your License Here]

