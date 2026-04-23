using System.Collections.Concurrent;
using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.IO.Temp.Enums;
using Lyo.IO.Temp.Models;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
#if !NET5_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace Lyo.IO.Temp;

// ReSharper disable once InconsistentNaming
public sealed class IOTempService : IIOTempService
{
    private static long _nameSequence;
    private readonly ConcurrentDictionary<string, IIOTempSession> _activeSessions = [];
    private readonly ConcurrentDictionary<string, IIOTempSession> _keyedSessions = [];
    private readonly object _keyedSessionLock = new();
    private readonly ILogger<IOTempService> _logger;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly IMetrics _metrics;

    private readonly IOTempServiceOptions _options;
    private bool _disposed;

    public string ServiceDirectory { get; }

    public IOTempService(IOTempServiceOptions? options = null, ILogger<IOTempService>? logger = null, IMetrics? metrics = null, ILoggerFactory? loggerFactory = null)
    {
        _options = options ?? new IOTempServiceOptions();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_options.TempRoot, nameof(_options.TempRoot));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_options.DirectoryName, nameof(_options.DirectoryName));
        _logger = logger ?? NullLogger<IOTempService>.Instance;
        _loggerFactory = loggerFactory;
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        EnsureRootExists();
        ServiceDirectory = CreateServiceDirectory();
    }

    public int ActiveSessionCount => _activeSessions.Count;

#region Sessions

    public IIOTempSession CreateSession(IOTempSessionOptions? options = null)
    {
        ThrowIfDisposed();
        var stopwatch = Stopwatch.StartNew();
        try {
            var sessionOptions = BuildSessionOptions(options);
            var sessionLogger = _loggerFactory?.CreateLogger<IOTempSession>();
            var session = new IOTempSession(sessionOptions, sessionLogger, _metrics, OnSessionDisposed);
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

    public IIOTempSession GetOrCreateSession(string key, IOTempSessionOptions? options = null)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));

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

    public void ReleaseSession(string key)
    {
        ThrowIfDisposed();
        if (_keyedSessions.TryRemove(key, out var session))
            session.Dispose();
    }

    public IOTempServiceStats GetStats()
    {
        ThrowIfDisposed();
        var totalBytes = _activeSessions.Values.Sum(s => s.GetTotalBytesUsed());
        return new IOTempServiceStats(
            ActiveSessionCount: _activeSessions.Count,
            KeyedSessionCount: _keyedSessions.Count,
            TotalBytesUsed: totalBytes,
            ServiceDirectory: ServiceDirectory
        );
    }

#endregion

#region One-offs

    public string CreateFile(string? name = null)
    {
        ThrowIfDisposed();
        var path = ResolveServicePath(name, false);
        using (File.Create(path)) { }

        _logger.LogDebug("Created IO temp one-off file at {FilePath}", path);
        return path;
    }

    public string CreateFile(ReadOnlyMemory<byte> data, string? name = null)
    {
        ThrowIfDisposed();
        var path = ResolveServicePath(name, false);
        File.WriteAllBytes(path, data.ToArray());
        _logger.LogDebug("Created IO temp one-off file at {FilePath}", path);
        return path;
    }

    public string CreateFile(Stream data, string? name = null)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        OperationHelpers.ThrowIfNotReadable(data, "Input stream must be readable to create temp file.");
        var path = ResolveServicePath(name, false);
        using var fs = File.Create(path);
        data.CopyTo(fs);
        _logger.LogDebug("Created IO temp one-off file at {FilePath}", path);
        return path;
    }

    public string CreateDirectory(string? name = null)
    {
        ThrowIfDisposed();
        var path = ResolveServicePath(name, true);
        Directory.CreateDirectory(path);
        _logger.LogDebug("Created IO temp one-off directory at {DirectoryPath}", path);
        return path;
    }

#endregion

