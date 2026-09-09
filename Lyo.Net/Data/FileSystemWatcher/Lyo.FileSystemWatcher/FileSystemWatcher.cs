using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.FileSystemWatcher.Enums;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Timer = System.Threading.Timer;

namespace Lyo.FileSystemWatcher;

// Known bug: a file move also fires Changed, and the source directory reports the same old/new counts as the old count. The destination directory is correct.

/// <summary>
/// Watches a directory by taking snapshots and comparing them. Batches rapid changes, detects moves and renames with hashes, and raises detailed events.
/// </summary>
/// <remarks>
/// <para>
/// The watcher takes periodic snapshots of the tree and diffs them to find file and directory changes. That is more reliable than depending only on native FileSystemWatcher
/// events.
/// </para>
/// <para>
/// Features:
/// <list type="bullet">
/// <item>Debouncing: rapid changes are batched so event storms do not fire</item>
/// <item>Hash-based move detection: hashing finds moves and renames even when the file system does not report them</item>
/// <item>Separate file and directory events with detailed change info</item>
/// <item>Safe to use from more than one thread</item>
/// <item>Optional Lyo.Metrics integration</item>
/// </list>
/// </para>
/// <para>
/// Performance notes:
/// <list type="bullet">
/// <item>Turn off file hashing on large trees if you need more speed</item>
/// <item>Snapshots keep a hierarchical tree in memory (paths split by directory). Very large trees will use more RAM</item>
/// <item>Raise or lower the debounce delay to trade latency against CPU</item>
/// </list>
/// </para>
/// </remarks>
public class FileSystemWatcher : IDisposable
{
    /// <summary>Raised for every file or directory change.</summary>
    public event EventHandler<FileSystemChangeInfo>? OnAnyChange;

    /// <summary>Raised when a file is created.</summary>
    public event EventHandler<FileSystemChangeInfo>? FileCreated;

    /// <summary>Raised when a file is deleted.</summary>
    public event EventHandler<FileSystemChangeInfo>? FileDeleted;

    /// <summary>Raised when a file is moved into a different directory.</summary>
    public event EventHandler<FileSystemChangeInfo>? FileMoved;

    /// <summary>Raised when a file is renamed, meaning it moved inside the same directory.</summary>
    public event EventHandler<FileSystemChangeInfo>? FileRenamed;

    /// <summary>Raised when a file's content changes.</summary>
    public event EventHandler<FileSystemChangeInfo>? FileChanged;

    /// <summary>Raised when a directory is created.</summary>
    public event EventHandler<FileSystemChangeInfo>? DirectoryCreated;

    /// <summary>Raised when a directory is deleted.</summary>
    public event EventHandler<FileSystemChangeInfo>? DirectoryDeleted;

    /// <summary>Raised when a directory is moved to a different parent.</summary>
    public event EventHandler<FileSystemChangeInfo>? DirectoryMoved;

    /// <summary>Raised when a directory is renamed, meaning it moved inside the same parent.</summary>
    public event EventHandler<FileSystemChangeInfo>? DirectoryRenamed;

    /// <summary>Raised when a directory's contents change (files or directories added, removed, or modified).</summary>
    public event EventHandler<FileSystemChangeInfo>? DirectoryChanged;

    /// <summary>Raised when snapshot or change detection fails.</summary>
    /// <remarks>Subscribe to hear about watcher errors. Errors are also logged when a logger is supplied.</remarks>
    public event EventHandler<Exception>? Error;

    /// <summary>
    /// Raised once per debounce after a scan, with the previous tree, current tree, and the full change list (including directory-content events).
    /// Copy DTOs immediately if you persist — live <c>FileInfo</c> goes stale. Do not block this handler with I/O.
    /// </summary>
    public event EventHandler<FileSystemScanCompletedEventArgs>? ScanCompleted;

    /// <summary>Last completed snapshot tree (the initial snapshot after construction, then each scan result).</summary>
    public SnapshotTree CurrentSnapshot => _previousSnapshot;

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
    /// <summary>Directory path this watcher is pointed at.</summary>
    public string Path { get; }

    /// <summary>Debounce delay in milliseconds. Changes inside this window are batched.</summary>
    /// <remarks>
    /// Rapid file-system changes are batched so events do not storm. The delay is how long to wait after the last change before processing the batch. Starts at 250ms. A lower
    /// value responds sooner and may use more CPU. A higher value uses less CPU and adds latency.
    /// </remarks>
    /// <exception cref="ArgumentException">Raised when the value is negative.</exception>
    public int DebounceTimerDelay {
        get => _options.DebounceTimerDelay;
        set {
            ArgumentHelpers.ThrowIfNegative(value);
            _options.DebounceTimerDelay = value;
        }
    }

