# Lyo.Barcode.TestWorkbench.Web.Components

Thin **MudBlazor** wrapper that hosts **`<BarcodeWorkbench />`** from [`Lyo.Barcode.Web.Components`](../Lyo.Barcode.Web.Components/README.md) inside an `MudContainer` for the Lyo
gateway test harness.

## Components

| Component                      | Purpose                                                                                                                        |
|--------------------------------|--------------------------------------------------------------------------------------------------------------------------------|
| **`<BarcodeTestWorkbench />`** | Page-level wrapper that titles the surface ("Barcode test workbench") and renders `<BarcodeWorkbench />`. No extra parameters. |

## DI / required services

Same as `Lyo.Barcode.Web.Components`: register an `IBarcodeService` implementation (e.g. `AddNativeBarcodeService`) and the standard `Lyo.Web.Components` host wiring.

## Dependencies

*(Synchronized from `Lyo.Barcode.TestWorkbench.Web.Components.csproj`.)*

**Target framework:** `net10.0`

**Framework references:** `Microsoft.AspNetCore.App`

### NuGet packages

| Package     | Version  |
|-------------|----------|
| `MudBlazor` | `[9.3,)` |

### Project references

- [`Lyo.Barcode.Web.Components`](../Lyo.Barcode.Web.Components/README.md)
