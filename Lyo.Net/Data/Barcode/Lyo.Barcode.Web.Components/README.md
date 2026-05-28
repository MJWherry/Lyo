# Lyo.Barcode.Web.Components

Reusable **Blazor / MudBlazor** components for exercising the **`IBarcodeService`** surface from [`Lyo.Barcode`](../Lyo.Barcode/README.md).

## Components

| Component                  | Purpose                                                                                                                                                                                                                                                                  |
|----------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`<BarcodeWorkbench />`** | Generate **Code 128** barcodes in BMP or SVG, with controls for module width, bar height, quiet zone, bar/background/border colors, optional human-readable caption (font, gap, padding, color override), and live preview/download via `IBarcodeService.GenerateAsync`. |

## DI / required services

The workbench resolves **`IBarcodeService`** plus `IJsInterop` and `ISnackbar` from the host. Register a barcode implementation (e.g. `Lyo.Barcode.Native`) and the standard
`Lyo.Web.Components` services:

```csharp
using Lyo.Barcode.Native;

services.AddNativeBarcodeService(o => o.DefaultFormat = BarcodeFormat.Svg);
// + the Lyo.Web.Components / MudBlazor wiring used elsewhere in the host
```

## Dependencies

*(Synchronized from `Lyo.Barcode.Web.Components.csproj`.)*

**Target framework:** `net10.0`

**Framework references:** `Microsoft.AspNetCore.App`

### NuGet packages

| Package     | Version  |
|-------------|----------|
| `MudBlazor` | `[9.3,)` |

### Project references

- [`Lyo.Barcode`](../Lyo.Barcode/README.md)
- [`Lyo.Web.Components`](../../../Integration/Web/Lyo.Web.Components/README.md)

## Related projects

- [`Lyo.Barcode.Native`](../Lyo.Barcode.Native/README.md) — generator/reader implementation typically registered behind the workbench.
- [`Lyo.Barcode.TestWorkbench.Web.Components`](../Lyo.Barcode.TestWorkbench.Web.Components/README.md) — focused decode/test surface for sample images.
