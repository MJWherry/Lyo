namespace Lyo.FileStorage.Models;

/// <summary>Parameters for relocating an existing stored file under a new path prefix (same file id).</summary>
public sealed record MoveFileRequest
{
    /// <summary>
    /// Target path prefix. Null or empty clears the logical prefix (backends may fall back to default sharding). Must pass the same path-prefix safety checks as save/copy.
    /// </summary>
    public string? PathPrefix { get; init; }
}
