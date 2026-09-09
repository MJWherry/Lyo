using System.Text;
using Lyo.Common.Core.Pathing;

namespace Lyo.IO.Temp.Storage;

/// <summary>
/// File I/O used by <see cref="IOTempService" />, <see cref="Lyo.IO.Temp.Models.IOTempSession" />, and <see cref="Lyo.IO.Temp.Models.IOTempFileGenerator" />. Plug in another
/// backend (memory, blob storage, …) without changing IOTemp's session logic.
/// </summary>
// ReSharper disable once InconsistentNaming
public interface IIOTempStorageProvider
{
    /// <summary>Root folder for every service and session directory this provider creates.</summary>
    string RootPath { get; }

    /// <summary>Separator and normalization for this backend. Host on real disk; Posix for in-memory or remote stores.</summary>
    PathStyle PathStyle { get; }

    /// <summary>True when <paramref name="path" /> exists and is a directory.</summary>
    bool DirectoryExists(string path);

    /// <summary>Creates the directory at <paramref name="path" />, including parents if this implementation requires it.</summary>
    void CreateDirectory(string path);

    /// <summary>Deletes <paramref name="path" /> and everything under it. Does nothing if it is missing.</summary>
    void DeleteDirectory(string path);

    /// <summary>Immediate child files and directories of <paramref name="path" />.</summary>
    IEnumerable<ProviderEntryInfo> EnumerateEntries(string path);

    /// <summary>
    /// Checks that <paramref name="path" /> can be read and written. Throw on access denied. In-memory backends may no-op.
    /// </summary>
    void EnsureDirectoryAccessible(string path);

    /// <summary>True when <paramref name="path" /> exists and is a file.</summary>
    bool FileExists(string path);

    /// <summary>Creates an empty file at <paramref name="path" />, replacing any file already there.</summary>
    void TouchFile(string path);

    /// <summary>Writes bytes to <paramref name="path" />, replacing any existing file.</summary>
    void WriteAllBytes(string path, byte[] data);

    /// <summary>Writes text to <paramref name="path" /> with <paramref name="encoding" />.</summary>
    void WriteAllText(string path, string text, Encoding encoding);

    /// <summary>Appends text to <paramref name="path" /> with <paramref name="encoding" />.</summary>
    void AppendAllText(string path, string text, Encoding encoding);

    /// <summary>Deletes a file. Missing-file behaviour is up to the implementation (no-op or tolerant).</summary>
    void DeleteFile(string path);

    /// <summary>Moves or renames a file from <paramref name="source" /> to <paramref name="dest" />.</summary>
    void MoveFile(string source, string dest);

    /// <summary>Copies a file from <paramref name="source" /> to <paramref name="dest" />.</summary>
    void CopyFile(string source, string dest);

    /// <summary>Opens <paramref name="path" /> for sequential reads.</summary>
    Stream OpenRead(string path);

    /// <summary>Opens <paramref name="path" /> for write, creating or truncating the file.</summary>
    Stream OpenCreate(string path);

    /// <summary>Opens <paramref name="path" /> for append.</summary>
    Stream OpenAppend(string path);

    /// <summary>Byte length of the file at <paramref name="path" />.</summary>
    long GetFileLength(string path);

    /// <summary>UTC creation time of the file at <paramref name="path" />.</summary>
    DateTimeOffset GetFileCreationTimeUtc(string path);

    /// <summary>Writes bytes to <paramref name="path" /> asynchronously.</summary>
    Task WriteAllBytesAsync(string path, byte[] data, CancellationToken ct);

    /// <summary>Writes text to <paramref name="path" /> asynchronously.</summary>
    Task WriteAllTextAsync(string path, string text, Encoding encoding, CancellationToken ct);

    /// <summary>Appends text to <paramref name="path" /> asynchronously.</summary>
    Task AppendAllTextAsync(string path, string text, Encoding encoding, CancellationToken ct);

    /// <summary>Copies <paramref name="source" /> into <paramref name="destPath" />.</summary>
    Task CopyStreamToFileAsync(Stream source, string destPath, CancellationToken ct);

    /// <summary>Copies a file from <paramref name="source" /> to <paramref name="dest" /> asynchronously.</summary>
    Task CopyFileAsync(string source, string dest, CancellationToken ct);
}