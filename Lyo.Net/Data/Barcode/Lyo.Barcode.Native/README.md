# Lyo.Barcode.Native

Lyo.Barcode `IBarcodeService` with no third-party barcode generator. Encodes Code 128 (subset B, ASCII 32 to 127), rasterizes to BMP (SixLabors.ImageSharp) or SVG, can draw a human-readable caption under the bars, and decodes images through ReadFromImageAsync via BarcodeZxingRead (ZXing.Net and ImageSharp).

## Features

- Code 128 subset B end to end. Code128Encoder computes start, checksum, and stop. Other symbologies return BARCODE_UNSUPPORTED_SYMBOLOGY.
- BMP (BarcodeFormat.Bmp) and SVG (BarcodeFormat.Svg) from BarcodeImageRenderer.
- Quiet zone is never smaller than the ISO minimum of 10 modules.
- If BarcodeOptions.ShowHumanReadableTextBelow is set, BMP output paints the payload under the bars with BarcodeBmpCaptionRenderer (SixLabors.Fonts / SixLabors.ImageSharp.Drawing).
- A frame comes from BarcodeOptions.ShowBorder, BorderWidthPixels, and BorderColorHex. Each axis grows by 2 × BorderWidthPixels. SVG uses an outer fill plus an inner background rect. BMP composites strips. Width stays between BarcodeServiceOptions.MinBorderWidthPixels and MaxBorderWidthPixels (defaults 1 to 64). BorderColorHex must be #RGB or #RRGGBB.
- ReadFromImageAsync(byte[]) calls BarcodeZxingRead.Decode (Code 128, Code 39, EAN, UPC, ITF, Codabar, PDF 417, Data Matrix).
- If BarcodeServiceOptions.EnableMetrics is true and an IMetrics is registered, generation timings plus success, failure, and cancellation counters under Lyo.Barcode.Constants.Metrics are emitted.

## Examples

### Add the service in DI

```csharp
using Lyo.Barcode;
using Lyo.Barcode.Native;
using Microsoft.Extensions.DependencyInjection;

services.AddNativeBarcodeService(o => {
    o.DefaultFormat = BarcodeFormat.Svg;
    o.DefaultModuleWidthPixels = 2;
    o.DefaultBarHeightPixels = 64;
    o.EnableMetrics = false;
});
```

### Generate and decode

```csharp
var barcodes = sp.GetRequiredService<IBarcodeService>();

var result = await barcodes.GenerateAsync(
    "HELLO-128",
    BarcodeSymbology.Code128,
    new BarcodeOptions {
        Format = BarcodeFormat.Bmp,
        ModuleWidthPixels = 2,
        BarHeightPixels = 80,
        ShowHumanReadableTextBelow = true,
        ShowBorder = true,
        BorderWidthPixels = 6,
        BorderColorHex = "#000000"
    });

if (result.IsSuccess && result is BarcodeResult br)
    await File.WriteAllBytesAsync("out.bmp", br.ImageBytes!);

var read = await barcodes.ReadFromImageAsync(File.ReadAllBytes("photo.png"));
```

## Container helpers

- AddNativeBarcodeService(BarcodeServiceOptions options). Pass an options instance.
- AddNativeBarcodeServiceFromConfiguration(IConfiguration, sectionName?). Binds BarcodeServiceOptions from the BarcodeService section by default (BarcodeServiceOptions.SectionName). No-op if the options were already registered.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Barcode` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `SixLabors.Fonts` `2.1.3` (direct, third-party)
- `SixLabors.ImageSharp` `3.1.12` (direct, third-party)
- `SixLabors.ImageSharp.Drawing` `2.1.7` (direct, third-party)
- `ZXing.Net` `0.16.11` (direct, third-party)
- `ZXing.Net.Bindings.ImageSharp.V3` `0.16.18` (direct, third-party)
- `Lyo.Metrics` (transitive, lyo)