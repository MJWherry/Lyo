# Lyo.QRCode.Web.Components

Reusable **Blazor** components for QR code generation and preview workflows (**MudBlazor**). The main surface is **`QrCodeWorkbench`**: a three-column layout (output and styling ·
typed payload · result), wired to **`IQRCodeService`**, **`IImageDecorationService`** (overlay / frame / caption / padding primitives from **`Lyo.Images`**), and
**`Lyo.Web.Components.FileUpload.LyoFileUpload`** for logo files.

## `QrCodeWorkbench`

| Column                 | Contents                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
|------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Output and styling** | Format (optional **`AllowedFormats`** parameter restricts the dropdown, e.g. PNG+SVG only), error correction, output size preset (and custom pixels per module), quiet zone, dark/light module colors, logo upload (**`LyoFileUpload`** with **`ChipFileNameMaxLength`** / **`ChipMaxWidthCss`** for long names in narrow layouts), logo size slider, logo border, PNG **badge preset** and caption options (font, min header, auto-size header), frame colors. |
| **Payload**            | **`QrPayloadKind`** selector and fields for plain text, URL, Wi‑Fi, mailto, tel, SMS, geo, vCard/meCard, WhatsApp, Telegram, Signal; encoded preview and generate.                                                                                                                                                                                                                                                                                             |
| **Result**             | Format, file size, frame label; module scale and dimensions; raster/SVG preview (SVG uses an **`<img>`** with **`object-fit: contain`** so large matrix sizes stay in the box). **Click the preview** to open the full image in a new tab (raster: data URL; SVG: **blob URL** so browsers allow a new tab—`data:image/svg+xml` navigations are often blocked). **Download** streams bytes via JS.                                                             |

**Decoration pipeline:** After the QR is generated, the workbench builds an `IImageDecorationPipeline` and queues stages based on user input:

- A **logo upload** (PNG/SVG only) queues an **`Overlay`** stage. The pad behind the logo uses the QR **light** color; when the *Logo border* checkbox is on, the stroke uses the
  QR **dark** color so the border stays visible.
- A **badge preset** other than *None* queues additional stages:
    - **Border only** — `AddFrame` (slate stroke, padding around the QR); a caption (if provided) becomes a footer band via `AddCaption(FooterBelow)`.
    - **Rounded panel** — `AddOuterPadding` (rounded card) + `AddFrame` (slate stroke).
    - **Badge with header** — `AddCaption(HeaderAbove, notch)` + `AddOuterPadding` (rounded card + drop shadow) + `AddFrame` (stroke matching the header chrome).

Mud color pickers are deconstructed into opaque `#RRGGBB` strings before being passed into the option builders (length-9 forms aren't always `#RRGGBBAA`).

### Restrict output formats (host)

```razor
<QrCodeWorkbench AllowedFormats="@(new[] { QRCodeFormat.Png, QRCodeFormat.Svg })"/>
```

When **`AllowedFormats`** is null or an empty collection, all **`QRCodeFormat`** enum values appear. When the host changes **`AllowedFormats`** and the current selection is no
longer allowed, the workbench resets to the first entry in the list.

## Related projects

- [`Lyo.Images`](../../Images/Lyo.Images/README.md) — **`IImageDecorationService`** primitives + **`IImageDecorationPipeline`** for chaining them.
- [`Lyo.QRCode`](../Lyo.QRCode/README.md) — **`IQRCodeService`**, **`QRCodeOptions`**, **`Payloads`**.
- [`Lyo.Web.Components`](../../../Integration/Web/Lyo.Web.Components/README.md) — **`LyoFileUpload`**.
