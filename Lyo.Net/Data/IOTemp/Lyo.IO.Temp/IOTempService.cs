using Lyo.Exceptions;
using Lyo.IO.Temp.Enums;
using Lyo.IO.Temp.Models;
using Lyo.IO.Temp.Storage;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

#if !NET5_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace Lyo.IO.Temp;

/// <summary>Default <see cref="IIOTempService" /> implementation using <see cref="IIOTempStorageProvider" /> for all I/O.</summary>
// ReSharper disable once InconsistentNaming
public sealed class IOTempService : IIOTempService
{
    private static long _nameSequence;
    private readonly ConcurrentDictionary<string, IIOTempSession> _activeSessions = [];
    private readonly object _keyedSessionLock = new();
    private readonly ConcurrentDictionary<string, IIOTempSession> _keyedSessions = [];
    private readonly ILogger<IOTempService> _logger;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly IMetrics _metrics;

    private readonly IOTempServiceOptions _options;
    private readonly IIOTempStorageProvider _storage;
    private bool _disposed;

    /// <summary>Initializes a new service instance, creates the shared root if configured, and allocates a unique <see cref="ServiceDirectory" />.</summary>
    /// <param name="options">Service and default session behaviour; defaults apply when null.</param>
    /// <param name="logger">Optional logger for operations and failures.</param>
    /// <param name="metrics">Recorded when <see cref="IOTempServiceOptions.EnableMetrics" /> is true and this reference is non-null.</param>
    /// <param name="loggerFactory">Optional factory used to create per-session loggers.</param>
    /// <param name="storageProvider">Storage backend; defaults to <see cref="FileSystemIOTempStorageProvider" /> rooted at <see cref="IOTempServiceOptions.RootDirectory" />.</param>
    public IOTempService(
        IOTempServiceOptions? options = null,
        ILogger<IOTempService>? logger = null,
        IMetrics? metrics = null,
        ILoggerFactory? loggerFactory = null,
        IIOTempStorageProvider? storageProvider = null)
    {
        _options = options ?? new IOTempServiceOptions();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_options.TempRoot);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_options.DirectoryName);
        _storage = storageProvider ?? new FileSystemIOTempStorageProvider(_options.RootDirectory);
        _logger = logger ?? NullLogger<IOTempService>.Instance;
        _loggerFactory = loggerFactory;
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        EnsureRootExists();
        ServiceDirectory = CreateServiceDirectory();
    }

    /// <inheritdoc />
    public string ServiceDirectory { get; }

    /// <inheritdoc />
    public int ActiveSessionCount => _activeSessions.Count;

#region Sessions

    /// <inheritdoc />
    public IIOTempSession CreateSession(IOTempSessionOptions? options = null)
    {
        ThrowIfDisposed();
        var stopwatch = Stopwatch.StartNew();
        try {
            var sessionOptions = BuildSessionOptions(options);
            var sessionLogger = _loggerFactory?.CreateLogger<IOTempSession>();
            var session = new IOTempSession(sessionOptions, sessionLogger, _metrics, OnSessionDisposed, _storage);
            _activeSessions.TryAdd(session.SessionDirectory, session);
            _logger.LogDebug("Created IO temp session at {SessionDirectory}", session.SessionDirectory);
            _metrics.RecordTiming(Constants.Metrics.CreateSessionDuration, stopwatch.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.CreateSessionSuccess);
            _metrics.RecordGauge(Constants.Metrics.ActiveSessionCount, _activeSessions.Count);
            return session;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to create IO temp session");
            _metrics.RecordError(Constants.Metrics.CreateSessionDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.CreateSessionFailure);
            throw;
        }
    }

    /// <inheritdoc />
    public IIOTempSession GetOrCreateSession(string key, IOTempSessionOptions? options = null)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        if (_keyedSessions.TryGetValue(key, out var existing))
            return existing;

        lock (_keyedSessionLock) {
            if (_keyedSessions.TryGetValue(key, out existing))
                return existing;

            var session = CreateSession(options);
            _keyedSessions[key] = session;
            return session;
        }
    }

    /// <inheritdoc />
    public void ReleaseSession(string key)
    {
        ThrowIfDisposed();
        if (_keyedSessions.TryRemove(key, out var session))
            session.Dispose();
    }

    /// <inheritdoc />
    public IOTempServiceStats GetStats()
    {
        ThrowIfDisposed();
        var totalBytes = _activeSessions.Values.Sum(s => s.GetTotalBytesUsed());
        return new(_activeSessions.Count, _keyedSessions.Count, totalBytes, ServiceDirectory);
    }

