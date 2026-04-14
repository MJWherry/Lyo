namespace Lyo.FileStorage.Models;

public sealed record FileDeletedResult(Guid FileId, bool Success, string? ErrorMessage = null)
    : FileStorageResult(FileId, DateTime.UtcNow);