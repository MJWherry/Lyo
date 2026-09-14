using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Lyo.Common.Core.Pathing;
using Lyo.Exceptions;
using Lyo.IO.FileSystem;

namespace Lyo.FileStorage.AzureBlob;

/// <summary>
/// Raw Azure Blob name tree as <see cref="IFileSystem" />. Directories are prefix + delimiter <c>/</c> via <see cref="BlobContainerClient.GetBlobsByHierarchyAsync" />.
/// Empty folders survive as a zero-byte <c>{path}/</c> marker. Container is constructor state, not a path segment.
/// </summary>
public sealed class AzureBlobFileSystem : IFileSystem
{
    private readonly BlobContainerClient _container;

    /// <summary>Opens a VFS on <paramref name="container" /> jailed to <paramref name="blobPrefix" /> (POSIX). Empty prefix jails at <c>/</c>.</summary>
    public AzureBlobFileSystem(BlobContainerClient container, string? blobPrefix = null)
    {
        ArgumentHelpers.ThrowIfNull(container);
        _container = container;
        RootPath = ObjectStoreVfs.NormalizeRoot(blobPrefix);
    }

    /// <inheritdoc />
    public string RootPath { get; }

    /// <inheritdoc />
    public PathStyle PathStyle => PathStyle.Posix;

    /// <inheritdoc />
    public FileSystemCapabilities Capabilities
        => FileSystemCapabilities.Read | FileSystemCapabilities.Write | FileSystemCapabilities.CreateDirectory | FileSystemCapabilities.Delete
           | FileSystemCapabilities.Move | FileSystemCapabilities.Copy;

    /// <inheritdoc />
    public IFileSystemWatch? Watch(string path, bool recursive = true) => null;

