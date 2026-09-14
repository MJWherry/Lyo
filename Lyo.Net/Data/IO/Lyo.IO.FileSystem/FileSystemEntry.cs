using System.Diagnostics;

namespace Lyo.IO.FileSystem;

/// <summary>One file or directory returned by <see cref="IFileSystem.ListDirectory" />.</summary>
/// <param name="Path">Absolute path under the file system's root jail.</param>
/// <param name="Name">Last path segment shown in a tree.</param>
/// <param name="IsDirectory">True when the entry is a directory.</param>
/// <param name="Length">File length in bytes; zero for directories unless the implementation sets something else.</param>
/// <param name="CreatedAtUtc">Creation time in UTC when the backend reports it.</param>
/// <param name="LastWriteTimeUtc">Last write time in UTC when the backend reports it.</param>
/// <param name="Properties">Optional extra fields (for example FileStorage <c>FileId</c>).</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileSystemEntry(
    string Path,
    string Name,
    bool IsDirectory,
    long Length,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastWriteTimeUtc,
    IReadOnlyDictionary<string, string>? Properties = null)
{
    /// <inheritdoc />
    public override string ToString()
        => IsDirectory ? $"dir {Name} ({Path})" : $"file {Name} ({Path}, {Length} bytes)";
}
