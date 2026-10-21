using System.Runtime.CompilerServices;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Lyo.Common.Core.Pathing;
using Lyo.Exceptions;
using Lyo.IO.FileSystem;

namespace Lyo.FileStorage.AzureBlob;

/// <summary>
/// Raw Azure Blob name tree as <see cref="IFileSystem" />. Directories are prefix + delimiter <c>/</c> via <see cref="BlobContainerClient.GetBlobsByHierarchyAsync" />. Empty
/// folders survive as a zero-byte <c>{path}/</c> marker. Container is constructor state, not a path segment.
/// </summary>
public sealed class AzureBlobFileSystem : IFileSystem
{
    /// <summary>
    /// Name of the zero-byte blob that keeps an otherwise empty directory visible. It is a child of the directory rather than the conventional <c>{path}/</c> blob because
    /// <see cref="BlobContainerClient.GetBlobClient" /> strips a trailing slash from the name, which would write the marker over the directory path itself.
    /// </summary>
    private const string DirectoryMarkerName = ".lyo-dir";

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
        => FileSystemCapabilities.Read | FileSystemCapabilities.Write | FileSystemCapabilities.CreateDirectory | FileSystemCapabilities.Delete | FileSystemCapabilities.Move |
            FileSystemCapabilities.Copy;

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

        var key = ObjectStoreVfs.DirectoryPrefix(jailed) + DirectoryMarkerName;
        using var empty = new MemoryStream([]);
        await _container.GetBlobClient(key).UploadAsync(empty, true, ct).ConfigureAwait(false);
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

