using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.FileSystemWatcher.Enums;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Timer = System.Threading.Timer;

namespace Lyo.FileSystemWatcher;

//bug when moving a file, changed event fires, shows same old and new as old count for from directory, new directory works

/// <summary>
/// A file system watcher that monitors a directory for changes using snapshot-based change detection. Provides debouncing, hash-based move/rename detection, and
/// comprehensive event notifications.
/// </summary>
/// <remarks>
/// <para>
/// This watcher uses a snapshot-based approach to detect changes, taking periodic snapshots of the directory structure and comparing them to detect file and directory changes.
/// This approach provides more reliable change detection than relying solely on the underlying FileSystemWatcher events.
/// </para>
/// <para>
/// Features:
/// <list type="bullet">
/// <item>Debouncing: Multiple rapid changes are batched together to prevent event storms</item>
/// <item>Hash-based move detection: Uses file hashing to detect moves and renames even when file system events don't provide this information</item>
/// <item>Comprehensive events: Separate events for files and directories, with detailed change information</item> <item>Thread-safe: Safe to use from multiple threads</item>
/// <item>Metrics support: Optional integration with Lyo.Metrics for observability</item>
/// </list>
/// </para>
/// <para>
/// Performance considerations:
/// <list type="bullet">
/// <item>File hashing can be disabled for better performance on large directories</item>
/// <item>Snapshots store a hierarchical tree (paths split by directory) in memory - consider memory usage for very large directory structures</item>
/// <item>Debounce delay can be adjusted to balance responsiveness vs. performance</item>
/// </list>
/// </para>
/// </remarks>
public class FileSystemWatcher : IDisposable
{
    /// <summary>Event fired for any file or directory change.</summary>
    public event EventHandler<FileSystemChangeInfo>? OnAnyChange;

    /// <summary>Event fired when a file is created.</summary>
    public event EventHandler<FileSystemChangeInfo>? FileCreated;

    /// <summary>Event fired when a file is deleted.</summary>
    public event EventHandler<FileSystemChangeInfo>? FileDeleted;

    /// <summary>Event fired when a file is moved to a different directory.</summary>
    public event EventHandler<FileSystemChangeInfo>? FileMoved;

    /// <summary>Event fired when a file is renamed (moved within the same directory).</summary>
    public event EventHandler<FileSystemChangeInfo>? FileRenamed;

    /// <summary>Event fired when a file's content is modified.</summary>
    public event EventHandler<FileSystemChangeInfo>? FileChanged;

    /// <summary>Event fired when a directory is created.</summary>
    public event EventHandler<FileSystemChangeInfo>? DirectoryCreated;

    /// <summary>Event fired when a directory is deleted.</summary>
    public event EventHandler<FileSystemChangeInfo>? DirectoryDeleted;

    /// <summary>Event fired when a directory is moved to a different parent directory.</summary>
    public event EventHandler<FileSystemChangeInfo>? DirectoryMoved;

    /// <summary>Event fired when a directory is renamed (moved within the same parent directory).</summary>
    public event EventHandler<FileSystemChangeInfo>? DirectoryRenamed;

    /// <summary>Event fired when a directory's content changes (files/directories added, removed, or modified).</summary>
    public event EventHandler<FileSystemChangeInfo>? DirectoryChanged;

    /// <summary>Event fired when an error occurs during snapshot or change detection.</summary>
    /// <remarks>Subscribe to this event to be notified of errors that occur during watcher operations. Errors are also logged if a logger is provided.</remarks>
    public event EventHandler<Exception>? Error;

    private readonly System.IO.FileSystemWatcher _watcher;
    private readonly Timer _debounceTimer;
    private SnapshotTree _previousSnapshot;
    private bool _scanScheduled;
    private readonly ILogger<FileSystemWatcher> _logger;
    private readonly FileSystemWatcherOptions _options;
    private readonly CancellationTokenSource _ctSource;
    private readonly IMetrics _metrics;
    private volatile bool _disposed;
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    /// <summary>Gets the directory path being watched.</summary>
    public string Path { get; }

    /// <summary>Gets or sets the debounce timer delay in milliseconds. Changes occurring within this delay will be batched together.</summary>
    /// <remarks>
    /// When multiple file system changes occur rapidly, they are debounced (batched together) to prevent event storms. The debounce delay determines how long to wait after the
    /// last change before processing all changes together. Default is 250ms. Lower values provide faster response but may increase CPU usage. Higher values reduce CPU usage but increase
    /// latency.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when value is negative.</exception>
    public int DebounceTimerDelay {
        get => _options.DebounceTimerDelay;
        set {
            ArgumentHelpers.ThrowIfNegative(value, nameof(value));
            _options.DebounceTimerDelay = value;
        }
    }

