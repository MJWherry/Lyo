using System.Diagnostics;

namespace Lyo.IO.FileSystem;

/// <summary>One change from <see cref="IFileSystemWatch.Changed" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class FileSystemChange : EventArgs
{
    /// <summary>Builds a change event.</summary>
    /// <param name="kind">Created, deleted, changed, moved, or renamed.</param>
    /// <param name="isDirectory">True when the change is about a directory.</param>
    /// <param name="oldPath">Previous path. Null when the item was created.</param>
    /// <param name="newPath">New path. Null when the item was deleted.</param>
    public FileSystemChange(FileSystemChangeKind kind, bool isDirectory, string? oldPath, string? newPath)
    {
        Kind = kind;
        IsDirectory = isDirectory;
        OldPath = oldPath;
        NewPath = newPath;
    }

    /// <summary>Created, deleted, changed, moved, or renamed.</summary>
    public FileSystemChangeKind Kind { get; }

    /// <summary>True when the change is about a directory.</summary>
    public bool IsDirectory { get; }

    /// <summary>Previous path. Null when the item was created.</summary>
    public string? OldPath { get; }

    /// <summary>New path. Null when the item was deleted.</summary>
    public string? NewPath { get; }

    /// <inheritdoc />
    public override string ToString()
        => $"{Kind} {(IsDirectory ? "dir" : "file")}: Old={OldPath} | New={NewPath}";
}
