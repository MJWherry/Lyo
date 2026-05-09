# Lyo.Images

Production-ready **raster image processing** for .NET using **SixLabors.ImageSharp**. Implements **`IImageService`** (resize, crop, rotate, watermark, format conversion, thumbnails, compression, metadata, palette extraction, batch processing) plus **QR frame compositing** and **center overlay** helpers used by QR workflows.

## Public API overview

| Type | Description |
|------|-------------|
| **`IImageService`** | Primary façade for stream-based image operations and file helpers. |
| **`ImageSharpImageService`** | ImageSharp-backed `IImageService`; rich **EXIF** via ImageSharp metadata. |
| **`ImageServiceBase`** | Abstract base shared by ImageSharp and other backends; implements common `IImageService` logic. |
| **`IQrFrameLayoutService`** | Lightweight service for **`CompositeQrFramePngAsync`** only (decorative frames around a square QR PNG). Registered automatically by **`AddImageSharpImageService`***. |
| **`QrFrameLayoutService`** | Default `IQrFrameLayoutService` implementation. |
| **`ISpriteSheetExportService`** / **`SpriteSheetExportService`** | Spritesheet export, frame crops, animated GIF helpers (`Lyo.Images.Sprite`). |
| **`Extensions`** | DI registration: **`AddImageSharpImageService`**, **`AddSpriteSheetExportService`**, configuration binding. |

\*Also registers **`IQrFrameLayoutService`** if not already present.

### Namespaces

- **`Lyo.Images`** — services, DI extensions, error codes.
- **`Lyo.Images.Models`** — **`ImageServiceOptions`**, **`ImageProcessRequest`**, **`WatermarkOptions`**, **`QrFrameLayoutOptions`**, **`ImageMetadata`**, **`ImageCenterOverlayOptions`** (QR logo overlay pad/stroke), enums such as **`ResizeMode`**, **`QrFrameStyle`**, **`WatermarkPosition`**.
- **`Lyo.Images.Sprite`** / **`Lyo.Images.Sprite.Models`** — spritesheet pipeline types.

## Features

- **Resize** — Max, Crop, Pad, BoxPad, Stretch (`ResizeMode`).
- **Crop**, **Rotate**, **Watermark**, **Convert format**, **Thumbnail**, **Compress**.
- **Metadata** — Dimensions, format, optional **EXIF** (device, GPS, date taken) via ImageSharp.
- **Palette** — Dominant colors (`GetPaletteAsync`); optional ignore of transparent pixels (`ImageServiceOptions`).
- **Batch** — `ProcessBatchAsync` with `ImageProcessRequest` / `ImageOperation` subclasses.
- **QR + logo** — `CompositeCenterOverlayPngAsync` (square canvas). **`ImageCenterOverlayOptions`**: **`BorderColorHex`** fills the pad behind the centered overlay (typically QR light modules); **`DrawOverlayBorder`** draws a stroke using **`OverlayBorderStrokeHex`** when set and parseable, otherwise a dark default so the edge contrasts the pad.
- **QR frames** — `CompositeQrFramePngAsync` + `QrFrameLayoutOptions` / `QrFrameStyle` (PNG output; fonts required for captioned styles). **`CaptionFontSizePx == 0`** selects an automatic caption size scaled from the QR raster side (readable on large exports). Pass opaque **`#RRGGBB`** (or **`#RGB`**) for color properties—**`HeaderBackgroundHex`** falls back to a default slate if parsing fails. For badge layouts, set **`CardOutlineHex`** to match **`HeaderBackgroundHex`** when you want the outer card stroke to align with the header chrome.
- **Thread-safe**, **async**, **logging/metrics**, **cancellation**.

## Quick start

```csharp
using Lyo.Images;
using Lyo.Images.Models;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddImageSharpImageService(o => {
    o.DefaultQuality = 90;
    o.MaxFileSizeBytes = 100 * 1024 * 1024;
});

var sp = services.BuildServiceProvider();
var images = sp.GetRequiredService<IImageService>();

await using var input = File.OpenRead("photo.jpg");
await using var output = File.Create("photo-800.jpg");
await images.ResizeAsync(input, output, 800, 600, ResizeMode.Max, ImageFormat.Jpeg, quality: 90);
```

### QR decorative frame (PNG)

```csharp
using Lyo.Images.Models;

var frames = sp.GetRequiredService<IQrFrameLayoutService>();
var framed = await frames.CompositeQrFramePngAsync(
    qrPngBytes,
    new QrFrameLayoutOptions {
        Style = QrFrameStyle.SimpleRoundedPanel,
        CaptionText = "Scan to open"
    },
    CancellationToken.None);
```

A reference MudBlazor workbench that builds **`QrFrameLayoutOptions`** from color pickers lives in [`Lyo.QRCode.Web.Components`](../../QRCode/Lyo.QRCode.Web.Components/README.md).

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
| `Microsoft.Extensions.Configuration.Abstractions`       | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder`             | `[10,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions`             | `[10,)` |
| `SixLabors.ImageSharp`                                  | `3.*`   |
| `SixLabors.ImageSharp.Drawing`                          | `2.*`   |

### Project references

- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Metrics`](../../../Core/Metrics/Lyo.Metrics/README.md)
