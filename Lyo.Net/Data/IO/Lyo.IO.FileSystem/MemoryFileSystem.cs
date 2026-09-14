using System.Collections.Concurrent;
using Lyo.Common.Core.Pathing;
using Lyo.Exceptions;

namespace Lyo.IO.FileSystem;

/// <summary>
/// In-memory <see cref="IFileSystem" /> on a <see cref="ConcurrentDictionary{TKey,TValue}" />. Use it in Blazor WASM, unit tests, or anywhere without a writable disk. Data
/// lasts for this instance's lifetime.
/// </summary>
public sealed class MemoryFileSystem : IFileSystem
{
    private readonly ConcurrentDictionary<string, Entry> _store = new(StringComparer.Ordinal);

    /// <summary>Opens an in-memory store with a unique synthetic <see cref="RootPath" />.</summary>
    public MemoryFileSystem()
        : this($"/mem/lyo-{Guid.NewGuid():N}") { }

    /// <summary>Opens an in-memory store jailed to <paramref name="rootPath" /> (POSIX).</summary>
    public MemoryFileSystem(string rootPath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(rootPath);
        RootPath = FileSystemPath.NormalizeRoot(PathStyle.Posix, rootPath);
        _store[RootPath] = new() { IsDirectory = true };
    }

    /// <inheritdoc />
    public string RootPath { get; }

    /// <inheritdoc />
    public PathStyle PathStyle => PathStyle.Posix;

    /// <inheritdoc />
    public FileSystemCapabilities Capabilities
        => FileSystemCapabilities.Read | FileSystemCapabilities.Write | FileSystemCapabilities.CreateDirectory | FileSystemCapabilities.Delete
           | FileSystemCapabilities.Move | FileSystemCapabilities.Copy;

    /// <inheritdoc />
    public IFileSystemWatch? Watch(string path, bool recursive = true) => null;

    /// <inheritdoc />
    public bool DirectoryExists(string path)
    {
        var n = Jail(path);
        return _store.TryGetValue(n, out var e) && e.IsDirectory;
    }

