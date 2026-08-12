using System.Collections.Concurrent;
using FluentFTP;
using FluentFTP.Exceptions;
using Lyo.Common.Pathing;
using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAsyncFtpClient = FluentFTP.AsyncFtpClient;
using FluentEncryptionMode = FluentFTP.FtpEncryptionMode;

namespace Lyo.Ftp.Client;

/// <summary>
/// Pooled FluentFTP client with logging and metrics. Async methods are canonical; sync methods block on them. Thread-safe for concurrent callers up to
/// <see cref="FtpClientOptions.MaxPooledClients" />.
/// </summary>
public sealed class FtpClient : IFtpClient
{
    private readonly ILogger _logger;
    private readonly IMetrics _metrics;
    private readonly FtpClientOptions _options;
    private readonly ConcurrentBag<PooledConnection> _pool = new();
    private readonly SemaphoreSlim _poolGate;
    private bool _disposed;
    private int _leased;

    /// <summary>Creates a client from validated options.</summary>
    public FtpClient(FtpClientOptions options, ILoggerFactory? loggerFactory = null, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(options);
        options.Validate();
        _options = options;
        RootRemoteDirectory = PathHelpers.GetFullPath(PathStyle.Posix, options.RootRemoteDirectory);
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger("Lyo.Ftp.Client");
        _metrics = options.EnableMetrics ? metrics ?? NullMetrics.Instance : NullMetrics.Instance;
        _poolGate = new(options.MaxPooledClients, options.MaxPooledClients);
        _logger.LogInformation(
            "FTP client configured for {Host}:{Port} user {User} root {Root} (max pool {Max}, encryption {Enc})", options.Host, options.Port, options.Username, RootRemoteDirectory,
            options.MaxPooledClients, options.EncryptionMode);
    }

    /// <inheritdoc />
    public string RootRemoteDirectory { get; }

    /// <inheritdoc />
    public string ResolvePath(string path)
    {
        ThrowIfDisposed();
        PathHelpers.ThrowIfNullOrWhiteSpace(path);
        var combined = PathHelpers.IsPathRooted(PathStyle.Posix, path) ? path : PathHelpers.Combine(PathStyle.Posix, RootRemoteDirectory, path);
        var full = PathHelpers.GetFullPath(PathStyle.Posix, combined);
        PathHelpers.ThrowIfEscapesRoot(PathStyle.Posix, RootRemoteDirectory, full);
        return full;
    }

