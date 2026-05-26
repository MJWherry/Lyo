# Lyo.Codes.ZXing

Shared **ZXing.Net + ImageSharp** image decoder used by both **`Lyo.QRCode`** and **`Lyo.Barcode`**. The single static class **`ZxingCodeImageDecoder`** turns an image byte buffer
into a neutral **`CodeReadPayload`** that callers map to QR-specific or barcode-specific result types.

This package has **no DI extensions** and **no options**: it is consumed directly by the QR/barcode services.

## Public API

| Type                                       | Description                                                                                                                                                                            |
|--------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`ZxingCodeImageDecoder`** (`static`)     | `DecodeQrCode(byte[] imageBytes)` and `DecodeBarcode(byte[] imageBytes)` returning `Result<CodeReadPayload>`.                                                                          |
| **`CodeReadPayload`** (record)             | `Text` (decoded payload) and `FormatName` (ZXing **`BarcodeFormat`** enum name, e.g. `QR_CODE`, `CODE_128`).                                                                           |
| **`ZxingDecodeErrorCodes`** (`static`)     | Stable error code constants: **`ZXING_IMAGE_EMPTY`**, **`ZXING_IMAGE_LOAD`**, **`ZXING_NO_QR`**, **`ZXING_NO_BARCODE`**.                                                               |

### `DecodeQrCode(byte[] imageBytes)`

- Loads the image with **`Image.Load<Rgba32>`** (PNG, JPEG, BMP, GIF, WebP — anything ImageSharp can decode).
- Configures **`BarcodeReader<Rgba32>`** with `AutoRotate = true`, `TryHarder = true`, `PureBarcode = false`, and `PossibleFormats = { QR_CODE }`.
- Returns the decoded text + format name on success; otherwise an `Error` with `ZXING_NO_QR`, `ZXING_IMAGE_EMPTY`, or `ZXING_IMAGE_LOAD`.

### `DecodeBarcode(byte[] imageBytes)`

Same loader and reader settings, but `PossibleFormats` covers the linear/2D set:

- **Code 128**, **Code 39**
- **EAN-13**, **EAN-8**
- **UPC-A**, **UPC-E**
- **ITF**, **Codabar**
- **PDF 417**, **Data Matrix**

Failure cases mirror QR decoding (`ZXING_NO_BARCODE` instead of `ZXING_NO_QR`).

## Usage

```csharp
using Lyo.Codes.ZXing;

var qr = ZxingCodeImageDecoder.DecodeQrCode(File.ReadAllBytes("qr.png"));
if (qr.IsSuccess)
    Console.WriteLine($"{qr.Value.FormatName}: {qr.Value.Text}");

var bc = ZxingCodeImageDecoder.DecodeBarcode(File.ReadAllBytes("barcode.png"));
```

Callers (e.g. `ZxingBarcodeService`, `QRCoderQrCodeService`) translate `CodeReadPayload.FormatName` into their own format enums.

## Dependencies

*(Synchronized from `Lyo.Codes.ZXing.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                | Version    |
|----------------------------------------|------------|
| `SixLabors.ImageSharp`                 | `3.*`      |
| `ZXing.Net`                            | `0.16.11`  |
| `ZXing.Net.Bindings.ImageSharp.V3`     | `0.16.15`  |

### Project references

- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md)

## Related packages

- [`Lyo.Barcode`](../../Barcode/Lyo.Barcode/README.md) — abstractions and DI for barcode read/generate.
- [`Lyo.Barcode.Native`](../../Barcode/Lyo.Barcode.Native/README.md) — ZXing-backed `IBarcodeService` consumer of this decoder.
- [`Lyo.QRCode`](../../QRCode/Lyo.QRCode/README.md) — QR abstractions and payload catalog.
- [`Lyo.QRCode.QRCoder`](../../QRCode/Lyo.QRCode.QRCoder/README.md) — QRCoder-backed `IQrCodeService` consumer of this decoder.
