namespace Lyo.IO.Temp.Models;

/// <summary>
/// Workspace scoped to one folder: it records files and directories it created, applies size/count limits, and removes <see cref="SessionDirectory" /> on dispose.
/// </summary>
/// <remarks>Text I/O is UTF-8 with no BOM.</remarks>
// ReSharper disable once InconsistentNaming
public interface IIOTempSession : IAsyncDisposable, IDisposable
{
    /// <summary>Full path of the session root folder.</summary>
    string SessionDirectory { get; }

    /// <summary>Absolute paths of files this session created or imported through its API.</summary>
    IReadOnlyList<string> Files { get; }

    /// <summary>Absolute paths of directories this session tracks. The session root is omitted unless it was added on purpose.</summary>
    IReadOnlyList<string> Directories { get; }

    /// <summary>Random file and fake-tree generator bound to this session.</summary>
    IIOTempFileGenerator Generator { get; }

    /// <summary>Fires after a file is created or copied into the session. The argument is the absolute path.</summary>
    event Action<string>? FileCreated;

    /// <summary>Fires after a directory is created or copied into the session. The argument is the absolute path.</summary>
    event Action<string>? DirectoryCreated;

    /// <summary>Fires after a successful overwrite from <see cref="WriteFile(string, string)" /> or another WriteFile overload. The argument is the absolute path.</summary>
    event Action<string>? FileWritten;

    /// <summary>Fires after a successful append from <see cref="AppendToFile(string, string)" /> or another AppendToFile overload. The argument is the absolute path.</summary>
    event Action<string>? FileAppended;

    /// <summary>
    /// Fires when a file is actually removed: <see cref="DeleteFile" /> returning true, overflow eviction, or a tracked file dropped by <see cref="DeleteDirectory" /> /
    /// <see cref="Clear" />. The argument is the absolute path. Already-missing files do not raise this.
    /// </summary>
    event Action<string>? FileDeleted;

    /// <summary>
    /// Fires when a directory is actually removed: <see cref="DeleteDirectory" /> returning true, or a tracked directory dropped by <see cref="Clear" /> or a nested
    /// <see cref="DeleteDirectory" />. The argument is the absolute path. Already-missing directories do not raise this.
    /// </summary>
    event Action<string>? DirectoryDeleted;

    /// <summary>
    /// Fires when delete-oldest / delete-largest overflow drops a file. The argument is that path. <see cref="FileDeleted" /> runs for the same path as well.
    /// </summary>
    event Action<string>? Overflow;

    /// <summary>Fires when <see cref="Clear" /> completes; the session folder stays. The argument is <see cref="SessionDirectory" />.</summary>
    event Action<string>? Cleared;

    /// <summary>
    /// Fires on dispose, at the same moment as the constructor <c>onDispose</c> callback. The argument is <see cref="SessionDirectory" />. Individual file deletes are not
    /// raised.
    /// </summary>
    event Action<string>? Disposed;

    /// <summary>Running total of bytes written into tracked files for this session.</summary>
    long GetTotalBytesUsed();

    /// <summary>Copy of files, directories, byte total, and creation time at this instant.</summary>
    TempSessionSnapshot GetSnapshot();

    /// <summary>
    /// Opens a nested session whose root sits inside this one. Tracking and dispose are separate. The nested folder is also listed on <see cref="Directories" />.
    /// </summary>
    IIOTempSession CreateSubSession();

    /// <summary>
    /// Lists every file under <see cref="SessionDirectory" /> on disk, including ones this session did not create. <paramref name="pattern" /> is an optional glob.
    /// </summary>
    /// <param name="pattern">File-name filter; <c>*</c> means all. Other patterns use simple <c>*</c> segments.</param>
    IEnumerable<string> EnumerateFiles(string? pattern = null);

    /// <summary>Lists every directory under <see cref="SessionDirectory" /> on disk.</summary>
    IEnumerable<string> EnumerateDirectories();

    /// <summary>
    /// Removes tracked files and directories and zeroes the byte counter. The session folder remains and the session can still be used.
    /// </summary>
    void Clear();

    /// <summary>Copies a file or directory at <paramref name="sourcePath" /> into the session and begins tracking it. Returns the destination path.</summary>
    string CopyFrom(string sourcePath);

