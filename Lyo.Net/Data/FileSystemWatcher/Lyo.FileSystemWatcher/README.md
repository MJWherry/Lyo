# Lyo.FileSystemWatcher

A production-ready file system watcher library for .NET that provides reliable change detection using snapshot-based
monitoring, debouncing, and hash-based move/rename detection.

## Features

- ✅ **Snapshot-Based Change Detection** - More reliable than relying solely on FileSystemWatcher events
- ✅ **Debouncing** - Batches rapid changes to prevent event storms
- ✅ **Hash-Based Move Detection** - Detects file moves and renames even when file system events don't provide this
  information
- ✅ **Comprehensive Events** - Separate events for files and directories with detailed change information
- ✅ **Thread-Safe** - Safe to use from multiple threads
- ✅ **Metrics Support** - Optional integration with Lyo.Metrics for observability
- ✅ **Configurable** - Extensive configuration options for performance and behavior tuning
- ✅ **Error Handling** - Comprehensive error handling with logging and error events
- ✅ **Cancellation Support** - Full cancellation token support for graceful shutdown
- ✅ **Structured Logging** - Full integration with Microsoft.Extensions.Logging

## Quick Start

### Basic Usage

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
    Console.WriteLine($"  Files: {e.OldFileCount} -> {e.NewFileCount}");
    Console.WriteLine($"  Directories: {e.OldDirectoryCount} -> {e.NewDirCount}");
};

// Watch for any change
watcher.OnAnyChange += (sender, e) =>
{
    Console.WriteLine($"Change detected: {e.ChangeType} - {e.NewPath ?? e.OldPath}");
};

// Keep the application running
Console.ReadLine();
```

### Advanced Configuration

```csharp
using Lyo.FileSystemWatcher;
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<FileSystemWatcher>();

