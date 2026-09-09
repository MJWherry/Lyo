# Lyo.SystemInformation

`Lyo.SystemInformation` takes a point-in-time picture of the machine a process is running on. `SystemInfoCollector.Collect()` returns a `SystemInfo` record covering hardware, software, network, and environment, stamped with `CollectedAtUtc`; the `ILogger` extensions emit it as structured log entries at a level you choose.

It was carved out of `Lyo.Common` because it is the one part of that package that legitimately needs `Microsoft.Extensions.Logging.Abstractions`. Keeping it separate is what let the rest of the Common family shed that dependency. Its only production consumer is `Lyo.Job.Worker`, which logs the host inventory when a worker starts.

Everything lives in the `Lyo.SystemInformation` namespace, matching the package.

## Features

- **`SystemInfoCollector`.** Take the full snapshot with `Collect()`, or pay only for one facet via `GetHardwareInfo()` / `GetSoftwareInfo()` / `GetNetworkInfo()` / `GetEnvironmentInfo()`.
- **Hardware inventory.** `HardwareInfo` covers CPU, memory, `DriveSpaceInfo` per volume, and `MonitorInfo` for attached displays.
- **EDID parsing** (`EdidParser`). Pulls manufacturer, model, and physical dimensions from the display's Extended Display Identification Data — details OS APIs often collapse to a device string.
- **Network inventory.** Interfaces, addresses, and link state on `NetworkInfo` and `NetworkInterfaceInfo`.
- **Software and environment.** `EnvironmentInfo` (machine and user context, working set) and `SoftwareInfo` (OS, runtime, architecture).
- **Structured logging** (`SystemInfoLoggerExtensions`). `LogSystemInfo(info, level)` plus per-facet `LogHardwareInfo` / `LogSoftwareInfo` / `LogNetworkInfo` / `LogEnvironmentInfo`, so a startup banner is one call and the fields stay queryable in the log sink.

## Examples

### Log the host inventory at startup

```csharp
using Lyo.SystemInformation;

var info = SystemInfoCollector.Collect();
logger.LogSystemInfo(info, LogLevel.Information);
```

### A single facet

```csharp
using Lyo.SystemInformation;

// Drive space for a pre-flight check, without collecting software or environment facets.
foreach (var drive in SystemInfoCollector.GetHardwareInfo().Drives)
    logger.LogInformation("{Name}: {Free} free of {Total}", drive.Name, drive.AvailableFreeSpaceBytes, drive.TotalSizeBytes);
```

## What platforms cover

Collection is best-effort and degrades instead of throwing: facets the current OS does not expose come back empty or with defaults. Monitor enumeration and EDID reads depend most on the platform, because they go through native display APIs on Windows and sysfs on Linux.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)