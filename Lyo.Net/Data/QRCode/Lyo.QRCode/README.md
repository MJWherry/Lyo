# Lyo.QRCode

QR code generation service for the Lyo framework using the QRCoder library.

## Features

- **Multiple Formats**: Generate QR codes in PNG, SVG, JPEG, and Bitmap formats
- **Error Correction Levels**: Support for Low, Medium, Quartile, and High error correction
- **Custom Colors**: Configure dark and light colors for QR codes
- **Icon Embedding**: Embed logos/icons in the center of QR codes (PNG, JPEG, Bitmap)
- **Batch Generation**: Generate multiple QR codes in a single operation
- **Logging**: Comprehensive logging support via Microsoft.Extensions.Logging
- **Metrics**: Optional metrics collection for monitoring QR code generation operations
- **Thread-Safe**: Safe for concurrent use
- **Async Support**: Full async/await support with cancellation token support

## Quick Start

### Basic Usage

```csharp
using Lyo.QRCode;
using Lyo.QRCode.Models;
using Microsoft.Extensions.DependencyInjection;

// Register the service
var services = new ServiceCollection();
services.AddQRCodeService(options =>
{
    options.DefaultSize = 256;
    options.DefaultFormat = QRCodeFormat.Png;
    options.DefaultErrorCorrectionLevel = QRCodeErrorCorrectionLevel.Medium;
    options.EnableMetrics = true;
});

var serviceProvider = services.BuildServiceProvider();
var qrCodeService = serviceProvider.GetRequiredService<IQRCodeService>();

// Generate a QR code
var result = await qrCodeService.GenerateAsync("https://example.com");

if (result.IsSuccess)
{
    // Save to file
    await File.WriteAllBytesAsync("qrcode.png", result.ImageBytes!);
}
```

### Generate with Custom Options

```csharp
var options = new QRCodeOptions
{
    Format = QRCodeFormat.Png,
    Size = 512,
    ErrorCorrectionLevel = QRCodeErrorCorrectionLevel.High,
    DarkColor = "#000000",
    LightColor = "#FFFFFF",
    DrawQuietZones = true
};

var result = await qrCodeService.GenerateAsync("https://example.com", options);
```

### Generate to File

```csharp
await qrCodeService.GenerateToFileAsync(
    "https://example.com",
    "qrcode.png",
    new QRCodeOptions { Size = 256 }
);
```

### Generate to Stream

```csharp
await using var stream = new MemoryStream();
await qrCodeService.GenerateToStreamAsync(
    "https://example.com",
    stream,
    new QRCodeOptions { Format = QRCodeFormat.Svg }
);
```

### Generate with Icon

```csharp
var iconBytes = await File.ReadAllBytesAsync("logo.png");
var options = new QRCodeOptions
{
    Size = 512,
    Icon = new QRCodeIconOptions
    {
        IconBytes = iconBytes,
        IconSizePercent = 20,
        DrawIconBorder = true
    }
};

var result = await qrCodeService.GenerateAsync("https://example.com", options);
```

### Batch Generation

```csharp
var requests = new[]
{
    new QRCodeRequest { Data = "https://example.com/page1", Id = "page1" },
    new QRCodeRequest { Data = "https://example.com/page2", Id = "page2" },
    new QRCodeRequest 
    { 
        Data = "https://example.com/page3", 
        Id = "page3",
        Options = new QRCodeOptions { Size = 512 }
    }
};

var batchResult = await qrCodeService.GenerateBatchAsync(requests);
Console.WriteLine($"Generated {batchResult.SuccessCount}/{batchResult.TotalCount} QR codes");
```

## Configuration Options

### QRCodeServiceOptions

- `DefaultSize` (int): Default QR code size in pixels (default: 256)
- `DefaultErrorCorrectionLevel` (QRCodeErrorCorrectionLevel): Default error correction level (default: Medium)
- `DefaultFormat` (QRCodeFormat): Default output format (default: PNG)
- `MinSize` (int): Minimum QR code size in pixels (default: 50)
- `MaxSize` (int): Maximum QR code size in pixels (default: 2000)
- `EnableMetrics` (bool): Enable metrics collection (default: false)

### QRCodeOptions

- `Format` (QRCodeFormat): Output format (PNG, SVG, JPEG, Bitmap)
- `Size` (int): QR code size in pixels
- `ErrorCorrectionLevel` (QRCodeErrorCorrectionLevel): Error correction level (Low, Medium, Quartile, High)
- `DarkColor` (string): Dark color in hex format (e.g., "#000000")
- `LightColor` (string): Light color in hex format (e.g., "#FFFFFF")
- `DrawQuietZones` (bool): Whether to draw quiet zones (default: true)
- `Icon` (QRCodeIconOptions): Optional icon/logo to embed

## Error Correction Levels

- **Low (L)**: ~7% recovery - Suitable for high-quality printing
- **Medium (M)**: ~15% recovery - Default, good balance
- **Quartile (Q)**: ~25% recovery - Good for damaged QR codes
- **High (H)**: ~30% recovery - Maximum error correction

## Production Readiness

✅ **Thread-Safe**: Safe for concurrent use  
✅ **Error Handling**: Comprehensive error handling with detailed error messages  
✅ **Logging**: Full logging support via Microsoft.Extensions.Logging  
✅ **Metrics**: Optional metrics collection for monitoring  
✅ **Async Support**: Full async/await support with cancellation tokens  
✅ **Validation**: Input validation for size limits and data requirements  
✅ **Test Coverage**: Comprehensive test coverage (recommended)




<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.QRCode.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions` | `[10,)` |

### Project references

- `Lyo.Codes.ZXing`
- `Lyo.Common`
- `Lyo.Exceptions`
- `Lyo.Images`
- `Lyo.Metrics`

## Public API (generated)

Top-level `public` types in `*.cs` (*21*). Nested types and file-scoped namespaces may omit some entries.

- `BlockedModules`
- `BuiltInQRCodeService`
- `ColorType`
- `Compression`
- `Constants`
- `ECCLevel`
- `EciMode`
- `IQRCodeService`
- `IsExternalInit`
- `Metrics`
- `PolynumEnumerator`
- `QRCodeBuilder`
- `QRCodeErrorCodes`
- `QRCodeErrorCorrectionLevel`
- `QRCodeFormat`
- `QRCodeIconOptions`
- `QRCodeOptions`
- `QRCodeRequest`
- `QRCodeServiceExtensions`
- `QRCodeServiceOptions`
- `QRCodeZxingRead`

<!-- LYO_README_SYNC:END -->

## License

Copyright © Lyo

