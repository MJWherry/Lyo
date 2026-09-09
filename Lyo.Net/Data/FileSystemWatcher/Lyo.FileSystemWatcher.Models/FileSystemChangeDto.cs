namespace Lyo.FileSystemWatcher.Models;

/// <summary>One persistable file-system change. Paths are relative to the snapshot root and use '/' separators.</summary>
public sealed class FileSystemChangeDto
{
    /// <summary>Previous relative path. Null when the item was created.</summary>
    public string? OldPath { get; set; }

    /// <summary>New relative path. Null when the item was deleted.</summary>
    public string? NewPath { get; set; }

    /// <summary>Kind of change that happened.</summary>
    public FileSystemChangeKind ChangeType { get; set; }

    /// <summary>True when the change is about a directory; false when it is about a file.</summary>
    public bool IsDirectory { get; set; }

    /// <summary>For directory changes, how many files were in the directory before. Null for file changes.</summary>
    public int? OldFileCount { get; set; }

    /// <summary>For directory changes, how many subdirectories existed before. Null for file changes.</summary>
    public int? OldDirectoryCount { get; set; }

    /// <summary>For directory changes, how many files are in the directory after. Null for file changes.</summary>
    public int? NewFileCount { get; set; }

    /// <summary>For directory changes, how many subdirectories exist after. Null for file changes.</summary>
    public int? NewDirCount { get; set; }

    /// <summary>UTC time the change was observed. Set by the mapper when converting a live event.</summary>
    public DateTime OccurredAtUtc { get; set; }
}
