using System.Runtime.CompilerServices;
using Lyo.Common.Core.Pathing;
using Lyo.Exceptions;
using Lyo.IO.FileSystem;

namespace Lyo.Sftp.Client;

/// <summary><see cref="IFileSystem" /> over <see cref="ISftpClient" />. Paths stay under the client's remote root jail.</summary>
public sealed class SftpFileSystem : IFileSystem
{
    private readonly ISftpClient _client;

    /// <summary>Wraps <paramref name="client" />. Does not take ownership; disposing this instance does not dispose the client.</summary>
    public SftpFileSystem(ISftpClient client)
    {
        ArgumentHelpers.ThrowIfNull(client);
        _client = client;
    }

    /// <inheritdoc />
    public string RootPath => _client.RootRemoteDirectory;

    /// <inheritdoc />
    public PathStyle PathStyle => PathStyle.Posix;

    /// <inheritdoc />
    public FileSystemCapabilities Capabilities
        => FileSystemCapabilities.Read | FileSystemCapabilities.Write | FileSystemCapabilities.CreateDirectory | FileSystemCapabilities.Delete
           | FileSystemCapabilities.Move | FileSystemCapabilities.MoveDirectory | FileSystemCapabilities.Copy | FileSystemCapabilities.Append;

    /// <inheritdoc />
    public IFileSystemWatch? Watch(string path, bool recursive = true) => null;

    /// <inheritdoc />
    public bool DirectoryExists(string path) => _client.DirectoryExists(path);

    /// <inheritdoc />
    public Task<bool> DirectoryExistsAsync(string path, CancellationToken ct = default) => _client.DirectoryExistsAsync(path, ct);

    /// <inheritdoc />
    public void CreateDirectory(string path) => _client.CreateDirectory(path);

    /// <inheritdoc />
    public Task CreateDirectoryAsync(string path, CancellationToken ct = default) => _client.CreateDirectoryAsync(path, ct);

    /// <inheritdoc />
    public void DeleteDirectory(string path, bool recursive = true)
    {
        if (!_client.DirectoryExists(path))
            return;

        if (!recursive && _client.ListDirectory(path).Count > 0)
            throw new IOException($"Directory is not empty: {path}");

        _client.DeleteDirectory(path, recursive);
    }