    /// <summary>Builds a FileSystemWatcher for <paramref name="path"/>.</summary>
    /// <param name="path">Directory to watch.</param>
    /// <param name="includeSubDirectories">If true, subdirectories are included.</param>
    /// <param name="debounceTimerDelay">Debounce delay in milliseconds. Starts at 250.</param>
    /// <exception cref="ArgumentException">Raised when path is null, empty, or whitespace.</exception>
    /// <exception cref="DirectoryNotFoundException">Raised when the directory does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Raised when access to the directory is denied.</exception>
    /// <exception cref="IOException">Raised when the directory cannot be opened because of I/O errors.</exception>
    public FileSystemWatcher(string path, bool includeSubDirectories, int debounceTimerDelay = 250)
        : this(path, new() { IncludeSubdirectories = includeSubDirectories, DebounceTimerDelay = debounceTimerDelay }, null) { }

    /// <summary>Builds a FileSystemWatcher for <paramref name="path"/> using <paramref name="options"/>.</summary>
    /// <param name="path">Directory to watch.</param>
    /// <param name="options">Watcher settings.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="metrics">Optional metrics. Required when EnableMetrics is on.</param>
    /// <exception cref="ArgumentException">Raised when path is null, empty, or whitespace, or when options fail validation.</exception>
    /// <exception cref="DirectoryNotFoundException">Raised when the directory does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Raised when access to the directory is denied.</exception>
    /// <exception cref="IOException">Raised when the directory cannot be opened because of I/O errors.</exception>
    /// <exception cref="InvalidOperationException">Raised when metrics are on in options but IMetrics was not supplied.</exception>
    public FileSystemWatcher(string path, FileSystemWatcherOptions? options = null, ILogger<FileSystemWatcher>? logger = null, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
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
            _previousSnapshot = Utilities.TakeSnapshot(Path, _options);
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

    /// <summary>Stops watching and releases resources held by the FileSystemWatcher.</summary>
    /// <remarks>After Dispose, the watcher cannot be used again.</remarks>
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
                // Debounce: rapid changes are batched on purpose so events do not storm.
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
            currentSnapshot = Utilities.TakeSnapshot(Path, _options, _ctSource.Token, _previousSnapshot);
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

            if (!_ctSource.Token.IsCancellationRequested)
                CheckDirectoryChanges(_previousSnapshot, currentSnapshot, changes);

            IReadOnlyList<FileSystemChangeInfo> batch = changes;
            foreach (var change in batch) {
                if (_ctSource.Token.IsCancellationRequested)
                    break;

                FireEvent(change);
            }

            var previous = _previousSnapshot;
            _previousSnapshot = currentSnapshot;
            if (!_ctSource.Token.IsCancellationRequested)
                OnScanCompleted(previous, currentSnapshot, batch);
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

    private void CheckDirectoryChanges(SnapshotTree oldSnapshot, SnapshotTree newSnapshot, List<FileSystemChangeInfo> existingChanges)
    {
        var stringComparer = _options.PathComparison == StringComparison.OrdinalIgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var affectedDirs = new HashSet<string>(stringComparer);
        var processedChanges = existingChanges.Where(c => c.OldPath != null || c.NewPath != null);

        // Gather parent directories that were touched.
        foreach (var change in processedChanges) {
            if (_ctSource.Token.IsCancellationRequested)
                return;

            if (change.OldPath != null)
                AddParentDir(change.OldPath, affectedDirs);

            if (change.NewPath != null)
                AddParentDir(change.NewPath, affectedDirs);
        }

        // See whether each touched directory's contents actually changed.
        foreach (var dirPath in affectedDirs.Where(Directory.Exists)) {
            if (_ctSource.Token.IsCancellationRequested)
                return;

            if (!Utilities.HasDirectoryChanged(dirPath, oldSnapshot, newSnapshot, _options.PathComparison))
                continue;

            var oldCounts = Utilities.GetSnapshotCounts(dirPath, oldSnapshot, _options.PathComparison);
            var newCounts = Utilities.GetDirectoryContentCounts(dirPath);
            var change = new FileSystemChangeInfo(dirPath, dirPath, ChangeTypeEnum.Changed, true, oldCounts.fileCount, oldCounts.dirCount, newCounts.fileCount, newCounts.dirCount);
            existingChanges.Add(change);
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

        // Raise the typed event and swallow handler failures so later subscribers still run.
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

    private void OnScanCompleted(SnapshotTree previous, SnapshotTree current, IReadOnlyList<FileSystemChangeInfo> changes)
    {
        try {
            ScanCompleted?.Invoke(this, new(previous, current, changes));
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error in ScanCompleted event handler for path {Path}", Path);
            _metrics.RecordError(Constants.Metrics.ErrorCount, ex, [(Constants.Metrics.Tags.Operation, "ScanCompleted")]);
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
            // Drop errors from the error handler so it cannot recurse.
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