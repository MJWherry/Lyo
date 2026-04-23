namespace Lyo.IO.Temp.Models;

// ReSharper disable once InconsistentNaming
public interface IIOTempSession : IAsyncDisposable, IDisposable
{
    string SessionDirectory { get; }

    IReadOnlyList<string> Files { get; }

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
    /// Creates a child session rooted inside this session's directory.
    /// The sub-session has its own file tracking and is independently disposable.
    /// It is also added to this session's <see cref="Directories"/> list.
    /// </summary>
    IIOTempSession CreateSubSession();

    /// <summary>
    /// Enumerates all files on disk under <see cref="SessionDirectory"/>, including those not tracked
    /// (e.g. written by external code). Supports optional glob <paramref name="pattern"/>.
    /// </summary>
    IEnumerable<string> EnumerateFiles(string? pattern = null);

    /// <summary>Enumerates all directories on disk under <see cref="SessionDirectory"/>.</summary>
    IEnumerable<string> EnumerateDirectories();

    /// <summary>
    /// Deletes all files and directories tracked in this session and resets byte tracking to zero.
    /// The session directory itself is preserved; the session remains usable after the call.
    /// </summary>
    void Clear();

    /// <summary>
    /// Copies a file or directory from <paramref name="sourcePath"/> into this session and starts tracking it.
    /// Returns the destination path.
    /// </summary>
    string CopyFrom(string sourcePath);

    /// <summary>Asynchronously copies a file or directory from <paramref name="sourcePath"/> into this session.</summary>
    Task<string> CopyFromAsync(string sourcePath, CancellationToken ct = default);

    /// <summary>
    /// Moves a file or directory from <paramref name="sourcePath"/> into this session, removing the source.
    /// Uses a fast rename when possible, falling back to copy-and-delete across devices.
    /// Returns the destination path.
    /// </summary>
    string MoveFrom(string sourcePath);

    /// <summary>Asynchronously moves a file or directory from <paramref name="sourcePath"/> into this session.</summary>
    Task<string> MoveFromAsync(string sourcePath, CancellationToken ct = default);

    /// <summary>Appends raw bytes to an existing tracked file. Updates the running byte total.</summary>
    string AppendToFile(string path, ReadOnlyMemory<byte> data);

    /// <summary>Appends UTF-8 text to an existing tracked file. Updates the running byte total.</summary>
    string AppendToFile(string path, string text);

    /// <summary>Asynchronously appends raw bytes to an existing tracked file.</summary>
    Task<string> AppendToFileAsync(string path, ReadOnlyMemory<byte> data, CancellationToken ct = default);

    /// <summary>Asynchronously appends UTF-8 text to an existing tracked file.</summary>
    Task<string> AppendToFileAsync(string path, string text, CancellationToken ct = default);

    /// <summary>
    /// Overwrites the content of a file within this session with the provided UTF-8 text.
    /// The file must already exist. Updates byte tracking to reflect the new size.
    /// </summary>
    string WriteFile(string path, string text);

    /// <summary>
    /// Overwrites the content of a file within this session with the provided bytes.
    /// The file must already exist. Updates byte tracking to reflect the new size.
    /// </summary>
    string WriteFile(string path, ReadOnlyMemory<byte> data);

    /// <summary>Asynchronously overwrites the content of a file with UTF-8 text.</summary>
    Task<string> WriteFileAsync(string path, string text, CancellationToken ct = default);

    /// <summary>Asynchronously overwrites the content of a file with bytes.</summary>
    Task<string> WriteFileAsync(string path, ReadOnlyMemory<byte> data, CancellationToken ct = default);

    /// <summary>
    /// Deletes a file from disk and removes it from session tracking.
    /// Returns <c>true</c> if the file existed and was deleted; <c>false</c> if it was already absent.
    /// </summary>
    bool DeleteFile(string path);

    /// <summary>
    /// Deletes a directory and all its contents from disk and removes it (and any tracked files/dirs inside it) from session tracking.
    /// Returns <c>true</c> if the directory existed and was deleted; <c>false</c> if it was already absent.
    /// </summary>
    bool DeleteDirectory(string path);

    string GetFilePath(string? name = null);

    string TouchFile(string? name = null);

    string CreateFile(string text);

    string CreateFile(ReadOnlyMemory<byte> data, string? name = null);

    string CreateFile(Stream data, string? name = null);

    // Async files
    Task<string> CreateFileAsync(string text, CancellationToken ct = default);

    Task<string> CreateFileAsync(ReadOnlyMemory<byte> data, string? name = null, CancellationToken ct = default);

    Task<string> CreateFileAsync(Stream data, string? name = null, CancellationToken ct = default);

    string GetDirectoryPath(string? name = null);

    string CreateDirectory(string? name = null);

    Task<string> CreateDirectoryAsync(string? name = null, CancellationToken ct = default);
}
