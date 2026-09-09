namespace Lyo.FileSystemWatcher.Models;

/// <summary>One file or directory in a persistable snapshot. Paths are relative to the snapshot root and use '/' separators.</summary>
public sealed class FileSystemSnapshotEntryDto
{
    /// <summary>Relative path from the snapshot root.</summary>
    public string Path { get; set; } = "";

    /// <summary>True when this entry is a directory.</summary>
    public bool IsDirectory { get; set; }

    /// <summary>File size in bytes when known. Null for directories or when size was not recorded.</summary>
    public long? FileSize { get; set; }

    /// <summary>UTC last-write time copied off the live <c>FileSystemInfo</c> at map time.</summary>
    public DateTime? LastWriteTimeUtc { get; set; }

    /// <summary>UTC creation time copied off the live <c>FileSystemInfo</c> at map time.</summary>
    public DateTime? CreationTimeUtc { get; set; }

    /// <summary>File-attribute flags as their integer value, when known.</summary>
    public int? Attributes { get; set; }

    /// <summary>Full-file MD5 hex when computed to disambiguate moves; otherwise null.</summary>
    public string? Hash { get; set; }

    /// <summary>Sparse fingerprint hex when one was taken; otherwise null.</summary>
    public string? Fingerprint { get; set; }
}
