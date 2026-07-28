namespace Lyo.FileStorage.Models;

/// <summary>Event payload for <c>FileMoved</c>. <see cref="File" /> is a redacted snapshot — sensitive fields such as the wrapped DEK and KEK salt are omitted.</summary>
public sealed record FileMovedResult(Guid FileId, FileStoreSnapshot File, string? PreviousPathPrefix)
    : FileStorageResult(FileId, DateTime.UtcNow);