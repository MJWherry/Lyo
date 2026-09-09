namespace Lyo.FileStorage.Abstractions;

/// <summary>Blob-level operations from concrete backends. Coordinators that cannot inherit template-method hooks call this surface.</summary>
internal interface IFileStoragePhysicalIO
{
    Task<Stream?> ReadFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct);

    Task<Stream> CreateOutputStreamAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct);

    Task<long> GetStorageSizeAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct);

    Task<FileStorageServiceBase.EncryptionHeaderInfo> ExtractEncryptionHeaderAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct);

    Task UpdateFileHeaderAsync(Guid fileId, string? pathPrefix, string targetKeyId, string targetKeyVersion, byte[] newEncryptedDek, CancellationToken ct);
}