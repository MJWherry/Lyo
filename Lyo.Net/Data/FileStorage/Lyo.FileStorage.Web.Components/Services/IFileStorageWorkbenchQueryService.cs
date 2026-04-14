using Lyo.FileMetadataStore.Models;
using Lyo.Keystore;

namespace Lyo.FileStorage.Web.Components.Services;

public interface IFileStorageWorkbenchQueryService
{
    Task<IReadOnlyList<FileStoreResult>> SearchFilesAsync(FileStorageWorkbenchFileQuery query, CancellationToken ct = default);

    Task<IReadOnlyList<FileStorageWorkbenchKeyRecord>> SearchKeysAsync(FileStorageWorkbenchKeyQuery query, CancellationToken ct = default);
}

public sealed record FileStorageWorkbenchFileQuery(string? SearchText = null, string? KeyId = null, string? KeyVersion = null, int Take = 25);

public sealed record FileStorageWorkbenchKeyQuery(string? SearchText = null, int Take = 25);

public sealed record FileStorageWorkbenchKeyRecord(string KeyId, string Version, bool IsCurrent, KeyMetadata? Metadata = null, int? FileCount = null);
