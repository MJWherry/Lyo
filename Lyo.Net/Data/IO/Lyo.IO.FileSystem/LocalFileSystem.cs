using Lyo.Common.Core.Pathing;
using Lyo.Exceptions;

namespace Lyo.IO.FileSystem;

/// <summary>
/// <see cref="IFileSystem" /> on <see cref="System.IO" /> with <see cref="Common.Core.Pathing.PathStyle.Host" />. Listing only; live watch is <c>WatchableLocalFileSystem</c>
/// .
/// </summary>
public sealed class LocalFileSystem : IFileSystem
{
    private const int BufferSize = 81920;

    /// <summary>Opens a file system jailed to <paramref name="rootPath" />, creating the root when it does not exist.</summary>
    public LocalFileSystem(string rootPath)
        : this(new LocalFileSystemOptions { RootPath = rootPath }) { }

    /// <summary>Opens a file system from <paramref name="options" />.</summary>
    public LocalFileSystem(LocalFileSystemOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        options.Validate();
        RootPath = FileSystemPath.NormalizeRoot(PathStyle.Host, options.RootPath);
        if (options.CreateRootIfMissing)
            Directory.CreateDirectory(RootPath);
    }

    /// <inheritdoc />
    public string RootPath { get; }

    /// <inheritdoc />
    public PathStyle PathStyle => PathStyle.Host;

    /// <inheritdoc />
    public FileSystemCapabilities Capabilities
        => FileSystemCapabilities.Read | FileSystemCapabilities.Write | FileSystemCapabilities.CreateDirectory | FileSystemCapabilities.Delete | FileSystemCapabilities.Move |
            FileSystemCapabilities.MoveDirectory | FileSystemCapabilities.Copy | FileSystemCapabilities.Append;

    /// <inheritdoc />
    public IFileSystemWatch? Watch(string path, bool recursive = true) => null;

    /// <inheritdoc />
    public bool DirectoryExists(string path) => Directory.Exists(Jail(path));

