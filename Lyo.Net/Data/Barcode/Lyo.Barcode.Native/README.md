# Lyo.Barcode.Native

Native barcode implementation for **`Lyo.Barcode`**: **Code 128** (subset B) encoding, **BMP** and **SVG** rasterization (SixLabors.ImageSharp), optional **human-readable caption** under bars on BMP (font-backed path), optional **border frame** around the symbol, and **ZXing**-based decode (**`ReadFromImageAsync`**).

## Features

- **Code128** encode/decode for ASCII 32–127.
- **Quiet zone** enforced to at least ISO minimum (10 modules) in the renderer.
- **Border**: set **`BarcodeOptions.ShowBorder`**, **`BorderWidthPixels`**, **`BorderColorHex`** — output size grows by twice the border width; SVG uses an outer fill plus inner background rect; BMP composites border strips.

## Related projects

- [`Lyo.Barcode`](../Lyo.Barcode/README.md)
- [`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md)
- [`Lyo.Codes.ZXing`](../../Codes/Lyo.Codes.ZXing/README.md)
