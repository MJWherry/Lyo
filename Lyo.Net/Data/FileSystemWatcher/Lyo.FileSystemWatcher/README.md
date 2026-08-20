# Lyo.FileSystemWatcher

Snapshot-based file watcher for .NET. Detects creates, deletes, changes, moves, and renames with debounce and SHA256 hashing.

## Features

- **Snapshot-based change detection.** Compares directory snapshots instead of relying only on FileSystemWatcher events.
- **Debouncing.** Batches rapid changes so you do not get an event storm.
- **Hash-based move detection.** Detects file moves and renames even when the file system does not report them.
- **File and directory events.** Separate events with `FileSystemChangeInfo` payloads.
- **Thread-safe.** Safe to use from multiple threads.
- **Metrics.** Optional `IMetrics` integration (`Lyo.Metrics`).
- **Options.** `FileSystemWatcherOptions` for debounce, hashing, path comparison, and subdirectory watching.
- **Errors.** Snapshot and detection failures go to `Error` and to `ILogger` when you pass one.
- **Cancellation.** `CancellationToken` on long-running snapshot work.
- **Logging.** Microsoft.Extensions.Logging when you pass an `ILogger`.

## Examples

### Subscribe to events

```csharp
using Lyo.FileSystemWatcher;
using Lyo.FileSystemWatcher.Enums;

// Create a watcher for a directory
using var watcher = new FileSystemWatcher("C:\\MyDirectory");

// Subscribe to events
watcher.FileCreated += (sender, e) =>
{
    Console.WriteLine($"File created: {e.NewPath}");
};

watcher.FileDeleted += (sender, e) =>
{
    Console.WriteLine($"File deleted: {e.OldPath}");
};

watcher.FileMoved += (sender, e) =>
{
    Console.WriteLine($"File moved: {e.OldPath} -> {e.NewPath}");
};

watcher.DirectoryChanged += (sender, e) =>
{
    Console.WriteLine($"Directory changed: {e.NewPath}");
    Console.WriteLine($" Files: {e.OldFileCount} -> {e.NewFileCount}");
    Console.WriteLine($" Directories: {e.OldDirectoryCount} -> {e.NewDirCount}");
};

// Watch for any change
watcher.OnAnyChange += (sender, e) =>
{
    Console.WriteLine($"Change detected: {e.ChangeType} - {e.NewPath ?? e.OldPath}");
};

// Keep the application running
Console.ReadLine();
```

### Configure options

```csharp
using Lyo.FileSystemWatcher;
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<FileSystemWatcher>();

var options = new FileSystemWatcherOptions
{
    IncludeSubdirectories = true, // Watch subdirectories
    DebounceTimerDelay = 500, // 500ms debounce delay
    EnableFileHashing = true, // Enable hash-based move detection
    PathComparison = StringComparison.OrdinalIgnoreCase, // Case-insensitive (Windows)
    EnableMetrics = true // Enable metrics collection
};

// Get metrics service (if using Lyo.Metrics)
var metrics = serviceProvider.GetService<IMetrics>();

using var watcher = new FileSystemWatcher("C:\\MyDirectory", options, logger, metrics);

// Handle errors
watcher.Error += (sender, ex) =>
{
    Console.WriteLine($"Watcher error: {ex.Message}");
};

// Subscribe to events...
```

### Disable file hashing for better performance

```csharp
var options = new FileSystemWatcherOptions
{
    EnableFileHashing = false // Significantly faster on large directories
};
```

### Adjust debounce delay

```csharp
var options = new FileSystemWatcherOptions
{
    DebounceTimerDelay = 100 // Lower = faster response, higher CPU
    // DebounceTimerDelay = 1000 // Higher = slower response, lower CPU
};
```

### Case-sensitive file systems (Linux/macOS)

```csharp
var options = new FileSystemWatcherOptions
{
    PathComparison = StringComparison.Ordinal // Case-sensitive
};
```

### Handle file change events

```csharp
using var watcher = new FileSystemWatcher("C:\\MyDirectory");

watcher.FileChanged += (sender, e) =>
{
    Console.WriteLine($"File changed: {e.NewPath}");
    // Process file change...
};

Console.ReadLine(); // Keep running
```

