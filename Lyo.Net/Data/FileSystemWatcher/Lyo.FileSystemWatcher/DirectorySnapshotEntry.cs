using System.Diagnostics;

namespace Lyo.FileSystemWatcher;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record DirectorySnapshotEntry(string Path, FileSystemInfo Info, string? Hash = null, string? Fingerprint = null, long? FileSize = null)
{
    public override string ToString() => $"{Path}: {Hash}";
}