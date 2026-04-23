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
[DebuggerDisplay("{DebugDisplay,nq}")]
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
    private readonly DateTimeOffset _createdAt = DateTimeOffset.UtcNow;
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
        Generator = new IOTempFileGenerator(BuildGeneratorContext());
        _metrics.IncrementCounter(Constants.Metrics.SessionCreated);
    }

    public string SessionDirectory { get; }

    public IReadOnlyList<string> Files => _files;

    public IReadOnlyList<string> Directories => _directories;

    public IIOTempFileGenerator Generator { get; }

    private string DebugDisplay
        => $"Session: {Path.GetFileName(SessionDirectory)} | Files: {_files.Count} | Bytes: {_totalBytesUsed:N0}";

    public event Action<string>? FileCreated;

    public event Action<string>? DirectoryCreated;

#region Inspection

    public long GetTotalBytesUsed() => Interlocked.Read(ref _totalBytesUsed);

    public TempSessionSnapshot GetSnapshot()
    {
        ThrowIfDisposed();
        return new TempSessionSnapshot(
            SessionDirectory,
            new List<string>(_files),
            new List<string>(_directories),
            GetTotalBytesUsed(),
            _createdAt
        );
    }

#endregion

#region Sub-sessions

    public IIOTempSession CreateSubSession()
    {
        ThrowIfDisposed();
        var subOptions = new IOTempSessionOptions {
            RootDirectory = SessionDirectory,
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
        var sub = new IOTempSession(subOptions, _logger, _metrics);
        _directories.Add(sub.SessionDirectory);
        DirectoryCreated?.Invoke(sub.SessionDirectory);
        return sub;
    }

#endregion

#region Discovery

    public IEnumerable<string> EnumerateFiles(string? pattern = null)
    {
        ThrowIfDisposed();
        return pattern == null
            ? Directory.EnumerateFiles(SessionDirectory, "*", SearchOption.AllDirectories)
            : Directory.EnumerateFiles(SessionDirectory, pattern, SearchOption.AllDirectories);
    }

    public IEnumerable<string> EnumerateDirectories()
    {
        ThrowIfDisposed();
        return Directory.EnumerateDirectories(SessionDirectory, "*", SearchOption.AllDirectories);
    }

#endregion

#region Delete operations

    public bool DeleteFile(string path)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path, nameof(path));
        var sw = Stopwatch.StartNew();
        try {
            var safePath = EnsurePathWithinSession(path);
            if (!File.Exists(safePath)) {
                _files.Remove(safePath);
                _metrics.IncrementCounter(Constants.Metrics.DeleteFileSuccess);
                return false;
            }

            var size = _files.Contains(safePath) ? new FileInfo(safePath).Length : 0L;
            File.Delete(safePath);
            if (_files.Remove(safePath))
                Interlocked.Add(ref _totalBytesUsed, -size);

            _metrics.RecordTiming(Constants.Metrics.DeleteFileDuration, sw.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.DeleteFileSuccess);
            return true;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.DeleteFileDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.DeleteFileFailure);
            _logger.LogError(ex, "Failed deleting file {FilePath} in session {SessionDirectory}", path, SessionDirectory);
            throw;
        }
    }

    public bool DeleteDirectory(string path)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path, nameof(path));
        var sw = Stopwatch.StartNew();
        try {
            var safePath = EnsurePathWithinSession(path);
            OperationHelpers.ThrowIf(
                string.Equals(safePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    SessionDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    GetPathComparison()),
                "Cannot delete the session root directory.");

            if (!Directory.Exists(safePath)) {
                _directories.Remove(safePath);
                _metrics.IncrementCounter(Constants.Metrics.DeleteDirectorySuccess);
                return false;
            }

            var prefix = safePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                         + Path.DirectorySeparatorChar;
            var comparison = GetPathComparison();

            var filesUnder = _files.Where(f => f.StartsWith(prefix, comparison)).ToList();
            var freedBytes = filesUnder.Sum(f => File.Exists(f) ? new FileInfo(f).Length : 0L);
            var dirsUnder = _directories.Where(d => d.StartsWith(prefix, comparison)).ToList();

            Directory.Delete(safePath, true);

            foreach (var f in filesUnder) _files.Remove(f);
            foreach (var d in dirsUnder) _directories.Remove(d);
            _directories.Remove(safePath);
            Interlocked.Add(ref _totalBytesUsed, -freedBytes);

            _metrics.RecordTiming(Constants.Metrics.DeleteDirectoryDuration, sw.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.DeleteDirectorySuccess);
            return true;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.DeleteDirectoryDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.DeleteDirectoryFailure);
            _logger.LogError(ex, "Failed deleting directory {DirectoryPath} in session {SessionDirectory}", path, SessionDirectory);
            throw;
        }
    }

