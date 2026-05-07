using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Text;
using Lyo.Exceptions;
using Lyo.IO.Temp.Enums;
using Lyo.IO.Temp.Storage;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
#if !NET5_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace Lyo.IO.Temp.Models;

/// <summary>Default <see cref="IIOTempSession" /> implementation.</summary>
// ReSharper disable once InconsistentNaming
[DebuggerDisplay("{DebugDisplay,nq}")]
public sealed class IOTempSession : IIOTempSession
{
    private const int DisposeRetryCount = 3;
    private const int DisposeRetryDelayMs = 150;

    /// <summary>UTF-8 without BOM so written bytes match <see cref="Encoding.GetByteCount" /> and stream roundtrips.</summary>
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    private static long _nameSequence;
    private readonly DateTimeOffset _createdAt = DateTimeOffset.UtcNow;
    private readonly List<string> _directories = [];
    private readonly List<string> _files = [];
    private readonly ILogger<IOTempSession> _logger;
    private readonly IMetrics _metrics;
    private readonly Action<string>? _onDispose;
    private readonly IOTempSessionOptions _options;
    private readonly IIOTempStorageProvider _storage;
    private bool _disposed;
    private long _totalBytesUsed;

    private string DebugDisplay => $"Session: {Path.GetFileName(SessionDirectory)} | Files: {_files.Count} | Bytes: {_totalBytesUsed:N0}";

    /// <summary>Creates a session under <see cref="IOTempSessionOptions.RootDirectory" />, creating the session directory on disk.</summary>
    /// <param name="options">Layout, limits, and naming; defaults when null.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="metrics">Honoured when <see cref="IOTempSessionOptions.EnableMetrics" /> is true.</param>
    /// <param name="onDispose">Optional callback invoked with <see cref="SessionDirectory" /> after disposal attempts (e.g. service unregistration).</param>
    /// <param name="storageProvider">Storage implementation; defaults to file system under the session root's logical root.</param>
    public IOTempSession(
        IOTempSessionOptions? options = null,
        ILogger<IOTempSession>? logger = null,
        IMetrics? metrics = null,
        Action<string>? onDispose = null,
        IIOTempStorageProvider? storageProvider = null)
    {
        _options = options ?? new IOTempSessionOptions();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_options.RootDirectory, nameof(_options.RootDirectory));
        _storage = storageProvider ?? new FileSystemIOTempStorageProvider(_options.RootDirectory);
        _logger = logger ?? NullLogger<IOTempSession>.Instance;
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _onDispose = onDispose;
        EnsureRootExistsAndAccessible();
        SessionDirectory = CreateSessionDirectory();
        Generator = new IOTempFileGenerator(BuildGeneratorContext());
        _metrics.IncrementCounter(Constants.Metrics.SessionCreated);
    }

    /// <inheritdoc />
    public string SessionDirectory { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Files => _files;

    /// <inheritdoc />
    public IReadOnlyList<string> Directories => _directories;

    /// <inheritdoc />
    public IIOTempFileGenerator Generator { get; }

    /// <inheritdoc />
    public event Action<string>? FileCreated;

    /// <inheritdoc />
    public event Action<string>? DirectoryCreated;

#region Sub-sessions

    /// <inheritdoc />
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

        var sub = new IOTempSession(subOptions, _logger, _metrics, storageProvider: _storage);
        _directories.Add(sub.SessionDirectory);
        DirectoryCreated?.Invoke(sub.SessionDirectory);
        return sub;
    }

#endregion

#region Inspection

    /// <inheritdoc />
    public long GetTotalBytesUsed() => Interlocked.Read(ref _totalBytesUsed);

    /// <inheritdoc />
    public TempSessionSnapshot GetSnapshot()
    {
        ThrowIfDisposed();
        return new(SessionDirectory, new List<string>(_files), new List<string>(_directories), GetTotalBytesUsed(), _createdAt);
    }

#endregion

