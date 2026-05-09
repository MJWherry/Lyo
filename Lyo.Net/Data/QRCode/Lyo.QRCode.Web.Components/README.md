# Lyo.QRCode.Web.Components

Reusable **Blazor** components for QR code generation and preview workflows (**MudBlazor**). The main surface is **`QrCodeWorkbench`**: a three-column layout (output and styling · typed payload · result), wired to **`IQRCodeService`**, optional **`IImageService`** / **`IQrFrameLayoutService`**, and **`Lyo.Web.Components.FileUpload.LyoFileUpload`** for logo files.

## `QrCodeWorkbench`

| Column | Contents |
|--------|----------|
| **Output and styling** | Format (optional **`AllowedFormats`** parameter restricts the dropdown, e.g. PNG+SVG only), error correction, output size preset (and custom pixels per module), quiet zone, dark/light module colors, logo upload (**`LyoFileUpload`** with **`ChipFileNameMaxLength`** / **`ChipMaxWidthCss`** for long names in narrow layouts), logo size slider, logo border, PNG **frame style** and caption options (font, min header, auto-size header), frame colors. |
| **Payload** | **`QrPayloadKind`** selector and fields for plain text, URL, Wi‑Fi, mailto, tel, SMS, geo, vCard/meCard, WhatsApp, Telegram, Signal; encoded preview and generate. |
| **Result** | Format, file size, frame label; module scale and dimensions; raster/SVG preview (SVG uses an **`<img>`** with **`object-fit: contain`** so large matrix sizes stay in the box). **Click the preview** to open the full image in a new tab (raster: data URL; SVG: **blob URL** so browsers allow a new tab—`data:image/svg+xml` navigations are often blocked). **Download** streams bytes via JS. |

**Frames (PNG):** After the QR PNG is generated, the workbench calls **`IQrFrameLayoutService.CompositeQrFramePngAsync`** (or **`IImageService.CompositeQrFramePngAsync`**) with a built **`QrFrameLayoutOptions`**. Mud color pickers are converted to opaque **`#RRGGBB`** using **`MudColor.Deconstruct`** so ImageSharp always receives parseable hex (avoiding fallback header/outline colors). For **`QrFrameStyle.BadgeWithHeader`**, **`CardOutlineHex`** is set to the same value as **`HeaderBackgroundHex`** so the outer card stroke matches the header band.

**Logo border:** Center logo compositing uses **`ImageCenterOverlayOptions`**: the pad behind the logo uses the QR **light** color; the optional stroke uses the QR **dark** color so the border stays visible on the pad.

### Restrict output formats (host)

```razor
<QrCodeWorkbench AllowedFormats="@(new[] { QRCodeFormat.Png, QRCodeFormat.Svg })"/>
```

When **`AllowedFormats`** is null or an empty collection, all **`QRCodeFormat`** enum values appear. When the host changes **`AllowedFormats`** and the current selection is no longer allowed, the workbench resets to the first entry in the list.

## Related projects

- [`Lyo.Images`](../../Images/Lyo.Images/README.md) — **`CompositeQrFramePngAsync`**, **`CompositeCenterOverlayPngAsync`**, **`QrFrameLayoutOptions`**, **`ImageCenterOverlayOptions`**
- [`Lyo.QRCode`](../Lyo.QRCode/README.md) — **`IQRCodeService`**, **`QRCodeOptions`**, **`Payloads`**
- [`Lyo.Web.Components`](../../../Integration/Web/Lyo.Web.Components/README.md) — **`LyoFileUpload`**
