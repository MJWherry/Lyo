# Lyo.Images.Web.Components

Reusable **Blazor / MudBlazor** components for exercising [`Lyo.Images`](../Lyo.Images/README.md): an `IImageService` workbench and a spritesheet animator/extractor built on `ISpriteSheetExportService`.

## Examples

### DI / required services

```csharp
using Lyo.Images;

services.AddImageSharpImageService(); // IImageService used by ImageWorkbench
services.AddSpriteSheetExportService(); // ISpriteSheetExportService used by SpriteSheetWorkbench
```

## Components

| Component | Purpose |
| ------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`<ImageWorkbench />`** | Upload an image, inspect metadata + palette, and exercise the `IImageService` surface (resize/crop/rotate/watermark/convert/thumbnail/compress). |
| **`<SpriteSheetWorkbench />`** | Two-tab workbench — **Animate** a flat strip and **Extract** a grid spritesheet from an animated source (GIF/WebP/APNG) via `ISpriteSheetExportService`. |
| **`<SpriteSheetAnimateUploader />`** | Multi-sheet upload + label/select chrome consumed by `SpriteSheetWorkbench`. |
| **`<SpriteSheetImageInfo />`** | Compact metadata readout (dimensions, format, byte size) for a loaded raster. |
| **`<SpriteSheetPlayer />`** | Plays a sliced strip in-browser using a JS animator (canvas-based) for live preview. |
| **`<SpriteSheetProcessor />`** | Slicing/parameter panel for spritesheets (frame size, padding, sample budget, FPS, grid). |

Supporting types live in **`SpriteSheetModels.cs`** (`SpriteSheetEntry`, frame/grid state used to drive the workbench).

## DI / required services

Each consumer must register the underlying image services from `Lyo.Images`: Snackbars, dialogs, and JS interop are wired by **`Lyo.Web.Components`** (MudBlazor + the Lyo file upload + `IJsInterop`).

## Static assets

`wwwroot/` ships the JS used by `<SpriteSheetPlayer />`; serve static web assets from your host as usual for a Razor Class Library.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Images` — (direct, lyo)
- `Lyo.Web.Components` — (direct, lyo)
- `MudBlazor` `9.3` — (direct, third-party)
- `Lyo.Api.Client` — (transitive, lyo)
- `Lyo.Api.Models` — (transitive, lyo)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.DateAndTime` — (transitive, lyo)
- `Lyo.Diagnostic` — (transitive, lyo)
- `Lyo.Encryption` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.IO.Temp` — (transitive, lyo)
- `Lyo.Keystore` — (transitive, lyo)
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
- `System.Buffers` `4.6.0` — (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` — (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)