### Handle directory change events

```csharp
using var watcher = new FileSystemWatcher("C:\\MyDirectory");

watcher.DirectoryChanged += (sender, e) =>
{
    var fileDelta = (e.NewFileCount ?? 0) - (e.OldFileCount ?? 0);
    var dirDelta = (e.NewDirCount ?? 0) - (e.OldDirectoryCount ?? 0);
    
    Console.WriteLine($"Directory {e.NewPath} changed:");
    Console.WriteLine($" Files: {e.OldFileCount} -> {e.NewFileCount} (delta: {fileDelta:+0;-0;0})");
    Console.WriteLine($" Directories: {e.OldDirectoryCount} -> {e.NewDirCount} (delta: {dirDelta:+0;-0;0})");
};
```

### Watch subdirectories

```csharp
var options = new FileSystemWatcherOptions
{
    IncludeSubdirectories = true
};

using var watcher = new FileSystemWatcher("C:\\MyDirectory", options);

watcher.OnAnyChange += (sender, e) =>
{
    Console.WriteLine($"Change in {e.NewPath ?? e.OldPath}: {e.ChangeType}");
};
```

### High-performance options

```csharp
var options = new FileSystemWatcherOptions
{
    EnableFileHashing = false, // Disable hashing for speed
    DebounceTimerDelay = 1000, // Longer debounce for lower CPU
    IncludeSubdirectories = true
};

using var watcher = new FileSystemWatcher("C:\\LargeDirectory", options);
```

### Register with DI

```csharp
// In Startup.cs or Program.cs
services.AddSingleton<ILogger<FileSystemWatcher>>(sp =>
    sp.GetRequiredService<ILoggerFactory>().CreateLogger<FileSystemWatcher>());

services.AddSingleton<FileSystemWatcher>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<FileSystemWatcher>>();
    var metrics = sp.GetService<IMetrics>();
    var options = new FileSystemWatcherOptions
    {
        EnableMetrics = true,
        IncludeSubdirectories = true
    };
    return new FileSystemWatcher("C:\\MyDirectory", options, logger, metrics);
});
```

### Change types

```csharp
public enum ChangeTypeEnum
{
    Unknown = 0,
    Created = 1, // File or directory created
    Changed = 2, // File content modified or directory content changed
    Deleted = 3, // File or directory deleted
    Renamed = 4, // Renamed within same parent directory
    Moved = 5 // Moved to different parent directory
}
```

### Example metrics setup

```csharp
using Lyo.Metrics;

// Register metrics service
services.AddLyoMetrics();

// Create watcher with metrics
var metrics = serviceProvider.GetRequiredService<IMetrics>();
var options = new FileSystemWatcherOptions { EnableMetrics = true };
var watcher = new FileSystemWatcher("C:\\MyDirectory", options, logger, metrics);
```

## FileSystemWatcherOptions

| Property | Type | Default | Description |
| ----------------------- | ------------------ | ------------------- | ------------------------------------------------------------------------------------ |
| `IncludeSubdirectories` | `bool` | `false` | Whether to watch subdirectories recursively |
| `DebounceTimerDelay` | `int` | `250` | Debounce delay in milliseconds. Changes within this delay are batched together |
| `EnableFileHashing` | `bool` | `true` | Enable file hashing for move/rename detection. Disable for better performance |
| `PathComparison` | `StringComparison` | `OrdinalIgnoreCase` | String comparison for path operations. Use `Ordinal` for case-sensitive file systems |
| `EnableMetrics` | `bool` | `false` | Enable metrics collection (requires IMetrics instance) |

## File events

- `FileCreated`. Fired when a file is created.
- `FileDeleted`. Fired when a file is deleted.
- `FileChanged`. Fired when a file's content is modified.
- `FileMoved`. Fired when a file is moved to a different directory.
- `FileRenamed`. Fired when a file is renamed (moved within same directory).

## Directory events

- `DirectoryCreated`. Fired when a directory is created.
- `DirectoryDeleted`. Fired when a directory is deleted.
- `DirectoryChanged`. Fired when a directory's content changes.
- `DirectoryMoved`. Fired when a directory is moved to a different parent.
- `DirectoryRenamed`. Fired when a directory is renamed (moved within same parent).

