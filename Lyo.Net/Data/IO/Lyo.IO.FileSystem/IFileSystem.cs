using Lyo.Common.Core.Pathing;

namespace Lyo.IO.FileSystem;

/// <summary>
/// Path-rooted file I/O. Async methods are canonical; sync methods park on the async implementation (same pattern as Lyo SFTP/FTP clients). Paths stay under
/// <see cref="RootPath" /> through <see cref="PathHelpers" />.
/// </summary>
public interface IFileSystem : IDisposable
{
    /// <summary>Root jail. Every path passed in must stay under this directory.</summary>
    string RootPath { get; }

    /// <summary>Host separators for local disk; Posix (<c>/</c>) for memory and remote stores.</summary>
    PathStyle PathStyle { get; }

    /// <summary>Operations this backend supports. <see cref="Watch" /> returns null when <see cref="FileSystemCapabilities.Watch" /> is unset.</summary>
    FileSystemCapabilities Capabilities { get; }

    /// <summary>Starts watching <paramref name="path" />. Returns null when <see cref="FileSystemCapabilities.Watch" /> is not in <see cref="Capabilities" />.</summary>
    IFileSystemWatch? Watch(string path, bool recursive = true);

    /// <summary>True when <paramref name="path" /> exists and is a directory. Blocks. Prefer <see cref="DirectoryExistsAsync" />.</summary>
    bool DirectoryExists(string path);

    /// <summary>True when <paramref name="path" /> exists and is a directory.</summary>
    Task<bool> DirectoryExistsAsync(string path, CancellationToken ct = default);

    /// <summary>Creates <paramref name="path" /> and any missing parents. Blocks. Prefer <see cref="CreateDirectoryAsync" />.</summary>
    void CreateDirectory(string path);

    /// <summary>Creates <paramref name="path" /> and any missing parents.</summary>
    Task CreateDirectoryAsync(string path, CancellationToken ct = default);

    /// <summary>Deletes <paramref name="path" />. Blocks. Prefer <see cref="DeleteDirectoryAsync" />.</summary>
    void DeleteDirectory(string path, bool recursive = true);

    /// <summary>Deletes <paramref name="path" />. Missing directories are a no-op.</summary>
    Task DeleteDirectoryAsync(string path, bool recursive = true, CancellationToken ct = default);

    /// <summary>Immediate child files and directories of <paramref name="path" />. Blocks. Prefer <see cref="ListDirectoryAsync" />.</summary>
    IReadOnlyList<FileSystemEntry> ListDirectory(string path);

    /// <summary>Immediate child files and directories of <paramref name="path" />. Empty when the directory is missing.</summary>
    Task<IReadOnlyList<FileSystemEntry>> ListDirectoryAsync(string path, CancellationToken ct = default);

    /// <summary>True when <paramref name="path" /> exists and is a file. Blocks. Prefer <see cref="FileExistsAsync" />.</summary>
    bool FileExists(string path);

    /// <summary>True when <paramref name="path" /> exists and is a file.</summary>
    Task<bool> FileExistsAsync(string path, CancellationToken ct = default);

    /// <summary>Deletes a file. Blocks. Prefer <see cref="DeleteFileAsync" />.</summary>
    void DeleteFile(string path);

    /// <summary>Deletes a file. Missing-file behaviour is up to the implementation.</summary>
    Task DeleteFileAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Moves or renames a file from <paramref name="source" /> to <paramref name="dest" />. Blocks. Prefer <see cref="MoveAsync" />. When
    /// <paramref name="source" /> is a directory, backends that support directory rename do so; others throw.
    /// </summary>
    void Move(string source, string dest);

    /// <summary>
    /// Moves or renames a file from <paramref name="source" /> to <paramref name="dest" />. When <paramref name="source" /> is a directory, backends that support
    /// directory rename do so; others throw.
    /// </summary>
    Task MoveAsync(string source, string dest, CancellationToken ct = default);

    /// <summary>Copies a file. Blocks. Prefer <see cref="CopyFileAsync" />.</summary>
    void CopyFile(string source, string dest);

    /// <summary>Copies a file from <paramref name="source" /> to <paramref name="dest" />.</summary>
    Task CopyFileAsync(string source, string dest, CancellationToken ct = default);

    /// <summary>Opens <paramref name="path" /> for sequential reads. Blocks. Prefer <see cref="OpenReadAsync" />.</summary>
    Stream OpenRead(string path);

    /// <summary>Opens <paramref name="path" /> for sequential reads. The caller disposes the stream.</summary>
    Task<Stream> OpenReadAsync(string path, CancellationToken ct = default);

    /// <summary>Opens <paramref name="path" /> for write, creating or truncating. Blocks. Prefer <see cref="OpenCreateAsync" />.</summary>
    Stream OpenCreate(string path);

    /// <summary>Opens <paramref name="path" /> for write, creating or truncating.</summary>
    Task<Stream> OpenCreateAsync(string path, CancellationToken ct = default);

    /// <summary>Opens <paramref name="path" /> for append. Blocks. Prefer <see cref="OpenAppendAsync" />.</summary>
    Stream OpenAppend(string path);

    /// <summary>Opens <paramref name="path" /> for append.</summary>
    Task<Stream> OpenAppendAsync(string path, CancellationToken ct = default);

    /// <summary>Byte length of the file at <paramref name="path" />. Blocks. Prefer <see cref="GetLengthAsync" />.</summary>
    long GetLength(string path);

    /// <summary>Byte length of the file at <paramref name="path" />.</summary>
    Task<long> GetLengthAsync(string path, CancellationToken ct = default);

    /// <summary>UTC creation time of the file or directory. Blocks. Prefer <see cref="GetCreationTimeUtcAsync" />.</summary>
    DateTimeOffset GetCreationTimeUtc(string path);

    /// <summary>UTC creation time of the file or directory.</summary>
    Task<DateTimeOffset> GetCreationTimeUtcAsync(string path, CancellationToken ct = default);

    /// <summary>UTC last-write time. Blocks. Prefer <see cref="GetLastWriteTimeUtcAsync" />.</summary>
    DateTimeOffset GetLastWriteTimeUtc(string path);

    /// <summary>UTC last-write time.</summary>
    Task<DateTimeOffset> GetLastWriteTimeUtcAsync(string path, CancellationToken ct = default);
}