#endregion

#region Move operations

    public string MoveFrom(string sourcePath)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sourcePath, nameof(sourcePath));
        var sw = Stopwatch.StartNew();
        try {
            string dest;
            if (File.Exists(sourcePath))
                dest = MoveFileFrom(sourcePath);
            else if (Directory.Exists(sourcePath))
                dest = MoveDirectoryFrom(sourcePath);
            else
                throw new FileNotFoundException($"Source path does not exist: {sourcePath}", sourcePath);

            _metrics.RecordTiming(Constants.Metrics.MoveFromDuration, sw.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.MoveFromSuccess);
            return dest;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.MoveFromDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.MoveFromFailure);
            _logger.LogError(ex, "Failed moving {SourcePath} into session {SessionDirectory}", sourcePath, SessionDirectory);
            throw;
        }
    }

    public Task<string> MoveFromAsync(string sourcePath, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return Task.Run(() => MoveFrom(sourcePath), ct);
    }

    private string MoveFileFrom(string sourcePath)
    {
        var sourceInfo = new FileInfo(sourcePath);
        ValidateFileSize(sourceInfo.Length);

        var destName = Path.GetFileName(sourcePath);
        var dest = Path.Combine(SessionDirectory, destName);
        if (File.Exists(dest)) {
            var ext = Path.GetExtension(destName);
            var stem = Path.GetFileNameWithoutExtension(destName);
            dest = Path.Combine(SessionDirectory, $"{stem}_{Guid.NewGuid():N}{ext}");
        }

        dest = EnsurePathWithinSession(dest);
        try {
            File.Move(sourcePath, dest);
        }
        catch (IOException) {
            // Cross-device: fall back to copy then delete
            File.Copy(sourcePath, dest);
            File.Delete(sourcePath);
        }

        _files.Add(dest);
        Interlocked.Add(ref _totalBytesUsed, sourceInfo.Length);
        FileCreated?.Invoke(dest);
        return dest;
    }

    private string MoveDirectoryFrom(string sourcePath)
    {
        var dirName = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var dest = Path.Combine(SessionDirectory, dirName);
        if (Directory.Exists(dest))
            dest = Path.Combine(SessionDirectory, $"{dirName}_{Guid.NewGuid():N}");
        dest = EnsurePathWithinSession(dest);
        CopyDirectoryRecursive(sourcePath, dest);
        Directory.Delete(sourcePath, true);
        _directories.Add(dest);
        DirectoryCreated?.Invoke(dest);
        return dest;
    }

#endregion

