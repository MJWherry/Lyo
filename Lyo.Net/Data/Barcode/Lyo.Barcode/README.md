# Lyo.Barcode

**Barcode generation and decoding abstractions** for Lyo: **`IBarcodeService`**, request/options models, and a fluent **`BarcodeBuilder`**. Concrete rendering and symbology support live in companion packages (for example **`Lyo.Barcode.Native`**).

## Public API overview

| Type | Description |
|------|-------------|
| **`IBarcodeService`** | Generate barcodes (string or **`BarcodeBuilder`**), stream/file output, batch, and **read** barcodes from raster bytes. |
| **`BarcodeBuilder`** | Fluent configuration of payload, **`BarcodeSymbology`**, **`BarcodeOptions`** (module width, colors, human-readable text, border). |
| **`BarcodeRequest`** | Batch item: **`Data`**, **`Symbology`**, optional **`Options`**, optional **`Id`**. |
| **`BarcodeOptions`** | Raster/SVG dimensions, colors, quiet zone, human-readable caption under bars, optional **border** frame. |
| **`BarcodeServiceOptions`** | Host limits and defaults for implementations (see **`SectionName`**). |
| **`BarcodeResult`** | **`Result<BarcodeRequest>`** carrying **`ImageBytes`**, dimensions, format. |
| **`BarcodeImageReadResult`** | Decoder output: **`Text`**, **`FormatName`**. |
| **`BarcodeSymbology`**, **`BarcodeFormat`** | Supported symbologies and output formats. |
| **`BarcodeErrorCodes`** | Stable error code strings for failures. |

## Quick start

```csharp
using Lyo.Barcode;
using Lyo.Barcode.Models;

// Assume IBarcodeService is registered (e.g. Native implementation).
IBarcodeService barcodes = /* ... */;

var result = await barcodes.GenerateAsync(
    "HELLO-128",
    BarcodeSymbology.Code128,
    new BarcodeOptions { Format = BarcodeFormat.Svg, ModuleWidthPixels = 2, BarHeightPixels = 64 });

if (result.IsSuccess && result is BarcodeResult br && br.ImageBytes != null)
    await File.WriteAllBytesAsync("out.svg", br.ImageBytes);
```

### Fluent builder

```csharp
var (data, sym, opts) = BarcodeBuilder.New()
    .WithData("SKU-12345")
    .WithSymbology(BarcodeSymbology.Code128)
    .WithFormat(BarcodeFormat.Bmp)
    .WithModuleWidthPixels(2)
    .WithBarHeightPixels(96)
    .WithShowHumanReadableTextBelow(true)
    .WithShowBorder(true)
    .WithBorderWidthPixels(4)
    .WithBorderColorHex("#000000")
    .Build();

await barcodes.GenerateAsync(data, sym, opts);
```

### Border (rendering)

When **`BarcodeOptions.ShowBorder`** is true, **`Lyo.Barcode.Native`** expands the output by **`2 × BorderWidthPixels`** on width and height and draws a filled frame in **`BorderColorHex`** around the symbol (inside that inset, the usual background and bars are unchanged). Width is clamped by **`BarcodeServiceOptions.MinBorderWidthPixels`** / **`MaxBorderWidthPixels`** (defaults **1–64**). **`BorderColorHex`** must be **`#RGB`** or **`#RRGGBB`** when the border is enabled.

### Decode from image bytes

```csharp
var read = await barcodes.ReadFromImageAsync(pngBytes);
if (read.IsSuccess && read.Data != null)
    Console.WriteLine(read.Data.Text);
```

## Configuration binding

Implementations may bind **`BarcodeServiceOptions`** from configuration using section **`BarcodeService`** (see **`BarcodeServiceOptions.SectionName`**).

## Related projects

- [`Lyo.Barcode.Native`](../Lyo.Barcode.Native/README.md) — native-backed generator/reader (when present in your solution).
- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Metrics`](../../../Core/Metrics/Lyo.Metrics/README.md)
- [`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md)
