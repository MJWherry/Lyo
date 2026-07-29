# Lyo.QRCode.QRCoder

**QRCoder**-backed implementation of **`IQRCodeService`** from [`Lyo.QRCode`](../Lyo.QRCode/README.md). Pick this when you need **JPEG / Bitmap** output on Windows or want to use
QRCoder's mature renderers; pick the built-in **`BuiltInQRCodeService`** when you want the in-library ISO encoder with no extra NuGet.

## Examples

### Usage

```csharp
using Lyo.QRCode;
using Lyo.QRCode.Models;
using Lyo.QRCode.QRCoder;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddQRCoderQrCodeService(o => {
    o.DefaultSize = 16; // pixels per module
    o.DefaultFormat = QRCodeFormat.Png; // Jpeg / Bmp also supported via QRCoder
    o.DefaultErrorCorrectionLevel = QRCodeErrorCorrectionLevel.Medium;
});

var qr = services.BuildServiceProvider().GetRequiredService<IQRCodeService>();
var result = await qr.GenerateAsync("https://example.com");
```

### Configuration binding

```csharp
services.AddQRCoderQrCodeServiceFromConfiguration(builder.Configuration);
```

### Configuration binding (2)

```json
{
  "QRCodeService": {
    "DefaultSize": 16,
    "DefaultFormat": "Png",
    "DefaultErrorCorrectionLevel": "Medium",
    "MinSize": 1,
    "MaxSize": 2000,
    "EnableMetrics": false
  }
}
```

## Public API

| Type | Description |
| ------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`QRCoderQRCodeService`** | `IQRCodeService` implementation. No image dependencies — decoration (logo, frame, caption, padding) is the consumer's job via `Lyo.Images.IImageDecorationService`. |
| **`QRCoderQrCodeServiceExtensions`** | DI: `AddQRCoderQrCodeService(Action<QRCodeServiceOptions>?)`, `AddQRCoderQrCodeService(QRCodeServiceOptions)`, `AddQRCoderQrCodeServiceFromConfiguration(IConfiguration, sectionName?)`. |

## Notes

- **JPEG / Bitmap** outputs go through `System.Drawing` and are only fully supported on **Windows**; PNG/SVG paths run cross-platform.
- Decoding (`ReadFromImageAsync`) uses **`QRCodeZxingRead`** in this package (ZXing.Net + ImageSharp), same as the built-in service.
- **Decoration is out of scope**: the QRCoder service no longer applies center logos or frames. Pipe the returned bytes through `Lyo.Images.IImageDecorationService.Pipeline(...)` and call `Overlay` / `AddFrame` / `AddCaption` / `AddOuterPadding` as needed (see the [`Lyo.Images` README](../../Images/Lyo.Images/README.md)).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.QRCode` — (direct, lyo)
- `Lyo.Result` — (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (direct, microsoft)
- `QRCoder` `1.8.0` — (direct, third-party)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `SixLabors.ImageSharp` `3.1.12` — (transitive, third-party)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `ZXing.Net` `0.16.11` — (transitive, third-party)
- `ZXing.Net.Bindings.ImageSharp.V3` `0.16.18` — (transitive, third-party)