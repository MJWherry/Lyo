using Lyo.FileMetadataStore.Models;

namespace Lyo.FileStorage.Models;

public sealed record FileMetadataRetrievedResult(Guid FileId, FileStoreResult FileStoreResult)
    : FileStorageResult(FileId, DateTime.UtcNow);