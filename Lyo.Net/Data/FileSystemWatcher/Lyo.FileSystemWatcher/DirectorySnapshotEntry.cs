using System.Diagnostics;

namespace Lyo.FileSystemWatcher;

/// <summary>One file or directory stored in a <see cref="SnapshotTree" />, plus optional content hash, sparse fingerprint, and size used for change detection.</summary>
/// <param name="Path">Full path of the file or directory.</param>
/// <param name="Info"><see cref="FileInfo" /> or <see cref="DirectoryInfo" /> from the file system.</param>
/// <param name="Hash">Full-file MD5 hex when computed to disambiguate; otherwise null.</param>
/// <param name="Fingerprint">Sparse fingerprint hex when one was taken; otherwise null.</param>
/// <param name="FileSize">File size in bytes when known.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DirectorySnapshotEntry(string Path, FileSystemInfo Info, string? Hash = null, string? Fingerprint = null, long? FileSize = null)
{
    public override string ToString() => $"{Path}: {Hash}";
}