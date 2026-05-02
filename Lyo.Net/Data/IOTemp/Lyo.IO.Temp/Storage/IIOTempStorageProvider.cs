using System.Text;

namespace Lyo.IO.Temp.Storage;

/// <summary>
/// Abstracts all file-system I/O performed by <c>IOTempService</c>, <c>IOTempSession</c>, and <c>IOTempFileGenerator</c>.  Implement this interface to swap in a different
/// storage backend (in-memory, FTP, SFTP, Azure Blob, etc.) without changing any IOTemp business logic.
/// </summary>
// ReSharper disable once InconsistentNaming
public interface IIOTempStorageProvider
{
    /// <summary>The root path under which all service and session directories are created.</summary>
    string RootPath { get; }

    bool DirectoryExists(string path);

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

    bool FileExists(string path);

    /// <summary>Creates an empty file at <paramref name="path" />, overwriting any existing file.</summary>
    void TouchFile(string path);

    void WriteAllBytes(string path, byte[] data);

    void WriteAllText(string path, string text, Encoding encoding);

    void AppendAllText(string path, string text, Encoding encoding);

    void DeleteFile(string path);

    void MoveFile(string source, string dest);

    void CopyFile(string source, string dest);

    Stream OpenRead(string path);

    Stream OpenCreate(string path);

    Stream OpenAppend(string path);

    long GetFileLength(string path);

    DateTimeOffset GetFileCreationTimeUtc(string path);

    Task WriteAllBytesAsync(string path, byte[] data, CancellationToken ct);

    Task WriteAllTextAsync(string path, string text, Encoding encoding, CancellationToken ct);

    Task AppendAllTextAsync(string path, string text, Encoding encoding, CancellationToken ct);

    Task CopyStreamToFileAsync(Stream source, string destPath, CancellationToken ct);

    Task CopyFileAsync(string source, string dest, CancellationToken ct);
}