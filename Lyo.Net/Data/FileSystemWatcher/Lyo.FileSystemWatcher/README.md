# Lyo.FileSystemWatcher

Snapshot-based .NET file watcher. Finds creates, deletes, changes, moves, and renames with debounce and SHA256 hashing.

## Features

- **Snapshot-based change detection.** Compares directory snapshots rather than relying only on FileSystemWatcher events.
- **Debouncing.** Batches rapid changes so an event storm does not fire.
- **Hash-based move detection.** Finds file moves and renames even when the file system does not report them.
- **Batch scan.** `ScanCompleted` fires once per debounce with the previous tree, current tree, and every detected change. `CurrentSnapshot` exposes the last completed tree.
- **Thread-safe.** Safe to call from multiple threads.
- **Metrics.** Optional `IMetrics` hook (`Lyo.Metrics`).
- **Options.** `FileSystemWatcherOptions` covers debounce, hashing, path comparison, subdirectory watching, and include/exclude regexes.
- **Errors.** Snapshot and detection failures go to `Error`, and to `ILogger` when you pass one.
- **Cancellation.** Long-running snapshot work accepts `CancellationToken`.
- **Logging.** Microsoft.Extensions.Logging when an `ILogger` is passed.

## Examples

### Hook events

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

### Set options

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

### Turn off hashing for speed

```csharp
var options = new FileSystemWatcherOptions
{
    EnableFileHashing = false // Significantly faster on large directories
};
```

### Change the debounce delay

```csharp
var options = new FileSystemWatcherOptions
{
    DebounceTimerDelay = 100 // Lower = faster response, higher CPU
    // DebounceTimerDelay = 1000 // Higher = slower response, lower CPU
};
```

### Case-sensitive paths (Linux/macOS)

```csharp
var options = new FileSystemWatcherOptions
{
    PathComparison = StringComparison.Ordinal // Case-sensitive
};
```

### React to file changes

```csharp
using var watcher = new FileSystemWatcher("C:\\MyDirectory");

watcher.FileChanged += (sender, e) =>
{
    Console.WriteLine($"File changed: {e.NewPath}");
    // Process file change...
};

Console.ReadLine(); // Keep running
```

### React to directory changes

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

### Include subdirectories

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

### Options for high throughput

```csharp
var options = new FileSystemWatcherOptions
{
    EnableFileHashing = false, // Disable hashing for speed
    DebounceTimerDelay = 1000, // Longer debounce for lower CPU
    IncludeSubdirectories = true
};

using var watcher = new FileSystemWatcher("C:\\LargeDirectory", options);
```

### Add the watcher in DI

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

### Kinds of change

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

### Wire metrics

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
| ----------------------- | ------------------ | ------------------- | ----------------------------------------------------------------------------------- |
| `IncludeSubdirectories` | `bool` | `false` | Whether subdirectories are watched recursively |
| `DebounceTimerDelay` | `int` | `250` | Debounce delay in milliseconds. Changes inside this delay are batched together |
| `EnableFileHashing` | `bool` | `true` | Enable file hashing for move/rename detection. Turn off for better performance |
| `PathComparison` | `StringComparison` | `OrdinalIgnoreCase` | String comparison for path operations. Use `Ordinal` on case-sensitive file systems |
| `EnableMetrics` | `bool` | `false` | Enable metrics collection (needs an IMetrics instance) |

## File events

- `FileCreated`. Raised when a file is created.
- `FileDeleted`. Raised when a file is deleted.
- `FileChanged`. Raised when a file's content is modified.
- `FileMoved`. Raised when a file is moved to a different directory.
- `FileRenamed`. Raised when a file is renamed (moved within the same directory).

## Directory change events

- `DirectoryCreated`. Raised when a directory is created.
- `DirectoryDeleted`. Raised when a directory is deleted.
- `DirectoryChanged`. Raised when a directory's content changes.
- `DirectoryMoved`. Raised when a directory is moved to a different parent.
- `DirectoryRenamed`. Raised when a directory is renamed (moved within the same parent).

## General events

- `OnAnyChange`. Raised for any file or directory change.
- `Error`. Raised when snapshot or change detection fails.

## Event data

Every event carries a `FileSystemChangeInfo` object with these properties:

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

When `EnableMetrics` is `true` and an `IMetrics` instance is provided, these metrics are recorded:

## Snapshot metrics

- `filesystemwatcher.snapshot.duration`. Duration of snapshot operations (timing).
- `filesystemwatcher.snapshot.duration_ms`. Snapshot duration in milliseconds (gauge).
- `filesystemwatcher.snapshot.file_count`. Number of files in snapshot (gauge).
- `filesystemwatcher.snapshot.directory_count`. Directory count in the snapshot (gauge).
- `filesystemwatcher.snapshot.item_count`. Total items in snapshot (gauge).

## Change detection metrics

- `filesystemwatcher.change_detection.duration`. Change-detection duration (timing).
- `filesystemwatcher.change_detection.duration_ms`. Change-detection duration in milliseconds (gauge).
- `filesystemwatcher.changes.detected`. Changes detected per scan (gauge).

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