#region Discovery

    /// <inheritdoc />
    public IEnumerable<string> EnumerateFiles(string? pattern = null)
    {
        ThrowIfDisposed();
        // Provider-level enumeration walks all descendants; apply glob pattern client-side if provided.
        return EnumerateAllFiles(SessionDirectory).Where(f => pattern == null || MatchesGlob(Path.GetFileName(f), pattern));
    }

    /// <inheritdoc />
    public IEnumerable<string> EnumerateDirectories()
    {
        ThrowIfDisposed();
        return EnumerateAllDirectories(SessionDirectory);
    }

    private IEnumerable<string> EnumerateAllFiles(string dir)
    {
        foreach (var entry in _storage.EnumerateEntries(dir)) {
            if (!entry.IsDirectory)
                yield return entry.FullPath;
            else {
                foreach (var sub in EnumerateAllFiles(entry.FullPath))
                    yield return sub;
            }
        }
    }

    private IEnumerable<string> EnumerateAllDirectories(string dir)
    {
        foreach (var entry in _storage.EnumerateEntries(dir)) {
            if (!entry.IsDirectory)
                continue;

            yield return entry.FullPath;

            foreach (var sub in EnumerateAllDirectories(entry.FullPath))
                yield return sub;
        }
    }

    private static bool MatchesGlob(string name, string pattern)
    {
        // Simple * wildcard support matching the original Directory.EnumerateFiles pattern behaviour.
        if (pattern == "*")
            return true;

        if (!pattern.Contains('*') && !pattern.Contains('?'))
            return string.Equals(name, pattern, StringComparison.OrdinalIgnoreCase);

        // Convert glob to a simple regex-like match.
        var parts = pattern.Split('*');
        var pos = 0;
        foreach (var part in parts) {
            if (part.Length == 0)
                continue;

            var idx = name.IndexOf(part, pos, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return false;

            pos = idx + part.Length;
        }

        return true;
    }

#endregion

#region Delete operations

    /// <inheritdoc />
    public bool DeleteFile(string path)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        var sw = Stopwatch.StartNew();
        try {
            var safePath = EnsurePathWithinSession(path);
            if (!_storage.FileExists(safePath)) {
                _files.Remove(safePath);
                _metrics.IncrementCounter(Constants.Metrics.DeleteFileSuccess);
                return false;
            }

            var size = _files.Contains(safePath) ? _storage.GetFileLength(safePath) : 0L;
            _storage.DeleteFile(safePath);
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

    /// <inheritdoc />
    public bool DeleteDirectory(string path)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        var sw = Stopwatch.StartNew();
        try {
            var safePath = EnsurePathWithinSession(path);
            OperationHelpers.ThrowIf(
                string.Equals(
                    safePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    SessionDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), GetPathComparison()), "Cannot delete the session root directory.");

            if (!_storage.DirectoryExists(safePath)) {
                _directories.Remove(safePath);
                _metrics.IncrementCounter(Constants.Metrics.DeleteDirectorySuccess);
                return false;
            }

            var prefix = safePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var comparison = GetPathComparison();
            var filesUnder = _files.Where(f => f.StartsWith(prefix, comparison)).ToList();
            var freedBytes = filesUnder.Sum(f => _storage.FileExists(f) ? _storage.GetFileLength(f) : 0L);
            var dirsUnder = _directories.Where(d => d.StartsWith(prefix, comparison)).ToList();
            _storage.DeleteDirectory(safePath);
            foreach (var f in filesUnder)
                _files.Remove(f);

            foreach (var d in dirsUnder)
                _directories.Remove(d);

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

    /// <inheritdoc />
    public string MoveFrom(string sourcePath)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sourcePath);
        var sw = Stopwatch.StartNew();
        try {
            string dest;
            if (_storage.FileExists(sourcePath))
                dest = MoveFileFrom(sourcePath);
            else if (_storage.DirectoryExists(sourcePath))
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

    /// <inheritdoc />
    public Task<string> MoveFromAsync(string sourcePath, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return Task.Run(() => MoveFrom(sourcePath), ct);
    }

    private string MoveFileFrom(string sourcePath)
    {
        var size = _storage.GetFileLength(sourcePath);
        ValidateFileSize(size);
        var destName = Path.GetFileName(sourcePath);
        var dest = Path.Combine(SessionDirectory, destName);
        if (_storage.FileExists(dest)) {
            var ext = Path.GetExtension(destName);
            var stem = Path.GetFileNameWithoutExtension(destName);
            dest = Path.Combine(SessionDirectory, $"{stem}_{Guid.NewGuid():N}{ext}");
        }

        dest = EnsurePathWithinSession(dest);
        _storage.MoveFile(sourcePath, dest);
        _files.Add(dest);
        Interlocked.Add(ref _totalBytesUsed, size);
        FileCreated?.Invoke(dest);
        return dest;
    }

    private string MoveDirectoryFrom(string sourcePath)
    {
        var dirName = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var dest = Path.Combine(SessionDirectory, dirName);
        if (_storage.DirectoryExists(dest))
            dest = Path.Combine(SessionDirectory, $"{dirName}_{Guid.NewGuid():N}");

        dest = EnsurePathWithinSession(dest);
        CopyDirectoryRecursive(sourcePath, dest);
        _storage.DeleteDirectory(sourcePath);
        _directories.Add(dest);
        DirectoryCreated?.Invoke(dest);
        return dest;
    }

#endregion

#region Write (overwrite) operations

    /// <inheritdoc />
    public string WriteFile(string path, string text)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNull(text);
        var sw = Stopwatch.StartNew();
        try {
            var safePath = EnsurePathWithinSession(path);
            if (!_storage.FileExists(safePath))
                throw new FileNotFoundException($"File not found: {safePath}", safePath);

            var oldSize = _storage.GetFileLength(safePath);
            var newSize = Utf8NoBom.GetByteCount(text);
            ValidateFileSizeForOverwrite(oldSize, newSize);
            _storage.WriteAllText(safePath, text, Utf8NoBom);
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

    /// <inheritdoc />
    public string WriteFile(string path, ReadOnlyMemory<byte> data)
    {
        ThrowIfDisposed();
        var sw = Stopwatch.StartNew();
        try {
            var safePath = EnsurePathWithinSession(path);
            if (!_storage.FileExists(safePath))
                throw new FileNotFoundException($"File not found: {safePath}", safePath);

            var oldSize = _storage.GetFileLength(safePath);
            ValidateFileSizeForOverwrite(oldSize, data.Length);
            _storage.WriteAllBytes(safePath, data.ToArray());
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
            _logger.LogError(ex, "Failed overwriting file with bytes {FilePath} in session {SessionDirectory}", path, SessionDirectory);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string text, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNull(text);
        var sw = Stopwatch.StartNew();
        try {
            var safePath = EnsurePathWithinSession(path);
            if (!_storage.FileExists(safePath))
                throw new FileNotFoundException($"File not found: {safePath}", safePath);

            var oldSize = _storage.GetFileLength(safePath);
            var newSize = Utf8NoBom.GetByteCount(text);
            ValidateFileSizeForOverwrite(oldSize, newSize);
            await _storage.WriteAllTextAsync(safePath, text, Utf8NoBom, ct).ConfigureAwait(false);
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

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        var sw = Stopwatch.StartNew();
        try {
            var safePath = EnsurePathWithinSession(path);
            if (!_storage.FileExists(safePath))
                throw new FileNotFoundException($"File not found: {safePath}", safePath);

            var oldSize = _storage.GetFileLength(safePath);
            ValidateFileSizeForOverwrite(oldSize, data.Length);
            await _storage.WriteAllBytesAsync(safePath, data.ToArray(), ct).ConfigureAwait(false);
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

    /// <inheritdoc />
    public void Clear()
    {
        ThrowIfDisposed();
        foreach (var file in _files.ToList()) {
            try {
                if (_storage.FileExists(file))
                    _storage.DeleteFile(file);
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Failed to delete file {File} during Clear() in session {SessionDirectory}", file, SessionDirectory);
            }
        }

        foreach (var dir in _directories.ToList()) {
            if (dir == SessionDirectory)
                continue;

            try {
                if (_storage.DirectoryExists(dir))
                    _storage.DeleteDirectory(dir);
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Failed to delete directory {Dir} during Clear() in session {SessionDirectory}", dir, SessionDirectory);
            }
        }

        _files.Clear();
        _directories.Clear();
        Interlocked.Exchange(ref _totalBytesUsed, 0);
    }

    /// <inheritdoc />
    public string CopyFrom(string sourcePath)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sourcePath);
        var sw = Stopwatch.StartNew();
        try {
            string dest;
            if (_storage.FileExists(sourcePath))
                dest = CopyFileFrom(sourcePath);
            else if (_storage.DirectoryExists(sourcePath))
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

    /// <inheritdoc />
    public async Task<string> CopyFromAsync(string sourcePath, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sourcePath);
        var sw = Stopwatch.StartNew();
        try {
            string dest;
            if (_storage.FileExists(sourcePath))
                dest = await CopyFileFromAsync(sourcePath, ct).ConfigureAwait(false);
            else if (_storage.DirectoryExists(sourcePath))
                dest = await CopyDirectoryFromAsync(sourcePath, ct).ConfigureAwait(false);
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

    /// <inheritdoc />
    public string AppendToFile(string path, ReadOnlyMemory<byte> data)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        var sw = Stopwatch.StartNew();
        try {
            var safePath = EnsurePathWithinSession(path);
            if (!_storage.FileExists(safePath))
                throw new FileNotFoundException($"File not found: {safePath}", safePath);

            ValidateFileSize(data.Length);
            using var fs = _storage.OpenAppend(safePath);
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

    /// <inheritdoc />
    public string AppendToFile(string path, string text)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        ArgumentHelpers.ThrowIfNull(text);
        var sw = Stopwatch.StartNew();
        try {
            var safePath = EnsurePathWithinSession(path);
            if (!_storage.FileExists(safePath))
                throw new FileNotFoundException($"File not found: {safePath}", safePath);

            var byteCount = Utf8NoBom.GetByteCount(text);
            ValidateFileSize(byteCount);
            _storage.AppendAllText(safePath, text, Utf8NoBom);
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

    /// <inheritdoc />
    public async Task<string> AppendToFileAsync(string path, ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        var sw = Stopwatch.StartNew();
        try {
            var safePath = EnsurePathWithinSession(path);
            if (!_storage.FileExists(safePath))
                throw new FileNotFoundException($"File not found: {safePath}", safePath);

            ValidateFileSize(data.Length);
            await _storage.AppendAllTextAsync(safePath, Utf8NoBom.GetString(data.ToArray()), Utf8NoBom, ct).ConfigureAwait(false);
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

    /// <inheritdoc />
    public async Task<string> AppendToFileAsync(string path, string text, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        ArgumentHelpers.ThrowIfNull(text);
        var sw = Stopwatch.StartNew();
        try {
            var safePath = EnsurePathWithinSession(path);
            if (!_storage.FileExists(safePath))
                throw new FileNotFoundException($"File not found: {safePath}", safePath);

            var byteCount = Utf8NoBom.GetByteCount(text);
            ValidateFileSize(byteCount);
            await _storage.AppendAllTextAsync(safePath, text, Utf8NoBom, ct).ConfigureAwait(false);
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
        var size = _storage.GetFileLength(sourcePath);
        ValidateFileSize(size);
        var destName = Path.GetFileName(sourcePath);
        var dest = Path.Combine(SessionDirectory, destName);
        if (_storage.FileExists(dest)) {
            var ext = Path.GetExtension(destName);
            var stem = Path.GetFileNameWithoutExtension(destName);
            dest = Path.Combine(SessionDirectory, $"{stem}_{Guid.NewGuid():N}{ext}");
        }

        dest = EnsurePathWithinSession(dest);
        _storage.CopyFile(sourcePath, dest);
        _files.Add(dest);
        Interlocked.Add(ref _totalBytesUsed, size);
        FileCreated?.Invoke(dest);
        return dest;
    }

    private string CopyDirectoryFrom(string sourcePath)
    {
        var dirName = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var dest = Path.Combine(SessionDirectory, dirName);
        if (_storage.DirectoryExists(dest))
            dest = Path.Combine(SessionDirectory, $"{dirName}_{Guid.NewGuid():N}");

        dest = EnsurePathWithinSession(dest);
        CopyDirectoryRecursive(sourcePath, dest);
        _directories.Add(dest);
        DirectoryCreated?.Invoke(dest);
        return dest;
    }

    private async Task<string> CopyFileFromAsync(string sourcePath, CancellationToken ct)
    {
        var size = _storage.GetFileLength(sourcePath);
        ValidateFileSize(size);
        var destName = Path.GetFileName(sourcePath);
        var dest = Path.Combine(SessionDirectory, destName);
        if (_storage.FileExists(dest)) {
            var ext = Path.GetExtension(destName);
            var stem = Path.GetFileNameWithoutExtension(destName);
            dest = Path.Combine(SessionDirectory, $"{stem}_{Guid.NewGuid():N}{ext}");
        }

        dest = EnsurePathWithinSession(dest);
        await _storage.CopyFileAsync(sourcePath, dest, ct).ConfigureAwait(false);
        _files.Add(dest);
        Interlocked.Add(ref _totalBytesUsed, size);
        FileCreated?.Invoke(dest);
        return dest;
    }

    private async Task<string> CopyDirectoryFromAsync(string sourcePath, CancellationToken ct)
    {
        var dirName = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var dest = Path.Combine(SessionDirectory, dirName);
        if (_storage.DirectoryExists(dest))
            dest = Path.Combine(SessionDirectory, $"{dirName}_{Guid.NewGuid():N}");

        dest = EnsurePathWithinSession(dest);
        await CopyDirectoryRecursiveAsync(sourcePath, dest, ct).ConfigureAwait(false);
        _directories.Add(dest);
        DirectoryCreated?.Invoke(dest);
        return dest;
    }

    private async Task CopyDirectoryRecursiveAsync(string src, string dst, CancellationToken ct)
    {
        _storage.CreateDirectory(dst);
        foreach (var entry in _storage.EnumerateEntries(src)) {
            ct.ThrowIfCancellationRequested();
            if (entry.IsDirectory) {
                var destSub = Path.Combine(dst, Path.GetFileName(entry.FullPath));
                _directories.Add(destSub);
                DirectoryCreated?.Invoke(destSub);
                await CopyDirectoryRecursiveAsync(entry.FullPath, destSub, ct).ConfigureAwait(false);
            }
            else {
                ValidateFileSize(entry.Length);
                var destFile = Path.Combine(dst, Path.GetFileName(entry.FullPath));
                await _storage.CopyFileAsync(entry.FullPath, destFile, ct).ConfigureAwait(false);
                _files.Add(destFile);
                Interlocked.Add(ref _totalBytesUsed, entry.Length);
                FileCreated?.Invoke(destFile);
            }
        }
    }

    private void CopyDirectoryRecursive(string src, string dst)
    {
        _storage.CreateDirectory(dst);
        foreach (var entry in _storage.EnumerateEntries(src)) {
            if (entry.IsDirectory) {
                var destSub = Path.Combine(dst, Path.GetFileName(entry.FullPath));
                _directories.Add(destSub);
                DirectoryCreated?.Invoke(destSub);
                CopyDirectoryRecursive(entry.FullPath, destSub);
            }
            else {
                ValidateFileSize(entry.Length);
                var destFile = Path.Combine(dst, Path.GetFileName(entry.FullPath));
                _storage.CopyFile(entry.FullPath, destFile);
                _files.Add(destFile);
                Interlocked.Add(ref _totalBytesUsed, entry.Length);
                FileCreated?.Invoke(destFile);
            }
        }
    }

#endregion

#region Files

    /// <inheritdoc />
    public string GetFilePath(string? name = null)
    {
        ThrowIfDisposed();
        return ResolvePath(name, false);
    }

    /// <inheritdoc />
    public string TouchFile(string? name = null)
    {
        ThrowIfDisposed();
        var stopwatch = Stopwatch.StartNew();
        try {
            var path = ResolvePath(name, false);
            _storage.TouchFile(path);
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

    /// <inheritdoc />
    public string CreateFile(string text)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNull(text);
        var stopwatch = Stopwatch.StartNew();
        try {
            var sizeBytes = Utf8NoBom.GetByteCount(text);
            ValidateFileSize(sizeBytes);
            var path = ResolvePath(null, false);
            _storage.WriteAllText(path, text, Utf8NoBom);
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

    /// <inheritdoc />
    public string CreateFile(ReadOnlyMemory<byte> data, string? name = null)
    {
        ThrowIfDisposed();
        var stopwatch = Stopwatch.StartNew();
        try {
            ValidateFileSize(data.Length);
            var path = ResolvePath(name, false);
            _storage.WriteAllBytes(path, data.ToArray());
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

    /// <inheritdoc />
    public string CreateFile(Stream data, string? name = null)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNull(data);
        OperationHelpers.ThrowIfNotReadable(data, "Input stream must be readable to create temp file.");
        var stopwatch = Stopwatch.StartNew();
        try {
            ValidateFileSize(data.Length);
            var path = ResolvePath(name, false);
            using var dest = _storage.OpenCreate(path);
            data.CopyTo(dest);
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

    /// <inheritdoc />
    public async Task<string> CreateFileAsync(string text, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNull(text);
        var stopwatch = Stopwatch.StartNew();
        try {
            var sizeBytes = Utf8NoBom.GetByteCount(text);
            ValidateFileSize(sizeBytes);
            var path = ResolvePath(null, false);
            await _storage.WriteAllTextAsync(path, text, Utf8NoBom, ct).ConfigureAwait(false);
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

    /// <inheritdoc />
    public async Task<string> CreateFileAsync(ReadOnlyMemory<byte> data, string? name = null, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        var stopwatch = Stopwatch.StartNew();
        try {
            ValidateFileSize(data.Length);
            var path = ResolvePath(name, false);
            await _storage.WriteAllBytesAsync(path, data.ToArray(), ct).ConfigureAwait(false);
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

    /// <inheritdoc />
    public async Task<string> CreateFileAsync(Stream data, string? name = null, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNull(data);
        OperationHelpers.ThrowIfNotReadable(data, "Input stream must be readable to create temp file.");
        var stopwatch = Stopwatch.StartNew();
        try {
            ValidateFileSize(data.Length);
            var path = ResolvePath(name, false);
            await _storage.CopyStreamToFileAsync(data, path, ct).ConfigureAwait(false);
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

    /// <inheritdoc />
    public string GetDirectoryPath(string? name = null)
    {
        ThrowIfDisposed();
        return ResolvePath(name, true);
    }

#endregion

#region Directories

    /// <inheritdoc />
    public string CreateDirectory(string? name = null)
    {
        ThrowIfDisposed();
        var stopwatch = Stopwatch.StartNew();
        try {
            var path = ResolvePath(name, true);
            _storage.CreateDirectory(path);
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

    /// <inheritdoc />
    public Task<string> CreateDirectoryAsync(string? name = null, CancellationToken ct = default) => Task.FromResult(CreateDirectory(name));

#endregion

#region Disposal

    /// <summary>
    /// Deletes <see cref="SessionDirectory" /> with retries on transient I/O errors, records metrics, and invokes <c>onDispose</c> from construction. Exceptions during delete
    /// are logged; the session is still considered disposed.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        var stopwatch = Stopwatch.StartNew();
        try {
            DeleteDirectoryWithRetry(SessionDirectory, DisposeRetryCount, DisposeRetryDelayMs);
            _metrics.RecordTiming(Constants.Metrics.DisposeSessionDuration, stopwatch.Elapsed);
            _metrics.IncrementCounter(Constants.Metrics.DisposeSessionSuccess);
        }
        catch (Exception ex) {
            _logger.LogError(
                ex, "Couldn't delete session directory {SessionDirectory} after {Retries} attempts during Dispose. Directory may be orphaned.", SessionDirectory,
                DisposeRetryCount);

            _metrics.RecordError(Constants.Metrics.DisposeSessionDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.DisposeSessionFailure);
        }
        finally {
            _onDispose?.Invoke(SessionDirectory);
        }
    }

    /// <summary>Equivalent to <see cref="Dispose" />; returns a completed value task.</summary>
    public ValueTask DisposeAsync()
    {
        Dispose();
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        return ValueTask.CompletedTask;
#else
        return new(Task.CompletedTask);
#endif
    }

#endregion

#region Helpers

    private IOTempGeneratorContext BuildGeneratorContext()
        => new(
            SessionDirectory, ThrowIfDisposed, ResolvePath, EnsurePathWithinSession, ValidateFileSize, (path, bytes) => {
                _files.Add(path);
                Interlocked.Add(ref _totalBytesUsed, bytes);
                FileCreated?.Invoke(path);
            }, path => {
                _directories.Add(path);
                DirectoryCreated?.Invoke(path);
            }, _options, _logger, _metrics, _storage);

    private string CreateSessionDirectory()
    {
        var name = GenerateName(_options.DirectoryPrefix, _options.DirectorySuffix, _options.DirectoryNamingStrategy);
        var path = Path.Combine(_options.RootDirectory, name);
        _storage.CreateDirectory(path);
        OperationHelpers.ThrowIf(!_storage.DirectoryExists(path), $"Failed to create IO temp session directory: {path}");
        _storage.EnsureDirectoryAccessible(path);
        return path;
    }

    private string ResolvePath(string? name, bool isDirectory)
    {
        if (name is not null) {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
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
        OperationHelpers.ThrowIf(
            _options.MaxFileSizeBytes.HasValue && sizeBytes > _options.MaxFileSizeBytes.Value,
            $"File size {sizeBytes:N0} bytes exceeds the per-file limit of {_options.MaxFileSizeBytes!.Value:N0} bytes.");

        if (_options.MaxFileCount.HasValue && _files.Count >= _options.MaxFileCount.Value) {
            switch (_options.OverflowStrategy) {
                case TempOverflowStrategy.ThrowException:
                    OperationHelpers.ThrowIf(true, $"Session already has {_files.Count} tracked files, which meets the limit of {_options.MaxFileCount.Value}.");
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
                OperationHelpers.ThrowIf(
                    true,
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
        OperationHelpers.ThrowIf(
            _options.MaxFileSizeBytes.HasValue && newSizeBytes > _options.MaxFileSizeBytes.Value,
            $"File size {newSizeBytes:N0} bytes exceeds the per-file limit of {_options.MaxFileSizeBytes!.Value:N0} bytes.");

        if (_options.MaxTotalSizeBytes == null)
            return;

        var delta = newSizeBytes - oldSizeBytes;
        if (delta <= 0)
            return;

        var projected = Interlocked.Read(ref _totalBytesUsed) + delta;
        OperationHelpers.ThrowIf(
            projected > _options.MaxTotalSizeBytes.Value,
            $"Writing {newSizeBytes:N0} bytes (delta: +{delta:N0}) would exceed session total limit of {_options.MaxTotalSizeBytes.Value:N0} bytes (current: {Interlocked.Read(ref _totalBytesUsed):N0} bytes).");
    }

    private void FreeFileCountFor(int requiredSlots)
    {
        var excess = _files.Count - _options.MaxFileCount!.Value + requiredSlots;
        if (excess <= 0)
            return;

        var candidates = _options.OverflowStrategy == TempOverflowStrategy.DeleteOldest
            ? _files.Where(_storage.FileExists).OrderBy(_storage.GetFileCreationTimeUtc).ToList()
            : _files.Where(_storage.FileExists).OrderByDescending(_storage.GetFileLength).ToList();

        var freed = 0;
        foreach (var file in candidates) {
            if (freed >= excess)
                break;

            var size = _storage.FileExists(file) ? _storage.GetFileLength(file) : 0L;
            try {
                _storage.DeleteFile(file);
                _files.Remove(file);
                Interlocked.Add(ref _totalBytesUsed, -size);
                freed++;
                _logger.LogInformation("Deleted {FilePath} ({Size:N0} bytes) to free a file slot in session {SessionDirectory}", file, size, SessionDirectory);
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Failed to delete {FilePath} during file-count overflow cleanup in session {SessionDirectory}", file, SessionDirectory);
            }
        }

        OperationHelpers.ThrowIf(freed < excess, $"Could not free {excess} file slot(s): only freed {freed}. Session file count limit: {_options.MaxFileCount.Value}.");
    }

    private void FreeTotalSpaceFor(long requiredBytes)
    {
        var needed = Interlocked.Read(ref _totalBytesUsed) + requiredBytes - _options.MaxTotalSizeBytes!.Value;
        if (needed <= 0)
            return;

        var candidates = _options.OverflowStrategy == TempOverflowStrategy.DeleteOldest
            ? _storage.EnumerateEntries(SessionDirectory).Where(e => !e.IsDirectory).OrderBy(e => e.CreationTimeUtc).ToList()
            : _storage.EnumerateEntries(SessionDirectory).Where(e => !e.IsDirectory).OrderByDescending(e => e.Length).ToList();

        var freed = 0L;
        foreach (var entry in candidates) {
            if (freed >= needed)
                break;

            try {
                _storage.DeleteFile(entry.FullPath);
                _files.Remove(entry.FullPath);
                Interlocked.Add(ref _totalBytesUsed, -entry.Length);
                freed += entry.Length;
                _logger.LogInformation(
                    "Deleted {FilePath} ({Size:N0} bytes) to make room for overflow in session {SessionDirectory}", entry.FullPath, entry.Length, SessionDirectory);
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Failed to delete {FilePath} during overflow cleanup in session {SessionDirectory}", entry.FullPath, SessionDirectory);
            }
        }

        OperationHelpers.ThrowIf(
            freed < needed,
            $"Could not free sufficient space: needed {needed:N0} bytes freed, freed {freed:N0} bytes. Session total limit: {_options.MaxTotalSizeBytes.Value:N0} bytes.");
    }

    private void DeleteDirectoryWithRetry(string path, int retries, int retryDelayMs)
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
                    _logger.LogDebug("Delete attempt {Attempt}/{Retries} failed for {Path}, retrying in {Delay}ms", attempt, retries, path, retryDelayMs);
                    Thread.Sleep(retryDelayMs);
                }
            }
        }

        if (lastEx != null)
            ExceptionDispatchInfo.Capture(lastEx).Throw();
    }

    private void ThrowIfDisposed() => OperationHelpers.ThrowIfDisposed(_disposed, nameof(IOTempSession));

    private void EnsureRootExistsAndAccessible()
    {
        try {
            if (!_storage.DirectoryExists(_options.RootDirectory))
                ExceptionThrower.ThrowIfDirectoryNotFound(_options.RootDirectory, nameof(_options.RootDirectory));

            _storage.EnsureDirectoryAccessible(_options.RootDirectory);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "IO temp session root directory {RootDirectory} is not accessible", _options.RootDirectory);
            throw;
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