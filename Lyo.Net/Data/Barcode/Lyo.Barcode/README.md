# Lyo.Barcode

Contracts for generating and decoding barcodes: IBarcodeService, request and options models, plus BarcodeBuilder. Companion packages such as Lyo.Barcode.Native own concrete rendering and symbology.

## Examples

### First generate call

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

### Build with the fluent API

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

### Read a barcode from image bytes

```csharp
var read = await barcodes.ReadFromImageAsync(pngBytes);
if (read.IsSuccess && read.Data != null)
    Console.WriteLine(read.Data.Text);
```

## Public types

| Type | Description |
| ------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| IBarcodeService | Build barcodes from a string or BarcodeBuilder, write them to a stream or file, run a batch, and decode raster bytes. |
| BarcodeBuilder | Fluent builder for payload, BarcodeSymbology, and BarcodeOptions (module width, colors, human-readable text, border). |
| BarcodeRequest | One batch item: Data, Symbology, optional Options, optional Id. |
| BarcodeOptions | Raster/SVG size, colors, quiet zone, caption under the bars, optional border frame. |
| BarcodeServiceOptions | Limits and defaults implementations inherit from the host. See SectionName. |
| BarcodeResult | Result<BarcodeRequest> carrying ImageBytes, dimensions, and format. |
| BarcodeImageReadResult | What the decoder returns: Text, FormatName. |
| BarcodeSymbology, BarcodeFormat | Symbologies and output formats that are supported. |
| BarcodeErrorCodes | Stable failure code strings. |

## How the border is drawn

If BarcodeOptions.ShowBorder is true, Lyo.Barcode.Native grows width and height by 2 × BorderWidthPixels and paints a filled frame in BorderColorHex around the symbol. The usual background and bars stay the same inside that inset. BarcodeServiceOptions.MinBorderWidthPixels / MaxBorderWidthPixels clamp the width (defaults 1 to 64). When the border is on, BorderColorHex must be #RGB or #RRGGBB.

## Binding from configuration

Implementations can bind BarcodeServiceOptions from the BarcodeService section (see BarcodeServiceOptions.SectionName).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)