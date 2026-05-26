using Lyo.FileMetadataStore.Models;

namespace Lyo.FileStorage.Abstractions;

/// <summary>Metadata reads through the owning <see cref="FileStorageServiceBase" /> virtual <c>GetMetadataAsync</c> path (e.g. diagnostics hooks).</summary>
internal interface IFileStorageMetadataLookup
{
    Task<FileStoreResult> GetMetadataForStorageAsync(Guid fileId, CancellationToken ct);
}