## General events

- `OnAnyChange`. Fired for any file or directory change.
- `Error`. Fired when snapshot or change detection fails.

## Event data

All events provide a `FileSystemChangeInfo` object with the following properties:

```csharp
public sealed record FileSystemChangeInfo(
    string? OldPath, // Previous path (null for created items)
    string? NewPath, // New path (null for deleted items)
    ChangeTypeEnum ChangeType, // Type of change
    bool IsDirectory, // True if directory, false if file
    int? OldFileCount = null, // Directory: files before change
    int? OldDirectoryCount = null,// Directory: subdirectories before change
    int? NewFileCount = null, // Directory: files after change
    int? NewDirCount = null) // Directory: subdirectories after change
```

## Metrics integration

When `EnableMetrics` is set to `true` and an `IMetrics` instance is provided, the following metrics are recorded:

## Snapshot metrics

- `filesystemwatcher.snapshot.duration`. Duration of snapshot operations (timing).
- `filesystemwatcher.snapshot.duration_ms`. Duration of snapshot operations in milliseconds (gauge).
- `filesystemwatcher.snapshot.file_count`. Number of files in snapshot (gauge).
- `filesystemwatcher.snapshot.directory_count`. Number of directories in snapshot (gauge).
- `filesystemwatcher.snapshot.item_count`. Total items in snapshot (gauge).

## Change detection metrics

- `filesystemwatcher.change_detection.duration`. Duration of change detection (timing).
- `filesystemwatcher.change_detection.duration_ms`. Duration of change detection in milliseconds (gauge).
- `filesystemwatcher.changes.detected`. Number of changes detected per scan (gauge).

## Event metrics

- `filesystemwatcher.file.created`. File created events (counter).
- `filesystemwatcher.file.deleted`. File deleted events (counter).
- `filesystemwatcher.file.changed`. File changed events (counter).
- `filesystemwatcher.file.moved`. File moved events (counter).
- `filesystemwatcher.file.renamed`. File renamed events (counter).
- `filesystemwatcher.directory.created`. Directory created events (counter).
- `filesystemwatcher.directory.deleted`. Directory deleted events (counter).
- `filesystemwatcher.directory.changed`. Directory changed events (counter).
- `filesystemwatcher.directory.moved`. Directory moved events (counter).
- `filesystemwatcher.directory.renamed`. Directory renamed events (counter).

## Error metrics

- `filesystemwatcher.error.count`. Number of errors encountered (counter).

## Error handling

Subscribe to `Error` for snapshot and detection failures. If you pass an `ILogger`, those errors are also logged.

```csharp
// Subscribe to error events
watcher.Error += (sender, ex) =>
{
    Console.WriteLine($"Error: {ex.Message}");
    // Handle error appropriately
};

// Errors are also logged if a logger is provided
var logger = loggerFactory.CreateLogger<FileSystemWatcher>();
var watcher = new FileSystemWatcher("C:\\MyDirectory", options, logger);
```

Typical failures:

- **Snapshot failures.** Directory access denied, disk errors.
- **Change detection.** Memory pressure, cancellation.
- **Event handler exceptions.** Caught and logged. They do not crash the watcher.

## File hashing

- **Enabled (default).** Accurate move/rename detection. Slower on large directories.
- **Disabled.** Faster. Move detection relies on file system events only.

## Memory usage

- Snapshots store the complete directory tree in memory
- For very large directory structures (10,000+ files), consider:
- Disabling file hashing
- Increasing debounce delay
- Monitoring memory usage

## Debounce delay

- **Lower values (50-100ms).** Faster response, higher CPU usage.
- **Higher values (500-1000ms).** Slower response, lower CPU usage.
- **Default (250ms).** A reasonable default for most directories.

## Expected snapshot times

- **Small directories (< 100 files).** Under 100ms per snapshot.
- **Medium directories (100-1000 files).** 100-500ms per snapshot.
- **Large directories (> 1000 files).** 500ms+ per snapshot. Hashing large files stretches this.

## Thread safety

`FileSystemWatcher` is thread-safe and can be used from multiple threads:

```csharp
// Safe to use from multiple threads
var watcher = new FileSystemWatcher("C:\\MyDirectory");

Task.Run(() => watcher.FileCreated += OnFileCreated);
Task.Run(() => watcher.FileDeleted += OnFileDeleted);
```

## Disposal

Always dispose of the watcher when done:

```csharp
using var watcher = new FileSystemWatcher("C:\\MyDirectory");
// Use watcher...
// Automatically disposed when leaving scope
```

Or manually:

```csharp
var watcher = new FileSystemWatcher("C:\\MyDirectory");
try
{
    // Use watcher...
}
finally
{
    watcher.Dispose();
}
```

## Troubleshooting: events not firing

- **Check path exists.** The directory must exist when creating the watcher.
- **Check permissions.** You need read access to the directory.
- **Check debounce delay.** Very rapid changes may be batched together.
- **Check event handlers.** Subscribe before changes occur.
- **Wait for initial snapshot.** The watcher needs time to take the first snapshot.

## Troubleshooting: high CPU usage

- **Disable file hashing.** Set `EnableFileHashing = false`.
- **Increase debounce delay.** Higher values reduce CPU usage.
- **Watch snapshot frequency.** Too many rapid changes can spike CPU.

## Troubleshooting: memory usage

- **Watch snapshot size.** Large directory trees consume more memory.
- **Disable hashing.** Reduces memory per file entry.
- **Dispose the watcher.** Leaks show up if you leave it running.

## Troubleshooting: missing move/rename events

- **Enable file hashing.** Required for reliable move/rename detection.
- **Check the file system.** Some file systems do not report move events.
- **Check timing.** Very rapid moves may show up as delete+create.
- **Directory moves.** The directory name must stay the same (different parent, same name).

## Known limitation: file move bug

There is a known bug where directory change events for the source directory when moving a file show incorrect counts. The destination directory works correctly.

## Known limitations: performance

- File hashing can be slow on large files or many files
- Snapshot operations are synchronous and can block briefly
- Very large directory structures consume significant memory

## Known limitations: directory move detection

- Directory move detection only works when the directory name stays the same but the parent changes
- If both name and parent change, it will be detected as delete + create

## Architecture: snapshot-based detection

The watcher takes periodic snapshots of the directory tree and diffs them. That is more reliable than FileSystemWatcher events alone.

## Architecture: debouncing

Multiple rapid changes are batched together using a debounce timer to prevent event storms and reduce CPU usage.

## Architecture: hash-based move detection

File hashing (SHA256) is used to detect moves and renames even when the file system doesn't provide this information directly.

## Architecture: error resilience

- Event handler exceptions are caught and logged, preventing one faulty handler from crashing the watcher
- Snapshot errors are caught and reported via the Error event
- Cancellation tokens allow graceful shutdown of long-running operations

## Types

| Type | Description |
| ---------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `FileSystemWatcher` | Snapshot-based, debounced watcher (`IDisposable`). Constructor: `FileSystemWatcher(string path, FileSystemWatcherOptions?, ILogger?, IMetrics?)`. Raises the file/directory/`OnAnyChange`/`Error` events listed above. |
| `FileSystemWatcherOptions` | `IncludeSubdirectories`, `DebounceTimerDelay`, `EnableFileHashing`, `PathComparison`, `EnableMetrics`. |
| `FileSystemChangeInfo` | `record` payload emitted by every change event. |
| `ChangeTypeEnum` | `Unknown` / `Created` / `Changed` / `Deleted` / `Renamed` / `Moved`. |
| `DirectorySnapshotEntry` | Single snapshot entry (path, info, optional `Hash`, `Fingerprint`, `FileSize`). |
| `SnapshotTree` / `SnapshotDirectoryNode` | In-memory snapshot of the watched tree used for diffing. |
| `Constants.Metrics` + `Constants.Metrics.Tags` | Metric and tag name constants (see snapshot, change detection, event, and error metrics above). |
| `Utilities` | Helpers shared by the watcher implementation. |

<!-- LYO_README_SYNC:END -->

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` (direct, lyo)
- `Lyo.Hashing` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Exceptions` (transitive, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)