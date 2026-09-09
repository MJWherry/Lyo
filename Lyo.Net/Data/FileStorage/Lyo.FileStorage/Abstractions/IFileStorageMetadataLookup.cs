using Lyo.FileMetadataStore.Models;

namespace Lyo.FileStorage.Abstractions;

/// <summary>Reads metadata through the owning <see cref="FileStorageServiceBase" /> virtual <c>GetMetadataAsync</c> path (diagnostics hooks, for example).</summary>
internal interface IFileStorageMetadataLookup
{
    Task<FileStoreResult> GetMetadataForStorageAsync(Guid fileId, CancellationToken ct);
}