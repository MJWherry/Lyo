# Lyo.IO.Temp

Service for creating and managing temporary files and directories with session support, configurable naming, and overflow handling. Ideal for upload processing, report generation,
or any workflow needing short-lived temp storage.

The public contract is **`IIOTempService`** and **`IIOTempSession`**; **`IOTempService`** / **`IOTempSession`** are the default implementations. With XML doc generation enabled in the
repo, IntelliSense surfaces the same summaries as this README. Implementation types use `<inheritdoc />` where they mirror the interfaces.

## Features

- **Session-based** – `IIOTempSession` groups temp files/dirs; cleanup on session dispose
- **Standalone files/dirs** – One-off `CreateFile` / `CreateDirectory` without a session
- **Pluggable storage** – `IIOTempStorageProvider` abstracts all I/O; ships with `FileSystemIOTempStorageProvider` (default) and `InMemoryIOTempStorageProvider` (WASM / tests)
- **Naming strategies** – `Guid`, `Sequential`, `Timestamp`, `RandomChars`
- **Overflow handling** – `ThrowException`, `DeleteOldest`, or `DeleteLargest` when per-file or total-size limits are exceeded
- **File generator** – `session.Generator` produces random-bytes files, structured text/CSV/JSON, zip archives, and simulated directory trees
- **Events** – `FileCreated` / `DirectoryCreated` callbacks on session for observability
- **Sub-sessions** – Nested sessions rooted inside a parent session
- **Session inspection** – Snapshots, byte totals, discovery enumerations
- **Keyed session pooling** – `GetOrCreateSession(key)` for per-request/per-pipeline pools
- **Fluent options** – `WithMaxFileSize` / `WithMaxTotalSize` extension methods on options objects
- **Assertion helpers** – `AssertFilesExist` / `AssertTotalSize` on `IIOTempSession` for test code
- **Auto-cleanup** – Background `IHostedService` that periodically calls `Cleanup()`
- **Metrics** – Session created, files created, cleanup counts (when `IMetrics` registered)

## Usage

```csharp
// Add to DI
services.AddIOTempService();  // uses default options

// Or configure options
services.AddIOTempService(options =>
{
    options.DirectoryName = "my-app-temp";
    options.MaxTotalSizeBytes = 500 * 1024 * 1024;  // 500 MB
});

// Add with automatic background cleanup
services.AddIOTempServiceWithAutoCleanup(
    cleanupInterval: TimeSpan.FromHours(1),
    initialDelay: TimeSpan.FromMinutes(5));
```

### Session-based (recommended)

```csharp
using var session = _ioTempService.CreateSession();

// Write your own data
var path = session.GetFilePath("report.pdf");
await File.WriteAllBytesAsync(path, reportBytes);

// Or create directly from data
var path2 = await session.CreateFileAsync(byteData);
var path3 = await session.CreateFileAsync(stream);

// Session dispose → all files/dirs cleaned up automatically
```

### File Generator

Access via `session.Generator`:

```csharp
// Random-bytes files
var file   = session.Generator.CreateRandomFile(FileSizeUnitInfo.Megabyte, 1);
var files  = session.Generator.CreateRandomFiles(5, FileSizeUnitInfo.Kilobyte, 64);

// Named random files (name selector per index)
var named = session.Generator.CreateRandomFiles(3, 1024, i => $"chunk_{i}.bin");

// Structured content
var txt  = session.Generator.CreateTextFile(lines: 100, charsPerLine: 80);
var csv  = session.Generator.CreateCsvFile(rows: 500, columns: 10);
var json = session.Generator.CreateJsonFile(depth: 3, keysPerObject: 5);

// Zip archive
var zip = session.Generator.CreateZipFile(TempDirectorySpec.Flat(10, 1024));

// Simulated directory tree
var dir = session.Generator.SimulateDirectory(TempDirectorySpec.Flat(20, 512));
```

### TempDirectorySpec

Describe a directory structure for simulation or zip creation:

```csharp
// Fluent builder
var spec = TempDirectorySpec.Builder()
    .WithFiles(5, FileSizeUnitInfo.Kilobyte, 4)
    .WithFileSizeSelector(i => (i + 1) * 512)      // per-file size varies
    .WithSubdirectory(sub => sub.WithFiles(3, 256))
    .WithSubdirectory(TempDirectorySpec.Flat(2, 128))
    .Build();

// Randomised spec
var randomSpec = TempDirectorySpec.Random(
    minFiles: 3, maxFiles: 10,
    minSize: 512, maxSize: 4096);
```

### Session mutation

```csharp
// Delete all tracked files/dirs, reset byte count
session.Clear();

// Copy an external file or directory into the session
var dest = session.CopyFrom("/path/to/external/file.csv");
var destDir = session.CopyFrom("/path/to/external/dir");

// Append data to an existing tracked file
session.AppendToFile(path, ReadOnlyMemory<byte>.Empty);
session.AppendToFile(path, "extra line\n");
```

### Events

```csharp
session.FileCreated      += path => Console.WriteLine($"Created: {path}");
session.DirectoryCreated += path => Console.WriteLine($"Dir created: {path}");
```

### Sub-sessions

```csharp
using var sub = session.CreateSubSession();
// sub is rooted inside session.SessionDirectory
// disposing session also removes all sub-session content
```

### Inspection

```csharp
long bytes   = session.GetTotalBytesUsed();
var snapshot = session.GetSnapshot();  // TempSessionSnapshot: frozen view

var files = session.EnumerateFiles("*.csv");    // all .csv on disk (including untracked)
var dirs  = session.EnumerateDirectories();
```

