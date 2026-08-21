using System.Diagnostics;

namespace Lyo.FileStorage.Models;

/// <summary>Event payload for <c>FileMetadataRetrieved</c>. <see cref="File" /> is a redacted snapshot — sensitive fields such as the wrapped DEK and KEK salt are omitted.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileMetadataRetrievedResult(Guid FileId, FileStoreSnapshot File)
    : FileStorageResult(FileId, DateTime.UtcNow)
{
    /// <inheritdoc />
    public override string ToString() => $"FileMetadataRetrievedResult: FileId={FileId}, OriginalFileName={File.OriginalFileName}";
}
