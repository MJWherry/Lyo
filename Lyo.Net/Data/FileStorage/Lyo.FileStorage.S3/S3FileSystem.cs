using Amazon.S3;
using Amazon.S3.Model;
using Lyo.Common.Core.Pathing;
using Lyo.Exceptions;
using Lyo.IO.FileSystem;

namespace Lyo.FileStorage.S3;

/// <summary>
/// Raw S3 key tree as <see cref="IFileSystem" />. Directories are prefix + <c>Delimiter=/</c>. Empty folders survive as a zero-byte <c>{path}/</c> marker. Bucket is
/// constructor state, not a path segment. <see cref="RootPath" /> is an optional key-prefix jail.
/// </summary>
public sealed class S3FileSystem : IFileSystem
{
    private readonly string _bucket;
    private readonly IAmazonS3 _client;

    /// <summary>Opens a VFS on <paramref name="bucket" /> jailed to <paramref name="keyPrefix" /> (POSIX). Empty prefix jails at <c>/</c>.</summary>
    public S3FileSystem(IAmazonS3 client, string bucket, string? keyPrefix = null)
    {
        ArgumentHelpers.ThrowIfNull(client);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(bucket);
        _client = client;
        _bucket = bucket;
        RootPath = ObjectStoreVfs.NormalizeRoot(keyPrefix);
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
        var listed = await ListPageAsync(prefix, delimiter: "/", maxKeys: 2, ct).ConfigureAwait(false);
        return listed.Objects.Count > 0 || listed.CommonPrefixes.Count > 0;
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
        await _client.PutObjectAsync(new() { BucketName = _bucket, Key = key, InputStream = empty }, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void DeleteDirectory(string path, bool recursive = true) => FileSystemSync.Wait(DeleteDirectoryAsync(path, recursive));

    /// <inheritdoc />
    public async Task DeleteDirectoryAsync(string path, bool recursive = true, CancellationToken ct = default)
    {
        var jailed = Jail(path);
        var prefix = ObjectStoreVfs.DirectoryPrefix(jailed);
        if (!recursive) {
            var page = await ListPageAsync(prefix, delimiter: "/", maxKeys: 10, ct).ConfigureAwait(false);
            var extras = page.Objects.Where(o => !string.Equals(o.Key, prefix, StringComparison.Ordinal)).ToList();
            if (extras.Count > 0 || page.CommonPrefixes.Count > 0)
                throw new IOException($"Directory is not empty: {path}");

            if (page.Objects.Any(o => string.Equals(o.Key, prefix, StringComparison.Ordinal)))
                await _client.DeleteObjectAsync(new() { BucketName = _bucket, Key = prefix }, ct).ConfigureAwait(false);

            return;
        }

        string? token = null;
        do {
            var page = await ListPageAsync(prefix, delimiter: null, maxKeys: 1000, ct, token).ConfigureAwait(false);
            foreach (var obj in page.Objects)
                await _client.DeleteObjectAsync(new() { BucketName = _bucket, Key = obj.Key }, ct).ConfigureAwait(false);

            token = page.NextToken;
        } while (token != null);
    }

    /// <inheritdoc />
    public IReadOnlyList<FileSystemEntry> ListDirectory(string path) => FileSystemSync.Wait(ListDirectoryAsync(path));

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileSystemEntry>> ListDirectoryAsync(string path, CancellationToken ct = default)
    {
        var jailed = Jail(path);
        var prefix = jailed == "/" ? "" : ObjectStoreVfs.DirectoryPrefix(jailed);
        List<FileSystemEntry> entries = [];
        string? token = null;
        do {
            var page = await ListPageAsync(prefix, delimiter: "/", maxKeys: 1000, ct, token).ConfigureAwait(false);
            foreach (var common in page.CommonPrefixes) {
                var dirPath = ObjectStoreVfs.ToVfsPath(common.TrimEnd('/'));
                var name = PathHelpers.GetFileName(PathStyle.Posix, dirPath);
                entries.Add(new(dirPath, name, true, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue));
            }

            foreach (var obj in page.Objects) {
                if (string.Equals(obj.Key, prefix, StringComparison.Ordinal) || obj.Key.EndsWith("/"))
                    continue;

                var filePath = ObjectStoreVfs.ToVfsPath(obj.Key);
                var name = PathHelpers.GetFileName(PathStyle.Posix, filePath);
                var last = UtcStamp(obj.LastModified);
                entries.Add(new(filePath, name, false, obj.Size ?? 0, last, last));
            }

            token = page.NextToken;
        } while (token != null);

        return entries;
    }

    /// <inheritdoc />
    public bool FileExists(string path) => FileSystemSync.Wait(FileExistsAsync(path));

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path, CancellationToken ct = default)
    {
        var key = ObjectStoreVfs.ToObjectKey(Jail(path));
        if (key.Length == 0 || key.EndsWith("/"))
            return false;

        try {
            await _client.GetObjectMetadataAsync(new() { BucketName = _bucket, Key = key }, ct).ConfigureAwait(false);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) {
            return false;
        }
    }

    /// <inheritdoc />
    public void DeleteFile(string path) => FileSystemSync.Wait(DeleteFileAsync(path));

    /// <inheritdoc />
    public async Task DeleteFileAsync(string path, CancellationToken ct = default)
    {
        var key = ObjectStoreVfs.ToObjectKey(Jail(path));
        await _client.DeleteObjectAsync(new() { BucketName = _bucket, Key = key }, ct).ConfigureAwait(false);
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
        var src = ObjectStoreVfs.ToObjectKey(Jail(source));
        var dst = ObjectStoreVfs.ToObjectKey(Jail(dest));
        await _client.CopyObjectAsync(
                new() { SourceBucket = _bucket, SourceKey = src, DestinationBucket = _bucket, DestinationKey = dst }, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Stream OpenRead(string path) => FileSystemSync.Wait(OpenReadAsync(path));

    /// <inheritdoc />
    public async Task<Stream> OpenReadAsync(string path, CancellationToken ct = default)
    {
        var key = ObjectStoreVfs.ToObjectKey(Jail(path));
        var response = await _client.GetObjectAsync(new() { BucketName = _bucket, Key = key }, ct).ConfigureAwait(false);
        return new S3GetObjectResponseStream(response);
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
            await _client.PutObjectAsync(new() { BucketName = _bucket, Key = key, InputStream = ms }, CancellationToken.None).ConfigureAwait(false);
        }));
    }

