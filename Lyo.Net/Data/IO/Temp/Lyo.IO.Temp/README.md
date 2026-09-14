# Lyo.IO.Temp

Session-scoped temp files and directories, with naming strategies and overflow policies.

Callers use `IIOTempService` and `IIOTempSession`. `IOTempService` and `IOTempSession` are the stock implementations. When the repo generates XML docs, IntelliSense repeats the same summaries as this README. Types that only implement those interfaces use `<inheritdoc />`.

## Features

- **Sessions.** `IIOTempSession` owns a set of temp files and dirs and deletes them when disposed.
- **One-offs.** `CreateFile` / `CreateDirectory` without opening a session.
- **Pluggable storage.** All I/O goes through `Lyo.IO.FileSystem.IFileSystem`. Default is `LocalFileSystem` (`PathStyle.Host`). Set `IOTempServiceOptions.StorageKind` to `Memory` for `MemoryFileSystem` (`PathStyle.Posix`), or register `IFileSystem` before `AddIOTempService`. Path combine/normalize/jail uses `Lyo.Common.Core.Pathing.PathHelpers`.
- **Naming.** `Guid`, `Sequential`, `Timestamp`, `RandomChars`.
- **Overflow.** When a per-file or total-size cap is crossed: `ThrowException`, `DeleteOldest`, or `DeleteLargest`.
- **Generator.** `session.Generator` writes random-byte files, text/CSV/JSON, zips, and fake directory trees.
- **Events.** Session: `FileCreated` / `DirectoryCreated` / `FileWritten` / `FileAppended` / `FileDeleted` / `DirectoryDeleted` / `Overflow` / `Cleared` / `Disposed`. Service: `SessionCreated` / `SessionDisposed` / one-off create and cleanup deletes.
- **Nested sessions.** Child sessions whose root sits inside a parent session.
- **Inspection.** Snapshots, byte totals, on-disk enumerations.
- **Keyed pools.** `GetOrCreateSession(key)` for per-request or per-pipeline reuse.
- **Fluent option helpers.** `WithMaxFileSize` / `WithMaxTotalSize` on options objects.
- **Test asserts.** `AssertFilesExist` / `AssertTotalSize` on `IIOTempSession`.
- **Auto-cleanup.** An `IHostedService` that calls `Cleanup()` on a timer.
- **Metrics.** Session/file/cleanup counters when `IMetrics` is registered.

## Examples

### DI registration

```csharp
// Add to DI
services.AddIOTempService(); // uses default options

// Or configure options
services.AddIOTempService(options =>
{
    options.DirectoryName = "my-app-temp";
    options.MaxTotalSizeBytes = 500 * 1024 * 1024; // 500 MB
});

// Add with automatic background cleanup
services.AddIOTempServiceWithAutoCleanup(
    cleanupInterval: TimeSpan.FromHours(1),
    initialDelay: TimeSpan.FromMinutes(5));
```

### Sessions (preferred)

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

### Generator

```csharp
// Random-bytes files
var file = session.Generator.CreateRandomFile(FileSizeUnitInfo.Megabyte, 1);
var files = session.Generator.CreateRandomFiles(5, FileSizeUnitInfo.Kilobyte, 64);

// Named random files (name selector per index)
var named = session.Generator.CreateRandomFiles(3, 1024, i => $"chunk_{i}.bin");

// Structured content
var txt = session.Generator.CreateTextFile(lines: 100, charsPerLine: 80);
var csv = session.Generator.CreateCsvFile(rows: 500, columns: 10);
var json = session.Generator.CreateJsonFile(depth: 3, keysPerObject: 5);

// Zip archive
var zip = session.Generator.CreateZipFile(TempDirectorySpec.Flat(10, 1024));

// Simulated directory tree
var dir = session.Generator.SimulateDirectory(TempDirectorySpec.Flat(20, 512));
```

### TempDirectorySpec

```csharp
// Fluent builder
var spec = TempDirectorySpec.Builder()
    .WithFiles(5, FileSizeUnitInfo.Kilobyte, 4)
    .WithFileSizeSelector(i => (i + 1) * 512) // per-file size varies
    .WithSubdirectory(sub => sub.WithFiles(3, 256))
    .WithSubdirectory(TempDirectorySpec.Flat(2, 128))
    .Build();

// Randomised spec
var randomSpec = TempDirectorySpec.Random(
    minFiles: 3, maxFiles: 10,
    minSize: 512, maxSize: 4096);
```

### Mutating a session

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
session.FileCreated += path => Console.WriteLine($"Created: {path}");
session.DirectoryCreated += path => Console.WriteLine($"Dir created: {path}");
session.FileWritten += path => Console.WriteLine($"Written: {path}");
session.FileAppended += path => Console.WriteLine($"Appended: {path}");
session.FileDeleted += path => Console.WriteLine($"Deleted: {path}");
session.DirectoryDeleted += path => Console.WriteLine($"Dir deleted: {path}");
session.Overflow += path => Console.WriteLine($"Evicted: {path}");
session.Cleared += path => Console.WriteLine($"Cleared: {path}");
session.Disposed += path => Console.WriteLine($"Disposed: {path}");

service.SessionCreated += path => Console.WriteLine($"Session: {path}");
service.SessionDisposed += path => Console.WriteLine($"Session gone: {path}");
```

### Nested sessions

```csharp
using var sub = session.CreateSubSession();
// sub is rooted inside session.SessionDirectory
// disposing session also removes all sub-session content
```

### Inspection

```csharp
long bytes = session.GetTotalBytesUsed();
var snapshot = session.GetSnapshot(); // TempSessionSnapshot: frozen view