#region Write (overwrite) operations

    public string WriteFile(string path, string text)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNull(text, nameof(text));
        var sw = Stopwatch.StartNew();
        try {
            var safePath = EnsurePathWithinSession(path);
            if (!File.Exists(safePath))
                throw new FileNotFoundException($"File not found: {safePath}", safePath);

            var oldSize = new FileInfo(safePath).Length;
            var newSize = Encoding.UTF8.GetByteCount(text);
            ValidateFileSizeForOverwrite(oldSize, newSize);
            File.WriteAllText(safePath, text, Encoding.UTF8);

            if (!_files.Contains(safePath))
                _files.Add(safePath);
            Interlocked.Add(ref _totalBytesUsed, newSize - oldSize);
            _metrics.RecordTiming(Constants.Metrics.WriteFileDuration, sw.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.WriteFileSuccess);
            return safePath;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.WriteFileDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.WriteFileFailure);
            _logger.LogError(ex, "Failed overwriting file {FilePath} in session {SessionDirectory}", path, SessionDirectory);
            throw;
        }
    }

    public string WriteFile(string path, ReadOnlyMemory<byte> data)
    {
        ThrowIfDisposed();
        var sw = Stopwatch.StartNew();
        try {
            var safePath = EnsurePathWithinSession(path);
            if (!File.Exists(safePath))
                throw new FileNotFoundException($"File not found: {safePath}", safePath);

            var oldSize = new FileInfo(safePath).Length;
            ValidateFileSizeForOverwrite(oldSize, data.Length);
            File.WriteAllBytes(safePath, data.ToArray());

            if (!_files.Contains(safePath))
                _files.Add(safePath);
            Interlocked.Add(ref _totalBytesUsed, data.Length - oldSize);
            _metrics.RecordTiming(Constants.Metrics.WriteFileDuration, sw.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.WriteFileSuccess);
            return safePath;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.WriteFileDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.WriteFileFailure);
            _logger.LogError(ex, "Failed overwriting file {FilePath} with bytes in session {SessionDirectory}", path, SessionDirectory);
            throw;
        }
    }

    public async Task<string> WriteFileAsync(string path, string text, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNull(text, nameof(text));
        var sw = Stopwatch.StartNew();
        try {
            var safePath = EnsurePathWithinSession(path);
            if (!File.Exists(safePath))
                throw new FileNotFoundException($"File not found: {safePath}", safePath);

            var oldSize = new FileInfo(safePath).Length;
            var newSize = Encoding.UTF8.GetByteCount(text);
            ValidateFileSizeForOverwrite(oldSize, newSize);
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            await File.WriteAllTextAsync(safePath, text, Encoding.UTF8, ct);
#else
            await Task.Run(() => { ct.ThrowIfCancellationRequested(); File.WriteAllText(safePath, text, Encoding.UTF8); }, ct).ConfigureAwait(false);
#endif
            if (!_files.Contains(safePath))
                _files.Add(safePath);
            Interlocked.Add(ref _totalBytesUsed, newSize - oldSize);
            _metrics.RecordTiming(Constants.Metrics.WriteFileDuration, sw.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.WriteFileSuccess);
            return safePath;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.WriteFileDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.WriteFileFailure);
            _logger.LogError(ex, "Failed overwriting file async {FilePath} in session {SessionDirectory}", path, SessionDirectory);
            throw;
        }
    }

    public async Task<string> WriteFileAsync(string path, ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        var sw = Stopwatch.StartNew();
        try {
            var safePath = EnsurePathWithinSession(path);
            if (!File.Exists(safePath))
                throw new FileNotFoundException($"File not found: {safePath}", safePath);

            var oldSize = new FileInfo(safePath).Length;
            ValidateFileSizeForOverwrite(oldSize, data.Length);
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            await File.WriteAllBytesAsync(safePath, data.ToArray(), ct);
#else
            await Task.Run(() => { ct.ThrowIfCancellationRequested(); File.WriteAllBytes(safePath, data.ToArray()); }, ct).ConfigureAwait(false);
#endif
            if (!_files.Contains(safePath))
                _files.Add(safePath);
            Interlocked.Add(ref _totalBytesUsed, data.Length - oldSize);
            _metrics.RecordTiming(Constants.Metrics.WriteFileDuration, sw.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.WriteFileSuccess);
            return safePath;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.WriteFileDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.WriteFileFailure);
            _logger.LogError(ex, "Failed overwriting file async with bytes {FilePath} in session {SessionDirectory}", path, SessionDirectory);
            throw;
        }
    }

#endregion

