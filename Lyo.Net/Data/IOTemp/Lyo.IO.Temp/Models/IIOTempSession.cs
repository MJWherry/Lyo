namespace Lyo.IO.Temp.Models;

/// <summary>A scoped workspace under a root directory: tracks created files and directories, enforces limits, and deletes its <see cref="SessionDirectory" /> tree when disposed.</summary>
/// <remarks>UTF-8 text operations use UTF-8 without a BOM.</remarks>
// ReSharper disable once InconsistentNaming
public interface IIOTempSession : IAsyncDisposable, IDisposable
{
    /// <summary>Absolute path to this session's root directory.</summary>
    string SessionDirectory { get; }

    /// <summary>Full paths of files tracked by this session (created or imported through the session API).</summary>
    IReadOnlyList<string> Files { get; }

    /// <summary>Full paths of directories tracked by this session (excluding the session root unless explicitly added).</summary>
    IReadOnlyList<string> Directories { get; }

    /// <summary>Generates random files and simulated directory trees within this session.</summary>
    IIOTempFileGenerator Generator { get; }

    /// <summary>Raised each time a file is successfully created (or copied) into this session. Argument is the full path.</summary>
    event Action<string>? FileCreated;

    /// <summary>Raised each time a directory is successfully created (or copied) into this session. Argument is the full path.</summary>
    event Action<string>? DirectoryCreated;

    /// <summary>Returns the cumulative number of bytes written to tracked files in this session.</summary>
    long GetTotalBytesUsed();

    /// <summary>Returns a point-in-time snapshot of files, directories, byte usage, and creation time.</summary>
    TempSessionSnapshot GetSnapshot();

    /// <summary>
    /// Creates a child session rooted inside this session's directory. The sub-session has its own file tracking and is independently disposable. It is also added to this
    /// session's <see cref="Directories" /> list.
    /// </summary>
    IIOTempSession CreateSubSession();

    /// <summary>
    /// Enumerates all files on disk under <see cref="SessionDirectory" />, including those not tracked (e.g. written by external code). Supports optional glob
    /// <paramref name="pattern" />.
    /// </summary>
    /// <param name="pattern">Optional file-name pattern; <c>*</c> matches all, otherwise simple <c>*</c> substring segments are supported.</param>
    IEnumerable<string> EnumerateFiles(string? pattern = null);

    /// <summary>Enumerates all directories on disk under <see cref="SessionDirectory" />.</summary>
    IEnumerable<string> EnumerateDirectories();

    /// <summary>
    /// Deletes all files and directories tracked in this session and resets byte tracking to zero. The session directory itself is preserved; the session remains usable after
    /// the call.
    /// </summary>
    void Clear();

    /// <summary>Copies a file or directory from <paramref name="sourcePath" /> into this session and starts tracking it. Returns the destination path.</summary>
    string CopyFrom(string sourcePath);

    /// <summary>Asynchronously copies a file or directory from <paramref name="sourcePath" /> into this session.</summary>
    Task<string> CopyFromAsync(string sourcePath, CancellationToken ct = default);

    /// <summary>
    /// Moves a file or directory from <paramref name="sourcePath" /> into this session, removing the source. Uses a fast rename when possible, falling back to copy-and-delete
    /// across devices. Returns the destination path.
    /// </summary>
    string MoveFrom(string sourcePath);

    /// <summary>Asynchronously moves a file or directory from <paramref name="sourcePath" /> into this session.</summary>
    Task<string> MoveFromAsync(string sourcePath, CancellationToken ct = default);

    /// <summary>Appends raw bytes to an existing tracked file. Updates the running byte total.</summary>
    string AppendToFile(string path, ReadOnlyMemory<byte> data);

    /// <summary>Appends UTF-8 text to an existing tracked file. Updates the running byte total.</summary>
    string AppendToFile(string path, string text);

    /// <summary>Asynchronously appends raw bytes to an existing tracked file.</summary>
    Task<string> AppendToFileAsync(string path, ReadOnlyMemory<byte> data, CancellationToken ct = default);

    /// <summary>Asynchronously appends UTF-8 text to an existing tracked file.</summary>
    Task<string> AppendToFileAsync(string path, string text, CancellationToken ct = default);

    /// <summary>Overwrites the content of a file within this session with the provided UTF-8 text. The file must already exist. Updates byte tracking to reflect the new size.</summary>
    string WriteFile(string path, string text);

    /// <summary>Overwrites the content of a file within this session with the provided bytes. The file must already exist. Updates byte tracking to reflect the new size.</summary>
    string WriteFile(string path, ReadOnlyMemory<byte> data);

    /// <summary>Asynchronously overwrites the content of a file with UTF-8 text.</summary>
    Task<string> WriteFileAsync(string path, string text, CancellationToken ct = default);

    /// <summary>Asynchronously overwrites the content of a file with bytes.</summary>
    Task<string> WriteFileAsync(string path, ReadOnlyMemory<byte> data, CancellationToken ct = default);

    /// <summary>Deletes a file from disk and removes it from session tracking. Returns <c>true</c> if the file existed and was deleted; <c>false</c> if it was already absent.</summary>
    bool DeleteFile(string path);

    /// <summary>
    /// Deletes a directory and all its contents from disk and removes it (and any tracked files/dirs inside it) from session tracking. Returns <c>true</c> if the directory
    /// existed and was deleted; <c>false</c> if it was already absent.
    /// </summary>
    bool DeleteDirectory(string path);

    /// <summary>Builds a path under <see cref="SessionDirectory" /> without creating a file.</summary>
    /// <param name="name">Optional relative path; a generated name is used if null or whitespace.</param>
    /// <returns>Absolute path within the session.</returns>
    string GetFilePath(string? name = null);

    /// <summary>Creates an empty tracked file at the resolved path (see <see cref="GetFilePath" /> naming rules).</summary>
    string TouchFile(string? name = null);

    /// <summary>Creates a new UTF-8 text file with generated name and tracks it.</summary>
    string CreateFile(string text);

    /// <summary>Creates a new file with the given bytes and optional relative name.</summary>
    string CreateFile(ReadOnlyMemory<byte> data, string? name = null);

    /// <summary>Creates a new file by copying a readable stream into the session.</summary>
    string CreateFile(Stream data, string? name = null);

    /// <summary>Creates a new UTF-8 text file with a generated name.</summary>
    Task<string> CreateFileAsync(string text, CancellationToken ct = default);

    /// <summary>Creates a new file from bytes asynchronously.</summary>
    Task<string> CreateFileAsync(ReadOnlyMemory<byte> data, string? name = null, CancellationToken ct = default);

    /// <summary>Creates a new file from a stream asynchronously.</summary>
    Task<string> CreateFileAsync(Stream data, string? name = null, CancellationToken ct = default);

    /// <summary>Builds a directory path under <see cref="SessionDirectory" /> without creating the directory.</summary>
    string GetDirectoryPath(string? name = null);

    /// <summary>Creates a tracked subdirectory and returns its full path.</summary>
    string CreateDirectory(string? name = null);

    /// <summary>Creates a tracked subdirectory asynchronously (currently completes synchronously on the default implementation).</summary>
    Task<string> CreateDirectoryAsync(string? name = null, CancellationToken ct = default);
}