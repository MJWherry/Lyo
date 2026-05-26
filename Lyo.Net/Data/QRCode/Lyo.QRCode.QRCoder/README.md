# Lyo.QRCode.QRCoder

**QRCoder**-backed implementation of **`IQRCodeService`** from [`Lyo.QRCode`](../Lyo.QRCode/README.md). Pick this when you need **JPEG / Bitmap** output on Windows or want to use
QRCoder's mature renderers; pick the built-in **`BuiltInQRCodeService`** when you want the in-library ISO encoder with no extra NuGet.

## Public API

| Type                                  | Description                                                                                                                                                |
|---------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`QRCoderQRCodeService`**            | `IQRCodeService` implementation. Optional dependencies: `IImageService` and `IQrFrameLayoutService` (used when QR options include an icon or frame).        |
| **`QRCoderQrCodeServiceExtensions`**  | DI: `AddQRCoderQrCodeService(Action<QRCodeServiceOptions>?)`, `AddQRCoderQrCodeService(QRCodeServiceOptions)`, `AddQRCoderQrCodeServiceFromConfiguration(IConfiguration, sectionName?)`. Each variant also registers `IQrFrameLayoutService` if one isn't already in the container. |

## Usage

```csharp
using Lyo.QRCode;
using Lyo.QRCode.Models;
using Lyo.QRCode.QRCoder;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddQRCoderQrCodeService(o => {
    o.DefaultSize = 16;                        // pixels per module
    o.DefaultFormat = QRCodeFormat.Png;        // Jpeg / Bmp also supported via QRCoder
    o.DefaultErrorCorrectionLevel = QRCodeErrorCorrectionLevel.Medium;
});

var qr = services.BuildServiceProvider().GetRequiredService<IQRCodeService>();
var result = await qr.GenerateAsync("https://example.com");
```

### Configuration binding

```csharp
services.AddQRCoderQrCodeServiceFromConfiguration(builder.Configuration);
```

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

## Notes

- **JPEG / Bitmap** outputs go through `System.Drawing` and are only fully supported on **Windows**; PNG/SVG paths run cross-platform.
- Decoding (`ReadFromImageAsync`) reuses `Lyo.Codes.ZXing` exactly like the built-in service.

## Dependencies

*(Synchronized from `Lyo.QRCode.QRCoder.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                                 | Version |
|---------------------------------------------------------|---------|
| `Microsoft.Extensions.Configuration.Abstractions`       | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder`             | `[10,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions`             | `[10,)` |
| `QRCoder`                                               | `1.*`   |

### Project references

- [`Lyo.Images`](../../Images/Lyo.Images/README.md)
- [`Lyo.QRCode`](../Lyo.QRCode/README.md)
- [`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md)
