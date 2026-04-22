using System.Diagnostics;
using System.Text;
using Lyo.Exceptions;
using Lyo.IO.Temp.Enums;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
#if !NET5_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace Lyo.IO.Temp.Models;

// ReSharper disable once InconsistentNaming
public sealed class IOTempSession : IIOTempSession
{
    private const int DisposeRetryCount = 3;
    private const int DisposeRetryDelayMs = 150;

    private static long _nameSequence;
    private readonly List<string> _directories = [];
    private readonly List<string> _files = [];
    private readonly ILogger<IOTempSession> _logger;
    private readonly IMetrics _metrics;
    private readonly Action<string>? _onDispose;
    private readonly IOTempSessionOptions _options;
    private bool _disposed;
    private long _totalBytesUsed;

    public IOTempSession(IOTempSessionOptions? options = null, ILogger<IOTempSession>? logger = null, IMetrics? metrics = null, Action<string>? onDispose = null)
    {
        _options = options ?? new IOTempSessionOptions();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_options.RootDirectory, nameof(_options.RootDirectory));
        _logger = logger ?? NullLogger<IOTempSession>.Instance;
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _onDispose = onDispose;
        EnsureRootExistsAndAccessible();
        SessionDirectory = CreateSessionDirectory();
        _metrics.IncrementCounter(Constants.Metrics.SessionCreated);
    }

    public string SessionDirectory { get; }

    public IReadOnlyList<string> Files => _files;

    public IReadOnlyList<string> Directories => _directories;

#region Files

    public string GetFilePath(string? name = null)
    {
        ThrowIfDisposed();
        return ResolvePath(name, false);
    }

    public string TouchFile(string? name = null)
    {
        ThrowIfDisposed();
        var stopwatch = Stopwatch.StartNew();
        try {
            var path = ResolvePath(name, false);
            File.Create(path).Dispose();
            _files.Add(path);
            _metrics.RecordTiming(Constants.Metrics.CreateFileDuration, stopwatch.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.CreateFileSuccess);
            return path;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.CreateFileDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.CreateFileFailure);
            _logger.LogError(ex, "Failed creating temp file in session {SessionDirectory}", SessionDirectory);
            throw;
        }
    }

    public string CreateFile(string text)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNull(text, nameof(text));
        var stopwatch = Stopwatch.StartNew();
        try {
            var sizeBytes = Encoding.UTF8.GetByteCount(text);
            ValidateFileSize(sizeBytes);
            var path = ResolvePath(null, false);
            File.WriteAllText(path, text);
            _files.Add(path);
            Interlocked.Add(ref _totalBytesUsed, sizeBytes);
            _metrics.RecordTiming(Constants.Metrics.CreateFileDuration, stopwatch.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.CreateFileSuccess);
            return path;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.CreateFileDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.CreateFileFailure);
            _logger.LogError(ex, "Failed creating temp file from text in session {SessionDirectory}", SessionDirectory);
            throw;
        }
    }

    public string CreateFile(ReadOnlyMemory<byte> data, string? name = null)
    {
        ThrowIfDisposed();
        var stopwatch = Stopwatch.StartNew();
        try {
            ValidateFileSize(data.Length);
            var path = ResolvePath(name, false);
            File.WriteAllBytes(path, data.ToArray());
            _files.Add(path);
            Interlocked.Add(ref _totalBytesUsed, data.Length);
            _metrics.RecordTiming(Constants.Metrics.CreateFileDuration, stopwatch.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.CreateFileSuccess);
            return path;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.CreateFileDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.CreateFileFailure);
            _logger.LogError(ex, "Failed creating temp file from bytes in session {SessionDirectory}", SessionDirectory);
            throw;
        }
    }

    public string CreateFile(Stream data, string? name = null)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        OperationHelpers.ThrowIfNotReadable(data, "Input stream must be readable to create temp file.");
        var stopwatch = Stopwatch.StartNew();
        try {
            ValidateFileSize(data.Length);
            var path = ResolvePath(name, false);
            using var fs = File.Create(path);
            data.CopyTo(fs);
            _files.Add(path);
            Interlocked.Add(ref _totalBytesUsed, data.Length);
            _metrics.RecordTiming(Constants.Metrics.CreateFileDuration, stopwatch.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.CreateFileSuccess);
            return path;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.CreateFileDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.CreateFileFailure);
            _logger.LogError(ex, "Failed creating temp file from stream in session {SessionDirectory}", SessionDirectory);
            throw;
        }
    }

    public async Task<string> CreateFileAsync(string text, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNull(text, nameof(text));
        var stopwatch = Stopwatch.StartNew();
        try {
            var sizeBytes = Encoding.UTF8.GetByteCount(text);
            ValidateFileSize(sizeBytes);
            var path = ResolvePath(null, false);
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            await File.WriteAllTextAsync(path, text, ct);
#else
            await Task.Run(
                    () => {
                        ct.ThrowIfCancellationRequested();
                        File.WriteAllText(path, text);
                    }, ct)
                .ConfigureAwait(false);
#endif
            _files.Add(path);
            Interlocked.Add(ref _totalBytesUsed, sizeBytes);
            _metrics.RecordTiming(Constants.Metrics.CreateFileDuration, stopwatch.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.CreateFileSuccess);
            return path;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.CreateFileDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.CreateFileFailure);
            _logger.LogError(ex, "Failed creating temp file async from text in session {SessionDirectory}", SessionDirectory);
            throw;
        }
    }

    public async Task<string> CreateFileAsync(ReadOnlyMemory<byte> data, string? name = null, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        var stopwatch = Stopwatch.StartNew();
        try {
            ValidateFileSize(data.Length);
            var path = ResolvePath(name, false);
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            await File.WriteAllBytesAsync(path, data.ToArray(), ct);
#else
            await Task.Run(
                    () => {
                        ct.ThrowIfCancellationRequested();
                        File.WriteAllBytes(path, data.ToArray());
                    }, ct)
                .ConfigureAwait(false);
#endif
            _files.Add(path);
            Interlocked.Add(ref _totalBytesUsed, data.Length);
            _metrics.RecordTiming(Constants.Metrics.CreateFileDuration, stopwatch.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.CreateFileSuccess);
            return path;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.CreateFileDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.CreateFileFailure);
            _logger.LogError(ex, "Failed creating temp file async from bytes in session {SessionDirectory}", SessionDirectory);
            throw;
        }
    }

    public async Task<string> CreateFileAsync(Stream data, string? name = null, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        OperationHelpers.ThrowIfNotReadable(data, "Input stream must be readable to create temp file.");
        var stopwatch = Stopwatch.StartNew();
        try {
            ValidateFileSize(data.Length);
            var path = ResolvePath(name, false);
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            await using var fs = File.Create(path);
            await data.CopyToAsync(fs, ct);
#else
            using var fs = File.Create(path);
            await data.CopyToAsync(fs, 81920, ct).ConfigureAwait(false);
#endif
            _files.Add(path);
            Interlocked.Add(ref _totalBytesUsed, data.Length);
            _metrics.RecordTiming(Constants.Metrics.CreateFileDuration, stopwatch.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.CreateFileSuccess);
            return path;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.CreateFileDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.CreateFileFailure);
            _logger.LogError(ex, "Failed creating temp file async from stream in session {SessionDirectory}", SessionDirectory);
            throw;
        }
    }

    public string GetDirectoryPath(string? name = null)
    {
        ThrowIfDisposed();
        return ResolvePath(name, true);
    }