var options = new FileSystemWatcherOptions
{
    IncludeSubdirectories = true,        // Watch subdirectories
    DebounceTimerDelay = 500,            // 500ms debounce delay
    EnableFileHashing = true,            // Enable hash-based move detection
    PathComparison = StringComparison.OrdinalIgnoreCase,  // Case-insensitive (Windows)
    EnableMetrics = true                  // Enable metrics collection
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

## Configuration Options

### FileSystemWatcherOptions

| Property                | Type               | Default             | Description                                                                          |
|-------------------------|--------------------|---------------------|--------------------------------------------------------------------------------------|
| `IncludeSubdirectories` | `bool`             | `false`             | Whether to watch subdirectories recursively                                          |
| `DebounceTimerDelay`    | `int`              | `250`               | Debounce delay in milliseconds. Changes within this delay are batched together       |
| `EnableFileHashing`     | `bool`             | `true`              | Enable file hashing for move/rename detection. Disable for better performance        |
| `PathComparison`        | `StringComparison` | `OrdinalIgnoreCase` | String comparison for path operations. Use `Ordinal` for case-sensitive file systems |
| `EnableMetrics`         | `bool`             | `false`             | Enable metrics collection (requires IMetrics instance)                               |

### Performance Tuning

**Disable File Hashing for Better Performance:**

```csharp
var options = new FileSystemWatcherOptions
{
    EnableFileHashing = false  // Significantly faster on large directories
};
```

**Adjust Debounce Delay:**

```csharp
var options = new FileSystemWatcherOptions
{
    DebounceTimerDelay = 100  // Lower = faster response, higher CPU
    // DebounceTimerDelay = 1000  // Higher = slower response, lower CPU
};
```

**Case-Sensitive File Systems (Linux/macOS):**

```csharp
var options = new FileSystemWatcherOptions
{
    PathComparison = StringComparison.Ordinal  // Case-sensitive
};
```

## Events

### File Events

- `FileCreated` - Fired when a file is created
- `FileDeleted` - Fired when a file is deleted
- `FileChanged` - Fired when a file's content is modified
- `FileMoved` - Fired when a file is moved to a different directory
- `FileRenamed` - Fired when a file is renamed (moved within same directory)

### Directory Events

- `DirectoryCreated` - Fired when a directory is created
- `DirectoryDeleted` - Fired when a directory is deleted
- `DirectoryChanged` - Fired when a directory's content changes
- `DirectoryMoved` - Fired when a directory is moved to a different parent
- `DirectoryRenamed` - Fired when a directory is renamed (moved within same parent)

### General Events

- `OnAnyChange` - Fired for any file or directory change
- `Error` - Fired when an error occurs during snapshot or change detection

## Event Data

All events provide a `FileSystemChangeInfo` object with the following properties:

```csharp
public sealed record FileSystemChangeInfo(
    string? OldPath,              // Previous path (null for created items)
    string? NewPath,              // New path (null for deleted items)
    ChangeTypeEnum ChangeType,    // Type of change
    bool IsDirectory,             // True if directory, false if file
    int? OldFileCount = null,     // Directory: files before change
    int? OldDirectoryCount = null,// Directory: subdirectories before change
    int? NewFileCount = null,     // Directory: files after change
    int? NewDirCount = null)      // Directory: subdirectories after change
```

## Change Types

```csharp
public enum ChangeTypeEnum
{
    Unknown = 0,
    Created = 1,   // File or directory created
    Changed = 2,   // File content modified or directory content changed
    Deleted = 3,   // File or directory deleted
    Renamed = 4,   // Renamed within same parent directory
    Moved = 5      // Moved to different parent directory
}
```

## Metrics Integration

When `EnableMetrics` is set to `true` and an `IMetrics` instance is provided, the following metrics are recorded:

### Snapshot Metrics

- `filesystemwatcher.snapshot.duration` - Duration of snapshot operations (timing)
- `filesystemwatcher.snapshot.duration_ms` - Duration of snapshot operations in milliseconds (gauge)
- `filesystemwatcher.snapshot.file_count` - Number of files in snapshot (gauge)
- `filesystemwatcher.snapshot.directory_count` - Number of directories in snapshot (gauge)
- `filesystemwatcher.snapshot.item_count` - Total items in snapshot (gauge)

### Change Detection Metrics

- `filesystemwatcher.change_detection.duration` - Duration of change detection (timing)
- `filesystemwatcher.change_detection.duration_ms` - Duration of change detection in milliseconds (gauge)
- `filesystemwatcher.changes.detected` - Number of changes detected per scan (gauge)

### Event Metrics

- `filesystemwatcher.file.created` - File created events (counter)
- `filesystemwatcher.file.deleted` - File deleted events (counter)
- `filesystemwatcher.file.changed` - File changed events (counter)
- `filesystemwatcher.file.moved` - File moved events (counter)
- `filesystemwatcher.file.renamed` - File renamed events (counter)
- `filesystemwatcher.directory.created` - Directory created events (counter)
- `filesystemwatcher.directory.deleted` - Directory deleted events (counter)
- `filesystemwatcher.directory.changed` - Directory changed events (counter)
- `filesystemwatcher.directory.moved` - Directory moved events (counter)
- `filesystemwatcher.directory.renamed` - Directory renamed events (counter)

All event metrics include tags: `change_type` and `item_type` (file/directory)

### Error Metrics

- `filesystemwatcher.error.count` - Number of errors encountered (counter)

### Example Metrics Setup

```csharp
using Lyo.Metrics;

// Register metrics service
services.AddLyoMetrics();

// Create watcher with metrics
var metrics = serviceProvider.GetRequiredService<IMetrics>();
var options = new FileSystemWatcherOptions { EnableMetrics = true };
var watcher = new FileSystemWatcher("C:\\MyDirectory", options, logger, metrics);
```

## Error Handling

The watcher provides comprehensive error handling:

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

Common error scenarios:

- **Snapshot failures**: Directory access denied, disk errors, etc.
- **Change detection errors**: Memory issues, cancellation, etc.
- **Event handler exceptions**: Errors in your event handlers are caught and logged (won't crash the watcher)

## Performance Considerations

### File Hashing

- **Enabled (default)**: Provides accurate move/rename detection but slower on large directories
- **Disabled**: Faster performance but move detection relies on file system events only

### Memory Usage

- Snapshots store the complete directory tree in memory
- For very large directory structures (10,000+ files), consider:
    - Disabling file hashing
    - Increasing debounce delay
    - Monitoring memory usage

### Debounce Delay

- **Lower values (50-100ms)**: Faster response, higher CPU usage
- **Higher values (500-1000ms)**: Slower response, lower CPU usage
- **Default (250ms)**: Good balance for most scenarios

### Expected Performance

- **Small directories (< 100 files)**: < 100ms per snapshot
- **Medium directories (100-1000 files)**: 100-500ms per snapshot
- **Large directories (> 1000 files)**: 500ms+ per snapshot (depends on file sizes if hashing enabled)

## Thread Safety

The `FileSystemWatcher` is **thread-safe** and can be used from multiple threads:

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

## Examples

### Watch for File Changes

```csharp
using var watcher = new FileSystemWatcher("C:\\MyDirectory");

watcher.FileChanged += (sender, e) =>
{
    Console.WriteLine($"File changed: {e.NewPath}");
    // Process file change...
};

Console.ReadLine(); // Keep running
```

### Monitor Directory Content Changes

```csharp
using var watcher = new FileSystemWatcher("C:\\MyDirectory");

watcher.DirectoryChanged += (sender, e) =>
{
    var fileDelta = (e.NewFileCount ?? 0) - (e.OldFileCount ?? 0);
    var dirDelta = (e.NewDirCount ?? 0) - (e.OldDirectoryCount ?? 0);
    
    Console.WriteLine($"Directory {e.NewPath} changed:");
    Console.WriteLine($"  Files: {e.OldFileCount} -> {e.NewFileCount} (delta: {fileDelta:+0;-0;0})");
    Console.WriteLine($"  Directories: {e.OldDirectoryCount} -> {e.NewDirCount} (delta: {dirDelta:+0;-0;0})");
};
```

### Watch Subdirectories

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

### High-Performance Configuration

```csharp
var options = new FileSystemWatcherOptions
{
    EnableFileHashing = false,      // Disable hashing for speed
    DebounceTimerDelay = 1000,      // Longer debounce for lower CPU
    IncludeSubdirectories = true
};

using var watcher = new FileSystemWatcher("C:\\LargeDirectory", options);
```

### Dependency Injection Example

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

## Troubleshooting

### Events Not Firing

1. **Check path exists**: The directory must exist when creating the watcher
2. **Check permissions**: Ensure read access to the directory
3. **Check debounce delay**: Very rapid changes may be batched together
4. **Check event handlers**: Ensure handlers are subscribed before changes occur
5. **Wait for initial snapshot**: The watcher needs time to take the initial snapshot

### High CPU Usage

1. **Disable file hashing**: Set `EnableFileHashing = false`
2. **Increase debounce delay**: Higher values reduce CPU usage
3. **Monitor snapshot frequency**: Too many rapid changes can cause high CPU

### Memory Usage

1. **Monitor snapshot size**: Large directory trees consume more memory
2. **Consider disabling hashing**: Reduces memory per file entry
3. **Watch for memory leaks**: Ensure watcher is properly disposed

### Missing Move/Rename Events

1. **Enable file hashing**: Required for reliable move/rename detection
2. **Check file system**: Some file systems may not provide move events
3. **Check timing**: Very rapid moves may be detected as delete+create
4. **Directory moves**: Directory name must stay the same for move detection (different parent, same name)

## Known Limitations

### File Move Bug

There is a known bug where directory change events for the source directory when moving a file show incorrect counts. The destination directory works correctly.

### Performance

- File hashing can be slow on large files or many files
- Snapshot operations are synchronous and can block briefly
- Very large directory structures consume significant memory

### Directory Move Detection

- Directory move detection only works when the directory name stays the same but the parent changes
- If both name and parent change, it will be detected as delete + create

## Architecture

### Snapshot-Based Detection

The watcher uses periodic snapshots of the directory structure, comparing them to detect changes. This provides more
reliable change detection than relying solely on FileSystemWatcher events.

### Debouncing

Multiple rapid changes are batched together using a debounce timer to prevent event storms and reduce CPU usage.

### Hash-Based Move Detection

File hashing (SHA256) is used to detect moves and renames even when the file system doesn't provide this information
directly.

### Error Resilience

- Event handler exceptions are caught and logged, preventing one faulty handler from crashing the watcher
- Snapshot errors are caught and reported via the Error event
- Cancellation tokens allow graceful shutdown of long-running operations




<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.FileSystemWatcher.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Microsoft.Extensions.Logging.Abstractions` | `[10,)` |

### Project references

- `Lyo.Metrics`

## Public API (generated)

Top-level `public` types in `*.cs` (*8*). Nested types and file-scoped namespaces may omit some entries.

- `ChangeTypeEnum`
- `Constants`
- `FileSystemWatcher`
- `FileSystemWatcherOptions`
- `IsExternalInit`
- `Metrics`
- `Tags`
- `Utilities`

<!-- LYO_README_SYNC:END -->

## License

[Your License Here]