    /// <summary>Initializes a new instance of FileSystemWatcher with the specified path.</summary>
    /// <param name="path">The directory to watch.</param>
    /// <param name="includeSubDirectories">Whether to include subdirectories.</param>
    /// <param name="debounceTimerDelay">Debounce delay in milliseconds. Default is 250ms.</param>
    /// <exception cref="ArgumentException">Thrown when path is null, empty, or whitespace.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the specified directory does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the directory is not accessible due to access restrictions.</exception>
    /// <exception cref="IOException">Thrown when the directory is not accessible due to I/O errors.</exception>
    public FileSystemWatcher(string path, bool includeSubDirectories, int debounceTimerDelay = 250)
        : this(path, new() { IncludeSubdirectories = includeSubDirectories, DebounceTimerDelay = debounceTimerDelay }, null) { }

    /// <summary>Initializes a new instance of FileSystemWatcher with the specified path and options.</summary>
    /// <param name="path">The directory to watch.</param>
    /// <param name="options">Configuration options.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <param name="metrics">Optional metrics instance. Required if EnableMetrics is true in options.</param>
    /// <exception cref="ArgumentException">Thrown when path is null, empty, or whitespace, or when options validation fails.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the specified directory does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the directory is not accessible due to access restrictions.</exception>
    /// <exception cref="IOException">Thrown when the directory is not accessible due to I/O errors.</exception>
    /// <exception cref="InvalidOperationException">Thrown when metrics are enabled in options but IMetrics is not provided.</exception>
    public FileSystemWatcher(string path, FileSystemWatcherOptions? options = null, ILogger<FileSystemWatcher>? logger = null, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path, nameof(path));
        ExceptionThrower.ThrowIfDirectoryNotFound(path);
        ExceptionThrower.ThrowIfDirectoryNotAccessible(path);
        _options = options ?? new();
        options?.Validate();
        _logger = logger ?? NullLogger<FileSystemWatcher>.Instance;
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _ctSource = new();
        DebounceTimerDelay = _options.DebounceTimerDelay;
        Path = path;
        _watcher = new(path) {
            IncludeSubdirectories = _options.IncludeSubdirectories,
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
        };