                if (item.Blob != null && string.Equals(item.Blob.Name, prefix + DirectoryMarkerName, StringComparison.Ordinal))
                    hasMarker = true;
                else
                    children++;
            }

            if (children > 0)
                throw new IOException($"Directory is not empty: {path}");

            if (hasMarker)
                await _container.GetBlobClient(prefix + DirectoryMarkerName).DeleteIfExistsAsync(cancellationToken: ct).ConfigureAwait(false);

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
            if (MapItem(item, prefix) is { } entry)
                entries.Add(entry);
        }

        return entries;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<FileSystemEntry> EnumerateDirectoryAsync(string path, bool recursive = false, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var pending = new Queue<string>();
        pending.Enqueue(path);
        while (pending.Count > 0) {
            ct.ThrowIfCancellationRequested();
            var jailed = Jail(pending.Dequeue());
            var prefix = jailed == "/" ? "" : ObjectStoreVfs.DirectoryPrefix(jailed);
            await foreach (var item in _container.GetBlobsByHierarchyAsync(BlobTraits.None, BlobStates.None, "/", prefix, ct).ConfigureAwait(false)) {
                ct.ThrowIfCancellationRequested();
                if (MapItem(item, prefix) is not { } entry)
                    continue;

                yield return entry;

                if (recursive && entry.IsDirectory)
                    pending.Enqueue(entry.Path);
            }
        }
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
    public FileSystemEntry? GetEntry(string path) => FileSystemSync.Wait(GetEntryAsync(path));

    /// <inheritdoc />
    public async Task<FileSystemEntry?> GetEntryAsync(string path, CancellationToken ct = default)
    {
        var jailed = Jail(path);
        // GetProperties carries length, created, and last-modified together, so an entry costs one request rather than the three the old chain took.
        if (await PropertiesOrNullAsync(jailed, ct).ConfigureAwait(false) is { } props) {
            var name = PathHelpers.GetFileName(PathStyle.Posix, jailed);
            return new(jailed, name, false, props.ContentLength, props.CreatedOn, props.LastModified);
        }

        if (!await DirectoryExistsAsync(path, ct).ConfigureAwait(false))
            return null;

        return new(jailed, PathHelpers.GetFileName(PathStyle.Posix, jailed), true, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue);
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
    public void Move(string source, string dest, bool overwrite = false) => FileSystemSync.Wait(MoveAsync(source, dest, overwrite));

    /// <inheritdoc />
    public async Task MoveAsync(string source, string dest, bool overwrite = false, CancellationToken ct = default)
    {
        // Move is copy-then-delete here, which only has file semantics. Renaming a prefix would mean re-keying every blob under it, so the capability stays off.
        if (await DirectoryExistsAsync(source, ct).ConfigureAwait(false) && !await FileExistsAsync(source, ct).ConfigureAwait(false))
            throw new NotSupportedException("Azure Blob cannot move a directory; copy the blobs and delete the prefix instead.");

        await CopyFileAsync(source, dest, overwrite, ct).ConfigureAwait(false);
        await DeleteFileAsync(source, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void CopyFile(string source, string dest, bool overwrite = false) => FileSystemSync.Wait(CopyFileAsync(source, dest, overwrite));

    /// <inheritdoc />
    public async Task CopyFileAsync(string source, string dest, bool overwrite = false, CancellationToken ct = default)
    {
        var srcJailed = Jail(source);
        var dstJailed = Jail(dest);
        if (await PropertiesOrNullAsync(srcJailed, ct).ConfigureAwait(false) is null)
            throw new FileNotFoundException($"Source file not found: {source}", source);

        if (!overwrite && await PropertiesOrNullAsync(dstJailed, ct).ConfigureAwait(false) is not null)
            throw new IOException($"Destination already exists: {dest}");

        var src = _container.GetBlobClient(ObjectStoreVfs.ToObjectKey(srcJailed));
        var dst = _container.GetBlobClient(ObjectStoreVfs.ToObjectKey(dstJailed));
        await dst.SyncCopyFromUriAsync(src.Uri, cancellationToken: ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Stream OpenRead(string path) => FileSystemSync.Wait(OpenReadAsync(path));

    /// <inheritdoc />
    public async Task<Stream> OpenReadAsync(string path, CancellationToken ct = default)
    {
        var key = ObjectStoreVfs.ToObjectKey(Jail(path));
        try {
            var response = await _container.GetBlobClient(key).DownloadStreamingAsync(cancellationToken: ct).ConfigureAwait(false);
            return new AzureBlobDownloadStream(response.Value.Content, response.Value);
        }
        catch (RequestFailedException ex) when (ex.Status == 404) {
            throw new FileNotFoundException($"File not found: {path}", path, ex);
        }
    }

    /// <inheritdoc />
    public Stream OpenCreate(string path) => FileSystemSync.Wait(OpenCreateAsync(path));

    /// <inheritdoc />
    public Task<Stream> OpenCreateAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var key = ObjectStoreVfs.ToObjectKey(Jail(path));
        return Task.FromResult<Stream>(
            new CommitOnCloseStream(async bytes => {
                using var ms = new MemoryStream(bytes);
                await _container.GetBlobClient(key).UploadAsync(ms, true, ct).ConfigureAwait(false);
            }));
    }

    /// <inheritdoc />
    public Stream OpenAppend(string path) => throw new NotSupportedException("Azure Blob does not support append through this adapter; FileSystemCapabilities.Append is not set.");

    /// <inheritdoc />
    public Task<Stream> OpenAppendAsync(string path, CancellationToken ct = default)
        => throw new NotSupportedException("Azure Blob does not support append through this adapter; FileSystemCapabilities.Append is not set.");

    /// <inheritdoc />
    public long GetLength(string path) => FileSystemSync.Wait(GetLengthAsync(path));

    /// <inheritdoc />
    public async Task<long> GetLengthAsync(string path, CancellationToken ct = default) => (await PropertiesOrThrowAsync(path, ct).ConfigureAwait(false)).ContentLength;

    /// <inheritdoc />
    public DateTimeOffset GetCreationTimeUtc(string path) => FileSystemSync.Wait(GetCreationTimeUtcAsync(path));

    /// <inheritdoc />
    public async Task<DateTimeOffset> GetCreationTimeUtcAsync(string path, CancellationToken ct = default)
        => (await PropertiesOrThrowAsync(path, ct).ConfigureAwait(false)).CreatedOn;

    /// <inheritdoc />
    public DateTimeOffset GetLastWriteTimeUtc(string path) => FileSystemSync.Wait(GetLastWriteTimeUtcAsync(path));

    /// <inheritdoc />
    public async Task<DateTimeOffset> GetLastWriteTimeUtcAsync(string path, CancellationToken ct = default)
        => (await PropertiesOrThrowAsync(path, ct).ConfigureAwait(false)).LastModified;

    /// <inheritdoc />
    public void Dispose() { }

    private string Jail(string path) => FileSystemPath.Jail(PathStyle.Posix, RootPath, path);

    private async Task<BlobProperties> PropertiesOrThrowAsync(string path, CancellationToken ct)
        => await PropertiesOrNullAsync(Jail(path), ct).ConfigureAwait(false) ?? throw new FileNotFoundException($"File not found: {path}", path);

    /// <summary>One GetProperties request. Returns null for a missing blob or for a path that names a directory marker rather than a blob.</summary>
    private async Task<BlobProperties?> PropertiesOrNullAsync(string jailed, CancellationToken ct)
    {
        var key = ObjectStoreVfs.ToObjectKey(jailed);
        if (key.Length == 0 || key.EndsWith('/'))
            return null;

        try {
            return (await _container.GetBlobClient(key).GetPropertiesAsync(cancellationToken: ct).ConfigureAwait(false)).Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404) {
            return null;
        }
    }

    private static FileSystemEntry? MapItem(BlobHierarchyItem item, string prefix)
    {
        if (item.IsPrefix) {
            var dirPath = ObjectStoreVfs.ToVfsPath(item.Prefix.TrimEnd('/'));
            return new(dirPath, PathHelpers.GetFileName(PathStyle.Posix, dirPath), true, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue);
        }

        if (item.Blob == null || item.Blob.Name.EndsWith('/'))
            return null;

        if (string.Equals(item.Blob.Name, prefix, StringComparison.Ordinal) || string.Equals(item.Blob.Name, prefix + DirectoryMarkerName, StringComparison.Ordinal))
            return null;

        var filePath = ObjectStoreVfs.ToVfsPath(item.Blob.Name);
        var last = item.Blob.Properties.LastModified ?? DateTimeOffset.MinValue;
        return new(filePath, PathHelpers.GetFileName(PathStyle.Posix, filePath), false, item.Blob.Properties.ContentLength ?? 0, item.Blob.Properties.CreatedOn ?? last, last);
    }

    /// <summary>Buffers the blob body and uploads it on close. Prefer <c>await using</c>: the synchronous path has to block on the upload.</summary>
    private sealed class CommitOnCloseStream(Func<byte[], Task> commit) : MemoryStream
    {
        private bool _committed;

        public override async ValueTask DisposeAsync()
        {
            if (!_committed) {
                _committed = true;
                await commit(ToArray()).ConfigureAwait(false);
            }

            await base.DisposeAsync().ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_committed) {
                _committed = true;
                FileSystemSync.Wait(commit(ToArray()));
            }

            base.Dispose(disposing);
        }
    }
}