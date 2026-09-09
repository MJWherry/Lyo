using System.Diagnostics;

namespace Lyo.FileStorage.Models;

/// <summary>Inputs for moving a stored file under a new path prefix. The file id stays the same.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record MoveFileRequest
{
    /// <summary>
    /// Target path prefix. Null or empty clears the logical prefix. Backends may fall back to default sharding. Must pass the same path-prefix safety checks as save and
    /// copy.
    /// </summary>
    public string? PathPrefix { get; init; }

    /// <inheritdoc />
    public override string ToString() => $"MoveFileRequest: PathPrefix={PathPrefix ?? "(none)"}";
}
