using System.Diagnostics;

namespace Lyo.FileSystemWatcher;

/// <summary>One file or directory captured in a <see cref="SnapshotTree" />, with optional content hash / sparse fingerprint / size for change detection.</summary>
/// <param name="Path">Full path to the file or directory.</param>
/// <param name="Info"><see cref="FileInfo" /> or <see cref="DirectoryInfo" /> from the file system.</param>
/// <param name="Hash">Full-file MD5 hex when computed for disambiguation; otherwise null.</param>
/// <param name="Fingerprint">Sparse fingerprint hex when available; otherwise null.</param>
/// <param name="FileSize">File length in bytes when known.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DirectorySnapshotEntry(string Path, FileSystemInfo Info, string? Hash = null, string? Fingerprint = null, long? FileSize = null)
{
    public override string ToString() => $"{Path}: {Hash}";
}