namespace Lyo.FileSystemWatcher.Models;

/// <summary>One directory node in a persistable snapshot tree.</summary>
public sealed class FileSystemSnapshotDirectoryDto
{
    /// <summary>Relative path from the snapshot root using '/' separators. Empty at the root.</summary>
    public string RelativePath { get; set; } = "";

    /// <summary>Immediate child directories keyed by segment name.</summary>
    public Dictionary<string, FileSystemSnapshotDirectoryDto> Directories { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Files in this directory keyed by file name.</summary>
    public Dictionary<string, FileSystemSnapshotEntryDto> Files { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
