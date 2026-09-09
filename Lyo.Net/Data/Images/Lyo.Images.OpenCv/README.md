# Lyo.Images.OpenCv

OpenCvSharp4 helpers for .NET. Split from higher-level pipelines (e.g. comic overlay) so hosts take native OpenCV only where they need it.

## Inpaint a color ROI

**Contract.** [`IOpenCvRoiInpaint`](IOpenCvRoiInpaint.cs) [`InpaintColorRoiPng`](IOpenCvRoiInpaint.cs) decodes a color PNG ROI, builds a binary mask (255 inside the
rectangle, 0 elsewhere), runs OpenCV inpaint with optional Telea or Navier-Stokes ([`OpenCvInpaintAlgorithm`](OpenCvInpaintAlgorithm.cs)), and returns PNG-encoded BGR
at the decoded source width and height.

- **Without DI.** [`OpenCvRoiInpaint`](OpenCvRoiInpaint.cs) (`InpaintColorRoiPng`, `InpaintTelea`) forwards to a shared [`OpenCvRoiInpaintService`](OpenCvRoiInpaintService.cs).
- **With DI.** [`AddOpenCvRoiInpaint()`](OpenCvImageServiceCollectionExtensions.cs) maps `IOpenCvRoiInpaint` → `OpenCvRoiInpaintService` when that mapping is not already registered.

Failure codes: `OpenCvInpaint.DecodeFailed`, `OpenCvInpaint.InpaintError`.

## Native runtimes on NuGet

`OpenCvSharp4.official.runtime.linux-x64` is referenced here for Linux CI and local Linux work. On Windows or other RIDs, add the matching official runtime package to the executable (e.g. Gateway) or to this library's `.csproj`. Search NuGet for `OpenCvSharp4.official.runtime` for your OS. If native libraries fail to load, confirm the right runtime package was restored and copied next to the app output.

## How to run tests

See [`../Lyo.Images.OpenCv.Tests/`](../Lyo.Images.OpenCv.Tests/). The repo uses xUnit v3 with `OutputType` Exe. Run:

```bash
dotnet run --project Lyo.Net/Data/Images/Lyo.Images.OpenCv.Tests/Lyo.Images.OpenCv.Tests.csproj
```

`dotnet test` may restore without always running the v3 in-process runner the same way. Prefer `dotnet run` for this project.

Tests need a working OpenCV native load, the same as a Linux host at runtime.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (direct, microsoft)
- `OpenCvSharp4` `4.13.0.20260627` (direct, third-party)
- `OpenCvSharp4.official.runtime.linux-x64` `4.13.0.20260627` (direct, third-party)