#endregion

#region Directories

    public string CreateDirectory(string? name = null)
    {
        ThrowIfDisposed();
        var stopwatch = Stopwatch.StartNew();
        try {
            var path = ResolvePath(name, true);
            Directory.CreateDirectory(path);
            _directories.Add(path);
            _metrics.RecordTiming(Constants.Metrics.CreateDirectoryDuration, stopwatch.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.CreateDirectorySuccess);
            return path;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.CreateDirectoryDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.CreateDirectoryFailure);
            _logger.LogError(ex, "Failed creating temp directory in session {SessionDirectory}", SessionDirectory);
            throw;
        }
    }

    public Task<string> CreateDirectoryAsync(string? name = null, CancellationToken ct = default)
        =>
            // Directory creation is synchronous on all platforms, no true async needed
            Task.FromResult(CreateDirectory(name));

#endregion

#region Disposal

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        var stopwatch = Stopwatch.StartNew();
        try {
            DeleteDirectoryWithRetry(SessionDirectory, _logger, DisposeRetryCount, DisposeRetryDelayMs);
            _metrics.RecordTiming(Constants.Metrics.DisposeSessionDuration, stopwatch.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.DisposeSessionSuccess);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Couldn't delete session directory {SessionDirectory} after {Retries} attempts during Dispose. Directory may be orphaned.", SessionDirectory, DisposeRetryCount);
            _metrics.RecordError(Constants.Metrics.DisposeSessionDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.DisposeSessionFailure);
        }
        finally {
            _onDispose?.Invoke(SessionDirectory);
        }
    }

    public ValueTask DisposeAsync()
    {
        // Directory.Delete has no true async equivalent; calling Dispose() is the correct pattern.
        Dispose();
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        return ValueTask.CompletedTask;
#else
        return new ValueTask(Task.CompletedTask);
#endif
    }

#endregion

