# Lyo.QRCode

Generate and read QR codes: `IQRCodeService`, `QRCodeBuilder`, in-box ISO Model 2 encoding (`BuiltInQRCodeService`), optional QRCoder adapter package `Lyo.QRCode.QRCoder`, and typed payload helpers (`Lyo.QRCode.Payloads`) for Wi-Fi, URLs, vCard, `mailto:`, etc.

## Examples

### Generate with the built-in encoder

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

### Wi-Fi payload example

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

## How the pieces fit

| Piece | Role |
| ---------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `IQRCodeService` | Generate to memory, stream, or file; batch; `ReadFromImageAsync` (ZXing). |
| `BuiltInQRCodeService` | In-library PNG/SVG rasterization. Encode does not use the QRCoder NuGet. JPEG/BMP are not supported here (platform / format limits). |
| `Lyo.QRCode.QRCoder` | Optional `QRCoderQRCodeService` + `AddQRCoderQrCodeService` when you want JPEG/Bitmap on Windows or a QRCoder render path. |
| `QRCodeBuilder` | `QRCodeOptions` plus `WithData` / `WithPayload(IQrPayload)`. |
| `Payloads` | `IQrPayload`, `QrPayloadKind`, `WifiQrPayload`, `HttpUrlPayload`, contacts, URI schemes, messaging URLs. Each serializes to the string `GenerateAsync` receives. |

## Optional QRCoder package

Reference `Lyo.QRCode.QRCoder` and call `AddQRCoderQrCodeService` (or `AddQRCoderQrCodeServiceFromConfiguration`) instead of or alongside `AddQRCodeService`, depending on how you register `IQRCodeService`.

## Payload helpers (`Lyo.QRCode.Payloads`)

- **Wi-Fi (`WifiQrPayload`).** Drops `H:` when the SSID is not hidden (better phone compatibility than `H:false`). Open networks omit `P` (not `P:;`).
- **SMS (`SmsPayload`).** Defaults to the `sms:` URI scheme. `smsto:` is opt-in (some Android SMS apps crash on long `smsto:` bodies). Very long URIs throw (`MaxSmsQrUriLength`) to avoid app crashes.

## Key options

- `QRCodeOptions.Size`. Pixels per module (each black/white square), not the image width. Total size ≈ module count per side × `Size`. Later decoration may grow that further.
- `QRCodeOptions.Icon`. Encoder hint only. The only field used is `IconSizePercent`, which raises the effective ECC level so a planned center logo does not break scanning. `IconBytes`, `IconFilePath`, and `DrawIconBorder` are metadata for the consumer's overlay call. The QR encoder never composites the icon. Apply the overlay (and any frame/caption/padding) after generation through `Lyo.Images.IImageDecorationService` (see the migration note below).

## Migration: decoration left the encoder

`QRCodeOptions.Frame`, `QRCodeBuilder.WithFrame(...)`, and the optional `IImageService` / `IQrFrameLayoutService` constructor parameters on `BuiltInQRCodeService` no longer exist.
Compose icons and chrome on the returned bytes with `Lyo.Images`:

```csharp
using Lyo.Images;
using Lyo.Images.Builders;
using Lyo.Common.Core.Enums;

var qr = await qrService.GenerateAsync(data, options);
var qrBytes = ((QRCodeResult)qr).ImageBytes!;

var decorated = await decoration.Pipeline(qrBytes)
    .Overlay(logoBytes, b => b.WithOverlaySizePercent(18).WithPadColor("#FFFFFF").WithBorder("#000000"))
    .AddCaption(b => b.WithText("Scan Me").WithNotch())
    .AddOuterPadding(b => b.WithPanelColor("#FFFFFF").WithCornerRadius(16))
    .AddFrame(b => b.WithStrokeColor("#1e293b").WithStrokeWidth(2).WithCornerRadius(16))
    .ToByteArrayAsync(ImageFormat.Png);
```

`OverlayAsync` accepts PNG (raster) and SVG (embeds a base64 PNG `<image>` before `</svg>`); the other primitives require raster input.

## Error correction

`QRCodeErrorCorrectionLevel`: Low (~7%), Medium (~15%), Quartile (~25%), High (~30%) recovery. Higher levels survive damage and logos better, but they raise the symbol version for the same payload.

## Optional Blazor UI

- [`Lyo.QRCode.Web.Components`](../Lyo.QRCode.Web.Components/README.md). `QrCodeWorkbench` plus related MudBlazor components.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `SixLabors.ImageSharp` `3.1.12` (direct, third-party)
- `ZXing.Net` `0.16.11` (direct, third-party)
- `ZXing.Net.Bindings.ImageSharp.V3` `0.16.18` (direct, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)