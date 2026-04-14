using Lyo.FileMetadataStore.Models;

namespace Lyo.FileStorage.Models;

public sealed record FileSavedResult(Guid FileId, FileStoreResult FileStoreResult, long OriginalSize, long FinalSize, bool WasCompressed, bool WasEncrypted)
    : FileStorageResult(FileId, DateTime.UtcNow);