#region Cleanup

    public void Cleanup() => Cleanup(_options.FileLifetime ?? TimeSpan.Zero);

    public Task CleanupAsync(CancellationToken ct = default) => CleanupAsync(_options.FileLifetime ?? TimeSpan.Zero, ct);

    public Task CleanupAsync(TimeSpan olderThan, CancellationToken ct = default) => Task.Run(() => Cleanup(olderThan, ct), ct);

    private void Cleanup(TimeSpan olderThan, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        var stopwatch = Stopwatch.StartNew();
        var deletedDirectories = 0;
        var deletedFiles = 0;
        var root = new DirectoryInfo(ServiceDirectory);
        if (!root.Exists) {
            _logger.LogDebug("Service directory {ServiceDirectory} does not exist, skipping cleanup", ServiceDirectory);
            return;
        }

        var cutoff = DateTimeOffset.UtcNow - olderThan;
        foreach (var dir in root.EnumerateDirectories()) {
            ct.ThrowIfCancellationRequested();
            if (dir.CreationTimeUtc > cutoff)
                continue;

            if (_activeSessions.ContainsKey(dir.FullName)) {
                _logger.LogDebug("Skipping cleanup for active session directory {SessionDirectory}", dir.FullName);
                continue;
            }

            try {
                dir.Delete(true);
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Failed deleting temp directory {DirectoryPath} during cleanup", dir.FullName);
            }
            finally {
                if (!dir.Exists)
                    deletedDirectories++;
            }
        }

        foreach (var file in root.EnumerateFiles()) {
            ct.ThrowIfCancellationRequested();
            if (file.CreationTimeUtc > cutoff)
                continue;

            try {
                file.Delete();
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Failed deleting temp file {FilePath} during cleanup", file.FullName);
            }
            finally {
                if (!file.Exists)
                    deletedFiles++;
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
            if (!Directory.Exists(_options.RootDirectory)) {
                if (_options.CreateRootDirectoryIfNotExists) {
                    Directory.CreateDirectory(_options.RootDirectory);
                    _logger.LogInformation("Created IO temp root directory at {RootDirectory}", _options.RootDirectory);
                }
                else
                    ExceptionThrower.ThrowIfDirectoryNotFound(_options.RootDirectory, nameof(_options.RootDirectory));
            }

            ValidateDirectoryReadableAndWritable(_options.RootDirectory, "RootDirectory");
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
        var serviceDirectory = Path.Combine(_options.RootDirectory, serviceDirName);
        try {
            Directory.CreateDirectory(serviceDirectory);
            OperationHelpers.ThrowIf(!Directory.Exists(serviceDirectory), $"Failed to create IO temp service directory: {serviceDirectory}");
            ValidateDirectoryReadableAndWritable(serviceDirectory, "ServiceDirectory");
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
        if (!_activeSessions.TryRemove(sessionDirectory, out _))
            return;

        // Remove from keyed pool if this session was registered there
        foreach (var kvp in _keyedSessions) {
            if (kvp.Value.SessionDirectory == sessionDirectory) {
                _keyedSessions.TryRemove(kvp.Key, out _);
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
        Directory.CreateDirectory(parentDir!);
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

    private static void ValidateDirectoryReadableAndWritable(string directoryPath, string paramName)
    {
        ExceptionThrower.ThrowIfDirectoryNotAccessible(directoryPath, paramName);
        var probePath = Path.Combine(directoryPath, $".rw-check-{Guid.NewGuid():N}.tmp");
        try {
            File.WriteAllText(probePath, "rw");
            ExceptionThrower.ThrowIfFileNotAccessible(probePath, nameof(probePath));
        }
        finally {
            if (File.Exists(probePath))
                File.Delete(probePath);
        }
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

    private static void DeleteDirectoryWithRetry(string path, ILogger logger, int retries = 3, int retryDelayMs = 150)
    {
        Exception? lastEx = null;
        for (var attempt = 1; attempt <= retries; attempt++) {
            try {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
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
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(lastEx).Throw();
    }

#endregion
}