# Lyo.QRCode.Web.Components

Reusable **Blazor** components for QR code generation and preview workflows (**MudBlazor**). The main surface is **`QrCodeWorkbench`**: a three-column layout (output and styling · typed payload · result), wired to **`IQRCodeService`**, **`IImageDecorationService`** (overlay / frame / caption / padding primitives from **`Lyo.Images`**), and **`Lyo.Web.Components.FileUpload.LyoFileUpload`** for logo files.

## `QrCodeWorkbench`

| Column | Contents |
| ---------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Output and styling** | Format (optional **`AllowedFormats`** parameter restricts the dropdown, e.g. PNG+SVG only), error correction, output size preset (and custom pixels per module), quiet zone, dark/light module colors, logo upload (**`LyoFileUpload`** with **`ChipFileNameMaxLength`** / **`ChipMaxWidthCss`** for long names in narrow layouts), logo size slider, logo border, PNG **badge preset** and caption options (font, min header, auto-size header), frame colors. |
| **Payload** | **`QrPayloadKind`** selector and fields for plain text, URL, Wi‑Fi, mailto, tel, SMS, geo, vCard/meCard, WhatsApp, Telegram, Signal; encoded preview and generate. |
| **Result** | Format, file size, frame label; module scale and dimensions; raster/SVG preview (SVG uses an **`<img>`** with **`object-fit: contain`** so large matrix sizes stay in the box). **Click the preview** to open the full image in a new tab (raster: data URL; SVG: **blob URL** so browsers allow a new tab—`data:image/svg+xml` navigations are often blocked). **Download** streams bytes via JS. |

**Decoration pipeline:** After the QR is generated, the workbench builds an `IImageDecorationPipeline` and queues stages based on user input:

- A **logo upload** (PNG/SVG only) queues an **`Overlay`** stage. The pad behind the logo uses the QR **light** color; when the *Logo border* checkbox is on, the stroke uses the
  QR **dark** color so the border stays visible.
- A **badge preset** other than *None* queues additional stages:
    - **Border only** — `AddFrame` (slate stroke, padding around the QR); a caption (if provided) becomes a footer band via `AddCaption(FooterBelow)`.
    - **Rounded panel** — `AddOuterPadding` (rounded card) + `AddFrame` (slate stroke).
    - **Badge with header** — `AddCaption(HeaderAbove, notch)` + `AddOuterPadding` (rounded card + drop shadow) + `AddFrame` (stroke matching the header chrome).

Mud color pickers are deconstructed into opaque `#RRGGBB` strings before being passed into the option builders (length-9 forms aren't always `#RRGGBBAA`).

## `QrCodeWorkbench` — Restrict output formats (host)

```razor
<QrCodeWorkbench AllowedFormats="@(new[] { QRCodeFormat.Png, QRCodeFormat.Svg })"/>
```

When **`AllowedFormats`** is null or an empty collection, all **`QRCodeFormat`** enum values appear. When the host changes **`AllowedFormats`** and the current selection is no
longer allowed, the workbench resets to the first entry in the list.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Images` — (direct, lyo)
- `Lyo.QRCode` — (direct, lyo)
- `Lyo.Web.Components` — (direct, lyo)
- `MudBlazor` `9.3` — (direct, third-party)
- `Lyo.Api.Client` — (transitive, lyo)
- `Lyo.Api.Models` — (transitive, lyo)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.DataTable.Models` — (transitive, lyo)
- `Lyo.DateAndTime` — (transitive, lyo)
- `Lyo.Diagnostic` — (transitive, lyo)
- `Lyo.Encryption` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.IO.Temp` — (transitive, lyo)
- `Lyo.KeyStore` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Lyo.PackageMetadata` — (transitive, lyo)
- `Lyo.Query.Models` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Lyo.Streams` — (transitive, lyo)
- `Lyo.Validation` — (transitive, lyo)
- `Blazored.LocalStorage` `4.5.0` — (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` — (transitive, third-party, netstandard2.0)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `SixLabors.Fonts` `2.1.3` — (transitive, third-party)
- `SixLabors.ImageSharp` `3.1.12` — (transitive, third-party)
- `SixLabors.ImageSharp.Drawing` `2.1.7` — (transitive, third-party)
- `System.Buffers` `4.6.1` — (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` — (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `ZXing.Net` `0.16.11` — (transitive, third-party)
- `ZXing.Net.Bindings.ImageSharp.V3` `0.16.18` — (transitive, third-party)