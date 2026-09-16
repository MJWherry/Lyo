using Lyo.Common.Core.Pathing;

namespace Lyo.IO.FileSystem;

/// <summary>
/// Path-rooted file I/O. Async methods are canonical; sync methods park on the async implementation (same pattern as Lyo SFTP/FTP clients). Paths stay under
/// <see cref="RootPath" /> through <see cref="PathHelpers" />.
/// </summary>
/// <remarks>
/// <para>
/// <b>Paths.</b> Every path argument is either absolute under <see cref="RootPath" /> or relative, in which case it resolves against <see cref="RootPath" />. A path that
/// escapes the root after normalization throws <see cref="Lyo.Exceptions.Models.InvalidFormatException" />.
/// </para>
/// <para>
/// <b>Errors.</b> Implementations translate backend-specific failures into BCL types so callers can be written once: a missing file is
/// <see cref="FileNotFoundException" />, an existing destination with <c>overwrite: false</c> is <see cref="IOException" />, and an operation the backend cannot perform is
/// <see cref="NotSupportedException" />. Check <see cref="Capabilities" /> before relying on append or directory move.
/// </para>
/// </remarks>
public interface IFileSystem : IDisposable
{
    /// <summary>
    /// Root jail. Every path passed in resolves under this directory. This is a path-containment guard applied after normalization, not a symlink-proof sandbox: on
    /// backends with real links, a link stored inside the root may still point outside it.
    /// </summary>
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

    /// <summary>Creates <paramref name="path" /> and any missing parents. Existing directories are a no-op.</summary>
    Task CreateDirectoryAsync(string path, CancellationToken ct = default);

    /// <summary>Deletes <paramref name="path" />. Blocks. Prefer <see cref="DeleteDirectoryAsync" />.</summary>
    void DeleteDirectory(string path, bool recursive = true);

    /// <summary>
    /// Deletes <paramref name="path" />. Missing directories are a no-op, and so is a path that names a file. With <paramref name="recursive" /> false a non-empty directory
    /// throws <see cref="IOException" />.
    /// </summary>
    Task DeleteDirectoryAsync(string path, bool recursive = true, CancellationToken ct = default);

    /// <summary>Immediate child files and directories of <paramref name="path" />. Blocks. Prefer <see cref="ListDirectoryAsync" />.</summary>
    IReadOnlyList<FileSystemEntry> ListDirectory(string path);

    /// <summary>Immediate child files and directories of <paramref name="path" />. Empty when the directory is missing or when the path names a file.</summary>
    Task<IReadOnlyList<FileSystemEntry>> ListDirectoryAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Streams entries under <paramref name="path" /> without materializing the whole listing. Set <paramref name="recursive" /> to walk descendants; directories are
    /// yielded before their contents. Empty when the directory is missing.
    /// </summary>
    IAsyncEnumerable<FileSystemEntry> EnumerateDirectoryAsync(string path, bool recursive = false, CancellationToken ct = default);

    /// <summary>True when <paramref name="path" /> exists and is a file. Blocks. Prefer <see cref="FileExistsAsync" />.</summary>
    bool FileExists(string path);

    /// <summary>True when <paramref name="path" /> exists and is a file.</summary>
    Task<bool> FileExistsAsync(string path, CancellationToken ct = default);

    /// <summary>Metadata for <paramref name="path" />, or null when nothing is there. Blocks. Prefer <see cref="GetEntryAsync" />.</summary>
    FileSystemEntry? GetEntry(string path);

    /// <summary>
    /// Metadata for <paramref name="path" /> in one call, or null when nothing is there. Prefer this over chaining <see cref="FileExistsAsync" />,
    /// <see cref="GetLengthAsync" />, and <see cref="GetLastWriteTimeUtcAsync" />, which costs one round trip each on remote backends.
    /// </summary>
    Task<FileSystemEntry?> GetEntryAsync(string path, CancellationToken ct = default);

    /// <summary>Deletes a file. Blocks. Prefer <see cref="DeleteFileAsync" />.</summary>
    void DeleteFile(string path);

