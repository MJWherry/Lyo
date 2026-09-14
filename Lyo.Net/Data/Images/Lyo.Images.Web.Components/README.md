# Lyo.Images.Web.Components

Blazor / MudBlazor UI for [`Lyo.Images`](../Lyo.Images/README.md): an `IImageService` workbench and a spritesheet animator/extractor on `ISpriteSheetExportService`.

## Examples

### Services the host must register

```csharp
using Lyo.Images;

services.AddImageSharpImageService(); // IImageService used by ImageWorkbench
services.AddSpriteSheetExportService(); // ISpriteSheetExportService used by SpriteSheetWorkbench
```

## Components

| Component | Purpose |
| -------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| `<ImageWorkbench />` | Upload an image, inspect metadata and palette, and call `IImageService` (resize, crop, rotate, watermark, convert, thumbnail, compress). |
| `<SpriteSheetWorkbench />` | Two tabs. Animate a flat strip, or pull a grid spritesheet from an animated source (GIF/WebP/APNG) through `ISpriteSheetExportService`. |
| `<SpriteSheetAnimateUploader />` | Multi-sheet upload plus label/select chrome that `SpriteSheetWorkbench` uses. |
| `<SpriteSheetImageInfo />` | Short metadata readout (dimensions, format, byte size) for a loaded raster. |
| `<SpriteSheetPlayer />` | Plays a sliced strip in the browser with a canvas JS animator for live preview. |
| `<SpriteSheetProcessor />` | Slicing and parameter panel for spritesheets (frame size, padding, sample budget, FPS, grid). |

Supporting types sit in `SpriteSheetModels.cs` (`SpriteSheetEntry`, frame/grid state that drives the workbench).

## Services the host must register

Every consumer registers the image services from `Lyo.Images`. Snackbars, dialogs, and JS interop come from `Lyo.Web.Components` (MudBlazor, the Lyo file upload, and `IJsInterop`).

## Browser scripts

`wwwroot/` holds the JS `<SpriteSheetPlayer />` uses. Serve static web assets from the host the usual Razor Class Library way.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Images` (direct, lyo)
- `Lyo.Web.Components` (direct, lyo)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Api.Client` (transitive, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Http.Client` (transitive, lyo)
- `Lyo.IO.FileSystem` (transitive, lyo)
- `Lyo.IO.Temp` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `Lyo.Web.Primitives` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Blazored.LocalStorage` `4.5.0` (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `SixLabors.Fonts` `2.1.3` (transitive, third-party)
- `SixLabors.ImageSharp` `3.1.12` (transitive, third-party)
- `SixLabors.ImageSharp.Drawing` `2.1.7` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)