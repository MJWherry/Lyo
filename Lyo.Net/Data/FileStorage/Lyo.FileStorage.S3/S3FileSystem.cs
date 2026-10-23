using System.Net;
using System.Runtime.CompilerServices;
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
        var listed = await ListPageAsync(prefix, "/", 2, ct).ConfigureAwait(false);
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
            var page = await ListPageAsync(prefix, "/", 10, ct).ConfigureAwait(false);
            var extras = page.Objects.Where(o => !string.Equals(o.Key, prefix, StringComparison.Ordinal)).ToList();
            if (extras.Count > 0 || page.CommonPrefixes.Count > 0)
                throw new IOException($"Directory is not empty: {path}");

            if (page.Objects.Any(o => string.Equals(o.Key, prefix, StringComparison.Ordinal)))
                await _client.DeleteObjectAsync(new() { BucketName = _bucket, Key = prefix }, ct).ConfigureAwait(false);

            return;
        }

        string? token = null;
        do {
            var page = await ListPageAsync(prefix, null, 1000, ct, token).ConfigureAwait(false);
            if (page.Objects.Count > 0) {
                // One DeleteObjects call per page instead of one DeleteObject per key: a thousand-object directory costs one round trip, not a thousand.
                var keys = page.Objects.Select(o => new KeyVersion { Key = o.Key }).ToList();
                await _client.DeleteObjectsAsync(new() { BucketName = _bucket, Objects = keys }, ct).ConfigureAwait(false);
            }

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
            var page = await ListPageAsync(prefix, "/", 1000, ct, token).ConfigureAwait(false);
            entries.AddRange(MapPage(page, prefix));
            token = page.NextToken;
        } while (token != null);

        return entries;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<FileSystemEntry> EnumerateDirectoryAsync(string path, bool recursive = false, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var pending = new Queue<string>();
        pending.Enqueue(path);
        while (pending.Count > 0) {
            ct.ThrowIfCancellationRequested();
            var current = pending.Dequeue();
            var jailed = Jail(current);
            var prefix = jailed == "/" ? "" : ObjectStoreVfs.DirectoryPrefix(jailed);
            string? token = null;
            do {
                var page = await ListPageAsync(prefix, "/", 1000, ct, token).ConfigureAwait(false);
                foreach (var entry in MapPage(page, prefix)) {
                    ct.ThrowIfCancellationRequested();
                    yield return entry;

                    if (recursive && entry.IsDirectory)
                        pending.Enqueue(entry.Path);
                }

                token = page.NextToken;
            } while (token != null);
        }
    }

    /// <inheritdoc />
    public bool FileExists(string path) => FileSystemSync.Wait(FileExistsAsync(path));

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path, CancellationToken ct = default) => await HeadOrNullAsync(Jail(path), ct).ConfigureAwait(false) is not null;

    /// <inheritdoc />
    public FileSystemEntry? GetEntry(string path) => FileSystemSync.Wait(GetEntryAsync(path));

    /// <inheritdoc />
    public async Task<FileSystemEntry?> GetEntryAsync(string path, CancellationToken ct = default)
    {
        var jailed = Jail(path);
        // HEAD carries length and last-modified together, so the whole entry costs one request instead of the three the old FileExists/GetLength/GetLastWrite chain took.
        if (await HeadOrNullAsync(jailed, ct).ConfigureAwait(false) is { } meta) {
            var last = UtcStamp(meta.LastModified);
            return new(jailed, PathHelpers.GetFileName(PathStyle.Posix, jailed), false, meta.ContentLength, last, last);
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
        await _client.DeleteObjectAsync(new() { BucketName = _bucket, Key = key }, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Move(string source, string dest, bool overwrite = false) => FileSystemSync.Wait(MoveAsync(source, dest, overwrite));

    /// <inheritdoc />
    public async Task MoveAsync(string source, string dest, bool overwrite = false, CancellationToken ct = default)
    {
        // Move is copy-then-delete here, which only has file semantics. Renaming a prefix would mean re-keying every object under it, so the capability stays off.
        if (await DirectoryExistsAsync(source, ct).ConfigureAwait(false) && !await FileExistsAsync(source, ct).ConfigureAwait(false))
            throw new NotSupportedException("S3 cannot move a directory; copy the objects and delete the prefix instead.");

        await CopyFileAsync(source, dest, overwrite, ct).ConfigureAwait(false);
        await DeleteFileAsync(source, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void CopyFile(string source, string dest, bool overwrite = false) => FileSystemSync.Wait(CopyFileAsync(source, dest, overwrite));

    /// <inheritdoc />
    public async Task CopyFileAsync(string source, string dest, bool overwrite = false, CancellationToken ct = default)
    {
        var src = ObjectStoreVfs.ToObjectKey(Jail(source));
        var dst = ObjectStoreVfs.ToObjectKey(Jail(dest));
        if (await HeadOrNullAsync(Jail(source), ct).ConfigureAwait(false) is null)
            throw new FileNotFoundException($"Source file not found: {source}", source);

        if (!overwrite && await HeadOrNullAsync(Jail(dest), ct).ConfigureAwait(false) is not null)
            throw new IOException($"Destination already exists: {dest}");

        await _client.CopyObjectAsync(
                new() {
                    SourceBucket = _bucket,
                    SourceKey = src,
                    DestinationBucket = _bucket,
                    DestinationKey = dst
                }, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Stream OpenRead(string path) => FileSystemSync.Wait(OpenReadAsync(path));

    /// <inheritdoc />
    public async Task<Stream> OpenReadAsync(string path, CancellationToken ct = default)
    {
        var key = ObjectStoreVfs.ToObjectKey(Jail(path));
        try {
            var response = await _client.GetObjectAsync(new() { BucketName = _bucket, Key = key }, ct).ConfigureAwait(false);
            return new S3GetObjectResponseStream(response);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
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
                await _client.PutObjectAsync(new() { BucketName = _bucket, Key = key, InputStream = ms }, ct).ConfigureAwait(false);
            }));
    }

    /// <inheritdoc />
    public Stream OpenAppend(string path) => throw new NotSupportedException("S3 does not support append; FileSystemCapabilities.Append is not set.");

    /// <inheritdoc />
    public Task<Stream> OpenAppendAsync(string path, CancellationToken ct = default)
        => throw new NotSupportedException("S3 does not support append; FileSystemCapabilities.Append is not set.");

    /// <inheritdoc />
    public long GetLength(string path) => FileSystemSync.Wait(GetLengthAsync(path));

    /// <inheritdoc />
    public async Task<long> GetLengthAsync(string path, CancellationToken ct = default) => (await HeadOrThrowAsync(path, ct).ConfigureAwait(false)).ContentLength;

    /// <inheritdoc />
    public DateTimeOffset GetCreationTimeUtc(string path) => GetLastWriteTimeUtc(path);

    /// <inheritdoc />
    public Task<DateTimeOffset> GetCreationTimeUtcAsync(string path, CancellationToken ct = default) => GetLastWriteTimeUtcAsync(path, ct);

    /// <inheritdoc />
    public DateTimeOffset GetLastWriteTimeUtc(string path) => FileSystemSync.Wait(GetLastWriteTimeUtcAsync(path));

    /// <inheritdoc />
    public async Task<DateTimeOffset> GetLastWriteTimeUtcAsync(string path, CancellationToken ct = default)
        => UtcStamp((await HeadOrThrowAsync(path, ct).ConfigureAwait(false)).LastModified);

    /// <inheritdoc />
    public void Dispose() { }

    private string Jail(string path) => FileSystemPath.Jail(PathStyle.Posix, RootPath, path);

    private async Task<GetObjectMetadataResponse> HeadOrThrowAsync(string path, CancellationToken ct)
        => await HeadOrNullAsync(Jail(path), ct).ConfigureAwait(false) ?? throw new FileNotFoundException($"File not found: {path}", path);

    /// <summary>One HEAD request. Returns null for a missing key or for a path that names a directory marker rather than an object.</summary>
    private async Task<GetObjectMetadataResponse?> HeadOrNullAsync(string jailed, CancellationToken ct)
    {
        var key = ObjectStoreVfs.ToObjectKey(jailed);
        if (key.Length == 0 || key.EndsWith("/"))
            return null;

        try {
            return await _client.GetObjectMetadataAsync(new() { BucketName = _bucket, Key = key }, ct).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
            return null;
        }
    }

    private static IEnumerable<FileSystemEntry> MapPage(ListPage page, string prefix)
    {
        foreach (var common in page.CommonPrefixes) {
            var dirPath = ObjectStoreVfs.ToVfsPath(common.TrimEnd('/'));
            yield return new(dirPath, PathHelpers.GetFileName(PathStyle.Posix, dirPath), true, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue);
        }

        foreach (var obj in page.Objects) {
            if (string.Equals(obj.Key, prefix, StringComparison.Ordinal) || obj.Key.EndsWith("/"))
                continue;

            var filePath = ObjectStoreVfs.ToVfsPath(obj.Key);
            var last = UtcStamp(obj.LastModified);
            yield return new(filePath, PathHelpers.GetFileName(PathStyle.Posix, filePath), false, obj.Size ?? 0, last, last);
        }
    }

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

    /// <summary>Buffers the object body and uploads it on close. Prefer <c>await using</c>: the synchronous path has to block on the upload.</summary>
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