    /// <inheritdoc />
    public bool Exists(string path) => SyncWait(ExistsAsync(path));

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string path, CancellationToken ct = default)
        => WithLeaseAsync(
            "exists", async (c, token) => {
                var p = ResolvePath(path);
                return await c.FileExists(p, token).ConfigureAwait(false) || await c.DirectoryExists(p, token).ConfigureAwait(false);
            }, ct);

    /// <inheritdoc />
    public bool DirectoryExists(string path) => SyncWait(DirectoryExistsAsync(path));

    /// <inheritdoc />
    public Task<bool> DirectoryExistsAsync(string path, CancellationToken ct = default)
        => WithLeaseAsync("exists_dir", async (c, token) => await c.DirectoryExists(ResolvePath(path), token).ConfigureAwait(false), ct);

    /// <inheritdoc />
    public bool FileExists(string path) => SyncWait(FileExistsAsync(path));

    /// <inheritdoc />
    public Task<bool> FileExistsAsync(string path, CancellationToken ct = default)
        => WithLeaseAsync("exists_file", async (c, token) => await c.FileExists(ResolvePath(path), token).ConfigureAwait(false), ct);

    /// <inheritdoc />
    public void CreateDirectory(string path) => SyncWait(CreateDirectoryAsync(path));

    /// <inheritdoc />
    public async Task CreateDirectoryAsync(string path, CancellationToken ct = default)
        => await WithLeaseAsync(
                "mkdir", async (c, token) => {
                    await c.CreateDirectory(ResolvePath(path), true, token).ConfigureAwait(false);
                    return 0;
                }, ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public void DeleteDirectory(string path, bool recursive = true) => SyncWait(DeleteDirectoryAsync(path, recursive));

    /// <inheritdoc />
    public async Task DeleteDirectoryAsync(string path, bool recursive = true, CancellationToken ct = default)
        => await WithLeaseAsync(
                "rmdir", async (c, token) => {
                    var p = ResolvePath(path);
                    if (!await c.DirectoryExists(p, token).ConfigureAwait(false))
                        return 0;

                    if (recursive)
                        await DeleteRecursiveAsync(c, p, token).ConfigureAwait(false);
                    else
                        await c.DeleteDirectory(p, token).ConfigureAwait(false);

                    return 0;
                }, ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public void DeleteFile(string path) => SyncWait(DeleteFileAsync(path));

    /// <inheritdoc />
    public async Task DeleteFileAsync(string path, CancellationToken ct = default)
        => await WithLeaseAsync(
                "delete", async (c, token) => {
                    var p = ResolvePath(path);
                    if (await c.FileExists(p, token).ConfigureAwait(false))
                        await c.DeleteFile(p, token).ConfigureAwait(false);

                    return 0;
                }, ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public IReadOnlyList<FtpEntryInfo> ListDirectory(string path) => SyncWait(ListDirectoryAsync(path));

    /// <inheritdoc />
    public Task<IReadOnlyList<FtpEntryInfo>> ListDirectoryAsync(string path, CancellationToken ct = default)
        => WithLeaseAsync(
            "list", async (c, token) => {
                var p = ResolvePath(path);
                var items = await c.GetListing(p, token).ConfigureAwait(false);
                var list = new List<FtpEntryInfo>();
                foreach (var entry in items) {
                    if (entry.Name is "." or "..")
                        continue;

                    var full = string.IsNullOrWhiteSpace(entry.FullName)
                        ? PathHelpers.Combine(PathStyle.Posix, p, entry.Name)
                        : PathHelpers.GetFullPath(PathStyle.Posix, entry.FullName.Replace('\\', '/'));

                    PathHelpers.ThrowIfEscapesRoot(PathStyle.Posix, RootRemoteDirectory, full);
                    var modified = entry.Modified == default ? DateTimeOffset.UtcNow : new(DateTime.SpecifyKind(entry.Modified.ToUniversalTime(), DateTimeKind.Utc));
                    list.Add(new(full, entry.Type == FtpObjectType.Directory, entry.Size < 0 ? 0 : entry.Size, modified));
                }

                return (IReadOnlyList<FtpEntryInfo>)list;
            }, ct);

    /// <inheritdoc />
    public void Upload(string path, byte[] data) => SyncWait(UploadAsync(path, data));

    /// <inheritdoc />
    public async Task UploadAsync(string path, byte[] data, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        using var ms = new MemoryStream(data, false);
        await UploadAsync(path, ms, ct).ConfigureAwait(false);
        _metrics.IncrementCounter("ftp.bytes", data.Length, [("direction", "up")]);
    }

    /// <inheritdoc />
    public void Upload(string path, Stream data) => SyncWait(UploadAsync(path, data));

    /// <inheritdoc />
    public async Task UploadAsync(string path, Stream data, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        await WithLeaseAsync(
                "upload", async (c, token) => {
                    var p = ResolvePath(path);
                    var status = await c.UploadStream(data, p, FtpRemoteExists.Overwrite, true, null, token).ConfigureAwait(false);
                    if (status == FtpStatus.Failed)
                        throw new FtpException($"FTP upload failed for '{p}'.");

                    return 0;
                }, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public byte[] DownloadBytes(string path) => SyncWait(DownloadBytesAsync(path));

    /// <inheritdoc />
    public async Task<byte[]> DownloadBytesAsync(string path, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await DownloadAsync(path, ms, ct).ConfigureAwait(false);
        var bytes = ms.ToArray();
        _metrics.IncrementCounter("ftp.bytes", bytes.Length, [("direction", "down")]);
        return bytes;
    }

    /// <inheritdoc />
    public void Download(string path, Stream destination) => SyncWait(DownloadAsync(path, destination));

    /// <inheritdoc />
    public async Task DownloadAsync(string path, Stream destination, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(destination);
        await WithLeaseAsync(
                "download", async (c, token) => {
                    var status = await c.DownloadStream(destination, ResolvePath(path), 0, null, token).ConfigureAwait(false);
                    if (!status)
                        throw new FtpException($"FTP download failed for '{path}'.");

                    return 0;
                }, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Rename(string source, string dest) => SyncWait(RenameAsync(source, dest));

    /// <inheritdoc />
    public async Task RenameAsync(string source, string dest, CancellationToken ct = default)
        => await WithLeaseAsync(
                "rename", async (c, token) => {
                    var s = ResolvePath(source);
                    var d = ResolvePath(dest);
                    var parent = PathHelpers.GetDirectoryName(PathStyle.Posix, d);
                    if (!string.IsNullOrEmpty(parent))
                        await c.CreateDirectory(parent, true, token).ConfigureAwait(false);

                    var ok = await c.MoveFile(s, d, FtpRemoteExists.Overwrite, token).ConfigureAwait(false);
                    if (!ok)
                        throw new FtpException($"FTP rename/move failed from '{s}' to '{d}'.");

                    return 0;
                }, ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public void CopyFile(string source, string dest) => SyncWait(CopyFileAsync(source, dest));

    /// <inheritdoc />
    public async Task CopyFileAsync(string source, string dest, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await DownloadAsync(source, ms, ct).ConfigureAwait(false);
        ms.Position = 0;
        await UploadAsync(dest, ms, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public long GetLength(string path) => SyncWait(GetLengthAsync(path));

    /// <inheritdoc />
    public Task<long> GetLengthAsync(string path, CancellationToken ct = default)
        => WithLeaseAsync("stat", async (c, token) => await c.GetFileSize(ResolvePath(path), -1, token).ConfigureAwait(false), ct);

    /// <inheritdoc />
    public DateTimeOffset GetLastWriteTimeUtc(string path) => SyncWait(GetLastWriteTimeUtcAsync(path));

    /// <inheritdoc />
    public Task<DateTimeOffset> GetLastWriteTimeUtcAsync(string path, CancellationToken ct = default)
        => WithLeaseAsync(
            "stat", async (c, token) => {
                var when = await c.GetModifiedTime(ResolvePath(path), token).ConfigureAwait(false);
                return new DateTimeOffset(DateTime.SpecifyKind(when.ToUniversalTime(), DateTimeKind.Utc));
            }, ct);

    /// <inheritdoc />
    public Stream OpenRead(string path) => SyncWait(OpenReadAsync(path));

    /// <inheritdoc />
    public async Task<Stream> OpenReadAsync(string path, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        using var timer = _metrics.StartTimer("ftp.operation", [("operation", "open_read")]);
        var lease = await AcquireLeaseAsync(ct).ConfigureAwait(false);
        var gateTaken = false;
        try {
            await lease.Gate.WaitAsync(ct).ConfigureAwait(false);
            gateTaken = true;
            await EnsureConnectedAsync(lease.Client, ct).ConfigureAwait(false);
            var remote = await lease.Client.OpenRead(ResolvePath(path), FtpDataType.Binary, 0, true, ct).ConfigureAwait(false);
            lease.Gate.Release();
            gateTaken = false;
            return new LeaseBoundStream(remote, lease, ReleaseLease, _logger);
        }
        catch (Exception ex) {
            if (gateTaken)
                lease.Gate.Release();

            ReleaseLease(lease);
            _metrics.RecordError("ftp.errors", ex, [("operation", "open_read")]);
            _logger.LogError(ex, "FTP open_read failed on {Host}", _options.Host);
            throw;
        }
    }

    /// <inheritdoc />
    public Stream OpenCreate(string path) => SyncWait(OpenCreateAsync(path));

    /// <inheritdoc />
    public Task<Stream> OpenCreateAsync(string path, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        var resolved = ResolvePath(path);
        Stream stream = new CommitOnCloseStream(bytes => SyncWait(UploadAsync(resolved, bytes, CancellationToken.None)));
        return Task.FromResult(stream);
    }

    /// <inheritdoc />
    public Stream OpenAppend(string path) => SyncWait(OpenAppendAsync(path));

    /// <inheritdoc />
    public async Task<Stream> OpenAppendAsync(string path, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        var resolved = ResolvePath(path);
        var existing = await FileExistsAsync(resolved, ct).ConfigureAwait(false) ? await DownloadBytesAsync(resolved, ct).ConfigureAwait(false) : [];
        var stream = new CommitOnCloseStream(bytes => SyncWait(UploadAsync(resolved, bytes, CancellationToken.None)));
        if (existing.Length <= 0)
            return stream;

        stream.Write(existing, 0, existing.Length);
        stream.Position = existing.Length;
        return stream;
    }

    /// <inheritdoc />
    public async Task HealthPingAsync(CancellationToken ct = default)
        => await WithLeaseAsync(
                "health", async (c, token) => {
                    _ = await c.GetListing(RootRemoteDirectory, token).ConfigureAwait(false);
                    return 0;
                }, ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        while (_pool.TryTake(out var item)) {
            try {
                if (item.Client.IsConnected)
                    item.Client.Disconnect();
            }
            catch (Exception ex) {
                _logger.LogDebug(ex, "Error disconnecting pooled FTP client");
            }

            item.Client.Dispose();
            item.Gate.Dispose();
        }

        _poolGate.Dispose();
        _logger.LogInformation("FTP client disposed for {Host}", _options.Host);
    }

    private async Task<T> WithLeaseAsync<T>(string operation, Func<FluentAsyncFtpClient, CancellationToken, Task<T>> action, CancellationToken ct)
    {
        ThrowIfDisposed();
        using var timer = _metrics.StartTimer("ftp.operation", [("operation", operation)]);
        var lease = await AcquireLeaseAsync(ct).ConfigureAwait(false);
        var gateTaken = false;
        try {
            await lease.Gate.WaitAsync(ct).ConfigureAwait(false);
            gateTaken = true;
            await EnsureConnectedAsync(lease.Client, ct).ConfigureAwait(false);
            return await action(lease.Client, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            _metrics.RecordError("ftp.errors", ex, [("operation", operation)]);
            _logger.LogError(ex, "FTP {Operation} failed on {Host}", operation, _options.Host);
            throw;
        }
        finally {
            if (gateTaken)
                lease.Gate.Release();

            ReleaseLease(lease);
        }
    }

    private async Task<PooledConnection> AcquireLeaseAsync(CancellationToken ct)
    {
        await _poolGate.WaitAsync(ct).ConfigureAwait(false);
        Interlocked.Increment(ref _leased);
        _metrics.RecordGauge("ftp.pool", _leased, [("state", "leased")]);
        _logger.LogDebug("Acquired FTP lease (leased={Leased})", _leased);
        if (_pool.TryTake(out var existing))
            return existing;

        try {
            return await CreateConnectedClientAsync(ct).ConfigureAwait(false);
        }
        catch {
            Interlocked.Decrement(ref _leased);
            _poolGate.Release();
            throw;
        }
    }

    private void ReleaseLease(PooledConnection lease)
    {
        try {
            if (!_disposed && lease.Client.IsConnected)
                _pool.Add(lease);
            else {
                try {
                    lease.Client.Dispose();
                }
                catch {
                    /* ignore */
                }

                try {
                    lease.Gate.Dispose();
                }
                catch {
                    /* ignore */
                }
            }
        }
        finally {
            Interlocked.Decrement(ref _leased);
            try {
                _poolGate.Release();
            }
            catch (ObjectDisposedException) {
                /* shutting down */
            }

            _metrics.RecordGauge("ftp.pool", _pool.Count, [("state", "available")]);
            _logger.LogDebug("Released FTP lease (available={Available})", _pool.Count);
        }
    }

    private async Task<PooledConnection> CreateConnectedClientAsync(CancellationToken ct)
    {
        using var timer = _metrics.StartTimer("ftp.connect");
        try {
            var client = new FluentAsyncFtpClient(_options.Host, _options.Username, _options.Password ?? string.Empty, _options.Port);
            ApplyConfig(client);
            await client.Connect(ct).ConfigureAwait(false);
            _logger.LogInformation("Connected FTP to {Host}:{Port}", _options.Host, _options.Port);
            return new(client, new(1, 1));
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            _metrics.RecordError("ftp.errors", ex, [("operation", "connect")]);
            _logger.LogError(ex, "Failed to connect FTP to {Host}:{Port}", _options.Host, _options.Port);
            throw;
        }
    }

    private void ApplyConfig(FluentAsyncFtpClient client)
    {
        client.Config.ConnectTimeout = (int)Math.Min(int.MaxValue, _options.ConnectTimeout.TotalMilliseconds);
        client.Config.ReadTimeout = (int)Math.Min(int.MaxValue, _options.OperationTimeout.TotalMilliseconds);
        client.Config.DataConnectionReadTimeout = client.Config.ReadTimeout;
        client.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;
        client.Config.EncryptionMode = _options.EncryptionMode switch {
            FtpEncryptionMode.Explicit => FluentEncryptionMode.Explicit,
            FtpEncryptionMode.Implicit => FluentEncryptionMode.Implicit,
            var _ => FluentEncryptionMode.None
        };

        if (_options.TlsPolicy == FtpTlsPolicy.AcceptAny) {
            client.Config.ValidateAnyCertificate = true;
            if (_options.EncryptionMode != FtpEncryptionMode.None)
                _logger.LogWarning("Accepting FTP TLS certificate without validation (AcceptAny policy)");
        }
    }

    private async Task EnsureConnectedAsync(FluentAsyncFtpClient client, CancellationToken ct)
    {
        if (client.IsConnected)
            return;

        _logger.LogDebug("Reconnecting FTP client to {Host}", _options.Host);
        await client.Connect(ct).ConfigureAwait(false);
    }

    private static async Task DeleteRecursiveAsync(FluentAsyncFtpClient client, string path, CancellationToken ct)
    {
        var items = await client.GetListing(path, ct).ConfigureAwait(false);
        foreach (var entry in items) {
            if (entry.Name is "." or "..")
                continue;

            var child = string.IsNullOrWhiteSpace(entry.FullName) ? PathHelpers.Combine(PathStyle.Posix, path, entry.Name) : entry.FullName.Replace('\\', '/');
            if (entry.Type == FtpObjectType.Directory)
                await DeleteRecursiveAsync(client, child, ct).ConfigureAwait(false);
            else
                await client.DeleteFile(child, ct).ConfigureAwait(false);
        }

        await client.DeleteDirectory(path, ct).ConfigureAwait(false);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(FtpClient));
    }

    private static T SyncWait<T>(Task<T> task) => task.ConfigureAwait(false).GetAwaiter().GetResult();

    private static void SyncWait(Task task) => task.ConfigureAwait(false).GetAwaiter().GetResult();

    private sealed class PooledConnection(FluentAsyncFtpClient client, SemaphoreSlim gate)
    {
        public FluentAsyncFtpClient Client { get; } = client;

        public SemaphoreSlim Gate { get; } = gate;
    }

    private sealed class LeaseBoundStream(Stream inner, PooledConnection lease, Action<PooledConnection> release, ILogger logger) : Stream
    {
        private bool _released;

        public override bool CanRead => inner.CanRead;

        public override bool CanSeek => inner.CanSeek;

        public override bool CanWrite => inner.CanWrite;

        public override long Length => inner.Length;

        public override long Position {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

        public override void SetLength(long value) => inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_released) {
                _released = true;
                try {
                    inner.Dispose();
                }
                catch (Exception ex) {
                    logger.LogDebug(ex, "Error disposing FTP read stream");
                }

                release(lease);
            }

            base.Dispose(disposing);
        }
    }

    private sealed class CommitOnCloseStream(Action<byte[]> commit) : MemoryStream
    {
        private bool _committed;

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_committed) {
                _committed = true;
                commit(ToArray());
            }

            base.Dispose(disposing);
        }
    }
}