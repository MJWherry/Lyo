using System.Text;

namespace Lyo.IO.Temp.Storage;

/// <summary>
/// Abstracts all file-system I/O performed by <see cref="IOTempService" />, <see cref="Lyo.IO.Temp.Models.IOTempSession" />, and
/// <see cref="Lyo.IO.Temp.Models.IOTempFileGenerator" />. Implement this interface to swap in a different storage backend (in-memory, cloud blob, etc.) without changing IOTemp
/// business logic.
/// </summary>
// ReSharper disable once InconsistentNaming
public interface IIOTempStorageProvider
{
    /// <summary>The root path under which all service and session directories are created.</summary>
    string RootPath { get; }

    /// <summary>Returns whether <paramref name="path" /> exists and is a directory.</summary>
    bool DirectoryExists(string path);

    /// <summary>Creates a directory at <paramref name="path" />, including parents if required by the implementation.</summary>
    void CreateDirectory(string path);

    /// <summary>Deletes <paramref name="path" /> and all its contents recursively. No-op if it does not exist.</summary>
    void DeleteDirectory(string path);

    /// <summary>Returns immediate children (files and directories) of <paramref name="path" />.</summary>
    IEnumerable<ProviderEntryInfo> EnumerateEntries(string path);

    /// <summary>
    /// Validates that the directory at <paramref name="path" /> is readable and writable. Throw if access is denied; implementations backed by in-memory storage may treat this
    /// as a no-op.
    /// </summary>
    void EnsureDirectoryAccessible(string path);

    /// <summary>Returns whether <paramref name="path" /> exists and is a file.</summary>
    bool FileExists(string path);

    /// <summary>Creates an empty file at <paramref name="path" />, overwriting any existing file.</summary>
    void TouchFile(string path);

    /// <summary>Writes bytes to <paramref name="path" />, replacing any existing file.</summary>
    void WriteAllBytes(string path, byte[] data);

    /// <summary>Writes text to <paramref name="path" /> using <paramref name="encoding" />.</summary>
    void WriteAllText(string path, string text, Encoding encoding);

    /// <summary>Appends text to <paramref name="path" /> using <paramref name="encoding" />.</summary>
    void AppendAllText(string path, string text, Encoding encoding);

    /// <summary>Deletes a file. No-op or tolerant behaviour if the file does not exist is implementation-defined.</summary>
    void DeleteFile(string path);

    /// <summary>Moves or renames a file from <paramref name="source" /> to <paramref name="dest" />.</summary>
    void MoveFile(string source, string dest);

    /// <summary>Copies a file from <paramref name="source" /> to <paramref name="dest" />.</summary>
    void CopyFile(string source, string dest);

    /// <summary>Opens <paramref name="path" /> for sequential read access.</summary>
    Stream OpenRead(string path);

    /// <summary>Opens <paramref name="path" /> for write, truncating or creating the file.</summary>
    Stream OpenCreate(string path);

    /// <summary>Opens <paramref name="path" /> for append.</summary>
    Stream OpenAppend(string path);

    /// <summary>Returns the length in bytes of the file at <paramref name="path" />.</summary>
    long GetFileLength(string path);

    /// <summary>Returns the creation time of the file at <paramref name="path" /> in UTC.</summary>
    DateTimeOffset GetFileCreationTimeUtc(string path);

    /// <summary>Asynchronously writes bytes to <paramref name="path" />.</summary>
    Task WriteAllBytesAsync(string path, byte[] data, CancellationToken ct);

    /// <summary>Asynchronously writes text to <paramref name="path" />.</summary>
    Task WriteAllTextAsync(string path, string text, Encoding encoding, CancellationToken ct);

    /// <summary>Asynchronously appends text to <paramref name="path" />.</summary>
    Task AppendAllTextAsync(string path, string text, Encoding encoding, CancellationToken ct);

    /// <summary>Copies <paramref name="source" /> into <paramref name="destPath" />.</summary>
    Task CopyStreamToFileAsync(Stream source, string destPath, CancellationToken ct);

    /// <summary>Asynchronously copies a file from <paramref name="source" /> to <paramref name="dest" />.</summary>
    Task CopyFileAsync(string source, string dest, CancellationToken ct);
}