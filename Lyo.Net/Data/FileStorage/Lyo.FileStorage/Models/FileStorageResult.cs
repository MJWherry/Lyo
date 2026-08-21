using System.Diagnostics;

namespace Lyo.FileStorage.Models;

/// <summary>Base event payload for file-storage operations (save, retrieve, delete, move, rename).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public abstract record FileStorageResult(Guid FileId, DateTime Timestamp)
{
    /// <inheritdoc />
    public override string ToString() => $"{GetType().Name}: FileId={FileId}, Timestamp={Timestamp:u}";
}