var files = session.EnumerateFiles("*.csv"); // all .csv on disk (including untracked)
var dirs = session.EnumerateDirectories();
```

### Keyed session pool

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

### Fluent option helpers

```csharp
var sessionOptions = new IOTempSessionOptions()
    .WithMaxFileSize(FileSizeUnitInfo.Megabyte, 5)
    .WithMaxTotalSize(FileSizeUnitInfo.Gigabyte, 1);
```

### Test assertions

```csharp
session.AssertFilesExist(); // all tracked files exist on disk
session.AssertTotalSize(expectedBytes: 2048, toleranceBytes: 64);
```

### One-offs (no session)

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

### Sessions in unit tests

```csharp
public sealed class MyServiceTests : IDisposable
{
    private readonly IIOTempSession _tempSession =
        IOTempSession.CreateForTests(nameof(MyServiceTests));

    public void Dispose() => _tempSession.Dispose();
}
```

## Generator

Use `session.Generator`:

## TempDirectorySpec

Describe a folder layout for simulation or zip building:

## Storage backends

All I/O goes through `IFileSystem`. `LocalFileSystem` and `MemoryFileSystem` ship in `Lyo.IO.FileSystem`. Register another `IFileSystem` via DI for a different backend (SFTP, FTP, S3, Azure Blob). Each backend reports `PathStyle` (`Host` on real disk, `Posix` for in-memory or remote). Service, session, and generator path combine/normalize/jail checks all go through `PathHelpers` with that style. `CopyFrom` / `MoveFrom` / `ExtractZipFile` may import a host path that sits outside the VFS jail.

## LocalFileSystem, the default

Calls `System.IO` with `PathStyle.Host`. Picked automatically when no `IFileSystem` is registered and `StorageKind` is `Local`.

```csharp
// Implicit — no registration needed
services.AddIOTempService();
```

## MemoryFileSystem

`ConcurrentDictionary` store with `PathStyle.Posix` (`/` separators, no OS path resolution). No filesystem access; fits Blazor WASM and unit tests.
Data lasts for the lifetime of the `IFileSystem` instance.

```csharp
// Blazor WASM (Program.cs)
builder.Services.AddMemoryFileSystem();
builder.Services.AddIOTempService();

// Or via options when nothing is registered
services.AddIOTempService(o => o.StorageKind = IOTempStorageKind.Memory);

// xUnit / NUnit — direct construction
var storage = new MemoryFileSystem();
var options = new IOTempSessionOptions { RootDirectory = storage.RootPath };
using var session = new IOTempSession(options, storageProvider: storage);
```

## Write a custom backend

Implement `IFileSystem` in the package that owns the backend (see `SftpFileSystem` / `FtpFileSystem` / `S3FileSystem`). Register it before `AddIOTempService`:

```csharp
services.AddSingleton<IFileSystem>(new FtpFileSystem(ftpClient));
services.AddIOTempService();
```

`FileSystemExtensions` covers `TouchFile`, `WriteAllBytes` / `WriteAllText` / `AppendAllText`, `CopyStreamToFileAsync`, and `EnsureDirectoryAccessible` (used for R/W probing; may be a no-op for in-memory backends).

## IOTempServiceOptions

| Option | Default | Description |
| ------------------- | -------------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| `TempRoot` | `Path.GetTempPath()` | OS temp root. Parent folder of `DirectoryName`. |
| `DirectoryName` | `"lyo-io-temp"` | Folder under `TempRoot`. |
| `StorageKind` | `Local` | `Local` (`LocalFileSystem` at `RootDirectory`) or `Memory` (`MemoryFileSystem`). Ignored when `IFileSystem` is already registered. |
| `FileLifetime` | `null` | Expiry `Cleanup()` uses when no argument is passed. |
| `MaxFileSizeBytes` | 1 GB | Hard cap per file. |
| `MaxTotalSizeBytes` | 10 GB | Cap on total size under the service directory. |
| `OverflowStrategy` | `ThrowException` | `ThrowException`, `DeleteOldest`, or `DeleteLargest`. |
| `EnableMetrics` | `true` | Write metrics through `IMetrics`. |

## IOTempSessionOptions

| Option | Default | Description |
| -------------------------------- | ------------------------------------ | ------------------------------------------------------------------------------------------------------ |
| `RootDirectory` | `{TempPath}/lyo-io-temp/{ProcessId}` | Folder that contains the session directory. A per-process suffix isolates parallel runners by default. |
| `CreateRootDirectoryIfNotExists` | `true` | If true, a missing `RootDirectory` is created at construction; if false, a missing root throws. |
| `FileNamingStrategy` | `Guid` | `Guid`, `Sequential`, `Timestamp`, or `RandomChars`. |
| `FileExtension` | `.tmp` | Suffix added to auto-named files. |
| `FilePrefix`/`Suffix` | `null` | Optional prefix or suffix on generated file names. |
| `MaxFileSizeBytes` | 1 GB | Hard cap per file. |
| `MaxTotalSizeBytes` | `null` | Total cap for one session. |
| `OverflowStrategy` | `ThrowException` | What happens when the total cap is crossed. |

## Sessions in unit tests

`IOTempSession.CreateForTests` is a one-liner for unit tests. The root is created for you and named after the test so leftover folders from a failed CI run are easy to find. Sessions sit under `{TempPath}/lyo-io-temp-tests/{subdirectoryName}/{Guid}/`, away from production temp dirs.

## IOTempCleanupOptions

| Option | Default | Description |
| -------------- | --------- | ---------------------------------------------------- |
| `InitialDelay` | 5 minutes | Wait after app startup before the first cleanup run. |
| `Interval` | 1 hour | How often cleanup runs after that first pass. |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.IO.FileSystem` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)