    /// <summary>Deletes a file. A missing file is a no-op.</summary>
    Task DeleteFileAsync(string path, CancellationToken ct = default);

    /// <summary>Moves or renames a file from <paramref name="source" /> to <paramref name="dest" />. Blocks. Prefer <see cref="MoveAsync" />.</summary>
    void Move(string source, string dest, bool overwrite = false);

    /// <summary>
    /// Moves or renames <paramref name="source" /> to <paramref name="dest" />. When <paramref name="dest" /> already exists, throws <see cref="IOException" /> unless
    /// <paramref name="overwrite" /> is set. A missing <paramref name="source" /> throws <see cref="FileNotFoundException" />. Moving a directory requires
    /// <see cref="FileSystemCapabilities.MoveDirectory" />; backends without it throw <see cref="NotSupportedException" />.
    /// </summary>
    Task MoveAsync(string source, string dest, bool overwrite = false, CancellationToken ct = default);

    /// <summary>Copies a file. Blocks. Prefer <see cref="CopyFileAsync" />.</summary>
    void CopyFile(string source, string dest, bool overwrite = false);

    /// <summary>
    /// Copies a file from <paramref name="source" /> to <paramref name="dest" />, creating missing parent directories. When <paramref name="dest" /> already exists, throws
    /// <see cref="IOException" /> unless <paramref name="overwrite" /> is set. A missing <paramref name="source" /> throws <see cref="FileNotFoundException" />.
    /// </summary>
    Task CopyFileAsync(string source, string dest, bool overwrite = false, CancellationToken ct = default);

    /// <summary>Opens <paramref name="path" /> for sequential reads. Blocks. Prefer <see cref="OpenReadAsync" />.</summary>
    Stream OpenRead(string path);

    /// <summary>Opens <paramref name="path" /> for sequential reads. The caller disposes the stream. A missing file throws <see cref="FileNotFoundException" />.</summary>
    Task<Stream> OpenReadAsync(string path, CancellationToken ct = default);

    /// <summary>Opens <paramref name="path" /> for write, creating or truncating. Blocks. Prefer <see cref="OpenCreateAsync" />.</summary>
    Stream OpenCreate(string path);

    /// <summary>Opens <paramref name="path" /> for write, creating or truncating, and creating missing parent directories.</summary>
    Task<Stream> OpenCreateAsync(string path, CancellationToken ct = default);

    /// <summary>Opens <paramref name="path" /> for append. Blocks. Prefer <see cref="OpenAppendAsync" />.</summary>
    Stream OpenAppend(string path);

    /// <summary>
    /// Opens <paramref name="path" /> for append. Requires <see cref="FileSystemCapabilities.Append" />; backends without it throw <see cref="NotSupportedException" />.
    /// </summary>
    Task<Stream> OpenAppendAsync(string path, CancellationToken ct = default);

    /// <summary>Byte length of the file at <paramref name="path" />. Blocks. Prefer <see cref="GetLengthAsync" />.</summary>
    long GetLength(string path);

    /// <summary>Byte length of the file at <paramref name="path" />. Throws <see cref="FileNotFoundException" /> when the path is missing or names a directory.</summary>
    Task<long> GetLengthAsync(string path, CancellationToken ct = default);

    /// <summary>UTC creation time of the file or directory. Blocks. Prefer <see cref="GetCreationTimeUtcAsync" />.</summary>
    DateTimeOffset GetCreationTimeUtc(string path);

    /// <summary>UTC creation time of the file or directory. Throws <see cref="FileNotFoundException" /> when the path is missing.</summary>
    Task<DateTimeOffset> GetCreationTimeUtcAsync(string path, CancellationToken ct = default);

    /// <summary>UTC last-write time. Blocks. Prefer <see cref="GetLastWriteTimeUtcAsync" />.</summary>
    DateTimeOffset GetLastWriteTimeUtc(string path);

    /// <summary>UTC last-write time. Throws <see cref="FileNotFoundException" /> when the path is missing.</summary>
    Task<DateTimeOffset> GetLastWriteTimeUtcAsync(string path, CancellationToken ct = default);
}
