# Lyo.Barcode.Native

Native **`IBarcodeService`** implementation for **`Lyo.Barcode`** with no third-party barcode generator dependency. Encodes **Code 128** (subset B, ASCII 32–127), rasterizes to *
*BMP** (SixLabors.ImageSharp) or **SVG**, optionally renders a **human-readable caption** under the bars (font-backed), and decodes via **`Lyo.Codes.ZXing`** through *
*`ReadFromImageAsync`**.

## Features

- **Encode:** Code 128 subset B end-to-end (start/checksum/stop computed in `Code128Encoder`); other symbologies return `BARCODE_UNSUPPORTED_SYMBOLOGY`.
- **Render:** **BMP** (`BarcodeFormat.Bmp`) and **SVG** (`BarcodeFormat.Svg`) via `BarcodeImageRenderer`.
- **Quiet zone:** clamped to at least the ISO minimum (10 modules).
- **Caption:** when `BarcodeOptions.ShowHumanReadableTextBelow` is set, BMP output draws the payload below the bars using `BarcodeBmpCaptionRenderer` (`SixLabors.Fonts` /
  `SixLabors.ImageSharp.Drawing`).
- **Border:** `BarcodeOptions.ShowBorder` + `BorderWidthPixels` + `BorderColorHex` add a frame; output grows by `2 × BorderWidthPixels` on each axis. SVG draws an outer fill plus
  inner background rect; BMP composites strips. Width is clamped between `BarcodeServiceOptions.MinBorderWidthPixels` and `MaxBorderWidthPixels` (defaults **1–64**).
  `BorderColorHex` must be `#RGB` or `#RRGGBB`.
- **Decode:** `ReadFromImageAsync(byte[])` delegates to `ZxingCodeImageDecoder.DecodeBarcode` (Code 128, Code 39, EAN, UPC, ITF, Codabar, PDF 417, Data Matrix).
- **Metrics / logging / cancellation:** when `BarcodeServiceOptions.EnableMetrics` is true and an `IMetrics` is registered, generation timings and success/failure/cancellation
  counters under `Lyo.Barcode.Constants.Metrics` are emitted.

## DI registration

The `Extensions` static class adds `IBarcodeService` → `NativeBarcodeService` plus a singleton `BarcodeServiceOptions`.

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

Variants:

- `AddNativeBarcodeService(BarcodeServiceOptions options)` — explicit options instance.
- `AddNativeBarcodeServiceFromConfiguration(IConfiguration, sectionName?)` — binds `BarcodeServiceOptions` from the **`BarcodeService`** section by default
  (`BarcodeServiceOptions.SectionName`); skipped if the options were already registered.

## Usage

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

## Dependencies

*(Synchronized from `Lyo.Barcode.Native.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                                 | Version |
|---------------------------------------------------------|---------|
| `Microsoft.Extensions.Configuration.Abstractions`       | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder`             | `[10,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions`             | `[10,)` |
| `SixLabors.Fonts`                                       | `2.*`   |
| `SixLabors.ImageSharp`                                  | `3.*`   |
| `SixLabors.ImageSharp.Drawing`                          | `2.*`   |

### Project references

- [`Lyo.Barcode`](../Lyo.Barcode/README.md)
- [`Lyo.Codes.ZXing`](../../Codes/Lyo.Codes.ZXing/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md)
