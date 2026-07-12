namespace Lyo.FileStorage.Models;

/// <summary>
/// Event payload for <c>FileRenamed</c>. <see cref="File" /> is a redacted snapshot — sensitive fields such as the wrapped DEK and KEK salt are omitted.
/// </summary>
public sealed record FileRenamedResult(Guid FileId, FileStoreSnapshot File, string? PreviousOriginalFileName)
    : FileStorageResult(FileId, DateTime.UtcNow);