#region Session mutation

    public void Clear()
    {
        ThrowIfDisposed();
        foreach (var file in _files.ToList()) {
            try {
                if (File.Exists(file)) File.Delete(file);
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Failed to delete file {File} during Clear() in session {SessionDirectory}", file, SessionDirectory);
            }
        }

        foreach (var dir in _directories.ToList()) {
            if (dir == SessionDirectory) continue;
            try {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Failed to delete directory {Dir} during Clear() in session {SessionDirectory}", dir, SessionDirectory);
            }
        }

        _files.Clear();
        _directories.Clear();
        Interlocked.Exchange(ref _totalBytesUsed, 0);
    }

    public string CopyFrom(string sourcePath)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sourcePath, nameof(sourcePath));
        var sw = Stopwatch.StartNew();
        try {
            string dest;
            if (File.Exists(sourcePath))
                dest = CopyFileFrom(sourcePath);
            else if (Directory.Exists(sourcePath))
                dest = CopyDirectoryFrom(sourcePath);
            else
                throw new FileNotFoundException($"Source path does not exist: {sourcePath}", sourcePath);

            _metrics.RecordTiming(Constants.Metrics.CopyFromDuration, sw.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.CopyFromSuccess);
            return dest;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.CopyFromDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.CopyFromFailure);
            _logger.LogError(ex, "Failed copying {SourcePath} into session {SessionDirectory}", sourcePath, SessionDirectory);
            throw;
        }
    }

    public async Task<string> CopyFromAsync(string sourcePath, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sourcePath, nameof(sourcePath));
        var sw = Stopwatch.StartNew();
        try {
            string dest;
            if (File.Exists(sourcePath))
                dest = await CopyFileFromAsync(sourcePath, ct);
            else if (Directory.Exists(sourcePath))
                dest = await CopyDirectoryFromAsync(sourcePath, ct);
            else
                throw new FileNotFoundException($"Source path does not exist: {sourcePath}", sourcePath);

            _metrics.RecordTiming(Constants.Metrics.CopyFromDuration, sw.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.CopyFromSuccess);
            return dest;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.CopyFromDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.CopyFromFailure);
            _logger.LogError(ex, "Failed copying async {SourcePath} into session {SessionDirectory}", sourcePath, SessionDirectory);
            throw;
        }
    }

    public string AppendToFile(string path, ReadOnlyMemory<byte> data)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path, nameof(path));
        var sw = Stopwatch.StartNew();
        try {
            var safePath = EnsurePathWithinSession(path);
            if (!File.Exists(safePath))
                throw new FileNotFoundException($"File not found: {safePath}", safePath);

            ValidateFileSize(data.Length);
            using var fs = new FileStream(safePath, FileMode.Append, FileAccess.Write, FileShare.None);
            var bytes = data.ToArray();
            fs.Write(bytes, 0, bytes.Length);

            if (!_files.Contains(safePath))
                _files.Add(safePath);

            Interlocked.Add(ref _totalBytesUsed, data.Length);
            _metrics.RecordTiming(Constants.Metrics.AppendToFileDuration, sw.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.AppendToFileSuccess);
            return safePath;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.AppendToFileDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.AppendToFileFailure);
            _logger.LogError(ex, "Failed appending bytes to file {FilePath} in session {SessionDirectory}", path, SessionDirectory);
            throw;
        }
    }

    public string AppendToFile(string path, string text)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path, nameof(path));
        ArgumentHelpers.ThrowIfNull(text, nameof(text));
        var sw = Stopwatch.StartNew();
        try {
            var safePath = EnsurePathWithinSession(path);
            if (!File.Exists(safePath))
                throw new FileNotFoundException($"File not found: {safePath}", safePath);

            var byteCount = Encoding.UTF8.GetByteCount(text);
            ValidateFileSize(byteCount);
            File.AppendAllText(safePath, text, Encoding.UTF8);

            if (!_files.Contains(safePath))
                _files.Add(safePath);

            Interlocked.Add(ref _totalBytesUsed, byteCount);
            _metrics.RecordTiming(Constants.Metrics.AppendToFileDuration, sw.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.AppendToFileSuccess);
            return safePath;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.AppendToFileDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.AppendToFileFailure);
            _logger.LogError(ex, "Failed appending text to file {FilePath} in session {SessionDirectory}", path, SessionDirectory);
            throw;
        }
    }

    public async Task<string> AppendToFileAsync(string path, ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path, nameof(path));
        var sw = Stopwatch.StartNew();
        try {
            var safePath = EnsurePathWithinSession(path);
            if (!File.Exists(safePath))
                throw new FileNotFoundException($"File not found: {safePath}", safePath);

            ValidateFileSize(data.Length);
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            await using var fs = new FileStream(safePath, FileMode.Append, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
            await fs.WriteAsync(data, ct);
#else
            using var fs = new FileStream(safePath, FileMode.Append, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
            var bytes = data.ToArray();
            await fs.WriteAsync(bytes, 0, bytes.Length, ct).ConfigureAwait(false);
#endif
            if (!_files.Contains(safePath))
                _files.Add(safePath);

            Interlocked.Add(ref _totalBytesUsed, data.Length);
            _metrics.RecordTiming(Constants.Metrics.AppendToFileDuration, sw.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.AppendToFileSuccess);
            return safePath;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.AppendToFileDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.AppendToFileFailure);
            _logger.LogError(ex, "Failed appending bytes async to file {FilePath} in session {SessionDirectory}", path, SessionDirectory);
            throw;
        }
    }

    public async Task<string> AppendToFileAsync(string path, string text, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path, nameof(path));
        ArgumentHelpers.ThrowIfNull(text, nameof(text));
        var sw = Stopwatch.StartNew();
        try {
            var safePath = EnsurePathWithinSession(path);
            if (!File.Exists(safePath))
                throw new FileNotFoundException($"File not found: {safePath}", safePath);

            var byteCount = Encoding.UTF8.GetByteCount(text);
            ValidateFileSize(byteCount);
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            await File.AppendAllTextAsync(safePath, text, Encoding.UTF8, ct);
#else
            await Task.Run(() => { ct.ThrowIfCancellationRequested(); File.AppendAllText(safePath, text, Encoding.UTF8); }, ct).ConfigureAwait(false);
#endif
            if (!_files.Contains(safePath))
                _files.Add(safePath);

            Interlocked.Add(ref _totalBytesUsed, byteCount);
            _metrics.RecordTiming(Constants.Metrics.AppendToFileDuration, sw.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.AppendToFileSuccess);
            return safePath;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.AppendToFileDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.AppendToFileFailure);
            _logger.LogError(ex, "Failed appending text async to file {FilePath} in session {SessionDirectory}", path, SessionDirectory);
            throw;
        }
    }

    private string CopyFileFrom(string sourcePath)
    {
        var sourceInfo = new FileInfo(sourcePath);
        ValidateFileSize(sourceInfo.Length);

        var destName = Path.GetFileName(sourcePath);
        var dest = Path.Combine(SessionDirectory, destName);

        if (File.Exists(dest)) {
            var ext = Path.GetExtension(destName);
            var stem = Path.GetFileNameWithoutExtension(destName);
            dest = Path.Combine(SessionDirectory, $"{stem}_{Guid.NewGuid():N}{ext}");
        }

        dest = EnsurePathWithinSession(dest);
        File.Copy(sourcePath, dest);
        _files.Add(dest);
        Interlocked.Add(ref _totalBytesUsed, sourceInfo.Length);
        FileCreated?.Invoke(dest);
        return dest;
    }

    private string CopyDirectoryFrom(string sourcePath)
    {
        var dirName = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var dest = Path.Combine(SessionDirectory, dirName);

        if (Directory.Exists(dest))
            dest = Path.Combine(SessionDirectory, $"{dirName}_{Guid.NewGuid():N}");

        dest = EnsurePathWithinSession(dest);
        CopyDirectoryRecursive(sourcePath, dest);
        _directories.Add(dest);
        DirectoryCreated?.Invoke(dest);
        return dest;
    }

    private async Task<string> CopyFileFromAsync(string sourcePath, CancellationToken ct)
    {
        var sourceInfo = new FileInfo(sourcePath);
        ValidateFileSize(sourceInfo.Length);

        var destName = Path.GetFileName(sourcePath);
        var dest = Path.Combine(SessionDirectory, destName);
        if (File.Exists(dest)) {
            var ext = Path.GetExtension(destName);
            var stem = Path.GetFileNameWithoutExtension(destName);
            dest = Path.Combine(SessionDirectory, $"{stem}_{Guid.NewGuid():N}{ext}");
        }

        dest = EnsurePathWithinSession(dest);
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        await using var srcStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var dstStream = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        await srcStream.CopyToAsync(dstStream, ct);
#else
        using var srcStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var dstStream = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        await srcStream.CopyToAsync(dstStream, 81920, ct).ConfigureAwait(false);
#endif
        _files.Add(dest);
        Interlocked.Add(ref _totalBytesUsed, sourceInfo.Length);
        FileCreated?.Invoke(dest);
        return dest;
    }

    private async Task<string> CopyDirectoryFromAsync(string sourcePath, CancellationToken ct)
    {
        var dirName = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var dest = Path.Combine(SessionDirectory, dirName);
        if (Directory.Exists(dest))
            dest = Path.Combine(SessionDirectory, $"{dirName}_{Guid.NewGuid():N}");
        dest = EnsurePathWithinSession(dest);
        await CopyDirectoryRecursiveAsync(sourcePath, dest, ct);
        _directories.Add(dest);
        DirectoryCreated?.Invoke(dest);
        return dest;
    }

    private async Task CopyDirectoryRecursiveAsync(string src, string dst, CancellationToken ct)
    {
        Directory.CreateDirectory(dst);
        foreach (var file in Directory.GetFiles(src)) {
            ct.ThrowIfCancellationRequested();
            var info = new FileInfo(file);
            ValidateFileSize(info.Length);
            var destFile = Path.Combine(dst, Path.GetFileName(file));
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            await using var srcStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using var dstStream = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
            await srcStream.CopyToAsync(dstStream, ct);
#else
            using var srcStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var dstStream = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
            await srcStream.CopyToAsync(dstStream, 81920, ct).ConfigureAwait(false);
#endif
            _files.Add(destFile);
            Interlocked.Add(ref _totalBytesUsed, info.Length);
            FileCreated?.Invoke(destFile);
        }

        foreach (var subDir in Directory.GetDirectories(src)) {
            ct.ThrowIfCancellationRequested();
            var destSub = Path.Combine(dst, Path.GetFileName(subDir));
            _directories.Add(destSub);
            DirectoryCreated?.Invoke(destSub);
            await CopyDirectoryRecursiveAsync(subDir, destSub, ct);
        }
    }

    private void CopyDirectoryRecursive(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (var file in Directory.GetFiles(src)) {
            var info = new FileInfo(file);
            ValidateFileSize(info.Length);
            var destFile = Path.Combine(dst, Path.GetFileName(file));
            File.Copy(file, destFile);
            _files.Add(destFile);
            Interlocked.Add(ref _totalBytesUsed, info.Length);
            FileCreated?.Invoke(destFile);
        }

        foreach (var subDir in Directory.GetDirectories(src)) {
            var destSub = Path.Combine(dst, Path.GetFileName(subDir));
            _directories.Add(destSub);
            DirectoryCreated?.Invoke(destSub);
            CopyDirectoryRecursive(subDir, destSub);
        }
    }