    /// <inheritdoc />
    public async Task DeleteDirectoryAsync(string path, bool recursive = true, CancellationToken ct = default)
    {
        if (!await _client.DirectoryExistsAsync(path, ct).ConfigureAwait(false))
            return;

        if (!recursive && (await _client.ListDirectoryAsync(path, ct).ConfigureAwait(false)).Count > 0)
            throw new IOException($"Directory is not empty: {path}");

        await _client.DeleteDirectoryAsync(path, recursive, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IReadOnlyList<FileSystemEntry> ListDirectory(string path) => _client.DirectoryExists(path) ? Map(_client.ListDirectory(path)) : [];

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileSystemEntry>> ListDirectoryAsync(string path, CancellationToken ct = default)
    {
        if (!await _client.DirectoryExistsAsync(path, ct).ConfigureAwait(false))
            return [];

        return Map(await _client.ListDirectoryAsync(path, ct).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<FileSystemEntry> EnumerateDirectoryAsync(
        string path, bool recursive = false, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var pending = new Queue<string>();
        pending.Enqueue(path);
        while (pending.Count > 0) {
            ct.ThrowIfCancellationRequested();
            foreach (var entry in await ListDirectoryAsync(pending.Dequeue(), ct).ConfigureAwait(false)) {
                ct.ThrowIfCancellationRequested();
                yield return entry;
                if (recursive && entry.IsDirectory)
                    pending.Enqueue(entry.Path);
            }
        }
    }

    /// <inheritdoc />
    public bool FileExists(string path) => _client.FileExists(path);

    /// <inheritdoc />
    public Task<bool> FileExistsAsync(string path, CancellationToken ct = default) => _client.FileExistsAsync(path, ct);

    /// <inheritdoc />
    /// <remarks>Unlike the local backends this still costs up to three round trips; the remote protocol has no single stat-and-classify call.</remarks>
    public FileSystemEntry? GetEntry(string path)
    {
        if (_client.FileExists(path)) {
            var lastWrite = _client.GetLastWriteTimeUtc(path);
            return new(path, PathHelpers.GetFileName(PathStyle.Posix, path), false, _client.GetLength(path), lastWrite, lastWrite);
        }

        return _client.DirectoryExists(path) ? DirectoryEntry(path) : null;
    }

    /// <inheritdoc />
    public async Task<FileSystemEntry?> GetEntryAsync(string path, CancellationToken ct = default)
    {
        if (await _client.FileExistsAsync(path, ct).ConfigureAwait(false)) {
            var lastWrite = await _client.GetLastWriteTimeUtcAsync(path, ct).ConfigureAwait(false);
            var length = await _client.GetLengthAsync(path, ct).ConfigureAwait(false);
            return new(path, PathHelpers.GetFileName(PathStyle.Posix, path), false, length, lastWrite, lastWrite);
        }

        return await _client.DirectoryExistsAsync(path, ct).ConfigureAwait(false) ? DirectoryEntry(path) : null;
    }

    /// <inheritdoc />
    public void DeleteFile(string path) => _client.DeleteFile(path);

    /// <inheritdoc />
    public Task DeleteFileAsync(string path, CancellationToken ct = default) => _client.DeleteFileAsync(path, ct);

    /// <inheritdoc />
    public void Move(string source, string dest, bool overwrite = false)
    {
        var sourceIsDirectory = _client.DirectoryExists(source);
        if (!sourceIsDirectory && !_client.FileExists(source))
            throw new FileNotFoundException($"Source not found: {source}", source);

        var destIsDirectory = _client.DirectoryExists(dest);
        if (destIsDirectory || _client.FileExists(dest)) {
            if (!overwrite)
                throw new IOException($"Destination already exists: {dest}");

            if (destIsDirectory)
                _client.DeleteDirectory(dest, true);
            else
                _client.DeleteFile(dest);
        }

        try {
            _client.Rename(source, dest);
        }
        catch (Exception ex) when (!sourceIsDirectory) {
            // Servers that keep the paths on different mounts reject rename. Copy-and-delete is the workaround, and only means anything for a single file.
            try {
                _client.CopyFile(source, dest);
                _client.DeleteFile(source);
            }
            catch (Exception fallback) {
                throw new IOException($"Move failed from '{source}' to '{dest}'.", new AggregateException(ex, fallback));
            }
        }
    }

    /// <inheritdoc />
    public async Task MoveAsync(string source, string dest, bool overwrite = false, CancellationToken ct = default)
    {
        var sourceIsDirectory = await _client.DirectoryExistsAsync(source, ct).ConfigureAwait(false);
        if (!sourceIsDirectory && !await _client.FileExistsAsync(source, ct).ConfigureAwait(false))
            throw new FileNotFoundException($"Source not found: {source}", source);

        var destIsDirectory = await _client.DirectoryExistsAsync(dest, ct).ConfigureAwait(false);
        if (destIsDirectory || await _client.FileExistsAsync(dest, ct).ConfigureAwait(false)) {
            if (!overwrite)
                throw new IOException($"Destination already exists: {dest}");

            if (destIsDirectory)
                await _client.DeleteDirectoryAsync(dest, true, ct).ConfigureAwait(false);
            else
                await _client.DeleteFileAsync(dest, ct).ConfigureAwait(false);
        }

        try {
            await _client.RenameAsync(source, dest, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (!sourceIsDirectory) {
            try {
                await _client.CopyFileAsync(source, dest, ct).ConfigureAwait(false);
                await _client.DeleteFileAsync(source, ct).ConfigureAwait(false);
            }
            catch (Exception fallback) {
                throw new IOException($"Move failed from '{source}' to '{dest}'.", new AggregateException(ex, fallback));
            }
        }
    }

    /// <inheritdoc />
    public void CopyFile(string source, string dest, bool overwrite = false)
    {
        if (!_client.FileExists(source))
            throw new FileNotFoundException($"Source file not found: {source}", source);

        if (!overwrite && _client.FileExists(dest))
            throw new IOException($"Destination already exists: {dest}");

        _client.CopyFile(source, dest);
    }

    /// <inheritdoc />
    public async Task CopyFileAsync(string source, string dest, bool overwrite = false, CancellationToken ct = default)
    {
        if (!await _client.FileExistsAsync(source, ct).ConfigureAwait(false))
            throw new FileNotFoundException($"Source file not found: {source}", source);

        if (!overwrite && await _client.FileExistsAsync(dest, ct).ConfigureAwait(false))
            throw new IOException($"Destination already exists: {dest}");

        await _client.CopyFileAsync(source, dest, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Stream OpenRead(string path)
    {
        ThrowIfNotAFile(_client.FileExists(path), path);
        return _client.OpenRead(path);
    }

    /// <inheritdoc />
    public async Task<Stream> OpenReadAsync(string path, CancellationToken ct = default)
    {
        ThrowIfNotAFile(await _client.FileExistsAsync(path, ct).ConfigureAwait(false), path);
        return await _client.OpenReadAsync(path, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Stream OpenCreate(string path) => _client.OpenCreate(path);

    /// <inheritdoc />
    public Task<Stream> OpenCreateAsync(string path, CancellationToken ct = default) => _client.OpenCreateAsync(path, ct);

    /// <inheritdoc />
    public Stream OpenAppend(string path) => _client.OpenAppend(path);

    /// <inheritdoc />
    public Task<Stream> OpenAppendAsync(string path, CancellationToken ct = default) => _client.OpenAppendAsync(path, ct);

    /// <inheritdoc />
    public long GetLength(string path)
    {
        ThrowIfNotAFile(_client.FileExists(path), path);
        return _client.GetLength(path);
    }

    /// <inheritdoc />
    public async Task<long> GetLengthAsync(string path, CancellationToken ct = default)
    {
        ThrowIfNotAFile(await _client.FileExistsAsync(path, ct).ConfigureAwait(false), path);
        return await _client.GetLengthAsync(path, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public DateTimeOffset GetCreationTimeUtc(string path) => _client.GetLastWriteTimeUtc(path);

    /// <inheritdoc />
    public Task<DateTimeOffset> GetCreationTimeUtcAsync(string path, CancellationToken ct = default) => _client.GetLastWriteTimeUtcAsync(path, ct);

    /// <inheritdoc />
    public DateTimeOffset GetLastWriteTimeUtc(string path) => _client.GetLastWriteTimeUtc(path);

    /// <inheritdoc />
    public Task<DateTimeOffset> GetLastWriteTimeUtcAsync(string path, CancellationToken ct = default) => _client.GetLastWriteTimeUtcAsync(path, ct);

    /// <inheritdoc />
    public void Dispose() { }

    private static void ThrowIfNotAFile(bool isFile, string path)
    {
        if (!isFile)
            throw new FileNotFoundException($"File not found: {path}", path);
    }

    private static FileSystemEntry DirectoryEntry(string path) => new(path, PathHelpers.GetFileName(PathStyle.Posix, path), true, 0, default, default);

    private static IReadOnlyList<FileSystemEntry> Map(IReadOnlyList<SftpEntryInfo> entries)
    {
        List<FileSystemEntry> mapped = new(entries.Count);
        foreach (var e in entries)
            mapped.Add(
                new(
                    e.FullPath, PathHelpers.GetFileName(PathStyle.Posix, e.FullPath), e.IsDirectory, e.Length, e.LastWriteTimeUtc, e.LastWriteTimeUtc));

        return mapped;
    }
}