    /// <inheritdoc />
    public Stream OpenAppend(string path) => throw new NotSupportedException("S3 does not support append.");

    /// <inheritdoc />
    public Task<Stream> OpenAppendAsync(string path, CancellationToken ct = default) => throw new NotSupportedException("S3 does not support append.");

    /// <inheritdoc />
    public long GetLength(string path) => FileSystemSync.Wait(GetLengthAsync(path));

    /// <inheritdoc />
    public async Task<long> GetLengthAsync(string path, CancellationToken ct = default)
    {
        var key = ObjectStoreVfs.ToObjectKey(Jail(path));
        var meta = await _client.GetObjectMetadataAsync(new() { BucketName = _bucket, Key = key }, ct).ConfigureAwait(false);
        return meta.ContentLength;
    }

    /// <inheritdoc />
    public DateTimeOffset GetCreationTimeUtc(string path) => GetLastWriteTimeUtc(path);

    /// <inheritdoc />
    public Task<DateTimeOffset> GetCreationTimeUtcAsync(string path, CancellationToken ct = default) => GetLastWriteTimeUtcAsync(path, ct);

    /// <inheritdoc />
    public DateTimeOffset GetLastWriteTimeUtc(string path) => FileSystemSync.Wait(GetLastWriteTimeUtcAsync(path));

    /// <inheritdoc />
    public async Task<DateTimeOffset> GetLastWriteTimeUtcAsync(string path, CancellationToken ct = default)
    {
        var key = ObjectStoreVfs.ToObjectKey(Jail(path));
        var meta = await _client.GetObjectMetadataAsync(new() { BucketName = _bucket, Key = key }, ct).ConfigureAwait(false);
        return UtcStamp(meta.LastModified);
    }

    /// <inheritdoc />
    public void Dispose() { }

    private string Jail(string path) => FileSystemPath.Jail(PathStyle.Posix, RootPath, path);

    private static DateTimeOffset UtcStamp(DateTime? value)
    {
        if (value is not { } dt || dt == default)
            return DateTimeOffset.MinValue;

        return new(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
    }

    private async Task<ListPage> ListPageAsync(string prefix, string? delimiter, int maxKeys, CancellationToken ct, string? token = null)
    {
        var resp = await _client.ListObjectsV2Async(
                new() {
                    BucketName = _bucket,
                    Prefix = prefix,
                    Delimiter = delimiter,
                    MaxKeys = maxKeys,
                    ContinuationToken = token
                }, ct)
            .ConfigureAwait(false);

        return new(resp.S3Objects ?? [], resp.CommonPrefixes ?? [], resp.IsTruncated == true ? resp.NextContinuationToken : null);
    }

    private sealed record ListPage(IList<S3Object> Objects, IList<string> CommonPrefixes, string? NextToken);

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
