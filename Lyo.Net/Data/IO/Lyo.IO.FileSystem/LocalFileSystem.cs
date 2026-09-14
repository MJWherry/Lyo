using Lyo.Common.Core.Pathing;
using Lyo.Exceptions;

namespace Lyo.IO.FileSystem;

/// <summary><see cref="IFileSystem" /> on <see cref="System.IO" /> with <see cref="PathStyle.Host" />. Listing only; live watch is <c>WatchableLocalFileSystem</c>.</summary>
public sealed class LocalFileSystem : IFileSystem
{
    private const int BufferSize = 81920;

    /// <summary>Opens a file system jailed to <paramref name="rootPath" />.</summary>
    public LocalFileSystem(string rootPath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(rootPath);
        RootPath = FileSystemPath.NormalizeRoot(PathStyle.Host, rootPath);
        Directory.CreateDirectory(RootPath);
    }

    /// <summary>Opens a file system from <paramref name="options" />.</summary>
    public LocalFileSystem(LocalFileSystemOptions options)
        : this(ArgumentHelpers.ThrowIfNullReturn(options).RootPath)
        => options.Validate();

    /// <inheritdoc />
    public string RootPath { get; }

    /// <inheritdoc />
    public PathStyle PathStyle => PathStyle.Host;

    /// <inheritdoc />
    public FileSystemCapabilities Capabilities
        => FileSystemCapabilities.Read | FileSystemCapabilities.Write | FileSystemCapabilities.CreateDirectory | FileSystemCapabilities.Delete
           | FileSystemCapabilities.Move | FileSystemCapabilities.Copy;

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
        if (Directory.Exists(jailed))
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
        var jailed = Jail(path);
        var root = new DirectoryInfo(jailed);
        if (!root.Exists)
            return [];

        List<FileSystemEntry> entries = [];
        foreach (var dir in root.EnumerateDirectories())
            entries.Add(ToEntry(dir.FullName, true, 0, dir.CreationTimeUtc, dir.LastWriteTimeUtc));

        foreach (var file in root.EnumerateFiles())
            entries.Add(ToEntry(file.FullName, false, file.Length, file.CreationTimeUtc, file.LastWriteTimeUtc));

        return entries;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<FileSystemEntry>> ListDirectoryAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ListDirectory(path));
    }

    /// <inheritdoc />
    public bool FileExists(string path) => File.Exists(Jail(path));

    /// <inheritdoc />
    public Task<bool> FileExistsAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(FileExists(path));
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
    public void Move(string source, string dest)
    {
        var src = Jail(source);
        var dst = Jail(dest);
        var destDir = Path.GetDirectoryName(dst);
        if (!string.IsNullOrEmpty(destDir))
            Directory.CreateDirectory(destDir);

        if (Directory.Exists(src)) {
            Directory.Move(src, dst);
            return;
        }

        try {
            File.Move(src, dst);
        }
        catch (IOException) {
            File.Copy(src, dst);
            File.Delete(src);
        }
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
        var dst = Jail(dest);
        var destDir = Path.GetDirectoryName(dst);
        if (!string.IsNullOrEmpty(destDir))
            Directory.CreateDirectory(destDir);

        File.Copy(Jail(source), dst);
    }

    /// <inheritdoc />
    public async Task CopyFileAsync(string source, string dest, CancellationToken ct = default)
    {
        var src = Jail(source);
        var dst = Jail(dest);
        var destDir = Path.GetDirectoryName(dst);
        if (!string.IsNullOrEmpty(destDir))
            Directory.CreateDirectory(destDir);

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        await using var inStream = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var outStream = new FileStream(dst, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous);
        await inStream.CopyToAsync(outStream, ct).ConfigureAwait(false);
#else
        using var inStream = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var outStream = new FileStream(dst, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous);
        await inStream.CopyToAsync(outStream, BufferSize, ct).ConfigureAwait(false);
#endif
    }

    /// <inheritdoc />
    public Stream OpenRead(string path)
        => new FileStream(Jail(path), FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan);

    /// <inheritdoc />
    public Task<Stream> OpenReadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(OpenRead(path));
    }

    /// <inheritdoc />
    public Stream OpenCreate(string path)
    {
        var jailed = Jail(path);
        var destDir = Path.GetDirectoryName(jailed);
        if (!string.IsNullOrEmpty(destDir))
            Directory.CreateDirectory(destDir);

        return new FileStream(jailed, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.None);
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
        var jailed = Jail(path);
        var destDir = Path.GetDirectoryName(jailed);
        if (!string.IsNullOrEmpty(destDir))
            Directory.CreateDirectory(destDir);

        return new FileStream(jailed, FileMode.Append, FileAccess.Write, FileShare.None, 4096, FileOptions.None);
    }

    /// <inheritdoc />
    public Task<Stream> OpenAppendAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(OpenAppend(path));
    }

    /// <inheritdoc />
    public long GetLength(string path) => new FileInfo(Jail(path)).Length;

    /// <inheritdoc />
    public Task<long> GetLengthAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(GetLength(path));
    }

    /// <inheritdoc />
    public DateTimeOffset GetCreationTimeUtc(string path)
    {
        var jailed = Jail(path);
        if (!Directory.Exists(jailed) && !File.Exists(jailed))
            throw new FileNotFoundException($"Path not found: {path}", path);

        var utc = Directory.Exists(jailed) ? Directory.GetCreationTimeUtc(jailed) : File.GetCreationTimeUtc(jailed);
        return new(utc, TimeSpan.Zero);
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
        var jailed = Jail(path);
        if (!Directory.Exists(jailed) && !File.Exists(jailed))
            throw new FileNotFoundException($"Path not found: {path}", path);

        var utc = Directory.Exists(jailed) ? Directory.GetLastWriteTimeUtc(jailed) : File.GetLastWriteTimeUtc(jailed);
        return new(utc, TimeSpan.Zero);
    }

    /// <inheritdoc />
    public Task<DateTimeOffset> GetLastWriteTimeUtcAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(GetLastWriteTimeUtc(path));
    }

    /// <inheritdoc />
    public void Dispose() { }

    private string Jail(string path) => FileSystemPath.Jail(PathStyle.Host, RootPath, path);

    private static FileSystemEntry ToEntry(string fullPath, bool isDirectory, long length, DateTime createdUtc, DateTime lastWriteUtc)
        => new(
            fullPath, Path.GetFileName(fullPath), isDirectory, length, new(createdUtc, TimeSpan.Zero), new(lastWriteUtc, TimeSpan.Zero));
}