    /// <summary>Copies a file or directory at <paramref name="sourcePath" /> into the session asynchronously.</summary>
    Task<string> CopyFromAsync(string sourcePath, CancellationToken ct = default);

    /// <summary>
    /// Relocates a file or directory at <paramref name="sourcePath" /> into the session and deletes the original. Rename is used when it works; otherwise copy then delete
    /// (including across volumes). Returns the destination path.
    /// </summary>
    string MoveFrom(string sourcePath);

    /// <summary>Relocates a file or directory at <paramref name="sourcePath" /> into the session asynchronously.</summary>
    Task<string> MoveFromAsync(string sourcePath, CancellationToken ct = default);

    /// <summary>Appends bytes onto a tracked file that already exists and adjusts the running byte total.</summary>
    string AppendToFile(string path, ReadOnlyMemory<byte> data);

    /// <summary>Appends UTF-8 text onto a tracked file that already exists and adjusts the running byte total.</summary>
    string AppendToFile(string path, string text);

    /// <summary>Appends bytes onto a tracked file asynchronously.</summary>
    Task<string> AppendToFileAsync(string path, ReadOnlyMemory<byte> data, CancellationToken ct = default);

    /// <summary>Appends UTF-8 text onto a tracked file asynchronously.</summary>
    Task<string> AppendToFileAsync(string path, string text, CancellationToken ct = default);

    /// <summary>Replaces an existing session file with UTF-8 text. The file must already exist. Byte tracking is updated to the new length.</summary>
    string WriteFile(string path, string text);

    /// <summary>Replaces an existing session file with bytes. The file must already exist. Byte tracking is updated to the new length.</summary>
    string WriteFile(string path, ReadOnlyMemory<byte> data);

    /// <summary>Replaces an existing file with UTF-8 text asynchronously.</summary>
    Task<string> WriteFileAsync(string path, string text, CancellationToken ct = default);

    /// <summary>Replaces an existing file with bytes asynchronously.</summary>
    Task<string> WriteFileAsync(string path, ReadOnlyMemory<byte> data, CancellationToken ct = default);

    /// <summary>Removes a file from disk and from session tracking. <c>true</c> if it existed and was deleted; <c>false</c> if it was already gone.</summary>
    bool DeleteFile(string path);

    /// <summary>
    /// Removes a directory and everything in it from disk, and drops it (plus any tracked files/dirs inside) from session tracking. <c>true</c> if it existed and was deleted;
    /// <c>false</c> if it was already gone.
    /// </summary>
    bool DeleteDirectory(string path);

    /// <summary>Resolves a path under <see cref="SessionDirectory" /> without creating a file.</summary>
    /// <param name="name">Relative path, or a generated name when null or whitespace.</param>
    /// <returns>Absolute path inside the session.</returns>
    string GetFilePath(string? name = null);

    /// <summary>Creates an empty tracked file using the same naming rules as <see cref="GetFilePath" />.</summary>
    string TouchFile(string? name = null);

    /// <summary>Creates a tracked UTF-8 text file with a generated name.</summary>
    string CreateFile(string text);

    /// <summary>Creates a tracked file from bytes, optionally at a relative name.</summary>
    string CreateFile(ReadOnlyMemory<byte> data, string? name = null);

    /// <summary>Creates a tracked file by copying a readable stream into the session.</summary>
    string CreateFile(Stream data, string? name = null);

    /// <summary>Creates a tracked UTF-8 text file with a generated name asynchronously.</summary>
    Task<string> CreateFileAsync(string text, CancellationToken ct = default);

    /// <summary>Creates a tracked file from bytes asynchronously.</summary>
    Task<string> CreateFileAsync(ReadOnlyMemory<byte> data, string? name = null, CancellationToken ct = default);

    /// <summary>Creates a tracked file from a stream asynchronously.</summary>
    Task<string> CreateFileAsync(Stream data, string? name = null, CancellationToken ct = default);

    /// <summary>Resolves a directory path under <see cref="SessionDirectory" /> without creating it.</summary>
    string GetDirectoryPath(string? name = null);

    /// <summary>Creates a tracked subdirectory and returns its absolute path.</summary>
    string CreateDirectory(string? name = null);

    /// <summary>Creates a tracked subdirectory asynchronously. The default implementation finishes synchronously.</summary>
    Task<string> CreateDirectoryAsync(string? name = null, CancellationToken ct = default);
}