#endregion

#region One-offs

    /// <inheritdoc />
    public string CreateFile(string? name = null)
    {
        ThrowIfDisposed();
        var path = ResolveServicePath(name, false);
        _storage.TouchFile(path);
        _logger.LogDebug("Created IO temp one-off file at {FilePath}", path);
        return path;
    }

    /// <inheritdoc />
    public string CreateFile(ReadOnlyMemory<byte> data, string? name = null)
    {
        ThrowIfDisposed();
        var path = ResolveServicePath(name, false);
        _storage.WriteAllBytes(path, data.ToArray());
        _logger.LogDebug("Created IO temp one-off file at {FilePath}", path);
        return path;
    }

    /// <inheritdoc />
    public string CreateFile(Stream data, string? name = null)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNull(data);
        OperationHelpers.ThrowIfNotReadable(data, "Input stream must be readable to create temp file.");
        var path = ResolveServicePath(name, false);
        using var dest = _storage.OpenCreate(path);
        data.CopyTo(dest);
        _logger.LogDebug("Created IO temp one-off file at {FilePath}", path);
        return path;
    }

    /// <inheritdoc />
    public string CreateDirectory(string? name = null)
    {
        ThrowIfDisposed();
        var path = ResolveServicePath(name, true);
        _storage.CreateDirectory(path);
        _logger.LogDebug("Created IO temp one-off directory at {DirectoryPath}", path);
        return path;
    }

#endregion

#region Cleanup

    /// <inheritdoc />
    public void Cleanup() => Cleanup(_options.FileLifetime ?? TimeSpan.Zero);

    /// <inheritdoc />
    public Task CleanupAsync(CancellationToken ct = default) => CleanupAsync(_options.FileLifetime ?? TimeSpan.Zero, ct);

    /// <inheritdoc />
    public Task CleanupAsync(TimeSpan olderThan, CancellationToken ct = default) => Task.Run(() => Cleanup(olderThan, ct), ct);

    private void Cleanup(TimeSpan olderThan, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        var stopwatch = Stopwatch.StartNew();
        var deletedDirectories = 0;
        var deletedFiles = 0;
        if (!_storage.DirectoryExists(ServiceDirectory)) {
            _logger.LogDebug("Service directory {ServiceDirectory} does not exist, skipping cleanup", ServiceDirectory);
            return;
        }

        var cutoff = DateTimeOffset.UtcNow - olderThan;
        foreach (var entry in _storage.EnumerateEntries(ServiceDirectory)) {
            ct.ThrowIfCancellationRequested();
            if (entry.CreationTimeUtc > cutoff)
                continue;

            if (entry.IsDirectory) {
                if (_activeSessions.ContainsKey(entry.FullPath)) {
                    _logger.LogDebug("Skipping cleanup for active session directory {SessionDirectory}", entry.FullPath);
                    continue;
                }

                try {
                    _storage.DeleteDirectory(entry.FullPath);
                }
                catch (Exception ex) {
                    _logger.LogWarning(ex, "Failed deleting temp directory {DirectoryPath} during cleanup", entry.FullPath);
                }
                finally {
                    if (!_storage.DirectoryExists(entry.FullPath))
                        deletedDirectories++;
                }
            }
            else {
                try {
                    _storage.DeleteFile(entry.FullPath);
                }
                catch (Exception ex) {
                    _logger.LogWarning(ex, "Failed deleting temp file {FilePath} during cleanup", entry.FullPath);
                }
                finally {
                    if (!_storage.FileExists(entry.FullPath))
                        deletedFiles++;
                }
            }
        }

        _logger.LogInformation(
            "IO temp cleanup completed. Deleted {DirectoryCount} directories and {FileCount} files from {ServiceDirectory}", deletedDirectories, deletedFiles, ServiceDirectory);

        _metrics.RecordTiming(Constants.Metrics.CleanupDuration, stopwatch.Elapsed);
        _metrics.IncrementCounter(Constants.Metrics.CleanupSuccess);
        _metrics.RecordGauge(Constants.Metrics.ActiveSessionCount, _activeSessions.Count);
    }