        _watcher.Changed += OnFileSystemChanged;
        _watcher.Created += OnFileSystemChanged;
        _watcher.Deleted += OnFileSystemChanged;
        _watcher.Renamed += OnFileSystemChanged;
        _debounceTimer = new(Scan, null, Timeout.Infinite, Timeout.Infinite);
        try {
            using var timer = _metrics.StartTimer(Constants.Metrics.SnapshotDuration);
            var sw = Stopwatch.StartNew();
            _previousSnapshot = Utilities.TakeSnapshot(Path, _options.EnableFileHashing, _options.PathComparison);
            sw.Stop();
            _logger.LogDebug("Initial snapshot taken for path: {Path}", path);
            _metrics.RecordTiming(Constants.Metrics.SnapshotDuration, sw.Elapsed);
            _metrics.RecordGauge(Constants.Metrics.SnapshotDurationMs, sw.ElapsedMilliseconds);
            _metrics.RecordGauge(Constants.Metrics.SnapshotFileCount, _previousSnapshot.FileCount);
            _metrics.RecordGauge(Constants.Metrics.SnapshotDirectoryCount, _previousSnapshot.DirectoryCount);
            _metrics.RecordGauge(Constants.Metrics.SnapshotItemCount, _previousSnapshot.TotalEntryCount);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to take initial snapshot for path: {Path}", path);
            _metrics.RecordError(Constants.Metrics.SnapshotDuration, ex, [(Constants.Metrics.Tags.Operation, "InitialSnapshot")]);
            _metrics.IncrementCounter(Constants.Metrics.ErrorCount);
            OnError(ex);
            throw;
        }
    }

    /// <summary>Releases all resources used by the FileSystemWatcher.</summary>
    /// <remarks>Calling Dispose will stop watching for changes and release all resources. After disposal, the watcher cannot be used again.</remarks>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _ctSource.Cancel();
        _ctSource.Dispose();
        _watcher.Dispose();
        _debounceTimer.Dispose();
    }

    private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
    {
        if (_disposed || _ctSource.Token.IsCancellationRequested)
            return;

        lock (_lock) {
            if (_scanScheduled) {
                // Debouncing: multiple rapid changes are batched together
                // This is intentional behavior to prevent event storms
                return;
            }

            _scanScheduled = true;
            _debounceTimer.Change(DebounceTimerDelay, Timeout.Infinite);
        }
    }

    private void Scan(object? state)
    {
        if (_disposed || _ctSource.Token.IsCancellationRequested) {
            lock (_lock)
                _scanScheduled = false;

            return;
        }

        SnapshotTree currentSnapshot;
        var snapshotSw = Stopwatch.StartNew();
        try {
            using var snapshotTimer = _metrics.StartTimer(Constants.Metrics.SnapshotDuration);
            currentSnapshot = Utilities.TakeSnapshot(Path, _options.EnableFileHashing, _options.PathComparison, _ctSource.Token, _previousSnapshot);
            snapshotSw.Stop();
            _metrics.RecordTiming(Constants.Metrics.SnapshotDuration, snapshotSw.Elapsed);
            _metrics.RecordGauge(Constants.Metrics.SnapshotDurationMs, snapshotSw.ElapsedMilliseconds);
            _metrics.RecordGauge(Constants.Metrics.SnapshotFileCount, currentSnapshot.FileCount);
            _metrics.RecordGauge(Constants.Metrics.SnapshotDirectoryCount, currentSnapshot.DirectoryCount);
            _metrics.RecordGauge(Constants.Metrics.SnapshotItemCount, currentSnapshot.TotalEntryCount);
        }
        catch (OperationCanceledException) {
            _logger.LogDebug("Snapshot operation was cancelled");
            lock (_lock)
                _scanScheduled = false;

            return;
        }
        catch (Exception ex) {
            snapshotSw.Stop();
            _logger.LogError(ex, "Failed to take snapshot for path: {Path}", Path);
            _metrics.RecordError(Constants.Metrics.SnapshotDuration, ex, [(Constants.Metrics.Tags.Operation, "Snapshot")]);
            _metrics.IncrementCounter(Constants.Metrics.ErrorCount);
            OnError(ex);
            lock (_lock)
                _scanScheduled = false;

            return;
        }

        var changeDetectionSw = Stopwatch.StartNew();
        try {
            using var changeDetectionTimer = _metrics.StartTimer(Constants.Metrics.ChangeDetectionDuration);
            var changes = Utilities.DetectChanges(_previousSnapshot, currentSnapshot, _options.PathComparison, _ctSource.Token);
            changeDetectionSw.Stop();
            if (changes.Count > 0) {
                _metrics.RecordGauge(Constants.Metrics.ChangesDetected, changes.Count);
                _metrics.RecordTiming(Constants.Metrics.ChangeDetectionDuration, changeDetectionSw.Elapsed);
                _metrics.RecordGauge(Constants.Metrics.ChangeDetectionDurationMs, changeDetectionSw.ElapsedMilliseconds);
            }

            // Fire events for all detected changes
            foreach (var change in changes) {
                if (_ctSource.Token.IsCancellationRequested)
                    break;

                FireEvent(change);
            }

            // Check for directory content changes before updating snapshot
            if (!_ctSource.Token.IsCancellationRequested)
                CheckDirectoryChanges(_previousSnapshot, currentSnapshot, changes);

            _previousSnapshot = currentSnapshot;
        }
        catch (OperationCanceledException) {
            _logger.LogDebug("Change detection operation was cancelled");
        }
        catch (Exception ex) {
            changeDetectionSw.Stop();
            _logger.LogError(ex, "Error during change detection for path: {Path}", Path);
            _metrics.RecordError(Constants.Metrics.ChangeDetectionDuration, ex, [(Constants.Metrics.Tags.Operation, "ChangeDetection")]);
            _metrics.IncrementCounter(Constants.Metrics.ErrorCount);
            OnError(ex);
        }
        finally {
            lock (_lock)
                _scanScheduled = false;
        }
    }

    private void CheckDirectoryChanges(
        SnapshotTree oldSnapshot,
        SnapshotTree newSnapshot,
        List<FileSystemChangeInfo> existingChanges)
    {
        var stringComparer = _options.PathComparison == StringComparison.OrdinalIgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var affectedDirs = new HashSet<string>(stringComparer);
        var processedChanges = existingChanges.Where(c => c.OldPath != null || c.NewPath != null);

        // Collect affected parent directories
        foreach (var change in processedChanges) {
            if (_ctSource.Token.IsCancellationRequested)
                return;

            if (change.OldPath != null)
                AddParentDir(change.OldPath, affectedDirs);

            if (change.NewPath != null)
                AddParentDir(change.NewPath, affectedDirs);
        }

        // Check each affected directory for content changes
        foreach (var dirPath in affectedDirs.Where(Directory.Exists)) {
            if (_ctSource.Token.IsCancellationRequested)
                return;

            if (!Utilities.HasDirectoryChanged(dirPath, oldSnapshot, newSnapshot, _options.PathComparison))
                continue;

            var oldCounts = Utilities.GetSnapshotCounts(dirPath, oldSnapshot, _options.PathComparison);
            var newCounts = Utilities.GetDirectoryContentCounts(dirPath);
            var change = new FileSystemChangeInfo(dirPath, dirPath, ChangeTypeEnum.Changed, true, oldCounts.fileCount, oldCounts.dirCount, newCounts.fileCount, newCounts.dirCount);
            FireEvent(change);
        }
    }

    private void AddParentDir(string path, HashSet<string> dirs)
    {
        var parent = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(parent) && parent.StartsWith(Path, _options.PathComparison))
            dirs.Add(parent);
    }

    private void FireEvent(FileSystemChangeInfo change)
    {
        (string, string)[] tags = [(Constants.Metrics.Tags.ChangeType, change.ChangeType.ToString()), (Constants.Metrics.Tags.ItemType, change.IsDirectory ? "directory" : "file")];
        if (change.IsDirectory) {
            switch (change.ChangeType) {
                case ChangeTypeEnum.Created:
                    _metrics.IncrementCounter(Constants.Metrics.DirectoryCreatedCount, tags: tags);
                    break;
                case ChangeTypeEnum.Deleted:
                    _metrics.IncrementCounter(Constants.Metrics.DirectoryDeletedCount, tags: tags);
                    break;
                case ChangeTypeEnum.Changed:
                    _metrics.IncrementCounter(Constants.Metrics.DirectoryChangedCount, tags: tags);
                    break;
                case ChangeTypeEnum.Moved:
                    _metrics.IncrementCounter(Constants.Metrics.DirectoryMovedCount, tags: tags);
                    break;
                case ChangeTypeEnum.Renamed:
                    _metrics.IncrementCounter(Constants.Metrics.DirectoryRenamedCount, tags: tags);
                    break;
            }
        }
        else {
            switch (change.ChangeType) {
                case ChangeTypeEnum.Created:
                    _metrics.IncrementCounter(Constants.Metrics.FileCreatedCount, tags: tags);
                    break;
                case ChangeTypeEnum.Deleted:
                    _metrics.IncrementCounter(Constants.Metrics.FileDeletedCount, tags: tags);
                    break;
                case ChangeTypeEnum.Changed:
                    _metrics.IncrementCounter(Constants.Metrics.FileChangedCount, tags: tags);
                    break;
                case ChangeTypeEnum.Moved:
                    _metrics.IncrementCounter(Constants.Metrics.FileMovedCount, tags: tags);
                    break;
                case ChangeTypeEnum.Renamed:
                    _metrics.IncrementCounter(Constants.Metrics.FileRenamedCount, tags: tags);
                    break;
            }
        }

        // Fire specific event with exception handling
        var eventToFire = change.IsDirectory ? GetDirectoryEvent(change.ChangeType) : GetFileEvent(change.ChangeType);
        try {
            eventToFire?.Invoke(this, change);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error in event handler for {ChangeType} event on {Path}", change.ChangeType, change.NewPath ?? change.OldPath);
            _metrics.RecordError(
                Constants.Metrics.ErrorCount, ex, [(Constants.Metrics.Tags.Operation, "EventFire"), (Constants.Metrics.Tags.ChangeType, change.ChangeType.ToString())]);

            _metrics.IncrementCounter(Constants.Metrics.ErrorCount);
            OnError(ex);
        }

        try {
            OnAnyChange?.Invoke(this, change);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error in OnAnyChange event handler for {ChangeType} event on {Path}", change.ChangeType, change.NewPath ?? change.OldPath);
            _metrics.RecordError(
                Constants.Metrics.ErrorCount, ex, [(Constants.Metrics.Tags.Operation, "OnAnyChange"), (Constants.Metrics.Tags.ChangeType, change.ChangeType.ToString())]);

            _metrics.IncrementCounter(Constants.Metrics.ErrorCount);
            OnError(ex);
        }
    }

    private void OnError(Exception exception)
    {
        try {
            Error?.Invoke(this, exception);
        }
        catch {
            // Ignore errors in error handler to prevent infinite loops
        }
    }

    private EventHandler<FileSystemChangeInfo>? GetFileEvent(ChangeTypeEnum changeType)
        => changeType switch {
            ChangeTypeEnum.Created => FileCreated,
            ChangeTypeEnum.Deleted => FileDeleted,
            ChangeTypeEnum.Moved => FileMoved,
            ChangeTypeEnum.Renamed => FileRenamed,
            ChangeTypeEnum.Changed => FileChanged,
            var _ => null
        };

    private EventHandler<FileSystemChangeInfo>? GetDirectoryEvent(ChangeTypeEnum changeType)
        => changeType switch {
            ChangeTypeEnum.Created => DirectoryCreated,
            ChangeTypeEnum.Deleted => DirectoryDeleted,
            ChangeTypeEnum.Moved => DirectoryMoved,
            ChangeTypeEnum.Renamed => DirectoryRenamed,
            ChangeTypeEnum.Changed => DirectoryChanged,
            var _ => null
        };
}