    /// <inheritdoc />
    public Task<bool> DirectoryExistsAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(DirectoryExists(path));
    }

    /// <inheritdoc />
    public void CreateDirectory(string path)
    {
        var n = Jail(path);
        var segments = n.Split('/');
        var current = string.Empty;
        foreach (var seg in segments) {
            if (seg.Length == 0) {
                current = "/";
                continue;
            }

            current = current == "/" ? "/" + seg : current + "/" + seg;
            if (_store.TryGetValue(current, out var existing)) {
                if (!existing.IsDirectory)
                    throw new IOException($"Cannot create directory '{path}': '{current}' is a file.");

                continue;
            }

            _store.TryAdd(current, new() { IsDirectory = true });
        }
    }

    /// <inheritdoc />
    public Task CreateDirectoryAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        CreateDirectory(path);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void DeleteDirectory(string path, bool recursive = true)
    {
        var n = Jail(path);
        if (_store.TryGetValue(n, out var existing) && !existing.IsDirectory)
            return;

        var prefix = n + "/";
        if (!recursive) {
            foreach (var _ in EnumerateImmediate(n))
                throw new IOException($"Directory is not empty: {path}");
        }

        foreach (var key in _store.Keys.Where(k => k == n || k.StartsWith(prefix, StringComparison.Ordinal)).ToList())
            _store.TryRemove(key, out _);
    }

    /// <inheritdoc />
    public Task DeleteDirectoryAsync(string path, bool recursive = true, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        DeleteDirectory(path, recursive);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public IReadOnlyList<FileSystemEntry> ListDirectory(string path)
    {
        var n = Jail(path);
        List<FileSystemEntry> entries = [];
        foreach (var (fullPath, entry) in EnumerateImmediate(n))
            entries.Add(ToEntry(fullPath, entry));

        return entries;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<FileSystemEntry>> ListDirectoryAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ListDirectory(path));
    }

    /// <inheritdoc />
    public bool FileExists(string path)
    {
        var n = Jail(path);
        return _store.TryGetValue(n, out var e) && !e.IsDirectory;
    }

    /// <inheritdoc />
    public Task<bool> FileExistsAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(FileExists(path));
    }

    /// <inheritdoc />
    public void DeleteFile(string path)
    {
        var n = Jail(path);
        if (_store.TryGetValue(n, out var e) && !e.IsDirectory)
            _store.TryRemove(n, out _);
    }

    /// <inheritdoc />
    public Task DeleteFileAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        DeleteFile(path);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Move(string source, string dest)
    {
        var srcN = Jail(source);
        var dstN = Jail(dest);
        if (!_store.TryGetValue(srcN, out var entry))
            throw new FileNotFoundException($"Source not found: {source}", source);

        if (entry.IsDirectory) {
            EnsureParentDirectory(dstN);
            if (_store.TryGetValue(dstN, out var destExisting) && !destExisting.IsDirectory)
                throw new IOException($"Destination is a file: {dest}");

            var srcPrefix = srcN + "/";
            foreach (var key in _store.Keys.Where(k => k == srcN || k.StartsWith(srcPrefix, StringComparison.Ordinal)).ToList()) {
                if (!_store.TryRemove(key, out var moved) || moved == null)
                    continue;

                var suffix = key.Length == srcN.Length ? "" : key.Substring(srcN.Length);
                _store[dstN + suffix] = moved;
            }

            return;
        }

        _store.TryRemove(srcN, out _);
        EnsureParentDirectory(dstN);
        _store[dstN] = entry;
    }

    /// <inheritdoc />
    public Task MoveAsync(string source, string dest, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Move(source, dest);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void CopyFile(string source, string dest)
    {
        var srcN = Jail(source);
        var dstN = Jail(dest);
        if (!_store.TryGetValue(srcN, out var entry) || entry.IsDirectory)
            throw new FileNotFoundException($"Source file not found: {source}", source);

        EnsureParentDirectory(dstN);
        _store[dstN] = new() { IsDirectory = false, Content = entry.Content?.ToArray(), CreatedAt = DateTimeOffset.UtcNow, LastWrite = DateTimeOffset.UtcNow };
    }

    /// <inheritdoc />
    public Task CopyFileAsync(string source, string dest, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        CopyFile(source, dest);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Stream OpenRead(string path)
    {
        var n = Jail(path);
        if (!_store.TryGetValue(n, out var entry) || entry.IsDirectory)
            throw new FileNotFoundException($"File not found: {path}", path);

        return new MemoryStream(entry.Content ?? [], writable: false);
    }

    /// <inheritdoc />
    public Task<Stream> OpenReadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(OpenRead(path));
    }

    /// <inheritdoc />
    public Stream OpenCreate(string path)
    {
        var n = Jail(path);
        EnsureParentDirectory(n);
        var entry = new Entry { IsDirectory = false, Content = [] };
        _store[n] = entry;
        return new CommitOnCloseStream(bytes => {
            entry.Content = bytes;
            entry.LastWrite = DateTimeOffset.UtcNow;
        });
    }

    /// <inheritdoc />
    public Task<Stream> OpenCreateAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(OpenCreate(path));
    }

    /// <inheritdoc />
    public Stream OpenAppend(string path)
    {
        var n = Jail(path);
        if (_store.TryGetValue(n, out var existingDir) && existingDir.IsDirectory)
            throw new IOException($"Cannot append to a directory: {path}");

        EnsureParentDirectory(n);
        var entry = _store.GetOrAdd(n, _ => new() { IsDirectory = false, Content = [] });
        if (entry.IsDirectory)
            throw new IOException($"Cannot append to a directory: {path}");

        var existing = entry.Content ?? [];
        return new CommitOnCloseStream(bytes => {
            var combined = new byte[existing.Length + bytes.Length];
            Buffer.BlockCopy(existing, 0, combined, 0, existing.Length);
            Buffer.BlockCopy(bytes, 0, combined, existing.Length, bytes.Length);
            entry.Content = combined;
            entry.LastWrite = DateTimeOffset.UtcNow;
        });
    }

    /// <inheritdoc />
    public Task<Stream> OpenAppendAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(OpenAppend(path));
    }

    /// <inheritdoc />
    public long GetLength(string path)
    {
        var n = Jail(path);
        if (!_store.TryGetValue(n, out var e) || e.IsDirectory)
            throw new FileNotFoundException($"File not found: {path}", path);

        return e.Content?.Length ?? 0;
    }

    /// <inheritdoc />
    public Task<long> GetLengthAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(GetLength(path));
    }

    /// <inheritdoc />
    public DateTimeOffset GetCreationTimeUtc(string path)
    {
        var n = Jail(path);
        if (!_store.TryGetValue(n, out var e))
            throw new FileNotFoundException($"Path not found: {path}", path);

        return e.CreatedAt;
    }

    /// <inheritdoc />
    public Task<DateTimeOffset> GetCreationTimeUtcAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(GetCreationTimeUtc(path));
    }

    /// <inheritdoc />
    public DateTimeOffset GetLastWriteTimeUtc(string path)
    {
        var n = Jail(path);
        if (!_store.TryGetValue(n, out var e))
            throw new FileNotFoundException($"Path not found: {path}", path);

        return e.LastWrite;
    }

    /// <inheritdoc />
    public Task<DateTimeOffset> GetLastWriteTimeUtcAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(GetLastWriteTimeUtc(path));
    }

    /// <inheritdoc />
    public void Dispose() { }

    private string Jail(string path) => FileSystemPath.Jail(PathStyle.Posix, RootPath, path);

    private IEnumerable<(string Path, Entry Entry)> EnumerateImmediate(string normalizedDir)
    {
        var prefix = normalizedDir == "/" ? "/" : normalizedDir + "/";
        foreach (var kvp in _store) {
            if (!kvp.Key.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            var rel = kvp.Key[prefix.Length..];
            if (rel.Length == 0 || rel.Contains('/'))
                continue;

            yield return (kvp.Key, kvp.Value);
        }
    }

    private static string? ParentOf(string normalizedPath)
    {
        var idx = normalizedPath.LastIndexOf('/');
        return idx <= 0 ? null : normalizedPath[..idx];
    }

    private void EnsureParentDirectory(string normalizedPath)
    {
        var parent = ParentOf(normalizedPath);
        if (parent != null)
            CreateDirectory(parent);
    }

    private static FileSystemEntry ToEntry(string fullPath, Entry entry)
    {
        var name = PathHelpers.GetFileName(PathStyle.Posix, fullPath);
        return new(fullPath, name, entry.IsDirectory, entry.Content?.Length ?? 0, entry.CreatedAt, entry.LastWrite);
    }

    private sealed class Entry
    {
        public bool IsDirectory { get; init; }

        public byte[]? Content { get; set; }

        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

        public DateTimeOffset LastWrite { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary><see cref="MemoryStream" /> that writes its buffer back into the store on close or dispose.</summary>
    private sealed class CommitOnCloseStream : MemoryStream
    {
        private readonly Action<byte[]> _commit;
        private bool _committed;

        public CommitOnCloseStream(Action<byte[]> commit) => _commit = commit;

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_committed) {
                _committed = true;
                _commit(ToArray());
            }

            base.Dispose(disposing);
        }

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        public override ValueTask DisposeAsync()
        {
            if (!_committed) {
                _committed = true;
                _commit(ToArray());
            }

            return base.DisposeAsync();
        }
#endif
    }
}