### Keyed session pooling

```csharp
// Get or create a named session (same instance returned for same key)
var session = service.GetOrCreateSession("pipeline-A");

// With custom options for initial creation
var session = service.GetOrCreateSession("pipeline-A", new IOTempSessionOptions { MaxTotalSizeBytes = 100_000 });

// Release when done
service.ReleaseSession("pipeline-A");

// Service-level stats
IOTempServiceStats stats = service.GetStats();
```

### Fluent options

```csharp
var sessionOptions = new IOTempSessionOptions()
    .WithMaxFileSize(FileSizeUnitInfo.Megabyte, 5)
    .WithMaxTotalSize(FileSizeUnitInfo.Gigabyte, 1);
```

### Assertion helpers (for tests)

```csharp
session.AssertFilesExist();                             // all tracked files exist on disk
session.AssertTotalSize(expectedBytes: 2048, toleranceBytes: 64);
```

### Standalone (one-offs without a session)

```csharp
var path = _ioTempService.CreateFile();
var pathWithData = _ioTempService.CreateFile(byteData, "myfile.bin");
var dir = _ioTempService.CreateDirectory();
```

### Cleanup

```csharp
_ioTempService.Cleanup();
await _ioTempService.CleanupAsync(ct);
await _ioTempService.CleanupAsync(TimeSpan.FromHours(1), ct);
```

## Storage Providers

All I/O is delegated through `IIOTempStorageProvider`, making the storage backend fully swappable.
Two implementations are included; register a custom one via DI to use any other backend.

### FileSystemIOTempStorageProvider (default)

Delegates to `System.IO`. Used automatically when no `IIOTempStorageProvider` is registered.

```csharp
// Implicit — no registration needed
services.AddIOTempService();
```

### InMemoryIOTempStorageProvider

Backed by a `ConcurrentDictionary`. No filesystem access; suitable for Blazor WASM and unit tests.
All data lives for the lifetime of the provider instance.

```csharp
// Blazor WASM (Program.cs)
builder.Services.AddSingleton<IIOTempStorageProvider>(new InMemoryIOTempStorageProvider());
builder.Services.AddIOTempService();

// xUnit / NUnit — direct construction
var storage = new InMemoryIOTempStorageProvider();
var options = new IOTempSessionOptions { RootDirectory = storage.RootPath };
using var session = new IOTempSession(options, storageProvider: storage);
```

### Custom Provider

Implement `IIOTempStorageProvider` once to use any backend (FTP, SFTP, Azure Blob, etc.):

```csharp
public sealed class FtpIOTempStorageProvider : IIOTempStorageProvider
{
    // implement RootPath, DirectoryExists, CreateDirectory, WriteAllBytes, OpenRead, ...
}

// Register it before AddIOTempService
services.AddSingleton<IIOTempStorageProvider>(new FtpIOTempStorageProvider(...));
services.AddIOTempService();
```

The provider interface covers: directory create/delete/enumerate, file touch/read/write/append/copy/move/delete, streaming open (read, create, append), file metadata (length,
creation time), async variants of all write operations, and an `EnsureDirectoryAccessible` hook (used for R/W probing; may be a no-op for in-memory providers).

## Configuration

### IOTempServiceOptions

| Option              | Default              | Description                                           |
|---------------------|----------------------|-------------------------------------------------------|
| `TempRoot`          | `Path.GetTempPath()` | OS temp root. Parent of `DirectoryName`.              |
| `DirectoryName`     | `"lyo-io-temp"`      | Subdirectory under `TempRoot`.                        |
| `FileLifetime`      | `null`               | Default expiry for `Cleanup()` with no argument.      |
| `MaxFileSizeBytes`  | 1 GB                 | Per-file hard limit.                                  |
| `MaxTotalSizeBytes` | 10 GB                | Total size limit across the service directory.        |
| `OverflowStrategy`  | `ThrowException`     | `ThrowException`, `DeleteOldest`, or `DeleteLargest`. |
| `EnableMetrics`     | `true`               | Record metrics via `IMetrics`.                        |

### IOTempSessionOptions

| Option                | Default          | Description                                          |
|-----------------------|------------------|------------------------------------------------------|
| `FileNamingStrategy`  | `Guid`           | `Guid`, `Sequential`, `Timestamp`, or `RandomChars`. |
| `FileExtension`       | `.tmp`           | Extension appended to auto-named files.              |
| `FilePrefix`/`Suffix` | `null`           | Optional pre/suffix for generated file names.        |
| `MaxFileSizeBytes`    | 1 GB             | Per-file hard limit.                                 |
| `MaxTotalSizeBytes`   | `null`           | Per-session total limit.                             |
| `OverflowStrategy`    | `ThrowException` | Action when total limit is exceeded.                 |

### IOTempCleanupOptions

| Option         | Default   | Description                                           |
|----------------|-----------|-------------------------------------------------------|
| `InitialDelay` | 5 minutes | Delay before the first cleanup run after app startup. |
| `Interval`     | 1 hour    | How often to run cleanup after the initial run.       |


## Dependencies

*(Synchronized from `Lyo.IO.Temp.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package                                                 | Version |
|---------------------------------------------------------|---------|
| `Microsoft.Extensions.Configuration.Abstractions`       | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder`             | `[10,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Hosting.Abstractions`             | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions`             | `[10,)` |

### Project references

- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Metrics`](../../../Core/Metrics/Lyo.Metrics/README.md)