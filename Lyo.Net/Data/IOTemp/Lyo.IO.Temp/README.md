# Lyo.IO.Temp

Service for creating and managing temporary files and directories with session support, configurable naming, and overflow handling. Ideal for upload processing, report generation,
or any workflow needing short-lived temp storage.

## Features

- **Session-based** – `IIOTempSession` groups temp files/dirs; cleanup on session dispose
- **Standalone files/dirs** – One-off `CreateFile` / `CreateDirectory` without a session
- **Naming strategies** – `Guid`, `Sequential`, `Timestamp`, `RandomChars`
- **Overflow handling** – `ThrowException`, `DeleteOldest`, or `DeleteLargest` when per-file or total-size limits are exceeded
- **Metrics** – Session created, files created, cleanup counts (when `IMetrics` registered)

## Usage

```csharp
// Add to DI
services.AddIOTempService();  // uses default options (TempRoot = OS temp, DirectoryName = "lyo-io-temp")

// Or with configuration
services.AddIOTempServiceFromConfiguration(configuration, "IOTempService");

// Or configure options
services.AddIOTempService(options =>
{
    options.DirectoryName = "my-app-temp";          // keep TempRoot as OS default
    options.MaxTotalSizeBytes = 1024 * 1024 * 500;  // 500 MB per session
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

| Option             | Default                    | Description                                                                    |
|--------------------|----------------------------|--------------------------------------------------------------------------------|
| `TempRoot`         | `Path.GetTempPath()`       | OS temp root. Override to change the parent folder.                            |
| `DirectoryName`    | `"lyo-io-temp"`            | Subdirectory under `TempRoot`. Set per-instance in tests to avoid collisions.  |
| `FileNamingStrategy` | `Guid`                   | `Guid`, `Sequential`, `Timestamp`, or `RandomChars`                            |
| `MaxFileSizeBytes` | 1 GB                       | Per-file hard limit (throws regardless of `OverflowStrategy`)                  |
| `MaxTotalSizeBytes`| 10 GB                      | Per-session total limit; enforced via `OverflowStrategy`                       |
| `FileLifetime`     | `null` (no expiry)         | Default age threshold used by `Cleanup()` / `CleanupAsync()` with no argument  |
| `OverflowStrategy` | `ThrowException`           | `ThrowException`, `DeleteOldest`, or `DeleteLargest`                           |
| `EnableMetrics`    | `true`                     | Record metrics via `IMetrics`                                                  |

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.IO.Temp.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package                                                 | Version |
|---------------------------------------------------------|---------|
| `Microsoft.Extensions.Configuration.Abstractions`       | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder`             | `[10,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions`             | `[10,)` |

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
- `TempNamingStrategy`
- `TempOverflowStrategy`

<!-- LYO_README_SYNC:END -->

