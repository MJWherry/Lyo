using Lyo.Common.Core.Pathing;
using Lyo.Exceptions;
using Lyo.FileSystemWatcher.Enums;
using Lyo.IO.FileSystem;

namespace Lyo.FileSystemWatcher;

/// <summary>
/// <see cref="IFileSystem" /> on local disk that lists through <see cref="LocalFileSystem" /> and watches through <see cref="FileSystemWatcher" />. Does not rewrite
/// snapshot/diff internals.
/// </summary>
public sealed class WatchableLocalFileSystem : IFileSystem
{
    private readonly LocalFileSystem _inner;

    /// <summary>Opens a watchable file system jailed to <paramref name="rootPath" />.</summary>
    public WatchableLocalFileSystem(string rootPath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(rootPath);
        _inner = new(rootPath);
    }

    /// <inheritdoc />
    public string RootPath => _inner.RootPath;

    /// <inheritdoc />
    public PathStyle PathStyle => _inner.PathStyle;

    /// <inheritdoc />
    public FileSystemCapabilities Capabilities => _inner.Capabilities | FileSystemCapabilities.Watch;

    /// <inheritdoc />
    public IFileSystemWatch Watch(string path, bool recursive = true)
    {
        var jailed = FileSystemPath.Jail(PathStyle, RootPath, path);
        Directory.CreateDirectory(jailed);
        return new LocalWatch(jailed, recursive);
    }

    /// <inheritdoc />
    public bool DirectoryExists(string path) => _inner.DirectoryExists(path);

    /// <inheritdoc />
    public Task<bool> DirectoryExistsAsync(string path, CancellationToken ct = default) => _inner.DirectoryExistsAsync(path, ct);

    /// <inheritdoc />
    public void CreateDirectory(string path) => _inner.CreateDirectory(path);

    /// <inheritdoc />
    public Task CreateDirectoryAsync(string path, CancellationToken ct = default) => _inner.CreateDirectoryAsync(path, ct);

    /// <inheritdoc />
    public void DeleteDirectory(string path, bool recursive = true) => _inner.DeleteDirectory(path, recursive);

    /// <inheritdoc />
    public Task DeleteDirectoryAsync(string path, bool recursive = true, CancellationToken ct = default) => _inner.DeleteDirectoryAsync(path, recursive, ct);

    /// <inheritdoc />
    public IReadOnlyList<FileSystemEntry> ListDirectory(string path) => _inner.ListDirectory(path);

    /// <inheritdoc />
    public Task<IReadOnlyList<FileSystemEntry>> ListDirectoryAsync(string path, CancellationToken ct = default) => _inner.ListDirectoryAsync(path, ct);

    /// <inheritdoc />
    public bool FileExists(string path) => _inner.FileExists(path);

    /// <inheritdoc />
    public Task<bool> FileExistsAsync(string path, CancellationToken ct = default) => _inner.FileExistsAsync(path, ct);

    /// <inheritdoc />
    public void DeleteFile(string path) => _inner.DeleteFile(path);

    /// <inheritdoc />
    public Task DeleteFileAsync(string path, CancellationToken ct = default) => _inner.DeleteFileAsync(path, ct);

    /// <inheritdoc />
    public void Move(string source, string dest) => _inner.Move(source, dest);

    /// <inheritdoc />
    public Task MoveAsync(string source, string dest, CancellationToken ct = default) => _inner.MoveAsync(source, dest, ct);

    /// <inheritdoc />
    public void CopyFile(string source, string dest) => _inner.CopyFile(source, dest);

    /// <inheritdoc />
    public Task CopyFileAsync(string source, string dest, CancellationToken ct = default) => _inner.CopyFileAsync(source, dest, ct);

    /// <inheritdoc />
    public Stream OpenRead(string path) => _inner.OpenRead(path);

    /// <inheritdoc />
    public Task<Stream> OpenReadAsync(string path, CancellationToken ct = default) => _inner.OpenReadAsync(path, ct);

    /// <inheritdoc />
    public Stream OpenCreate(string path) => _inner.OpenCreate(path);

    /// <inheritdoc />
    public Task<Stream> OpenCreateAsync(string path, CancellationToken ct = default) => _inner.OpenCreateAsync(path, ct);

    /// <inheritdoc />
    public Stream OpenAppend(string path) => _inner.OpenAppend(path);

    /// <inheritdoc />
    public Task<Stream> OpenAppendAsync(string path, CancellationToken ct = default) => _inner.OpenAppendAsync(path, ct);

    /// <inheritdoc />
    public long GetLength(string path) => _inner.GetLength(path);

    /// <inheritdoc />
    public Task<long> GetLengthAsync(string path, CancellationToken ct = default) => _inner.GetLengthAsync(path, ct);

    /// <inheritdoc />
    public DateTimeOffset GetCreationTimeUtc(string path) => _inner.GetCreationTimeUtc(path);

    /// <inheritdoc />
    public Task<DateTimeOffset> GetCreationTimeUtcAsync(string path, CancellationToken ct = default) => _inner.GetCreationTimeUtcAsync(path, ct);

    /// <inheritdoc />
    public DateTimeOffset GetLastWriteTimeUtc(string path) => _inner.GetLastWriteTimeUtc(path);

    /// <inheritdoc />
    public Task<DateTimeOffset> GetLastWriteTimeUtcAsync(string path, CancellationToken ct = default) => _inner.GetLastWriteTimeUtcAsync(path, ct);

    /// <inheritdoc />
    public void Dispose() => _inner.Dispose();

    private sealed class LocalWatch : IFileSystemWatch
    {
        private readonly FileSystemWatcher _watcher;

        public LocalWatch(string path, bool recursive)
        {
            _watcher = new(path, recursive);
            _watcher.OnAnyChange += OnAnyChange;
        }

        public event EventHandler<FileSystemChange>? Changed;

        public void Dispose()
        {
            _watcher.OnAnyChange -= OnAnyChange;
            _watcher.Dispose();
        }

        private void OnAnyChange(object? sender, FileSystemChangeInfo info)
        {
            var kind = info.ChangeType switch {
                ChangeTypeEnum.Created => FileSystemChangeKind.Created,
                ChangeTypeEnum.Deleted => FileSystemChangeKind.Deleted,
                ChangeTypeEnum.Changed => FileSystemChangeKind.Changed,
                ChangeTypeEnum.Moved => FileSystemChangeKind.Moved,
                ChangeTypeEnum.Renamed => FileSystemChangeKind.Renamed,
                _ => FileSystemChangeKind.Changed
            };
            Changed?.Invoke(this, new FileSystemChange(kind, info.IsDirectory, info.OldPath, info.NewPath));
        }
    }
}
