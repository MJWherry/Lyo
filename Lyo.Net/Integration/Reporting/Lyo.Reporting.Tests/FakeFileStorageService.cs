using Lyo.Compression.Models;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Models;
using Lyo.Health;

namespace Lyo.Reporting.Tests;

/// <summary>Minimal in-memory <see cref="IFileStorageService" /> for generation tests.</summary>
public sealed class FakeFileStorageService : IFileStorageService
{
    public Dictionary<Guid, byte[]> Files { get; } = new();

    public string HealthCheckName => nameof(FakeFileStorageService);

    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default) => Task.FromResult(HealthResult.Healthy(TimeSpan.Zero));

    public Task<FileStoreResult> SaveFileAsync(
        byte[] data,
        string? originalFileName = null,
        bool compress = false,
        bool encrypt = false,
        string? keyId = null,
        string? pathPrefix = null,
        int? chunkSize = null,
        string? contentType = null,
        string? tenantId = null,
        CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        Files[id] = data;
        return Task.FromResult(CreateResult(id, originalFileName, data.LongLength, contentType, pathPrefix));
    }

    public Task<FileStoreResult> SaveFileAsync(
        string filePath,
        string? originalFileName = null,
        bool compress = false,
        bool encrypt = false,
        string? keyId = null,
        string? pathPrefix = null,
        int? chunkSize = null,
        string? contentType = null,
        string? tenantId = null,
        CancellationToken ct = default)
        => SaveFileAsync(File.ReadAllBytes(filePath), originalFileName ?? Path.GetFileName(filePath), compress, encrypt, keyId, pathPrefix, chunkSize, contentType, tenantId, ct);

    public Task<FileStoreResult> SaveFromStreamAsync(
        Stream input,
        long declaredLength,
        string? originalFileName = null,
        bool compress = false,
        bool encrypt = false,
        string? keyId = null,
        string? pathPrefix = null,
        int? chunkSize = null,
        string? contentType = null,
        string? tenantId = null,
        FileAvailability? availabilityOverride = null,
        Guid? fileId = null,
        CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        input.CopyTo(ms);
        return SaveFileAsync(ms.ToArray(), originalFileName, compress, encrypt, keyId, pathPrefix, chunkSize, contentType, tenantId, ct);
    }

    public Task<string> GetPreSignedReadUrlAsync(Guid fileId, TimeSpan? expiration = null, string? pathPrefix = null, CancellationToken ct = default)
        => throw new NotSupportedException();

    public Task<string> GetPreSignedReadUrlAsync(Guid fileId, TimeSpan? expiration, string? pathPrefix, PreSignedReadUrlOptions? urlResponseOptions, CancellationToken ct = default)
        => throw new NotSupportedException();

    public Task<DirectUploadBeginResult> BeginDirectUploadAsync(DirectUploadBeginRequest request, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<FileStoreResult> CompleteDirectUploadAsync(Guid fileId, DirectUploadCompleteRequest? completeRequest = null, CancellationToken ct = default)
        => throw new NotSupportedException();

    public Task<FileStoreResult> CopyFileAsync(Guid sourceFileId, CopyFileRequest? request = null, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<FileStoreResult> MoveFileAsync(Guid fileId, MoveFileRequest request, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<FileStoreResult> RenameFileAsync(Guid fileId, RenameFileRequest request, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<byte[]> GetFileAsync(Guid fileId, CompressionAlgorithm? compressionAlgorithmOverride = null, CancellationToken ct = default)
        => Task.FromResult(Files.TryGetValue(fileId, out var data) ? data : []);

    public Task<Stream?> GetFileStreamAsync(Guid fileId, CompressionAlgorithm? compressionAlgorithmOverride = null, CancellationToken ct = default)
        => Task.FromResult<Stream?>(Files.TryGetValue(fileId, out var data) ? new MemoryStream(data) : null);

    public Task<bool> DeleteFileAsync(Guid fileId, FileDeletionMode mode = FileDeletionMode.RemoveObjectAndTombstoneMetadata, CancellationToken ct = default)
    {
        var removed = Files.Remove(fileId);
        return Task.FromResult(removed);
    }

    public Task<FileStoreResult> GetMetadataAsync(Guid fileId, CancellationToken ct = default)
    {
        if (!Files.TryGetValue(fileId, out var data))
            throw new FileNotFoundException();

        return Task.FromResult(CreateResult(fileId, null, data.LongLength, null, null));
    }

    public Task<DekMigrationResult> MigrateDeksAsync(
        string sourceKeyId,
        string? sourceKeyVersion = null,
        string? targetKeyId = null,
        string? targetKeyVersion = null,
        int batchSize = 100,
        CancellationToken ct = default)
        => throw new NotSupportedException();

    public Task<DekMigrationResult> RotateDeksAsync(
        IReadOnlyCollection<Guid> fileIds,
        string? targetKeyId = null,
        string? targetKeyVersion = null,
        int batchSize = 100,
        CancellationToken ct = default)
        => throw new NotSupportedException();

    private static FileStoreResult CreateResult(Guid id, string? name, long size, string? contentType, string? pathPrefix)
        => new(
            id, name, size, [], name ?? "x.bin", size, [], false, null, null, null, false, null, null, null, null, null, null, null, null, DateTime.UtcNow, pathPrefix, null,
            contentType);

#pragma warning disable CS0067 // Events required by IFileStorageService
    public event EventHandler<FileSavedResult>? FileSaved;

    public event EventHandler<FileRetrievedResult>? FileRetrieved;

    public event EventHandler<FileDeletedResult>? FileDeleted;

    public event EventHandler<FileMovedResult>? FileMoved;

    public event EventHandler<FileRenamedResult>? FileRenamed;

    public event EventHandler<FileMetadataRetrievedResult>? FileMetadataRetrieved;

    public event EventHandler<FileAuditEventArgs>? FileAuditOccurred;
#pragma warning restore CS0067
}