    /// <inheritdoc />
    public bool DirectoryExists(string path) => FileSystemSync.Wait(DirectoryExistsAsync(path));

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path, CancellationToken ct = default)
    {
        var jailed = Jail(path);
        if (string.Equals(jailed, RootPath, StringComparison.Ordinal) || jailed == "/")
            return true;

        var prefix = ObjectStoreVfs.DirectoryPrefix(jailed);
        await foreach (var _ in _container.GetBlobsByHierarchyAsync(BlobTraits.None, BlobStates.None, "/", prefix, ct).ConfigureAwait(false))
            return true;

        return false;
    }

    /// <inheritdoc />
    public void CreateDirectory(string path) => FileSystemSync.Wait(CreateDirectoryAsync(path));

    /// <inheritdoc />
    public async Task CreateDirectoryAsync(string path, CancellationToken ct = default)
    {
        var jailed = Jail(path);
        if (string.Equals(jailed, RootPath, StringComparison.Ordinal) || jailed == "/")
            return;

        var key = ObjectStoreVfs.DirectoryPrefix(jailed);
        using var empty = new MemoryStream([]);
        await _container.GetBlobClient(key).UploadAsync(empty, overwrite: true, cancellationToken: ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void DeleteDirectory(string path, bool recursive = true) => FileSystemSync.Wait(DeleteDirectoryAsync(path, recursive));

    /// <inheritdoc />
    public async Task DeleteDirectoryAsync(string path, bool recursive = true, CancellationToken ct = default)
    {
        var jailed = Jail(path);
        var prefix = ObjectStoreVfs.DirectoryPrefix(jailed);
        if (!recursive) {
            var children = 0;
            var hasMarker = false;
            await foreach (var item in _container.GetBlobsByHierarchyAsync(BlobTraits.None, BlobStates.None, "/", prefix, ct).ConfigureAwait(false)) {
                if (item.IsPrefix) {
                    children++;
                    continue;
                }

                if (item.Blob != null && string.Equals(item.Blob.Name, prefix, StringComparison.Ordinal))
                    hasMarker = true;
                else
                    children++;
            }

            if (children > 0)
                throw new IOException($"Directory is not empty: {path}");

            if (hasMarker)
                await _container.GetBlobClient(prefix).DeleteIfExistsAsync(cancellationToken: ct).ConfigureAwait(false);

            return;
        }

        await foreach (var blob in _container.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix, ct).ConfigureAwait(false))
            await _container.GetBlobClient(blob.Name).DeleteIfExistsAsync(cancellationToken: ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IReadOnlyList<FileSystemEntry> ListDirectory(string path) => FileSystemSync.Wait(ListDirectoryAsync(path));

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileSystemEntry>> ListDirectoryAsync(string path, CancellationToken ct = default)
    {
        var jailed = Jail(path);
        var prefix = jailed == "/" ? "" : ObjectStoreVfs.DirectoryPrefix(jailed);
        List<FileSystemEntry> entries = [];
        await foreach (var item in _container.GetBlobsByHierarchyAsync(BlobTraits.None, BlobStates.None, "/", prefix, ct).ConfigureAwait(false)) {
            if (item.IsPrefix) {
                var dirPath = ObjectStoreVfs.ToVfsPath(item.Prefix.TrimEnd('/'));
                var name = PathHelpers.GetFileName(PathStyle.Posix, dirPath);
                entries.Add(new(dirPath, name, true, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue));
                continue;
            }

            if (item.Blob == null)
                continue;

            if (string.Equals(item.Blob.Name, prefix, StringComparison.Ordinal) || item.Blob.Name.EndsWith('/'))
                continue;

            var filePath = ObjectStoreVfs.ToVfsPath(item.Blob.Name);
            var fileName = PathHelpers.GetFileName(PathStyle.Posix, filePath);
            var last = item.Blob.Properties.LastModified ?? DateTimeOffset.MinValue;
            var created = item.Blob.Properties.CreatedOn ?? last;
            entries.Add(new(filePath, fileName, false, item.Blob.Properties.ContentLength ?? 0, created, last));
        }

        return entries;
    }

    /// <inheritdoc />
    public bool FileExists(string path) => FileSystemSync.Wait(FileExistsAsync(path));

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path, CancellationToken ct = default)
    {
        var key = ObjectStoreVfs.ToObjectKey(Jail(path));
        if (key.Length == 0 || key.EndsWith('/'))
            return false;

        var exists = await _container.GetBlobClient(key).ExistsAsync(ct).ConfigureAwait(false);
        return exists.Value;
    }

    /// <inheritdoc />
    public void DeleteFile(string path) => FileSystemSync.Wait(DeleteFileAsync(path));

    /// <inheritdoc />
    public async Task DeleteFileAsync(string path, CancellationToken ct = default)
    {
        var key = ObjectStoreVfs.ToObjectKey(Jail(path));
        await _container.GetBlobClient(key).DeleteIfExistsAsync(cancellationToken: ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Move(string source, string dest) => FileSystemSync.Wait(MoveAsync(source, dest));

    /// <inheritdoc />
    public async Task MoveAsync(string source, string dest, CancellationToken ct = default)
    {
        await CopyFileAsync(source, dest, ct).ConfigureAwait(false);
        await DeleteFileAsync(source, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void CopyFile(string source, string dest) => FileSystemSync.Wait(CopyFileAsync(source, dest));

    /// <inheritdoc />
    public async Task CopyFileAsync(string source, string dest, CancellationToken ct = default)
    {
        var src = _container.GetBlobClient(ObjectStoreVfs.ToObjectKey(Jail(source)));
        var dst = _container.GetBlobClient(ObjectStoreVfs.ToObjectKey(Jail(dest)));
        await dst.SyncCopyFromUriAsync(src.Uri, cancellationToken: ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Stream OpenRead(string path) => FileSystemSync.Wait(OpenReadAsync(path));

    /// <inheritdoc />
    public async Task<Stream> OpenReadAsync(string path, CancellationToken ct = default)
    {
        var key = ObjectStoreVfs.ToObjectKey(Jail(path));
        var response = await _container.GetBlobClient(key).DownloadStreamingAsync(cancellationToken: ct).ConfigureAwait(false);
        return new AzureBlobDownloadStream(response.Value.Content, response.Value);
    }

    /// <inheritdoc />
    public Stream OpenCreate(string path) => FileSystemSync.Wait(OpenCreateAsync(path));

    /// <inheritdoc />
    public Task<Stream> OpenCreateAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var key = ObjectStoreVfs.ToObjectKey(Jail(path));
        return Task.FromResult<Stream>(new CommitOnCloseStream(async bytes => {
            using var ms = new MemoryStream(bytes);
            await _container.GetBlobClient(key).UploadAsync(ms, overwrite: true, cancellationToken: CancellationToken.None).ConfigureAwait(false);
        }));
    }

    /// <inheritdoc />
    public Stream OpenAppend(string path) => throw new NotSupportedException("Azure Blob does not support append through this adapter.");

    /// <inheritdoc />
    public Task<Stream> OpenAppendAsync(string path, CancellationToken ct = default)
        => throw new NotSupportedException("Azure Blob does not support append through this adapter.");

    /// <inheritdoc />
    public long GetLength(string path) => FileSystemSync.Wait(GetLengthAsync(path));

    /// <inheritdoc />
    public async Task<long> GetLengthAsync(string path, CancellationToken ct = default)
    {
        var key = ObjectStoreVfs.ToObjectKey(Jail(path));
        var props = await _container.GetBlobClient(key).GetPropertiesAsync(cancellationToken: ct).ConfigureAwait(false);
        return props.Value.ContentLength;
    }

    /// <inheritdoc />
    public DateTimeOffset GetCreationTimeUtc(string path) => FileSystemSync.Wait(GetCreationTimeUtcAsync(path));

    /// <inheritdoc />
    public async Task<DateTimeOffset> GetCreationTimeUtcAsync(string path, CancellationToken ct = default)
    {
        var key = ObjectStoreVfs.ToObjectKey(Jail(path));
        var props = await _container.GetBlobClient(key).GetPropertiesAsync(cancellationToken: ct).ConfigureAwait(false);
        return props.Value.CreatedOn;
    }

    /// <inheritdoc />
    public DateTimeOffset GetLastWriteTimeUtc(string path) => FileSystemSync.Wait(GetLastWriteTimeUtcAsync(path));

    /// <inheritdoc />
    public async Task<DateTimeOffset> GetLastWriteTimeUtcAsync(string path, CancellationToken ct = default)
    {
        var key = ObjectStoreVfs.ToObjectKey(Jail(path));
        var props = await _container.GetBlobClient(key).GetPropertiesAsync(cancellationToken: ct).ConfigureAwait(false);
        return props.Value.LastModified;
    }

    /// <inheritdoc />
    public void Dispose() { }

    private string Jail(string path) => FileSystemPath.Jail(PathStyle.Posix, RootPath, path);

    private sealed class CommitOnCloseStream : MemoryStream
    {
        private readonly Func<byte[], Task> _commit;
        private bool _committed;

        public CommitOnCloseStream(Func<byte[], Task> commit) => _commit = commit;

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_committed) {
                _committed = true;
                FileSystemSync.Wait(_commit(ToArray()));
            }

            base.Dispose(disposing);
        }
    }
}
