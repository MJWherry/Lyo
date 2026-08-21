using System.Diagnostics;

namespace Lyo.FileStorage.Models;

/// <summary>Event payload for <c>FileDeleted</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileDeletedResult(Guid FileId, bool Success, string? ErrorMessage = null)
    : FileStorageResult(FileId, DateTime.UtcNow)
{
    /// <inheritdoc />
    public override string ToString()
        => $"FileDeletedResult: FileId={FileId}, Success={Success}{(ErrorMessage == null ? "" : $", Error={ErrorMessage}")}";
}
