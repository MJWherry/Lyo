namespace Lyo.FileStorage.Abstractions;

/// <summary>Polymorphic blob-level operations supplied by concrete file storage backends. Used internally by coordinators that cannot inherit template-method hooks directly.</summary>
internal interface IFileStoragePhysicalIo
{
    Task<Stream?> ReadFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct);

    Task<Stream> CreateOutputStreamAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct);

    Task<long> GetStorageSizeAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct);

    Task<FileStorageServiceBase.EncryptionHeaderInfo> ExtractEncryptionHeaderAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct);

    Task UpdateFileHeaderAsync(Guid fileId, string? pathPrefix, string targetKeyId, string targetKeyVersion, byte[] newEncryptedDek, CancellationToken ct);
}