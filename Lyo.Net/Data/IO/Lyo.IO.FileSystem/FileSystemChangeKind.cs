namespace Lyo.IO.FileSystem;

/// <summary>Kind of change reported by <see cref="IFileSystemWatch" />.</summary>
public enum FileSystemChangeKind
{
    /// <summary>A file or directory was created.</summary>
    Created = 0,

    /// <summary>A file or directory was deleted.</summary>
    Deleted = 1,

    /// <summary>A file's content or a directory's contents changed.</summary>
    Changed = 2,

    /// <summary>A file or directory moved to a different parent.</summary>
    Moved = 3,

    /// <summary>A file or directory was renamed inside the same parent.</summary>
    Renamed = 4
}
