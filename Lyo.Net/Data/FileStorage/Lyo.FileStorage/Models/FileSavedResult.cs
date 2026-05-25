namespace Lyo.FileStorage.Models;

/// <summary>
/// Event payload for <c>FileSaved</c>. <see cref="File" /> is a redacted snapshot — sensitive fields such as the wrapped DEK and KEK salt are omitted.
/// Subscribers needing those fields should call <c>GetMetadataAsync</c> through an authorized service.
/// </summary>
public sealed record FileSavedResult(Guid FileId, FileStoreSnapshot File, long OriginalSize, long FinalSize, bool WasCompressed, bool WasEncrypted)
    : FileStorageResult(FileId, DateTime.UtcNow);
