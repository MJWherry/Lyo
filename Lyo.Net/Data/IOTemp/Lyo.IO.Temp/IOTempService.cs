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
    private readonly ConcurrentDictionary<string, byte> _activeSessions = [];
    private readonly ILogger<IOTempService> _logger;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly IMetrics _metrics;

    private readonly IOTempServiceOptions _options;
    private bool _disposed;

    public string ServiceDirectory { get; }

    public IOTempService(IOTempServiceOptions? options = null, ILogger<IOTempService>? logger = null, IMetrics? metrics = null, ILoggerFactory? loggerFactory = null)
    {
        _options = options ?? new IOTempServiceOptions();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_options.RootDirectory, nameof(_options.RootDirectory));
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
            _activeSessions.TryAdd(session.SessionDirectory, 0);
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

    public void Cleanup() => Cleanup(TimeSpan.Zero);

    public Task CleanupAsync(CancellationToken ct = default) => CleanupAsync(TimeSpan.Zero, ct);

    public Task CleanupAsync(TimeSpan olderThan, CancellationToken ct = default) => Task.Run(() => Cleanup(olderThan), ct);

    private void Cleanup(TimeSpan olderThan)
    {
        ThrowIfDisposed();
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
            "IO temp cleanup completed. Deleted {DirectoryCount} directories and {FileCount} files from {RootDirectory}", deletedDirectories, deletedFiles, _options.RootDirectory);

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
            OverflowStrategy = overrides.OverflowStrategy
        };
    }

    private void OnSessionDisposed(string sessionDirectory)
    {
        if (!_activeSessions.TryRemove(sessionDirectory, out var _))
            return;

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

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(IOTempService));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        var stopwatch = Stopwatch.StartNew();
        try {
            if (Directory.Exists(ServiceDirectory))
                Directory.Delete(ServiceDirectory, true);

            _activeSessions.Clear();
            _logger.LogInformation("Disposed IO temp service directory {ServiceDirectory}", ServiceDirectory);
            _metrics.RecordTiming(Constants.Metrics.DisposeServiceDirectoryDuration, stopwatch.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.DisposeServiceDirectorySuccess);
            _metrics.RecordGauge(Constants.Metrics.ActiveSessionCount, 0);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to dispose IO temp service directory {ServiceDirectory}", ServiceDirectory);
            _metrics.RecordError(Constants.Metrics.DisposeServiceDirectoryDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.DisposeServiceDirectoryFailure);
        }
    }

#endregion
}