# Lyo.QRCode.QRCoder

`IQRCodeService` from [`Lyo.QRCode`](../Lyo.QRCode/README.md) implemented with QRCoder. Pick this for JPEG / Bitmap on Windows or for QRCoder's renderers. `BuiltInQRCodeService` is the in-library ISO encoder with no extra NuGet.

## Examples

### Generate with QRCoder

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

### Bind options from configuration

```csharp
services.AddQRCoderQrCodeServiceFromConfiguration(builder.Configuration);
```

### appsettings sample

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

## Types

| Type | Description |
| -------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `QRCoderQRCodeService` | `IQRCodeService` implementation. No image dependencies. Logo, frame, caption, and padding are the caller's job through `Lyo.Images.IImageDecorationService`. |
| `QRCoderQrCodeServiceExtensions` | Container helpers: `AddQRCoderQrCodeService(Action<QRCodeServiceOptions>?)`, `AddQRCoderQrCodeService(QRCodeServiceOptions)`, `AddQRCoderQrCodeServiceFromConfiguration(IConfiguration, sectionName?)`. |

## Platform and decoration

- JPEG / Bitmap go through `System.Drawing` and run on Windows. PNG/SVG paths are cross-platform.
- `ReadFromImageAsync` decoding uses `QRCodeZxingRead` in this package (ZXing.Net + ImageSharp), matching the built-in service.
- **Decoration is out of scope.** Center logos and frames are no longer applied by the QRCoder service. Send the returned bytes through `Lyo.Images.IImageDecorationService.Pipeline(...)` and call `Overlay` / `AddFrame` / `AddCaption` / `AddOuterPadding` as needed (see the [`Lyo.Images` README](../../Images/Lyo.Images/README.md)).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.QRCode` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `QRCoder` `1.8.0` (direct, third-party)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `SixLabors.ImageSharp` `3.1.12` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `ZXing.Net` `0.16.11` (transitive, third-party)
- `ZXing.Net.Bindings.ImageSharp.V3` `0.16.18` (transitive, third-party)