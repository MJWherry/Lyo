using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Lyo.Common.Core.Extensions;
using Lyo.Common.Core.Pathing;
using Lyo.Exceptions;
using Lyo.IO.Temp.Enums;
using Lyo.IO.Temp.Models;
using Lyo.IO.FileSystem;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.IO.Temp;

/// <summary>Stock <see cref="IIOTempService" /> using <see cref="IFileSystem" /> for every I/O call.</summary>
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
    private readonly IFileSystem _storage;
    private bool _disposed;

    /// <summary>Builds a service instance, creates the shared root when configured, and assigns a unique <see cref="ServiceDirectory" />.</summary>
    /// <param name="options">Service and default session settings; defaults when null.</param>
    /// <param name="logger">Optional logger for work and failures.</param>
    /// <param name="metrics">Recorded when <see cref="IOTempServiceOptions.EnableMetrics" /> is true and this is non-null.</param>
    /// <param name="loggerFactory">Optional factory for per-session loggers.</param>
    /// <param name="storageProvider">Storage backend; default is <see cref="LocalFileSystem" /> at <see cref="IOTempServiceOptions.RootDirectory" />.</param>
    public IOTempService(
        IOTempServiceOptions? options = null,
        ILogger<IOTempService>? logger = null,
        IMetrics? metrics = null,
        ILoggerFactory? loggerFactory = null,
        IFileSystem? storageProvider = null)
    {
        _options = options ?? new IOTempServiceOptions();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_options.TempRoot);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_options.DirectoryName);
        _storage = storageProvider ?? CreateDefaultStorage(_options);
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

    /// <inheritdoc />
    public event Action<string>? SessionCreated;

    /// <inheritdoc />
    public event Action<string>? SessionDisposed;

    /// <inheritdoc />
    public event Action<string>? FileCreated;

    /// <inheritdoc />
    public event Action<string>? DirectoryCreated;

    /// <inheritdoc />
    public event Action<string>? FileDeleted;

    /// <inheritdoc />
    public event Action<string>? DirectoryDeleted;

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
            SessionCreated?.Invoke(session.SessionDirectory);
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
        FileCreated?.Invoke(path);
        return path;
    }

    /// <inheritdoc />
    public string CreateFile(ReadOnlyMemory<byte> data, string? name = null)
    {
        ThrowIfDisposed();
        var path = ResolveServicePath(name, false);
        _storage.WriteAllBytes(path, data.ToArray());
        _logger.LogDebug("Created IO temp one-off file at {FilePath}", path);
        FileCreated?.Invoke(path);
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
        FileCreated?.Invoke(path);
        return path;
    }

    /// <inheritdoc />
    public string CreateDirectory(string? name = null)
    {
        ThrowIfDisposed();
        var path = ResolveServicePath(name, true);
        _storage.CreateDirectory(path);
        _logger.LogDebug("Created IO temp one-off directory at {DirectoryPath}", path);
        DirectoryCreated?.Invoke(path);
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
        foreach (var entry in _storage.ListDirectory(ServiceDirectory)) {
            ct.ThrowIfCancellationRequested();
            if (entry.CreatedAtUtc > cutoff)
                continue;

            if (entry.IsDirectory) {
                if (_activeSessions.ContainsKey(entry.Path)) {
                    _logger.LogDebug("Skipping cleanup for active session directory {SessionDirectory}", entry.Path);
                    continue;
                }

                try {
                    _storage.DeleteDirectory(entry.Path);
                }
                catch (Exception ex) {
                    _logger.LogWarning(ex, "Failed deleting temp directory {DirectoryPath} during cleanup", entry.Path);
                }
                finally {
                    if (!_storage.DirectoryExists(entry.Path)) {
                        deletedDirectories++;
                        DirectoryDeleted?.Invoke(entry.Path);
                    }
                }
            }
            else {
                try {
                    _storage.DeleteFile(entry.Path);
                }
                catch (Exception ex) {
                    _logger.LogWarning(ex, "Failed deleting temp file {FilePath} during cleanup", entry.Path);
                }
                finally {
                    if (!_storage.FileExists(entry.Path)) {
                        deletedFiles++;
                        FileDeleted?.Invoke(entry.Path);
                    }
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
            var root = _storage.RootPath;
            if (!_storage.DirectoryExists(root)) {
                if (_options.CreateRootDirectoryIfNotExists) {
                    _storage.CreateDirectory(root);
                    _logger.LogInformation("Created IO temp root directory at {RootDirectory}", root);
                }
                else
                    ExceptionThrower.ThrowIfDirectoryNotFound(root);
            }

            _storage.EnsureDirectoryAccessible(root);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "IO temp root directory {RootDirectory} is not accessible for read/write operations", _storage.RootPath);
            throw;
        }
    }

    private string CreateServiceDirectory()
    {
        var stopwatch = Stopwatch.StartNew();
        var serviceDirName = $"service-{Guid.NewGuid():N}";
        var serviceDirectory = PathHelpers.Combine(_storage.PathStyle, _storage.RootPath, serviceDirName);
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
        // Session folders always live under the service folder.
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

        // Drop this session from the keyed pool if it was registered there.
        foreach (var kvp in _keyedSessions) {
            if (kvp.Value.SessionDirectory == sessionDirectory) {
                _keyedSessions.TryRemove(kvp.Key, out var _);
                break;
            }
        }

        _logger.LogDebug("Disposed IO temp session at {SessionDirectory}", sessionDirectory);
        _metrics.RecordGauge(Constants.Metrics.ActiveSessionCount, _activeSessions.Count);
        SessionDisposed?.Invoke(sessionDirectory);
    }

    private string ResolveServicePath(string? name, bool isDirectory)
    {
        string path;
        if (!name.IsNullOrWhitespace()) {
            var combined = PathHelpers.Combine(_storage.PathStyle, ServiceDirectory, name);
            path = EnsurePathWithinDirectory(ServiceDirectory, combined, nameof(name));
        }
        else {
            var generated = isDirectory
                ? GenerateName(_options.DirectoryPrefix, _options.DirectorySuffix, _options.DirectoryNamingStrategy)
                : GenerateName(_options.FilePrefix, _options.FileSuffix, _options.FileNamingStrategy) + _options.FileExtension;

            path = PathHelpers.Combine(_storage.PathStyle, ServiceDirectory, generated);
        }

        var parentDir = PathHelpers.GetDirectoryName(_storage.PathStyle, path);
        OperationHelpers.ThrowIfNullOrWhiteSpace(parentDir, "Could not determine parent directory for service temp path.");
        _storage.CreateDirectory(parentDir);
        return path;
    }

    private string EnsurePathWithinDirectory(string baseDirectory, string candidatePath, string paramName)
    {
        PathHelpers.ThrowIfEscapesRoot(_storage.PathStyle, baseDirectory, candidatePath, paramName);
        return PathHelpers.GetFullPath(_storage.PathStyle, candidatePath);
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

    private void ThrowIfDisposed() => OperationHelpers.ThrowIfDisposed(_disposed, nameof(IOTempService));

    /// <summary>
    /// Marks the service disposed, empties the active-session table, and deletes <see cref="ServiceDirectory" /> with retries on short-lived I/O errors. Delete failures are
    /// logged; keyed entries may still point at disposed sessions until they are removed.
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

    internal static IFileSystem CreateDefaultStorage(IOTempServiceOptions options)
        => options.StorageKind == IOTempStorageKind.Memory ? new MemoryFileSystem() : new LocalFileSystem(options.RootDirectory);

#endregion
}