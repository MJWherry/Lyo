using Lyo.Common.Core.Identifiers;
using Lyo.Common.Core.Pathing;
using Lyo.Exceptions;

namespace Lyo.IO.FileSystem;

/// <summary>
/// In-memory <see cref="IFileSystem" />. Use it in Blazor WASM, unit tests, or anywhere without a writable disk. Data lasts for this instance's lifetime.
/// </summary>
/// <remarks>
/// Storage is a hybrid: a flat map from full path to node gives O(1) point lookups, and each directory node holds the set of its immediate children so listing costs
/// O(children) and a subtree walk costs O(subtree) rather than a scan of the whole store. A single <see cref="ReaderWriterLockSlim" /> guards both, which also makes the
/// compound operations (create the parent chain, move a subtree, append to a file) atomic against each other.
/// </remarks>
public sealed class MemoryFileSystem : IFileSystem
{
    private readonly Dictionary<string, Node> _nodes = new(StringComparer.Ordinal);
    private readonly ReaderWriterLockSlim _lock = new();
    private bool _disposed;

    /// <summary>Opens an in-memory store with a unique synthetic <see cref="RootPath" />.</summary>
    public MemoryFileSystem()
        : this($"/mem/lyo-{LyoGuid.CreateV7():N}") { }

    /// <summary>Opens an in-memory store jailed to <paramref name="rootPath" /> (POSIX).</summary>
    public MemoryFileSystem(string rootPath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(rootPath);
        RootPath = FileSystemPath.NormalizeRoot(PathStyle.Posix, rootPath);
        _nodes[RootPath] = new(isDirectory: true);
    }

    /// <inheritdoc />
    public string RootPath { get; }

    /// <inheritdoc />
    public PathStyle PathStyle => PathStyle.Posix;

    /// <inheritdoc />
    public FileSystemCapabilities Capabilities
        => FileSystemCapabilities.Read | FileSystemCapabilities.Write | FileSystemCapabilities.CreateDirectory | FileSystemCapabilities.Delete
           | FileSystemCapabilities.Move | FileSystemCapabilities.MoveDirectory | FileSystemCapabilities.Copy | FileSystemCapabilities.Append;

    /// <inheritdoc />
    public IFileSystemWatch? Watch(string path, bool recursive = true) => null;