#region Helpers

    private string CreateSessionDirectory()
    {
        var name = GenerateName(_options.DirectoryPrefix, _options.DirectorySuffix, _options.DirectoryNamingStrategy);
        var path = Path.Combine(_options.RootDirectory, name);
        Directory.CreateDirectory(path);
        OperationHelpers.ThrowIf(!Directory.Exists(path), $"Failed to create IO temp session directory: {path}");
        ValidateDirectoryReadableAndWritable(path);
        return path;
    }

    private string ResolvePath(string? name, bool isDirectory)
    {
        if (name is not null) {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(name, nameof(name));
            var combined = Path.Combine(SessionDirectory, name);
            return EnsurePathWithinSession(combined);
        }

        var generated = isDirectory
            ? GenerateName(_options.DirectoryPrefix, _options.DirectorySuffix, _options.DirectoryNamingStrategy)
            : GenerateName(_options.FilePrefix, _options.FileSuffix, _options.FileNamingStrategy) + _options.FileExtension;

        return Path.Combine(SessionDirectory, generated);
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

    private string EnsurePathWithinSession(string candidatePath)
    {
        var fullBase = Path.GetFullPath(SessionDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullCandidate = Path.GetFullPath(candidatePath);
        var comparison = GetPathComparison();
        OperationHelpers.ThrowIf(!fullCandidate.StartsWith(fullBase, comparison), $"Path escapes the session directory: {candidatePath}");
        return fullCandidate;
    }

    private void ValidateFileSize(long sizeBytes)
    {
        // Per-file hard limit: no overflow strategy can free space for a single oversized file.
        if (_options.MaxFileSizeBytes.HasValue && sizeBytes > _options.MaxFileSizeBytes.Value)
            throw new InvalidOperationException(
                $"File size {sizeBytes:N0} bytes exceeds the per-file limit of {_options.MaxFileSizeBytes.Value:N0} bytes.");

        if (_options.MaxTotalSizeBytes == null)
            return;

        var projectedTotal = Interlocked.Read(ref _totalBytesUsed) + sizeBytes;
        if (projectedTotal <= _options.MaxTotalSizeBytes.Value)
            return;

        switch (_options.OverflowStrategy) {
            case TempOverflowStrategy.ThrowException:
                throw new InvalidOperationException(
                    $"Adding {sizeBytes:N0} bytes would exceed session total limit of {_options.MaxTotalSizeBytes.Value:N0} bytes (current: {Interlocked.Read(ref _totalBytesUsed):N0} bytes).");
            case TempOverflowStrategy.DeleteOldest:
            case TempOverflowStrategy.DeleteLargest:
                FreeTotalSpaceFor(sizeBytes);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_options.OverflowStrategy), _options.OverflowStrategy, null);
        }
    }

    private void FreeTotalSpaceFor(long requiredBytes)
    {
        var needed = Interlocked.Read(ref _totalBytesUsed) + requiredBytes - _options.MaxTotalSizeBytes!.Value;
        if (needed <= 0)
            return;

        var sessionDir = new DirectoryInfo(SessionDirectory);
        if (!sessionDir.Exists)
            return;

        var candidates = _options.OverflowStrategy == TempOverflowStrategy.DeleteOldest
            ? sessionDir.EnumerateFiles().OrderBy(f => f.CreationTimeUtc).ToList()
            : sessionDir.EnumerateFiles().OrderByDescending(f => f.Length).ToList();

        var freed = 0L;
        foreach (var file in candidates) {
            if (freed >= needed)
                break;

            var fileSize = file.Length;
            try {
                file.Delete();
                _files.Remove(file.FullName);
                Interlocked.Add(ref _totalBytesUsed, -fileSize);
                freed += fileSize;
                _logger.LogInformation("Deleted {FilePath} ({Size:N0} bytes) to make room for overflow in session {SessionDirectory}", file.FullName, fileSize, SessionDirectory);
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Failed to delete {FilePath} during overflow cleanup in session {SessionDirectory}", file.FullName, SessionDirectory);
            }
        }

        if (freed < needed)
            throw new InvalidOperationException(
                $"Could not free sufficient space: needed {needed:N0} bytes freed, freed {freed:N0} bytes. Session total limit: {_options.MaxTotalSizeBytes.Value:N0} bytes.");
    }

    private static void DeleteDirectoryWithRetry(string path, ILogger logger, int retries, int retryDelayMs)
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

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(IOTempSession));
    }

    private void EnsureRootExistsAndAccessible()
    {
        try {
            ExceptionThrower.ThrowIfDirectoryNotFound(_options.RootDirectory, nameof(_options.RootDirectory));
            ExceptionThrower.ThrowIfDirectoryNotAccessible(_options.RootDirectory, nameof(_options.RootDirectory));
        }
        catch (Exception ex) {
            _logger.LogError(ex, "IO temp session root directory {RootDirectory} is not accessible", _options.RootDirectory);
            throw;
        }
    }

    private static void ValidateDirectoryReadableAndWritable(string directoryPath)
    {
        ExceptionThrower.ThrowIfDirectoryNotAccessible(directoryPath, nameof(directoryPath));
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

#endregion
}