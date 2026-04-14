namespace Lyo.FileStorage.Models;

public sealed record FileRetrievedResult(Guid FileId, long FileSize, bool WasCompressed, bool WasEncrypted)
    : FileStorageResult(FileId, DateTime.UtcNow);