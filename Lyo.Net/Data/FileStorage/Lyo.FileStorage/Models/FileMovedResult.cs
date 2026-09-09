using System.Diagnostics;

namespace Lyo.FileStorage.Models;

/// <summary>Payload raised on <c>FileMoved</c>. <see cref="File" /> is a redacted snapshot. Wrapped DEK and KEK salt are omitted.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileMovedResult(Guid FileId, FileStoreSnapshot File, string? PreviousPathPrefix)
    : FileStorageResult(FileId, DateTime.UtcNow)
{
    /// <inheritdoc />
    public override string ToString() => $"FileMovedResult: FileId={FileId}, PreviousPathPrefix={PreviousPathPrefix ?? "(none)"}, PathPrefix={File.PathPrefix ?? "(none)"}";
}
