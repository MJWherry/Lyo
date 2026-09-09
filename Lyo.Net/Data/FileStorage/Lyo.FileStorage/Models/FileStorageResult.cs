using System.Diagnostics;

namespace Lyo.FileStorage.Models;

/// <summary>Shared event payload for file-storage work: save, retrieve, delete, move, and rename.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public abstract record FileStorageResult(Guid FileId, DateTime Timestamp)
{
    /// <inheritdoc />
    public override string ToString() => $"{GetType().Name}: FileId={FileId}, Timestamp={Timestamp:u}";
}