Subscribe to `Error` for snapshot and detection failures. Pass an `ILogger` and those errors are logged as well.

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

Typical failure modes:

- **Snapshot failures.** Directory access denied, or disk errors.
- **Change detection.** Memory pressure or cancellation.
- **Event handler exceptions.** Caught and logged. They do not take down the watcher.

## File hashing

- **Enabled (default).** Accurate move/rename detection. Slower against large directories.
- **Disabled.** Faster. Move detection relies on file system events only.

## Memory usage

- Snapshots keep the complete directory tree in memory
- For very large directory structures (10,000+ files), consider these:
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
- **Large directories (> 1000 files).** 500ms+ per snapshot. Hashing large files stretches that further.

## Thread safety

`FileSystemWatcher` is thread-safe and can be used from more than one thread:

```csharp
// Safe to use from multiple threads
var watcher = new FileSystemWatcher("C:\\MyDirectory");

Task.Run(() => watcher.FileCreated += OnFileCreated);
Task.Run(() => watcher.FileDeleted += OnFileDeleted);
```

## Disposal

Always dispose the watcher when you are done:

```csharp
using var watcher = new FileSystemWatcher("C:\\MyDirectory");
// Use watcher...
// Automatically disposed when leaving scope
```

Or dispose by hand:

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

## Events are not firing

- **Check path exists.** The directory must exist when the watcher is created.
- **Check permissions.** Read access to the directory is required.
- **Check debounce delay.** Very rapid changes may be batched.
- **Check event handlers.** Subscribe before changes occur.
- **Wait for initial snapshot.** The watcher needs time to take that first snapshot.

## CPU is high

- **Disable file hashing.** Set `EnableFileHashing = false`.
- **Increase debounce delay.** Higher values reduce CPU usage.
- **Watch snapshot frequency.** Too many rapid changes can spike CPU use.

## Memory is high

- **Watch snapshot size.** Large directory trees consume more memory.
- **Disable hashing.** Reduces memory per file entry.
- **Dispose the watcher.** Leaks show up if it is left running.

## Move/rename events are missing

- **Enable file hashing.** Needed for reliable move/rename detection.
- **Check the file system.** Some file systems never report move events.
- **Check timing.** Very rapid moves may show up as delete+create.
- **Directory moves.** The directory name must stay the same (new parent, same name).

## Known limitation: file move bug

There is a known bug: directory change events for the source directory when moving a file show incorrect counts. The destination directory works correctly.

## Known limitations: performance

- File hashing can be slow on large files or on many files
- Snapshot operations are synchronous and can block for a moment
- Very large directory structures consume significant memory

## Known limitations: directory move detection

- Directory move detection only works when the directory name stays the same and the parent changes
- If both name and parent change, it is detected as delete + create

## Architecture: snapshot-based detection

The watcher takes periodic snapshots of the directory tree and diffs them. That is more reliable than FileSystemWatcher events by themselves.

## Architecture: debouncing

A debounce timer batches multiple rapid changes, which prevents event storms and reduces CPU usage.

## Architecture: hash-based move detection

File hashing (SHA256) detects moves and renames even when the file system does not provide that information directly.

## Architecture: error resilience

- Event handler exceptions are caught and logged, so one faulty handler cannot crash the watcher
- Snapshot errors are caught and reported through the Error event
- Cancellation tokens allow graceful shutdown of long-running operations

## Types

| Type | Description |
| ---------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `FileSystemWatcher` | Snapshot-based, debounced watcher (`IDisposable`). Constructor: `FileSystemWatcher(string path, FileSystemWatcherOptions?, ILogger?, IMetrics?)`. Raises the file/directory/`OnAnyChange`/`ScanCompleted`/`Error` events listed above. |
| `FileSystemWatcherOptions` | `IncludeSubdirectories`, `DebounceTimerDelay`, `EnableFileHashing`, `PathComparison`, `EnableMetrics`, `IncludePatterns`, `ExcludePatterns`. |
| `FileSystemChangeInfo` | `record` payload emitted by every change event. |
| `ChangeTypeEnum` | `Unknown` / `Created` / `Changed` / `Deleted` / `Renamed` / `Moved`. |
| `DirectorySnapshotEntry` | Single snapshot entry (path, info, optional `Hash`, `Fingerprint`, `FileSize`). |
| `SnapshotTree` / `SnapshotDirectoryNode` | In-memory snapshot of the watched tree, used for diffing. |
| `FileSystemScanCompletedEventArgs` / `ScanCompleted` | One event per debounce with previous tree, current tree, and the full change list. `CurrentSnapshot` is the last completed tree. |
| `FileSystemWatcherMapping` | `ToDto()` extensions that copy size/timestamps onto persistable models (relative '/' paths). |
| `Constants.Metrics` + `Constants.Metrics.Tags` | Metric and tag name constants (snapshot, change detection, event, and error metrics above). |
| `Utilities` | Helpers the watcher implementation shares. |

<!-- LYO_README_SYNC:END -->

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.FileSystemWatcher.Models` (direct, lyo)
- `Lyo.Hashing` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)