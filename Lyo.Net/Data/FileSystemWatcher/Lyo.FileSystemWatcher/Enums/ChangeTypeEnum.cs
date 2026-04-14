namespace Lyo.FileSystemWatcher.Enums;

/// <summary>Represents the type of file system change that occurred.</summary>
public enum ChangeTypeEnum
{
    /// <summary>Unknown change type (should not occur in normal operation).</summary>
    Unknown = 0,

    /// <summary>A file or directory was created.</summary>
    Created = 1,

    /// <summary>A file's content was modified, or a directory's content changed (files/directories added/removed/modified).</summary>
    Changed = 2,

    /// <summary>A file or directory was deleted.</summary>
    Deleted = 3,

    /// <summary>A file or directory was renamed (moved within the same parent directory).</summary>
    Renamed = 4,

    /// <summary>A file or directory was moved to a different parent directory.</summary>
    Moved = 5
}