#endregion

#region Helpers

    private void EnsureRootExists()
    {
        try {
            if (!_storage.DirectoryExists(_options.RootDirectory)) {
                if (_options.CreateRootDirectoryIfNotExists) {
                    _storage.CreateDirectory(_options.RootDirectory);
                    _logger.LogInformation("Created IO temp root directory at {RootDirectory}", _options.RootDirectory);
                }
                else
                    ExceptionThrower.ThrowIfDirectoryNotFound(_options.RootDirectory);
            }

            _storage.EnsureDirectoryAccessible(_options.RootDirectory);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "IO temp root directory {RootDirectory} is not accessible for read/write operations", _options.RootDirectory);
            throw;
        }
    }

    private string CreateServiceDirectory()
    {
        var stopwatch = Stopwatch.StartNew();
        var serviceDirName = $"service-{Guid.NewGuid():N}";
        var serviceDirectory = Path.Combine(_storage.RootPath, serviceDirName);
        try {
            _storage.CreateDirectory(serviceDirectory);
            OperationHelpers.ThrowIf(!_storage.DirectoryExists(serviceDirectory), $"Failed to create IO temp service directory: {serviceDirectory}");
            _storage.EnsureDirectoryAccessible(serviceDirectory);
            _logger.LogInformation("Created IO temp service directory at {ServiceDirectory}", serviceDirectory);
            _metrics.RecordTiming(Constants.Metrics.CreateServiceDirectoryDuration, stopwatch.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.CreateServiceDirectorySuccess);
            return serviceDirectory;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to create IO temp service directory at {ServiceDirectory}", serviceDirectory);
            _metrics.RecordError(Constants.Metrics.CreateServiceDirectoryDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.CreateServiceDirectoryFailure);
            throw;
        }
    }

    private IOTempSessionOptions BuildSessionOptions(IOTempSessionOptions? overrides)
    {
        // Session directories are always nested under the service directory.
        if (overrides == null) {
            return new() {
                RootDirectory = ServiceDirectory,
                EnableMetrics = _options.EnableMetrics,
                FilePrefix = _options.FilePrefix,
                FileSuffix = _options.FileSuffix,
                FileExtension = _options.FileExtension,
                FileNamingStrategy = _options.FileNamingStrategy,
                DirectoryPrefix = _options.DirectoryPrefix,
                DirectorySuffix = _options.DirectorySuffix,
                DirectoryNamingStrategy = _options.DirectoryNamingStrategy,
                MaxFileSizeBytes = _options.MaxFileSizeBytes,
                MaxTotalSizeBytes = _options.MaxTotalSizeBytes,
                MaxFileCount = _options.MaxFileCount,
                FileLifetime = _options.FileLifetime,
                OverflowStrategy = _options.OverflowStrategy
            };
        }

        return new() {
            RootDirectory = ServiceDirectory,
            EnableMetrics = overrides.EnableMetrics,
            FilePrefix = overrides.FilePrefix ?? _options.FilePrefix,
            FileSuffix = overrides.FileSuffix ?? _options.FileSuffix,
            FileExtension = string.IsNullOrWhiteSpace(overrides.FileExtension) ? _options.FileExtension : overrides.FileExtension,
            FileNamingStrategy = overrides.FileNamingStrategy,
            DirectoryPrefix = overrides.DirectoryPrefix ?? _options.DirectoryPrefix,
            DirectorySuffix = overrides.DirectorySuffix ?? _options.DirectorySuffix,
            DirectoryNamingStrategy = overrides.DirectoryNamingStrategy,
            MaxFileSizeBytes = overrides.MaxFileSizeBytes ?? _options.MaxFileSizeBytes,
            MaxTotalSizeBytes = overrides.MaxTotalSizeBytes ?? _options.MaxTotalSizeBytes,
            MaxFileCount = overrides.MaxFileCount ?? _options.MaxFileCount,
            FileLifetime = overrides.FileLifetime ?? _options.FileLifetime,
            OverflowStrategy = overrides.OverflowStrategy
        };
    }

    private void OnSessionDisposed(string sessionDirectory)
    {
        if (!_activeSessions.TryRemove(sessionDirectory, out var _))
            return;

        // Remove from keyed pool if this session was registered there
        foreach (var kvp in _keyedSessions) {
            if (kvp.Value.SessionDirectory == sessionDirectory) {
                _keyedSessions.TryRemove(kvp.Key, out var _);
                break;
            }
        }

        _logger.LogDebug("Disposed IO temp session at {SessionDirectory}", sessionDirectory);
        _metrics.RecordGauge(Constants.Metrics.ActiveSessionCount, _activeSessions.Count);
    }

    private string ResolveServicePath(string? name, bool isDirectory)
    {
        string path;
        if (!string.IsNullOrWhiteSpace(name)) {
            var combined = Path.Combine(ServiceDirectory, name);
            path = EnsurePathWithinDirectory(ServiceDirectory, combined, nameof(name));
        }
        else {
            var generated = isDirectory
                ? GenerateName(_options.DirectoryPrefix, _options.DirectorySuffix, _options.DirectoryNamingStrategy)
                : GenerateName(_options.FilePrefix, _options.FileSuffix, _options.FileNamingStrategy) + _options.FileExtension;

            path = Path.Combine(ServiceDirectory, generated);
        }

        var parentDir = Path.GetDirectoryName(path);
        OperationHelpers.ThrowIfNullOrWhiteSpace(parentDir, "Could not determine parent directory for service temp path.");
        _storage.CreateDirectory(parentDir);
        return path;
    }

    private static string EnsurePathWithinDirectory(string baseDirectory, string candidatePath, string paramName)
    {
        var fullBase = Path.GetFullPath(baseDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullCandidate = Path.GetFullPath(candidatePath);
        var comparison = GetPathComparison();
        OperationHelpers.ThrowIf(!fullCandidate.StartsWith(fullBase, comparison), $"Path escapes the service directory: {candidatePath}");
        return fullCandidate;
    }

    private static string GenerateName(string? prefix, string? suffix, TempNamingStrategy strategy)
    {
        var middle = strategy switch {
            TempNamingStrategy.Guid => Guid.NewGuid().ToString("N"),
            TempNamingStrategy.Timestamp => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
            TempNamingStrategy.Sequential => Interlocked.Increment(ref _nameSequence).ToString(),
            TempNamingStrategy.RandomChars => Path.GetRandomFileName().Replace(".", string.Empty),
            var _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null)
        };

        return $"{prefix}{middle}{suffix}";
    }

    private static StringComparison GetPathComparison()
    {
#if NET5_0_OR_GREATER
        return OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
#else
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
#endif
    }

    private void ThrowIfDisposed() => OperationHelpers.ThrowIfDisposed(_disposed, nameof(IOTempService));

    /// <summary>
    /// Marks the service disposed, clears the active-session table, and deletes <see cref="ServiceDirectory" /> with retries on transient I/O errors. Exceptions during delete are
    /// logged; keyed session entries may still reference disposed sessions until removed.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        var stopwatch = Stopwatch.StartNew();
        try {
            DeleteDirectoryWithRetry(ServiceDirectory, _logger);
            _activeSessions.Clear();
            _logger.LogInformation("Disposed IO temp service directory {ServiceDirectory}", ServiceDirectory);
            _metrics.RecordTiming(Constants.Metrics.DisposeServiceDirectoryDuration, stopwatch.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.DisposeServiceDirectorySuccess);
            _metrics.RecordGauge(Constants.Metrics.ActiveSessionCount, 0);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to dispose IO temp service directory {ServiceDirectory} after retries", ServiceDirectory);
            _metrics.RecordError(Constants.Metrics.DisposeServiceDirectoryDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.DisposeServiceDirectoryFailure);
        }
    }

    private void DeleteDirectoryWithRetry(string path, ILogger logger, int retries = 3, int retryDelayMs = 150)
    {
        Exception? lastEx = null;
        for (var attempt = 1; attempt <= retries; attempt++) {
            try {
                _storage.DeleteDirectory(path);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
                lastEx = ex;
                if (attempt < retries) {
                    logger.LogDebug("Delete attempt {Attempt}/{Retries} failed for {Path}, retrying in {Delay}ms", attempt, retries, path, retryDelayMs);
                    Thread.Sleep(retryDelayMs);
                }
            }
        }

        if (lastEx != null)
            ExceptionDispatchInfo.Capture(lastEx).Throw();
    }

#endregion
}