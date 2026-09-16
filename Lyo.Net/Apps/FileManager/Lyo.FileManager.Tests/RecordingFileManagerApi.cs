using Lyo.Api.FileStorage.Models;
using Lyo.FileMetadataStore.Models;

namespace Lyo.FileManager.Tests;

sealed class RecordingFileManagerApi : IFileManagerApi
{
    public FileManagerHealth Health { get; set; } = new(true, null);

    public Dictionary<string, FileManagerListing> Folders { get; } = new(StringComparer.Ordinal);

    public List<string> Lists { get; } = [];

    public List<(Guid Id, string? Prefix)> Moves { get; } = [];

    public List<(Guid Id, string? Prefix)> Copies { get; } = [];

    public List<Guid> Deletes { get; } = [];

    public List<(Guid Id, string Name)> Renames { get; } = [];

    public List<(string? Prefix, bool Compress, bool Encrypt, string? KeyId)> Uploads { get; } = [];

    public List<(string? Name, string? Prefix)> Queries { get; } = [];

    public List<(string Key, string? Prefix)> PhysicalMoves { get; } = [];

    public List<(string Key, string Name)> PhysicalRenames { get; } = [];

    public List<string> PhysicalDeletes { get; } = [];

    public Guid? FailMove { get; set; }

    public int ListCount => Lists.Count;

    public FileStorageCapabilitiesResponse Capabilities { get; set; } = new(true, true, true, true, true, true, true, true, true, true);

    public Task<FileManagerHealth> CheckHealthAsync(CancellationToken ct) => Task.FromResult(Health);

    public Task<FileStorageCapabilitiesResponse> GetCapabilitiesAsync(CancellationToken ct) => Task.FromResult(Capabilities);

    public Task<FileManagerListing> ListAsync(string? pathPrefix, CancellationToken ct)
    {
        var key = pathPrefix?.Trim().Trim('/') ?? "";
        Lists.Add(key);
        return Task.FromResult(Folders.TryGetValue(key, out var listing) ? listing : new FileManagerListing([], false));
    }

    public Task<IReadOnlyList<FileManagerQueryRow>> QueryAsync(string? nameContains, string? pathPrefix, CancellationToken ct)
    {
        Queries.Add((nameContains, pathPrefix));
        return Task.FromResult<IReadOnlyList<FileManagerQueryRow>>([]);
    }

    public Task UploadAsync(string filePath, string? pathPrefix, FileManagerUploadOptions options, CancellationToken ct)
    {
        Uploads.Add((pathPrefix, options.Compress, options.Encrypt, options.KeyId));
        return Task.CompletedTask;
    }

    public Task DownloadAsync(Guid fileId, string destinationPath, CancellationToken ct)
    {
        File.WriteAllText(destinationPath, fileId.ToString("D"));
        return Task.CompletedTask;
    }

    public Task DownloadArchiveAsync(IReadOnlyList<Guid> fileIds, string destinationPath, CancellationToken ct)
    {
        File.WriteAllText(destinationPath, string.Join(",", fileIds));
        return Task.CompletedTask;
    }

    public Task<FileStoreResult> GetMetadataAsync(Guid fileId, bool includeDeleted, CancellationToken ct) => throw new NotSupportedException();

    public Task RenameAsync(Guid fileId, string originalFileName, CancellationToken ct)
    {
        Renames.Add((fileId, originalFileName));
        return Task.CompletedTask;
    }

    public Task MoveAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
    {
        if (FailMove == fileId)
            throw new InvalidOperationException("move failed");
        Moves.Add((fileId, pathPrefix));
        return Task.CompletedTask;
    }

    public Task CopyAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
    {
        Copies.Add((fileId, pathPrefix));
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid fileId, CancellationToken ct)
    {
        Deletes.Add(fileId);
        return Task.CompletedTask;
    }

    public Task<string> CreateAccessLinkAsync(Guid fileId, DateTime? expiresAtUtc, int? maxDownloads, CancellationToken ct)
        => Task.FromResult("link");

    public Task<string> PresignedReadAsync(Guid fileId, CancellationToken ct) => Task.FromResult("https://example/read");

    public Task DownloadPhysicalAsync(string physicalKey, string destinationPath, CancellationToken ct) => Task.CompletedTask;

    public Task RenamePhysicalAsync(string physicalKey, string newFileName, CancellationToken ct)
    {
        PhysicalRenames.Add((physicalKey, newFileName));
        return Task.CompletedTask;
    }

    public Task MovePhysicalAsync(string physicalKey, string? destinationPrefix, CancellationToken ct)
    {
        PhysicalMoves.Add((physicalKey, destinationPrefix));
        return Task.CompletedTask;
    }

    public Task CopyPhysicalAsync(string physicalKey, string? destinationPrefix, CancellationToken ct) => Task.CompletedTask;

    public Task DeletePhysicalAsync(string physicalKey, CancellationToken ct)
    {
        PhysicalDeletes.Add(physicalKey);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ListKeyIdsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<string>>([]);

    public Task<DekMigrationResult> RotateAsync(IReadOnlyList<Guid> fileIds, string? targetKeyId, string? targetKeyVersion, CancellationToken ct)
        => Task.FromResult(new DekMigrationResult(fileIds.Count, fileIds.Count, 0, [], []));

    public Task<DekMigrationResult> MigrateAsync(string sourceKeyId, string? sourceKeyVersion, string? targetKeyId, string? targetKeyVersion, CancellationToken ct)
        => Task.FromResult(new DekMigrationResult(0, 0, 0, [], []));

    public void Dispose() { }

    public static FileManagerEntry Dir(string name, string prefix)
        => new(name, true, prefix, null, FileStoragePresence.Store, 0, null);

    public static FileManagerEntry Stored(string name, string? prefix, Guid id)
        => new(name, false, prefix, id, FileStoragePresence.Both, 4, prefix + "/" + name);
}
