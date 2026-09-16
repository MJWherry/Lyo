using System.Text;

namespace Lyo.IO.FileSystem.Tests;

/// <summary>
/// Dispatches every <see cref="IFileSystem" /> operation to either the synchronous or the asynchronous member, chosen once at construction. Contract tests take
/// <c>useAsync</c> as a theory parameter and drive this, so one test body covers both halves of the interface.
/// </summary>
/// <param name="FileSystem">Backend under test.</param>
/// <param name="UseAsync">When true, calls the <c>*Async</c> member; when false, the blocking one.</param>
public sealed record FileSystemApi(IFileSystem FileSystem, bool UseAsync)
{
    /// <summary>Root jail of the wrapped backend.</summary>
    public string RootPath => FileSystem.RootPath;

    /// <summary>Operations the wrapped backend supports.</summary>
    public FileSystemCapabilities Capabilities => FileSystem.Capabilities;

    /// <inheritdoc cref="IFileSystem.DirectoryExistsAsync" />
    public Task<bool> DirectoryExistsAsync(string path, CancellationToken ct = default)
        => UseAsync ? FileSystem.DirectoryExistsAsync(path, ct) : Task.FromResult(FileSystem.DirectoryExists(path));

    /// <inheritdoc cref="IFileSystem.CreateDirectoryAsync" />
    public Task CreateDirectoryAsync(string path, CancellationToken ct = default)
    {
        if (UseAsync)
            return FileSystem.CreateDirectoryAsync(path, ct);

        FileSystem.CreateDirectory(path);
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="IFileSystem.DeleteDirectoryAsync" />
    public Task DeleteDirectoryAsync(string path, bool recursive = true, CancellationToken ct = default)
    {
        if (UseAsync)
            return FileSystem.DeleteDirectoryAsync(path, recursive, ct);

        FileSystem.DeleteDirectory(path, recursive);
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="IFileSystem.ListDirectoryAsync" />
    public Task<IReadOnlyList<FileSystemEntry>> ListDirectoryAsync(string path, CancellationToken ct = default)
        => UseAsync ? FileSystem.ListDirectoryAsync(path, ct) : Task.FromResult(FileSystem.ListDirectory(path));

    /// <inheritdoc cref="IFileSystem.FileExistsAsync" />
    public Task<bool> FileExistsAsync(string path, CancellationToken ct = default)
        => UseAsync ? FileSystem.FileExistsAsync(path, ct) : Task.FromResult(FileSystem.FileExists(path));

    /// <inheritdoc cref="IFileSystem.DeleteFileAsync" />
    public Task DeleteFileAsync(string path, CancellationToken ct = default)
    {
        if (UseAsync)
            return FileSystem.DeleteFileAsync(path, ct);

        FileSystem.DeleteFile(path);
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="IFileSystem.MoveAsync" />
    public Task MoveAsync(string source, string dest, bool overwrite = false, CancellationToken ct = default)
    {
        if (UseAsync)
            return FileSystem.MoveAsync(source, dest, overwrite, ct);

        FileSystem.Move(source, dest, overwrite);
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="IFileSystem.CopyFileAsync" />
    public Task CopyFileAsync(string source, string dest, bool overwrite = false, CancellationToken ct = default)
    {
        if (UseAsync)
            return FileSystem.CopyFileAsync(source, dest, overwrite, ct);

        FileSystem.CopyFile(source, dest, overwrite);
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="IFileSystem.OpenReadAsync" />
    public Task<Stream> OpenReadAsync(string path, CancellationToken ct = default)
        => UseAsync ? FileSystem.OpenReadAsync(path, ct) : Task.FromResult(FileSystem.OpenRead(path));

    /// <inheritdoc cref="IFileSystem.OpenCreateAsync" />
    public Task<Stream> OpenCreateAsync(string path, CancellationToken ct = default)
        => UseAsync ? FileSystem.OpenCreateAsync(path, ct) : Task.FromResult(FileSystem.OpenCreate(path));

    /// <inheritdoc cref="IFileSystem.OpenAppendAsync" />
    public Task<Stream> OpenAppendAsync(string path, CancellationToken ct = default)
        => UseAsync ? FileSystem.OpenAppendAsync(path, ct) : Task.FromResult(FileSystem.OpenAppend(path));

    /// <inheritdoc cref="IFileSystem.GetLengthAsync" />
    public Task<long> GetLengthAsync(string path, CancellationToken ct = default)
        => UseAsync ? FileSystem.GetLengthAsync(path, ct) : Task.FromResult(FileSystem.GetLength(path));

    /// <inheritdoc cref="IFileSystem.GetCreationTimeUtcAsync" />
    public Task<DateTimeOffset> GetCreationTimeUtcAsync(string path, CancellationToken ct = default)
        => UseAsync ? FileSystem.GetCreationTimeUtcAsync(path, ct) : Task.FromResult(FileSystem.GetCreationTimeUtc(path));

    /// <inheritdoc cref="IFileSystem.GetLastWriteTimeUtcAsync" />
    public Task<DateTimeOffset> GetLastWriteTimeUtcAsync(string path, CancellationToken ct = default)
        => UseAsync ? FileSystem.GetLastWriteTimeUtcAsync(path, ct) : Task.FromResult(FileSystem.GetLastWriteTimeUtc(path));

    /// <inheritdoc cref="IFileSystem.GetEntryAsync" />
    public Task<FileSystemEntry?> GetEntryAsync(string path, CancellationToken ct = default)
        => UseAsync ? FileSystem.GetEntryAsync(path, ct) : Task.FromResult(FileSystem.GetEntry(path));

    /// <summary>Writes <paramref name="data" /> to <paramref name="path" />, replacing any existing file.</summary>
    public async Task WriteAllBytesAsync(string path, byte[] data, CancellationToken ct = default)
    {
        using var stream = await OpenCreateAsync(path, ct).ConfigureAwait(false);
        await stream.WriteAsync(data.AsMemory(), ct).ConfigureAwait(false);
    }

    /// <summary>Writes <paramref name="text" /> to <paramref name="path" /> as UTF-8.</summary>
    public Task WriteAllTextAsync(string path, string text, CancellationToken ct = default)
        => WriteAllBytesAsync(path, Encoding.UTF8.GetBytes(text), ct);

    /// <summary>Reads the whole file at <paramref name="path" />.</summary>
    public async Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct = default)
    {
        using var stream = await OpenReadAsync(path, ct).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct).ConfigureAwait(false);
        return buffer.ToArray();
    }
}
