using Lyo.Common.Core.Pathing;
using Lyo.Exceptions;
using Lyo.IO.FileSystem;

namespace Lyo.Ftp.Client;

/// <summary><see cref="IFileSystem" /> over <see cref="IFtpClient" />. Paths stay under the client's remote root jail.</summary>
public sealed class FtpFileSystem : IFileSystem
{
    private readonly IFtpClient _client;

    /// <summary>Wraps <paramref name="client" />. Does not take ownership; disposing this instance does not dispose the client.</summary>
    public FtpFileSystem(IFtpClient client)
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
           | FileSystemCapabilities.Move | FileSystemCapabilities.Copy;

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
    public void DeleteDirectory(string path, bool recursive = true) => _client.DeleteDirectory(path, recursive);

    /// <inheritdoc />
    public Task DeleteDirectoryAsync(string path, bool recursive = true, CancellationToken ct = default) => _client.DeleteDirectoryAsync(path, recursive, ct);

    /// <inheritdoc />
    public IReadOnlyList<FileSystemEntry> ListDirectory(string path) => Map(_client.ListDirectory(path));

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileSystemEntry>> ListDirectoryAsync(string path, CancellationToken ct = default)
        => Map(await _client.ListDirectoryAsync(path, ct).ConfigureAwait(false));

    /// <inheritdoc />
    public bool FileExists(string path) => _client.FileExists(path);

    /// <inheritdoc />
    public Task<bool> FileExistsAsync(string path, CancellationToken ct = default) => _client.FileExistsAsync(path, ct);

    /// <inheritdoc />
    public void DeleteFile(string path) => _client.DeleteFile(path);

    /// <inheritdoc />
    public Task DeleteFileAsync(string path, CancellationToken ct = default) => _client.DeleteFileAsync(path, ct);

    /// <inheritdoc />
    public void Move(string source, string dest)
    {
        try {
            _client.Rename(source, dest);
        }
        catch {
            _client.CopyFile(source, dest);
            _client.DeleteFile(source);
        }
    }

    /// <inheritdoc />
    public async Task MoveAsync(string source, string dest, CancellationToken ct = default)
    {
        try {
            await _client.RenameAsync(source, dest, ct).ConfigureAwait(false);
        }
        catch {
            await _client.CopyFileAsync(source, dest, ct).ConfigureAwait(false);
            await _client.DeleteFileAsync(source, ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void CopyFile(string source, string dest) => _client.CopyFile(source, dest);

    /// <inheritdoc />
    public Task CopyFileAsync(string source, string dest, CancellationToken ct = default) => _client.CopyFileAsync(source, dest, ct);

    /// <inheritdoc />
    public Stream OpenRead(string path) => _client.OpenRead(path);

    /// <inheritdoc />
    public Task<Stream> OpenReadAsync(string path, CancellationToken ct = default) => _client.OpenReadAsync(path, ct);

    /// <inheritdoc />
    public Stream OpenCreate(string path) => _client.OpenCreate(path);

    /// <inheritdoc />
    public Task<Stream> OpenCreateAsync(string path, CancellationToken ct = default) => _client.OpenCreateAsync(path, ct);

    /// <inheritdoc />
    public Stream OpenAppend(string path) => _client.OpenAppend(path);

    /// <inheritdoc />
    public Task<Stream> OpenAppendAsync(string path, CancellationToken ct = default) => _client.OpenAppendAsync(path, ct);

    /// <inheritdoc />
    public long GetLength(string path) => _client.GetLength(path);

    /// <inheritdoc />
    public Task<long> GetLengthAsync(string path, CancellationToken ct = default) => _client.GetLengthAsync(path, ct);

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

    private static IReadOnlyList<FileSystemEntry> Map(IReadOnlyList<FtpEntryInfo> entries)
    {
        List<FileSystemEntry> mapped = new(entries.Count);
        mapped.AddRange(entries.Select(
            e => new FileSystemEntry(e.FullPath, PathHelpers.GetFileName(PathStyle.Posix, e.FullPath), 
                e.IsDirectory, e.Length, e.LastWriteTimeUtc, e.LastWriteTimeUtc)));
        return mapped;
    }
}
