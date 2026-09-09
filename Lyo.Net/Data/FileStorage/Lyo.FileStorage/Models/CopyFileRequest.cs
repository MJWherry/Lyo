using System.Diagnostics;

namespace Lyo.FileStorage.Models;

/// <summary>Inputs for copying a stored file onto a new file id.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record CopyFileRequest
{
    /// <summary>If set, the copy uses this path prefix. Otherwise the source prefix is reused.</summary>
    public string? PathPrefix { get; init; }

    /// <inheritdoc />
    public override string ToString() => $"CopyFileRequest: PathPrefix={PathPrefix ?? "(none)"}";
}
