# Lyo.IO.Temp

Service for creating and managing temporary files and directories with session support, configurable naming, and overflow handling. Ideal for upload processing, report generation,
or any workflow needing short-lived temp storage.

## Features

- **Session-based** – `IIOTempSession` groups temp files/dirs; cleanup on session dispose
- **Standalone files/dirs** – One-off `CreateFile` / `CreateDirectory` without a session
- **Naming strategies** – Guid or Sequential
- **Overflow handling** – ThrowException, CleanOldest, or custom when limits exceeded
- **Metrics** – Session created, files created, cleanup counts (when `IMetrics` registered)

## Usage

```csharp
// Add to DI
services.AddIOTempService();  // uses default options

// Or with configuration
services.AddIOTempService(configuration, "IOTempService");

// Or configure options
services.AddIOTempService(options =>
{
    options.RootDirectory = Path.Combine(Path.GetTempPath(), "my-app-temp");
    options.MaxTotalSizeBytes = 1024 * 1024 * 500;  // 500 MB
});
```

### Session-based (recommended)

```csharp
using var session = _ioTempService.CreateSession();
var filePath = session.GetFilePath("report.pdf");
await File.WriteAllBytesAsync(filePath, reportBytes);

// Or create file with data
var path = session.GetFilePath(null);  // auto-named
using (var fs = File.Create(path))
    await dataStream.CopyToAsync(fs);

// Session disposes → all session files/dirs cleaned up
```

### Standalone (one-offs)

```csharp
var path = _ioTempService.CreateFile();
var pathWithData = _ioTempService.CreateFile(byteData, "myfile.bin");
var dir = _ioTempService.CreateDirectory();
```

### Cleanup

```csharp
_ioTempService.Cleanup();                    // remove all
await _ioTempService.CleanupAsync(ct);        // remove all
await _ioTempService.CleanupAsync(TimeSpan.FromHours(1), ct);  // older than 1 hour
```

## Configuration

| Option             | Default                          | Description                   |
|--------------------|----------------------------------|-------------------------------|
| RootDirectory      | `Path.GetTempPath()/lyo-io-temp` | Base temp directory           |
| FileNamingStrategy | Guid                             | Guid or Sequential            |
| MaxFileSizeBytes   | 1 GB                             | Per-file limit                |
| MaxTotalSizeBytes  | 10 GB                            | Total size limit              |
| OverflowStrategy   | ThrowException                   | ThrowException or CleanOldest |
| EnableMetrics      | true                             | Record metrics                |

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.IO.Temp.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions` | `[10,)` |

### Project references

- `Lyo.Common`
- `Lyo.Exceptions`
- `Lyo.Metrics`

## Public API (generated)

Top-level `public` types in `*.cs` (*11*). Nested types and file-scoped namespaces may omit some entries.

- `Constants`
- `Extensions`
- `IIOTempService`
- `IIOTempSession`
- `IOTempService`
- `IOTempServiceOptions`
- `IOTempSession`
- `IOTempSessionOptions`
- `Metrics`
- `TempNamingStrategy`
- `TempOverflowStrategy`

<!-- LYO_README_SYNC:END -->

