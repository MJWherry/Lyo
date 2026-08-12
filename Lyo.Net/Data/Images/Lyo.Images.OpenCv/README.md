# Lyo.Images.OpenCv

OpenCV helpers for .NET via **OpenCvSharp4**, kept separate from higher-level pipelines (e.g. comic overlay) so hosts can reference native OpenCV only where needed.

## ROI inpaint

**Contract:** [`IOpenCvRoiInpaint`](IOpenCvRoiInpaint.cs) — [`InpaintColorRoiPng`](IOpenCvRoiInpaint.cs) decodes a **color PNG ROI**, builds a binary mask (255 inside the
rectangle, 0 elsewhere), runs OpenCV inpaint with optional **Telea** or **Navier–Stokes** ([`OpenCvInpaintAlgorithm`](OpenCvInpaintAlgorithm.cs)), and returns PNG-encoded **BGR**
output with the **same width and height** as the decoded source.

- **Without DI:** [`OpenCvRoiInpaint`](OpenCvRoiInpaint.cs) (`InpaintColorRoiPng`, `InpaintTelea`) delegates to a shared [`OpenCvRoiInpaintService`](OpenCvRoiInpaintService.cs).
- **With DI:** [`AddOpenCvRoiInpaint()`](OpenCvImageServiceCollectionExtensions.cs) registers `IOpenCvRoiInpaint` → `OpenCvRoiInpaintService` if not already registered.

Failure codes: **`OpenCvInpaint.DecodeFailed`**, **`OpenCvInpaint.InpaintError`**.

## Native runtimes (NuGet)

This package references **`OpenCvSharp4.official.runtime.linux-x64`**, suitable for Linux CI and local Linux development. On **Windows** (or other RIDs), add a matching official
runtime package to the **executable** project (e.g. Gateway) or to this library’s `.csproj`, for example search NuGet for `OpenCvSharp4.official.runtime` for your OS. If native
libraries fail to load at runtime, verify the correct runtime package is restored and copied next to the app output.

## Tests

See [`../Lyo.Images.OpenCv.Tests/`](../Lyo.Images.OpenCv.Tests/). This repo uses **xUnit v3** with `OutputType` **Exe**; run:

```bash
dotnet run --project Lyo.Net/Data/Images/Lyo.Images.OpenCv.Tests/Lyo.Images.OpenCv.Tests.csproj
```

(`dotnet test` may restore but not always execute the v3 in-process runner the same way—prefer `dotnet run` for this project.)

Tests require a working OpenCV native load (same as production use on Linux).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Result` — (direct, lyo)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` — (direct, microsoft)
- `OpenCvSharp4` `4.13.0.20260627` — (direct, third-party)
- `OpenCvSharp4.official.runtime.linux-x64` `4.13.0.20260627` — (direct, third-party)
- `Lyo.Common` — (transitive, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)