# Lyo.QRCode

**QR code generation and reading** for Lyo: **`IQRCodeService`**, **`QRCodeBuilder`**, ISO **Model 2** encoding in-box (**`BuiltInQRCodeService`**), optional **QRCoder** adapter
package **`Lyo.QRCode.QRCoder`**, and typed **payload helpers** (`Lyo.QRCode.Payloads`) for Wi‑Fi, URLs, vCard, `mailto:`, etc.

## Architecture

| Piece                      | Role                                                                                                                                                                                 |
|----------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`IQRCodeService`**       | Generate to memory, stream, file; batch; **`ReadFromImageAsync`** (ZXing).                                                                                                           |
| **`BuiltInQRCodeService`** | In-library **PNG/SVG** rasterization; **no QRCoder NuGet** for encode. JPEG/BMP not supported here (platform / format limits).                                                       |
| **`Lyo.QRCode.QRCoder`**   | Optional **`QRCoderQRCodeService`** + **`AddQRCoderQrCodeService`** for JPEG/Bitmap on Windows and QRCoder-based render path.                                                        |
| **`QRCodeBuilder`**        | Fluent **`QRCodeOptions`** + **`WithData`** / **`WithPayload(IQrPayload)`**.                                                                                                         |
| **`Payloads`**             | **`IQrPayload`**, **`QrPayloadKind`**, **`WifiQrPayload`**, **`HttpUrlPayload`**, contacts, URI schemes, messaging URLs — all serialize to the string passed to **`GenerateAsync`**. |

## Quick start (built-in encoder)

```csharp
using Lyo.QRCode;
using Lyo.QRCode.Models;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddQRCodeService(o => {
    o.DefaultSize = 16; // pixels per module, not total width
    o.DefaultFormat = QRCodeFormat.Png;
    o.DefaultErrorCorrectionLevel = QRCodeErrorCorrectionLevel.Medium;
});

var qr = services.BuildServiceProvider().GetRequiredService<IQRCodeService>();
var result = await qr.GenerateAsync("https://example.com");

if (result.IsSuccess && result is QRCodeResult r && r.ImageBytes != null)
    await File.WriteAllBytesAsync("qr.png", r.ImageBytes);
```

### Typed payload (Wi‑Fi example)

```csharp
using Lyo.QRCode;
using Lyo.QRCode.Payloads;

var payload = new WifiQrPayload("MySSID", "secret", QrWifiSecurityType.Wpa);
var (_, opts) = QRCodeBuilder.New()
    .WithPayload(payload)
    .WithFormat(QRCodeFormat.Png)
    .WithSize(12)
    .Build();
```

### Optional: QRCoder package

Add project/package reference to **`Lyo.QRCode.QRCoder`** and call **`AddQRCoderQrCodeService`** (or **`AddQRCoderQrCodeServiceFromConfiguration`**) instead of or in addition to *
*`AddQRCodeService`**, depending on how you register **`IQRCodeService`**.

## Payload helpers (`Lyo.QRCode.Payloads`)

- **Wi‑Fi (`WifiQrPayload`)** — Omits **`H:`** when the SSID is not hidden (better phone compatibility than **`H:false`**). Open networks omit **`P`** (not **`P:;`**).
- **SMS (`SmsPayload`)** — Defaults to **`sms:`** URI scheme; **`smsto:`** is opt-in (some Android SMS apps crash on long **`smsto:`** bodies). Very long URIs throw (*
  *`MaxSmsQrUriLength`**) to avoid app crashes.

## Key options

- **`QRCodeOptions.Size`** — **Pixels per module** (each black/white square), not the full image width. Total size ≈ module count per side × **`Size`** (decoration applied
  separately may grow that further).
- **`QRCodeOptions.Icon`** — **Hint only** for the encoder: the only field consumed is **`IconSizePercent`**, which bumps the effective ECC level so a planned center logo
  doesn't break scanning. **`IconBytes`**, **`IconFilePath`**, and **`DrawIconBorder`** are metadata for the consumer's overlay call — the QR encoder never composites the icon.
  Apply the actual overlay (and any frame/caption/padding) post-generation through **`Lyo.Images.IImageDecorationService`** (see migration note below).

### Migration: decoration moved out of the encoder

`QRCodeOptions.Frame`, `QRCodeBuilder.WithFrame(...)`, and the optional `IImageService` / `IQrFrameLayoutService` constructor parameters on `BuiltInQRCodeService` are gone.
Compose icons and chrome on the returned bytes with `Lyo.Images`:

```csharp
using Lyo.Images;
using Lyo.Images.Builders;
using Lyo.Common.Enums;

var qr = await qrService.GenerateAsync(data, options);
var qrBytes = ((QRCodeResult)qr).ImageBytes!;

var decorated = await decoration.Pipeline(qrBytes)
    .Overlay(logoBytes, b => b.WithOverlaySizePercent(18).WithPadColor("#FFFFFF").WithBorder("#000000"))
    .AddCaption(b => b.WithText("Scan Me").WithNotch())
    .AddOuterPadding(b => b.WithPanelColor("#FFFFFF").WithCornerRadius(16))
    .AddFrame(b => b.WithStrokeColor("#1e293b").WithStrokeWidth(2).WithCornerRadius(16))
    .ToByteArrayAsync(ImageFormat.Png);
```

`OverlayAsync` works on PNG (raster) and SVG (embeds a base64 PNG `<image>` before `</svg>`); the other primitives require raster input.

## Error correction

**`QRCodeErrorCorrectionLevel`**: Low (~7%), Medium (~15%), Quartile (~25%), High (~30%) recovery. Higher levels tolerate damage and logos better but increase symbol version for
the same payload.

## Dependencies

*(Synchronized from `Lyo.QRCode.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                                 | Version   |
|---------------------------------------------------------|-----------|
| `Microsoft.Extensions.Configuration.Binder`             | `[10,)`   |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)`   |
| `Microsoft.Extensions.Logging.Abstractions`             | `[10,)`   |
| `SixLabors.ImageSharp`                                  | `3.*`     |
| `ZXing.Net`                                             | `0.16.11` |
| `ZXing.Net.Bindings.ImageSharp.V3`                      | `0.16.15` |

### Project references

- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Metrics`](../../../Core/Metrics/Lyo.Metrics/README.md)
- [`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md)

## Blazor UI (optional)

- [`Lyo.QRCode.Web.Components`](../Lyo.QRCode.Web.Components/README.md) — **`QrCodeWorkbench`** and related MudBlazor components.
