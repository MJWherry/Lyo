# Lyo.Barcode.Native

IBarcodeService implementation for Lyo.Barcode. No third-party barcode generator. Encodes Code 128 (subset B, ASCII 32 to 127), rasterizes to BMP (SixLabors.ImageSharp) or SVG, optionally draws a human-readable caption under the bars, and decodes images via BarcodeZxingRead (ZXing.Net and ImageSharp) through ReadFromImageAsync.

## Features

- Code 128 subset B end-to-end. Start, checksum, and stop are computed in Code128Encoder. Other symbologies return BARCODE_UNSUPPORTED_SYMBOLOGY.
- BMP (BarcodeFormat.Bmp) and SVG (BarcodeFormat.Svg) via BarcodeImageRenderer.
- Quiet zone is clamped to at least the ISO minimum of 10 modules.
- When BarcodeOptions.ShowHumanReadableTextBelow is set, BMP output draws the payload below the bars using BarcodeBmpCaptionRenderer (SixLabors.Fonts / SixLabors.ImageSharp.Drawing).
- BarcodeOptions.ShowBorder, BorderWidthPixels, and BorderColorHex add a frame. Output grows by 2 × BorderWidthPixels on each axis. SVG draws an outer fill plus inner background rect. BMP composites strips. Width is clamped between BarcodeServiceOptions.MinBorderWidthPixels and MaxBorderWidthPixels (defaults 1 to 64). BorderColorHex must be #RGB or #RRGGBB.
- ReadFromImageAsync(byte[]) delegates to BarcodeZxingRead.Decode (Code 128, Code 39, EAN, UPC, ITF, Codabar, PDF 417, Data Matrix).
- When BarcodeServiceOptions.EnableMetrics is true and an IMetrics is registered, generation timings and success, failure, and cancellation counters under Lyo.Barcode.Constants.Metrics are emitted.

## Examples

### DI registration

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

### Usage

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

## DI registration

- AddNativeBarcodeService(BarcodeServiceOptions options). Options instance.
- AddNativeBarcodeServiceFromConfiguration(IConfiguration, sectionName?). Binds BarcodeServiceOptions from the BarcodeService section by default (BarcodeServiceOptions.SectionName). Skipped if the options were already registered.

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
- `Lyo.Common` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)