#endregion

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
            FileCreated?.Invoke(path);
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
            FileCreated?.Invoke(path);
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
            FileCreated?.Invoke(path);
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
            FileCreated?.Invoke(path);
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
            FileCreated?.Invoke(path);
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
            FileCreated?.Invoke(path);
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
            FileCreated?.Invoke(path);
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
            DirectoryCreated?.Invoke(path);
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

    private IOTempGeneratorContext BuildGeneratorContext()
        => new(
            SessionDirectory: SessionDirectory,
            ThrowIfDisposed: ThrowIfDisposed,
            ResolvePath: ResolvePath,
            EnsureWithinSession: EnsurePathWithinSession,
            ValidateSize: ValidateFileSize,
            RegisterFile: (path, bytes) => {
                _files.Add(path);
                Interlocked.Add(ref _totalBytesUsed, bytes);
                FileCreated?.Invoke(path);
            },
            RegisterDirectory: path => {
                _directories.Add(path);
                DirectoryCreated?.Invoke(path);
            },
            Options: _options,
            Logger: _logger,
            Metrics: _metrics
        );

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
        OperationHelpers.ThrowIf(_options.MaxFileSizeBytes.HasValue && sizeBytes > _options.MaxFileSizeBytes.Value,
            $"File size {sizeBytes:N0} bytes exceeds the per-file limit of {_options.MaxFileSizeBytes!.Value:N0} bytes.");

        // File count limit
        if (_options.MaxFileCount.HasValue && _files.Count >= _options.MaxFileCount.Value) {
            switch (_options.OverflowStrategy) {
                case TempOverflowStrategy.ThrowException:
                    OperationHelpers.ThrowIf(true,
                        $"Session already has {_files.Count} tracked files, which meets the limit of {_options.MaxFileCount.Value}.");
                    break;
                case TempOverflowStrategy.DeleteOldest:
                case TempOverflowStrategy.DeleteLargest:
                    FreeFileCountFor(1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_options.OverflowStrategy), _options.OverflowStrategy, null);
            }
        }

        if (_options.MaxTotalSizeBytes == null)
            return;

        var projectedTotal = Interlocked.Read(ref _totalBytesUsed) + sizeBytes;
        if (projectedTotal <= _options.MaxTotalSizeBytes.Value)
            return;

        switch (_options.OverflowStrategy) {
            case TempOverflowStrategy.ThrowException:
                OperationHelpers.ThrowIf(true,
                    $"Adding {sizeBytes:N0} bytes would exceed session total limit of {_options.MaxTotalSizeBytes.Value:N0} bytes (current: {Interlocked.Read(ref _totalBytesUsed):N0} bytes).");
                break;
            case TempOverflowStrategy.DeleteOldest:
            case TempOverflowStrategy.DeleteLargest:
                FreeTotalSpaceFor(sizeBytes);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_options.OverflowStrategy), _options.OverflowStrategy, null);
        }
    }

    private void ValidateFileSizeForOverwrite(long oldSizeBytes, long newSizeBytes)
    {
        OperationHelpers.ThrowIf(_options.MaxFileSizeBytes.HasValue && newSizeBytes > _options.MaxFileSizeBytes.Value,
            $"File size {newSizeBytes:N0} bytes exceeds the per-file limit of {_options.MaxFileSizeBytes!.Value:N0} bytes.");

        if (_options.MaxTotalSizeBytes == null) return;
        var delta = newSizeBytes - oldSizeBytes;
        if (delta <= 0) return;

        var projected = Interlocked.Read(ref _totalBytesUsed) + delta;
        OperationHelpers.ThrowIf(projected > _options.MaxTotalSizeBytes.Value,
            $"Writing {newSizeBytes:N0} bytes (delta: +{delta:N0}) would exceed session total limit of {_options.MaxTotalSizeBytes.Value:N0} bytes (current: {Interlocked.Read(ref _totalBytesUsed):N0} bytes).");
    }

    private void FreeFileCountFor(int requiredSlots)
    {
        var excess = _files.Count - _options.MaxFileCount!.Value + requiredSlots;
        if (excess <= 0) return;

        var candidates = _options.OverflowStrategy == TempOverflowStrategy.DeleteOldest
            ? _files.Where(File.Exists).OrderBy(f => new FileInfo(f).CreationTimeUtc).ToList()
            : _files.Where(File.Exists).OrderByDescending(f => new FileInfo(f).Length).ToList();

        var freed = 0;
        foreach (var file in candidates) {
            if (freed >= excess) break;
            var size = new FileInfo(file).Exists ? new FileInfo(file).Length : 0L;
            try {
                File.Delete(file);
                _files.Remove(file);
                Interlocked.Add(ref _totalBytesUsed, -size);
                freed++;
                _logger.LogInformation("Deleted {FilePath} ({Size:N0} bytes) to free a file slot in session {SessionDirectory}", file, size, SessionDirectory);
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Failed to delete {FilePath} during file-count overflow cleanup in session {SessionDirectory}", file, SessionDirectory);
            }
        }

        OperationHelpers.ThrowIf(freed < excess,
            $"Could not free {excess} file slot(s): only freed {freed}. Session file count limit: {_options.MaxFileCount.Value}.");
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

        OperationHelpers.ThrowIf(freed < needed,
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

    private void ThrowIfDisposed() => OperationHelpers.ThrowIfDisposed(_disposed, nameof(IOTempSession));

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