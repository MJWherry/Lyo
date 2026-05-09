# Lyo.QRCode

**QR code generation and reading** for Lyo: **`IQRCodeService`**, **`QRCodeBuilder`**, ISO **Model 2** encoding in-box (**`BuiltInQRCodeService`**), optional **QRCoder** adapter package **`Lyo.QRCode.QRCoder`**, and typed **payload helpers** (`Lyo.QRCode.Payloads`) for Wi‑Fi, URLs, vCard, `mailto:`, etc.

## Architecture

| Piece | Role |
|-------|------|
| **`IQRCodeService`** | Generate to memory, stream, file; batch; **`ReadFromImageAsync`** (ZXing). |
| **`BuiltInQRCodeService`** | In-library **PNG/SVG** rasterization; **no QRCoder NuGet** for encode. JPEG/BMP not supported here (platform / format limits). |
| **`Lyo.QRCode.QRCoder`** | Optional **`QRCoderQRCodeService`** + **`AddQRCoderQrCodeService`** for JPEG/Bitmap on Windows and QRCoder-based render path. |
| **`QRCodeBuilder`** | Fluent **`QRCodeOptions`** + **`WithData`** / **`WithPayload(IQrPayload)`**. |
| **`Payloads`** | **`IQrPayload`**, **`QrPayloadKind`**, **`WifiQrPayload`**, **`HttpUrlPayload`**, contacts, URI schemes, messaging URLs — all serialize to the string passed to **`GenerateAsync`**. |

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

Add project/package reference to **`Lyo.QRCode.QRCoder`** and call **`AddQRCoderQrCodeService`** (or **`AddQRCoderQrCodeServiceFromConfiguration`**) instead of or in addition to **`AddQRCodeService`**, depending on how you register **`IQRCodeService`**.

## Payload helpers (`Lyo.QRCode.Payloads`)

- **Wi‑Fi (`WifiQrPayload`)** — Omits **`H:`** when the SSID is not hidden (better phone compatibility than **`H:false`**). Open networks omit **`P`** (not **`P:;`**).
- **SMS (`SmsPayload`)** — Defaults to **`sms:`** URI scheme; **`smsto:`** is opt-in (some Android SMS apps crash on long **`smsto:`** bodies). Very long URIs throw (**`MaxSmsQrUriLength`**) to avoid app crashes.

## Key options

- **`QRCodeOptions.Size`** — **Pixels per module** (each black/white square), not the full image width. Total size ≈ module count per side × **`Size`** (and more if a PNG **frame** is composited separately).
- **`QRCodeOptions.Icon`** — Center logo; built-in path needs **`IImageService`** registered when an icon is set. **`DrawIconBorder`**: the compositor clears a light pad (**`LightColor`**) behind the logo and draws the stroke in **`DarkColor`** so the border remains visible (a light-on-light stroke would disappear).
- **`QRCodeOptions.Frame`** — Decorative frame; **`BuiltInQRCodeService`** can apply **`QrFrameLayoutOptions`** when registered with frame support; Blazor workbenches often composite frames in a second step via **`IQrFrameLayoutService`** / **`IImageService.CompositeQrFramePngAsync`** (see **`Lyo.Images`**).

## Error correction

**`QRCodeErrorCorrectionLevel`**: Low (~7%), Medium (~15%), Quartile (~25%), High (~30%) recovery. Higher levels tolerate damage and logos better but increase symbol version for the same payload.

## Dependencies

*(Synchronized from `Lyo.QRCode.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                                 | Version |
|---------------------------------------------------------|---------|
| `Microsoft.Extensions.Configuration.Abstractions`       | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder`             | `[10,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions`             | `[10,)` |

### Project references

- [`Lyo.Codes.ZXing`](../../Codes/Lyo.Codes.ZXing/README.md)
- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Images`](../../Images/Lyo.Images/README.md)
- [`Lyo.Metrics`](../../../Core/Metrics/Lyo.Metrics/README.md)

## Blazor UI (optional)

- [`Lyo.QRCode.Web.Components`](../Lyo.QRCode.Web.Components/README.md) — **`QrCodeWorkbench`** and related MudBlazor components.
