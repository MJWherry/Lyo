using System.Diagnostics;
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
    private static long _nameSequence;
    private readonly List<string> _directories = [];
    private readonly List<string> _files = [];
    private readonly ILogger<IOTempSession> _logger;
    private readonly IMetrics _metrics;
    private readonly Action<string>? _onDispose;

    private readonly IOTempSessionOptions _options;
    private bool _disposed;

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
            var path = ResolvePath(null, false);
            File.WriteAllText(path, text);
            _files.Add(path);
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
            if (Directory.Exists(SessionDirectory))
                Directory.Delete(SessionDirectory, true);

            _metrics.RecordTiming(Constants.Metrics.DisposeSessionDuration, stopwatch.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.DisposeSessionSuccess);
        }
        catch (Exception ex) {
            //todo possibly try a poll approach here
            _logger.LogError(ex, "Couldn't delete session directory {SessionDirectory} during Dispose. This may lead to orphaned temp files.", SessionDirectory);
            _metrics.RecordError(Constants.Metrics.DisposeSessionDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.DisposeSessionFailure);
        }
        finally {
            _onDispose?.Invoke(SessionDirectory);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        var stopwatch = Stopwatch.StartNew();
        await Task.Run(() => {
            try {
                if (Directory.Exists(SessionDirectory))
                    Directory.Delete(SessionDirectory, true);

                _metrics.RecordTiming(Constants.Metrics.DisposeSessionDuration, stopwatch.Elapsed);
                _metrics.IncrementCounter(Constants.Metrics.DisposeSessionSuccess);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Couldn't delete session directory {SessionDirectory} during DisposeAsync. This may lead to orphaned temp files.", SessionDirectory);
                _metrics.RecordError(Constants.Metrics.DisposeSessionDuration, ex);
                _metrics.IncrementCounter(Constants.Metrics.DisposeSessionFailure);
            }
            finally {
                _onDispose?.Invoke(SessionDirectory);
            }
        });
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
        if (sizeBytes <= _options.MaxFileSizeBytes)
            return;

        OperationHelpers.ThrowIf(
            _options.OverflowStrategy == TempOverflowStrategy.ThrowException,
            $"File size {sizeBytes} bytes exceeds the maximum allowed size of {_options.MaxFileSizeBytes} bytes.");
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