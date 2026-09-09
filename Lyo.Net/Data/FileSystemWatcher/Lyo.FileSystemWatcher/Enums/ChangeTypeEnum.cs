namespace Lyo.FileSystemWatcher.Enums;

/// <summary>Kind of file-system change that was detected.</summary>
public enum ChangeTypeEnum
{
    /// <summary>Unknown change. Should not appear during normal operation.</summary>
    Unknown = 0,

    /// <summary>A file or directory was created.</summary>
    Created = 1,

    /// <summary>A file's content changed, or a directory's contents changed (items added, removed, or modified).</summary>
    Changed = 2,

    /// <summary>A file or directory was deleted.</summary>
    Deleted = 3,

    /// <summary>A file or directory was renamed, meaning it moved inside the same parent directory.</summary>
    Renamed = 4,

    /// <summary>A file or directory was moved to a different parent directory.</summary>
    Moved = 5
}