    /// <inheritdoc />
    public bool DirectoryExists(string path)
    {
        var target = Jail(path);
        _lock.EnterReadLock();
        try {
            return _nodes.TryGetValue(target, out var node) && node.IsDirectory;
        }
        finally {
            _lock.ExitReadLock();
        }
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
        var target = Jail(path);
        _lock.EnterWriteLock();
        try {
            CreateDirectoryLocked(target);
        }
        finally {
            _lock.ExitWriteLock();
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
        var target = Jail(path);
        _lock.EnterWriteLock();
        try {
            if (!_nodes.TryGetValue(target, out var node) || !node.IsDirectory)
                return;

            if (!recursive && node.Children!.Count > 0)
                throw new IOException($"Directory is not empty: {path}");

            RemoveSubtreeLocked(target);
        }
        finally {
            _lock.ExitWriteLock();
        }
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
        var target = Jail(path);
        _lock.EnterReadLock();
        try {
            return SnapshotLocked(target, recursive: false);
        }
        finally {
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<FileSystemEntry>> ListDirectoryAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ListDirectory(path));
    }

    /// <inheritdoc />
    public IAsyncEnumerable<FileSystemEntry> EnumerateDirectoryAsync(string path, bool recursive = false, CancellationToken ct = default)
    {
        var target = Jail(path);
        _lock.EnterReadLock();
        try {
            // Snapshot under the lock rather than streaming live nodes, so a concurrent write cannot invalidate the walk mid-iteration.
            return SynchronousAsyncEnumerable.Create(SnapshotLocked(target, recursive), ct);
        }
        finally {
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public bool FileExists(string path)
    {
        var target = Jail(path);
        _lock.EnterReadLock();
        try {
            return _nodes.TryGetValue(target, out var node) && !node.IsDirectory;
        }
        finally {
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public Task<bool> FileExistsAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(FileExists(path));
    }

    /// <inheritdoc />
    public FileSystemEntry? GetEntry(string path)
    {
        var target = Jail(path);
        _lock.EnterReadLock();
        try {
            return _nodes.TryGetValue(target, out var node) ? ToEntry(target, node) : null;
        }
        finally {
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public Task<FileSystemEntry?> GetEntryAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(GetEntry(path));
    }

    /// <inheritdoc />
    public void DeleteFile(string path)
    {
        var target = Jail(path);
        _lock.EnterWriteLock();
        try {
            if (!_nodes.TryGetValue(target, out var node) || node.IsDirectory)
                return;

            _nodes.Remove(target);
            UnlinkChildLocked(target);
        }
        finally {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public Task DeleteFileAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        DeleteFile(path);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Move(string source, string dest, bool overwrite = false)
    {
        var src = Jail(source);
        var dst = Jail(dest);
        _lock.EnterWriteLock();
        try {
            if (!_nodes.ContainsKey(src))
                throw new FileNotFoundException($"Source not found: {source}", source);

            if (_nodes.ContainsKey(dst)) {
                if (!overwrite)
                    throw new IOException($"Destination already exists: {dest}");

                RemoveSubtreeLocked(dst);
            }

            EnsureParentDirectoryLocked(dst);
            MoveSubtreeLocked(src, dst);
        }
        finally {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public Task MoveAsync(string source, string dest, bool overwrite = false, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Move(source, dest, overwrite);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void CopyFile(string source, string dest, bool overwrite = false)
    {
        var src = Jail(source);
        var dst = Jail(dest);
        _lock.EnterWriteLock();
        try {
            if (!_nodes.TryGetValue(src, out var node) || node.IsDirectory)
                throw new FileNotFoundException($"Source file not found: {source}", source);

            if (!overwrite && _nodes.ContainsKey(dst))
                throw new IOException($"Destination already exists: {dest}");

            WriteFileLocked(dst, node.Content?.ToArray() ?? []);
        }
        finally {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public Task CopyFileAsync(string source, string dest, bool overwrite = false, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        CopyFile(source, dest, overwrite);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Stream OpenRead(string path)
    {
        var target = Jail(path);
        _lock.EnterReadLock();
        try {
            if (!_nodes.TryGetValue(target, out var node) || node.IsDirectory)
                throw new FileNotFoundException($"File not found: {path}", path);

            return new MemoryStream(node.Content ?? [], writable: false);
        }
        finally {
            _lock.ExitReadLock();
        }
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
        var target = Jail(path);
        _lock.EnterWriteLock();
        try {
            ThrowIfDirectoryLocked(target, path);
            WriteFileLocked(target, []);
        }
        finally {
            _lock.ExitWriteLock();
        }

        // Commit by path, not by holding the node captured at open time: a concurrent delete-and-recreate must not receive this stream's bytes.
        return new CommitOnCloseStream(bytes => Commit(target, bytes, append: false));
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
        var target = Jail(path);
        _lock.EnterWriteLock();
        try {
            ThrowIfDirectoryLocked(target, path);
            if (!_nodes.ContainsKey(target))
                WriteFileLocked(target, []);
        }
        finally {
            _lock.ExitWriteLock();
        }

        // The existing content is read at commit time, under the write lock. Reading it at open time loses whatever another writer appended in between.
        return new CommitOnCloseStream(bytes => Commit(target, bytes, append: true));
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
        var target = Jail(path);
        _lock.EnterReadLock();
        try {
            if (!_nodes.TryGetValue(target, out var node) || node.IsDirectory)
                throw new FileNotFoundException($"File not found: {path}", path);

            return node.Content?.Length ?? 0;
        }
        finally {
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public Task<long> GetLengthAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(GetLength(path));
    }

    /// <inheritdoc />
    public DateTimeOffset GetCreationTimeUtc(string path) => StatOrThrow(path).CreatedAt;

    /// <inheritdoc />
    public Task<DateTimeOffset> GetCreationTimeUtcAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(GetCreationTimeUtc(path));
    }

    /// <inheritdoc />
    public DateTimeOffset GetLastWriteTimeUtc(string path) => StatOrThrow(path).LastWrite;

    /// <inheritdoc />
    public Task<DateTimeOffset> GetLastWriteTimeUtcAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(GetLastWriteTimeUtc(path));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _lock.Dispose();
    }

    private string Jail(string path) => FileSystemPath.Jail(PathStyle.Posix, RootPath, path);

    private Node StatOrThrow(string path)
    {
        var target = Jail(path);
        _lock.EnterReadLock();
        try {
            return _nodes.TryGetValue(target, out var node) ? node : throw new FileNotFoundException($"Path not found: {path}", path);
        }
        finally {
            _lock.ExitReadLock();
        }
    }

    private void Commit(string target, byte[] bytes, bool append)
    {
        _lock.EnterWriteLock();
        try {
            var existing = append && _nodes.TryGetValue(target, out var node) && !node.IsDirectory ? node.Content ?? [] : [];
            if (existing.Length == 0) {
                WriteFileLocked(target, bytes);
                return;
            }

            var combined = new byte[existing.Length + bytes.Length];
            Buffer.BlockCopy(existing, 0, combined, 0, existing.Length);
            Buffer.BlockCopy(bytes, 0, combined, existing.Length, bytes.Length);
            WriteFileLocked(target, combined);
        }
        finally {
            _lock.ExitWriteLock();
        }
    }

    private void ThrowIfDirectoryLocked(string target, string reportedPath)
    {
        if (_nodes.TryGetValue(target, out var node) && node.IsDirectory)
            throw new IOException($"Path is a directory: {reportedPath}");
    }

    /// <summary>Replaces the file at <paramref name="target" />, creating the parent chain and preserving the original creation time.</summary>
    private void WriteFileLocked(string target, byte[] content)
    {
        EnsureParentDirectoryLocked(target);
        _nodes.TryGetValue(target, out var previous);
        _nodes[target] = new(isDirectory: false, previous is { IsDirectory: false } ? previous.CreatedAt : null) { Content = content };
        LinkChildLocked(target);
    }

    private void CreateDirectoryLocked(string target)
    {
        if (_nodes.TryGetValue(target, out var existing)) {
            if (!existing.IsDirectory)
                throw new IOException($"Cannot create directory '{target}': the path is a file.");

            return;
        }

        EnsureParentDirectoryLocked(target);
        _nodes[target] = new(isDirectory: true);
        LinkChildLocked(target);
    }

    private void EnsureParentDirectoryLocked(string target)
    {
        // Jail guarantees target is at or under RootPath, so anything strictly longer has a parent that is also in the jail.
        if (target.Length > RootPath.Length && ParentOf(target) is { } parent)
            CreateDirectoryLocked(parent);
    }

    private void LinkChildLocked(string target)
    {
        if (ParentOf(target) is { } parent && _nodes.TryGetValue(parent, out var node))
            node.Children?.Add(target);
    }

    private void UnlinkChildLocked(string target)
    {
        if (ParentOf(target) is { } parent && _nodes.TryGetValue(parent, out var node))
            node.Children?.Remove(target);
    }

    private void RemoveSubtreeLocked(string target)
    {
        var pending = new Stack<string>();
        pending.Push(target);
        while (pending.Count > 0) {
            var current = pending.Pop();
            if (!_nodes.TryGetValue(current, out var node))
                continue;

            _nodes.Remove(current);
            if (node.Children is null)
                continue;

            foreach (var child in node.Children)
                pending.Push(child);
        }

        UnlinkChildLocked(target);
    }

    private void MoveSubtreeLocked(string src, string dst)
    {
        var items = CollectSubtreeLocked(src);
        foreach (var (path, _) in items)
            _nodes.Remove(path);

        UnlinkChildLocked(src);

        // Breadth-first order puts every parent ahead of its children, so LinkChildLocked always finds a rebased parent already in place.
        foreach (var (path, node) in items) {
            var rebased = dst + path.Substring(src.Length);
            _nodes[rebased] = new(node.IsDirectory, node.CreatedAt) { Content = node.Content, LastWrite = node.LastWrite };
            LinkChildLocked(rebased);
        }
    }

    private List<(string Path, Node Node)> CollectSubtreeLocked(string target)
    {
        List<(string Path, Node Node)> items = [];
        var pending = new Queue<string>();
        pending.Enqueue(target);
        while (pending.Count > 0) {
            var current = pending.Dequeue();
            if (!_nodes.TryGetValue(current, out var node))
                continue;

            items.Add((current, node));
            if (node.Children is null)
                continue;

            foreach (var child in node.Children)
                pending.Enqueue(child);
        }

        return items;
    }

    private IReadOnlyList<FileSystemEntry> SnapshotLocked(string target, bool recursive)
    {
        if (!_nodes.TryGetValue(target, out var root) || !root.IsDirectory)
            return [];

        List<FileSystemEntry> entries = [];
        var pending = new Queue<string>(root.Children!);
        while (pending.Count > 0) {
            var current = pending.Dequeue();
            if (!_nodes.TryGetValue(current, out var node))
                continue;

            entries.Add(ToEntry(current, node));
            if (!recursive || node.Children is null)
                continue;

            foreach (var child in node.Children)
                pending.Enqueue(child);
        }

        return entries;
    }

    private static string? ParentOf(string normalizedPath)
    {
        if (normalizedPath.Length <= 1)
            return null;

        var idx = normalizedPath.LastIndexOf('/');
        return idx switch {
            < 0 => null,
            0 => "/",
            _ => normalizedPath.Substring(0, idx)
        };
    }

    private static FileSystemEntry ToEntry(string fullPath, Node node)
        => new(fullPath, PathHelpers.GetFileName(PathStyle.Posix, fullPath), node.IsDirectory, node.Content?.Length ?? 0, node.CreatedAt, node.LastWrite);

    /// <summary>One file or directory. Directories carry the set of their immediate children by full path, which is the child index.</summary>
    private sealed class Node(bool isDirectory, DateTimeOffset? createdAt = null)
    {
        public bool IsDirectory { get; } = isDirectory;

        public HashSet<string>? Children { get; } = isDirectory ? new(StringComparer.Ordinal) : null;

        public byte[]? Content { get; set; }

        public DateTimeOffset CreatedAt { get; } = createdAt ?? DateTimeOffset.UtcNow;

        public DateTimeOffset LastWrite { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary><see cref="MemoryStream" /> that hands its buffer to <paramref name="commit" /> on close or dispose.</summary>
    private sealed class CommitOnCloseStream(Action<byte[]> commit) : MemoryStream
    {
        private bool _committed;

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_committed) {
                _committed = true;
                commit(ToArray());
            }

            base.Dispose(disposing);
        }

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        public override ValueTask DisposeAsync()
        {
            if (!_committed) {
                _committed = true;
                commit(ToArray());
            }

            return base.DisposeAsync();
        }
#endif
    }
}
