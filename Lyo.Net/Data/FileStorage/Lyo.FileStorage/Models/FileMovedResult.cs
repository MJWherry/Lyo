using System.Diagnostics;

namespace Lyo.FileStorage.Models;

/// <summary>Event payload for <c>FileMoved</c>. <see cref="File" /> is a redacted snapshot — sensitive fields such as the wrapped DEK and KEK salt are omitted.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileMovedResult(Guid FileId, FileStoreSnapshot File, string? PreviousPathPrefix)
    : FileStorageResult(FileId, DateTime.UtcNow)
{
    /// <inheritdoc />
    public override string ToString() => $"FileMovedResult: FileId={FileId}, PreviousPathPrefix={PreviousPathPrefix ?? "(none)"}, PathPrefix={File.PathPrefix ?? "(none)"}";
}
