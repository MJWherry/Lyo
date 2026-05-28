# Lyo.Images.Web.Components

Reusable **Blazor / MudBlazor** components for exercising [`Lyo.Images`](../Lyo.Images/README.md): an `IImageService` workbench and a spritesheet
animator/extractor built on `ISpriteSheetExportService`.

## Components

| Component                            | Purpose                                                                                                                                                  |
|--------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`<ImageWorkbench />`**             | Upload an image, inspect metadata + palette, and exercise the `IImageService` surface (resize/crop/rotate/watermark/convert/thumbnail/compress).         |
| **`<SpriteSheetWorkbench />`**       | Two-tab workbench — **Animate** a flat strip and **Extract** a grid spritesheet from an animated source (GIF/WebP/APNG) via `ISpriteSheetExportService`. |
| **`<SpriteSheetAnimateUploader />`** | Multi-sheet upload + label/select chrome consumed by `SpriteSheetWorkbench`.                                                                             |
| **`<SpriteSheetImageInfo />`**       | Compact metadata readout (dimensions, format, byte size) for a loaded raster.                                                                            |
| **`<SpriteSheetPlayer />`**          | Plays a sliced strip in-browser using a JS animator (canvas-based) for live preview.                                                                     |
| **`<SpriteSheetProcessor />`**       | Slicing/parameter panel for spritesheets (frame size, padding, sample budget, FPS, grid).                                                                |

Supporting types live in **`SpriteSheetModels.cs`** (`SpriteSheetEntry`, frame/grid state used to drive the workbench).

## DI / required services

Each consumer must register the underlying image services from `Lyo.Images`:

```csharp
using Lyo.Images;

services.AddImageSharpImageService();   // IImageService used by ImageWorkbench
services.AddSpriteSheetExportService(); // ISpriteSheetExportService used by SpriteSheetWorkbench
```

Snackbars, dialogs, and JS interop are wired by **`Lyo.Web.Components`** (MudBlazor + the Lyo file upload + `IJsInterop`).

## Static assets

`wwwroot/` ships the JS used by `<SpriteSheetPlayer />`; serve static web assets from your host as usual for a Razor Class Library.

## Dependencies

*(Synchronized from `Lyo.Images.Web.Components.csproj`.)*

**Target framework:** `net10.0`

**Framework references:** `Microsoft.AspNetCore.App`

### NuGet packages

| Package     | Version  |
|-------------|----------|
| `MudBlazor` | `[9.3,)` |

### Project references

- [`Lyo.Images`](../Lyo.Images/README.md)
- [`Lyo.Web.Components`](../../../Integration/Web/Lyo.Web.Components/README.md)