    /// <inheritdoc />
    public Task<bool> DirectoryExistsAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(DirectoryExists(path));
    }

    /// <inheritdoc />
    public void CreateDirectory(string path) => Directory.CreateDirectory(Jail(path));

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
        var jailed = Jail(path);
        if (!Directory.Exists(jailed))
            return;

        // Directory.Delete(_, false) reports a non-empty directory as IOException already, which is the contract.
        Directory.Delete(jailed, recursive);
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
        var root = new DirectoryInfo(Jail(path));
        if (!root.Exists)
            return [];

        List<FileSystemEntry> entries = [];
        // One directory scan, not one per kind: EnumerateFileSystemInfos already carries the metadata each entry needs.
        foreach (var info in root.EnumerateFileSystemInfos())
            entries.Add(ToEntry(info));

        return entries;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<FileSystemEntry>> ListDirectoryAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ListDirectory(path));
    }

    /// <inheritdoc />
    public IAsyncEnumerable<FileSystemEntry> EnumerateDirectoryAsync(string path, bool recursive = false, CancellationToken ct = default)
        => SynchronousAsyncEnumerable.Create(Enumerate(Jail(path), recursive), ct);

    /// <inheritdoc />
    public bool FileExists(string path) => File.Exists(Jail(path));

    /// <inheritdoc />
    public Task<bool> FileExistsAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(FileExists(path));
    }

    /// <inheritdoc />
    public FileSystemEntry? GetEntry(string path)
    {
        var jailed = Jail(path);
        var file = new FileInfo(jailed);
        if (file.Exists)
            return ToEntry(file);

        var dir = new DirectoryInfo(jailed);
        return dir.Exists ? ToEntry(dir) : null;
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
        var jailed = Jail(path);
        if (File.Exists(jailed))
            File.Delete(jailed);
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
        EnsureParentDirectory(dst);
        var sourceIsDirectory = Directory.Exists(src);
        if (!sourceIsDirectory && !File.Exists(src))
            throw new FileNotFoundException($"Source not found: {source}", source);

        ClearDestination(dst, dest, overwrite);
        if (sourceIsDirectory)
            Directory.Move(src, dst);
        else
            File.Move(src, dst);
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
        if (!File.Exists(src))
            throw new FileNotFoundException($"Source file not found: {source}", source);

        EnsureParentDirectory(dst);
        File.Copy(src, dst, overwrite);
    }

    /// <inheritdoc />
    public async Task CopyFileAsync(string source, string dest, bool overwrite = false, CancellationToken ct = default)
    {
        var src = Jail(source);
        var dst = Jail(dest);
        if (!File.Exists(src))
            throw new FileNotFoundException($"Source file not found: {source}", source);

        if (!overwrite && File.Exists(dst))
            throw new IOException($"Destination already exists: {dest}");

        EnsureParentDirectory(dst);
        var destMode = overwrite ? FileMode.Create : FileMode.CreateNew;
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        await using var inStream = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var outStream = new FileStream(dst, destMode, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous);
        await inStream.CopyToAsync(outStream, ct).ConfigureAwait(false);
#else
        using var inStream = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var outStream = new FileStream(dst, destMode, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous);
        await inStream.CopyToAsync(outStream, BufferSize, ct).ConfigureAwait(false);
#endif
    }

    /// <inheritdoc />
    public Stream OpenRead(string path) => OpenRead(path, false);

    /// <inheritdoc />
    public Task<Stream> OpenReadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(OpenRead(path, true));
    }

    /// <inheritdoc />
    public Stream OpenCreate(string path) => OpenCreate(path, false);

    /// <inheritdoc />
    public Task<Stream> OpenCreateAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(OpenCreate(path, true));
    }

    /// <inheritdoc />
    public Stream OpenAppend(string path) => OpenAppend(path, false);

    /// <inheritdoc />
    public Task<Stream> OpenAppendAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(OpenAppend(path, true));
    }

    /// <inheritdoc />
    public long GetLength(string path)
    {
        var file = new FileInfo(Jail(path));
        if (!file.Exists)
            throw new FileNotFoundException($"File not found: {path}", path);

        return file.Length;
    }

    /// <inheritdoc />
    public Task<long> GetLengthAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(GetLength(path));
    }

    /// <inheritdoc />
    public DateTimeOffset GetCreationTimeUtc(string path) => new(StatOrThrow(path).CreationTimeUtc, TimeSpan.Zero);

    /// <inheritdoc />
    public Task<DateTimeOffset> GetCreationTimeUtcAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(GetCreationTimeUtc(path));
    }

    /// <inheritdoc />
    public DateTimeOffset GetLastWriteTimeUtc(string path) => new(StatOrThrow(path).LastWriteTimeUtc, TimeSpan.Zero);

    /// <inheritdoc />
    public Task<DateTimeOffset> GetLastWriteTimeUtcAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(GetLastWriteTimeUtc(path));
    }

    /// <inheritdoc />
    public void Dispose() { }

    private static IEnumerable<FileSystemEntry> Enumerate(string jailed, bool recursive)
    {
        var root = new DirectoryInfo(jailed);
        if (!root.Exists)
            yield break;

        var scope = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        foreach (var info in root.EnumerateFileSystemInfos("*", scope))
            yield return ToEntry(info);
    }

    private Stream OpenRead(string path, bool async)
    {
        var options = FileOptions.SequentialScan | (async ? FileOptions.Asynchronous : FileOptions.None);
        return new FileStream(Jail(path), FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, options);
    }

    private Stream OpenCreate(string path, bool async)
    {
        var jailed = Jail(path);
        EnsureParentDirectory(jailed);
        return new FileStream(jailed, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, async ? FileOptions.Asynchronous : FileOptions.None);
    }

    private Stream OpenAppend(string path, bool async)
    {
        var jailed = Jail(path);
        EnsureParentDirectory(jailed);
        return new FileStream(jailed, FileMode.Append, FileAccess.Write, FileShare.None, 4096, async ? FileOptions.Asynchronous : FileOptions.None);
    }

    /// <summary>
    /// One stat call for the common case. <see cref="FileSystemInfo" /> caches its attributes on first access, so callers read as many properties as they like off the returned
    /// instance without paying again.
    /// </summary>
    private FileSystemInfo StatOrThrow(string path)
    {
        var jailed = Jail(path);
        var file = new FileInfo(jailed);
        if (file.Exists)
            return file;

        var dir = new DirectoryInfo(jailed);
        if (dir.Exists)
            return dir;

        throw new FileNotFoundException($"Path not found: {path}", path);
    }

    /// <summary>Removes an existing destination so the move can proceed, or throws when the caller did not ask for a replace.</summary>
    private static void ClearDestination(string dst, string reportedPath, bool overwrite)
    {
        var destIsDirectory = Directory.Exists(dst);
        if (!destIsDirectory && !File.Exists(dst))
            return;

        if (!overwrite)
            throw new IOException($"Destination already exists: {reportedPath}");

        if (destIsDirectory)
            Directory.Delete(dst, true);
        else
            File.Delete(dst);
    }

    private static void EnsureParentDirectory(string fullPath)
    {
        var parent = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(parent))
            Directory.CreateDirectory(parent);
    }

    private string Jail(string path) => FileSystemPath.Jail(PathStyle.Host, RootPath, path);

    private static FileSystemEntry ToEntry(FileSystemInfo info)
    {
        var isDirectory = info is DirectoryInfo;
        var length = info is FileInfo file ? file.Length : 0;
        return new(info.FullName, info.Name, isDirectory, length, new(info.CreationTimeUtc, TimeSpan.Zero), new(info.LastWriteTimeUtc, TimeSpan.Zero));
    }
}