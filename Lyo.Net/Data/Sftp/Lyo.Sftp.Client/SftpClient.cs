using System.Collections.Concurrent;
using System.Text;
using Lyo.Common.Pathing;
using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Renci.SshNet;
using Renci.SshNet.Common;
using SshNetSftpClient = Renci.SshNet.SftpClient;

namespace Lyo.Sftp.Client;

/// <summary>
/// Pooled SSH.NET SFTP client with logging and metrics. Async methods are canonical; sync methods block on them. Thread-safe for concurrent callers up to
/// <see cref="SftpClientOptions.MaxPooledClients" />.
/// </summary>
public sealed class SftpClient : ISftpClient
{
    private readonly ILogger _logger;
    private readonly IMetrics _metrics;
    private readonly SftpClientOptions _options;
    private readonly ConcurrentBag<PooledConnection> _pool = new();
    private readonly SemaphoreSlim _poolGate;
    private bool _disposed;
    private int _leased;

    /// <summary>Creates a client from validated options.</summary>
    public SftpClient(SftpClientOptions options, ILoggerFactory? loggerFactory = null, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(options);
        options.Validate();
        _options = options;
        RootRemoteDirectory = PathHelpers.GetFullPath(PathStyle.Posix, options.RootRemoteDirectory);
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger("Lyo.Sftp.Client");
        _metrics = options.EnableMetrics ? metrics ?? NullMetrics.Instance : NullMetrics.Instance;
        _poolGate = new(options.MaxPooledClients, options.MaxPooledClients);
        _logger.LogInformation(
            "SFTP client configured for {Host}:{Port} user {User} root {Root} (max pool {Max})", options.Host, options.Port, options.Username, RootRemoteDirectory,
            options.MaxPooledClients);
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
        => WithLeaseAsync("exists", async (c, token) => await c.ExistsAsync(ResolvePath(path), token).ConfigureAwait(false), ct);

    /// <inheritdoc />
    public bool DirectoryExists(string path) => SyncWait(DirectoryExistsAsync(path));

    /// <inheritdoc />
    public Task<bool> DirectoryExistsAsync(string path, CancellationToken ct = default)
        => WithLeaseAsync(
            "exists_dir", async (c, token) => {
                var p = ResolvePath(path);
                if (!await c.ExistsAsync(p, token).ConfigureAwait(false))
                    return false;

                var attrs = await c.GetAttributesAsync(p, token).ConfigureAwait(false);
                return attrs.IsDirectory;
            }, ct);

    /// <inheritdoc />
    public bool FileExists(string path) => SyncWait(FileExistsAsync(path));

    /// <inheritdoc />
    public Task<bool> FileExistsAsync(string path, CancellationToken ct = default)
        => WithLeaseAsync(
            "exists_file", async (c, token) => {
                var p = ResolvePath(path);
                if (!await c.ExistsAsync(p, token).ConfigureAwait(false))
                    return false;

                var attrs = await c.GetAttributesAsync(p, token).ConfigureAwait(false);
                return !attrs.IsDirectory;
            }, ct);

    /// <inheritdoc />
    public void CreateDirectory(string path) => SyncWait(CreateDirectoryAsync(path));

    /// <inheritdoc />
    public async Task CreateDirectoryAsync(string path, CancellationToken ct = default)
        => await WithLeaseAsync(
                "mkdir", async (c, token) => {
                    await EnsureDirectoryAsync(c, ResolvePath(path), token).ConfigureAwait(false);
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
                    if (!await c.ExistsAsync(p, token).ConfigureAwait(false))
                        return 0;

                    if (recursive)
                        await DeleteRecursiveAsync(c, p, token).ConfigureAwait(false);
                    else
                        await c.DeleteDirectoryAsync(p, token).ConfigureAwait(false);

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
                    if (await c.ExistsAsync(p, token).ConfigureAwait(false))
                        await c.DeleteFileAsync(p, token).ConfigureAwait(false);

                    return 0;
                }, ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public IReadOnlyList<SftpEntryInfo> ListDirectory(string path) => SyncWait(ListDirectoryAsync(path));

    /// <inheritdoc />
    public Task<IReadOnlyList<SftpEntryInfo>> ListDirectoryAsync(string path, CancellationToken ct = default)
        => WithLeaseAsync(
            "list", async (c, token) => {
                var p = ResolvePath(path);
                var list = new List<SftpEntryInfo>();
                await foreach (var entry in c.ListDirectoryAsync(p, token).ConfigureAwait(false)) {
                    if (entry.Name is "." or "..")
                        continue;

                    list.Add(
                        new(
                            PathHelpers.Combine(PathStyle.Posix, p, entry.Name), entry.IsDirectory, entry.Length,
                            new(DateTime.SpecifyKind(entry.LastWriteTimeUtc, DateTimeKind.Utc))));
                }

                return (IReadOnlyList<SftpEntryInfo>)list;
            }, ct);

    /// <inheritdoc />
    public void Upload(string path, byte[] data) => SyncWait(UploadAsync(path, data));

    /// <inheritdoc />
    public async Task UploadAsync(string path, byte[] data, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        using var ms = new MemoryStream(data, false);
        await UploadAsync(path, ms, ct).ConfigureAwait(false);
        _metrics.IncrementCounter("sftp.bytes", data.Length, [("direction", "up")]);
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
                    var parent = PathHelpers.GetDirectoryName(PathStyle.Posix, p);
                    if (!string.IsNullOrEmpty(parent))
                        await EnsureDirectoryAsync(c, parent!, token).ConfigureAwait(false);

                    await c.UploadFileAsync(data, p, token).ConfigureAwait(false);
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
        _metrics.IncrementCounter("sftp.bytes", bytes.Length, [("direction", "down")]);
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
                    await c.DownloadFileAsync(ResolvePath(path), destination, token).ConfigureAwait(false);
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
                        await EnsureDirectoryAsync(c, parent!, token).ConfigureAwait(false);

                    await c.RenameFileAsync(s, d, token).ConfigureAwait(false);
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
        => WithLeaseAsync(
            "stat", async (c, token) => {
                var attrs = await c.GetAttributesAsync(ResolvePath(path), token).ConfigureAwait(false);
                return attrs.Size;
            }, ct);

    /// <inheritdoc />
    public DateTimeOffset GetLastWriteTimeUtc(string path) => SyncWait(GetLastWriteTimeUtcAsync(path));

    /// <inheritdoc />
    public Task<DateTimeOffset> GetLastWriteTimeUtcAsync(string path, CancellationToken ct = default)
        => WithLeaseAsync(
            "stat", async (c, token) => {
                var attrs = await c.GetAttributesAsync(ResolvePath(path), token).ConfigureAwait(false);
                var utc = attrs.LastWriteTimeUtc == default ? attrs.LastAccessTimeUtc : attrs.LastWriteTimeUtc;
                return new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
            }, ct);

    /// <inheritdoc />
    public Stream OpenRead(string path) => SyncWait(OpenReadAsync(path));

    /// <inheritdoc />
    public async Task<Stream> OpenReadAsync(string path, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        using var timer = _metrics.StartTimer("sftp.operation", [("operation", "open_read")]);
        var lease = await AcquireLeaseAsync(ct).ConfigureAwait(false);
        var gateTaken = false;
        try {
            await lease.Gate.WaitAsync(ct).ConfigureAwait(false);
            gateTaken = true;
            await EnsureConnectedAsync(lease.Client, ct).ConfigureAwait(false);
            var remote = await lease.Client.OpenAsync(ResolvePath(path), FileMode.Open, FileAccess.Read, ct).ConfigureAwait(false);
            lease.Gate.Release();
            gateTaken = false;
            return new LeaseBoundStream(remote, lease, ReleaseLease, _logger);
        }
        catch (Exception ex) {
            if (gateTaken)
                lease.Gate.Release();

            ReleaseLease(lease);
            _metrics.RecordError("sftp.errors", ex, [("operation", "open_read")]);
            _logger.LogError(ex, "SFTP open_read failed on {Host}", _options.Host);
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
                    await foreach (var _ in c.ListDirectoryAsync(RootRemoteDirectory, token).ConfigureAwait(false))
                        break;

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
                _logger.LogDebug(ex, "Error disconnecting pooled SFTP client");
            }

            item.Client.Dispose();
            item.Gate.Dispose();
        }

        _poolGate.Dispose();
        _logger.LogInformation("SFTP client disposed for {Host}", _options.Host);
    }

    private async Task<T> WithLeaseAsync<T>(string operation, Func<SshNetSftpClient, CancellationToken, Task<T>> action, CancellationToken ct)
    {
        ThrowIfDisposed();
        using var timer = _metrics.StartTimer("sftp.operation", [("operation", operation)]);
        var lease = await AcquireLeaseAsync(ct).ConfigureAwait(false);
        var gateTaken = false;
        try {
            await lease.Gate.WaitAsync(ct).ConfigureAwait(false);
            gateTaken = true;
            await EnsureConnectedAsync(lease.Client, ct).ConfigureAwait(false);
            return await action(lease.Client, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            _metrics.RecordError("sftp.errors", ex, [("operation", operation)]);
            _logger.LogError(ex, "SFTP {Operation} failed on {Host}", operation, _options.Host);
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
        _metrics.RecordGauge("sftp.pool", _leased, [("state", "leased")]);
        _logger.LogDebug("Acquired SFTP lease (leased={Leased})", _leased);
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

            _metrics.RecordGauge("sftp.pool", _pool.Count, [("state", "available")]);
            _logger.LogDebug("Released SFTP lease (available={Available})", _pool.Count);
        }
    }

    private async Task<PooledConnection> CreateConnectedClientAsync(CancellationToken ct)
    {
        using var timer = _metrics.StartTimer("sftp.connect");
        try {
            var auth = BuildAuthMethods();
            var connectionInfo = new ConnectionInfo(_options.Host, _options.Port, _options.Username, auth) { Timeout = _options.ConnectTimeout };
            var client = new SshNetSftpClient(connectionInfo) { OperationTimeout = _options.OperationTimeout };
            client.HostKeyReceived += OnHostKeyReceived;
            await client.ConnectAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Connected SFTP to {Host}:{Port}", _options.Host, _options.Port);
            return new(client, new(1, 1));
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            _metrics.RecordError("sftp.errors", ex, [("operation", "connect")]);
            _logger.LogError(ex, "Failed to connect SFTP to {Host}:{Port}", _options.Host, _options.Port);
            throw;
        }
    }

    private async Task EnsureConnectedAsync(SshNetSftpClient client, CancellationToken ct)
    {
        if (client.IsConnected)
            return;

        _logger.LogDebug("Reconnecting SFTP client to {Host}", _options.Host);
        await client.ConnectAsync(ct).ConfigureAwait(false);
    }

    private AuthenticationMethod[] BuildAuthMethods()
    {
        var methods = new List<AuthenticationMethod>();
        PrivateKeyFile? keyFile = null;
        if (!string.IsNullOrWhiteSpace(_options.PrivateKeyPem)) {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(_options.PrivateKeyPem));
            keyFile = string.IsNullOrEmpty(_options.PrivateKeyPassphrase) ? new(ms) : new PrivateKeyFile(ms, _options.PrivateKeyPassphrase);
        }
        else if (!string.IsNullOrWhiteSpace(_options.PrivateKeyPath)) {
            keyFile = string.IsNullOrEmpty(_options.PrivateKeyPassphrase)
                ? new(_options.PrivateKeyPath!)
                : new PrivateKeyFile(_options.PrivateKeyPath!, _options.PrivateKeyPassphrase);
        }

        if (keyFile != null)
            methods.Add(new PrivateKeyAuthenticationMethod(_options.Username, keyFile));

        if (!string.IsNullOrEmpty(_options.Password))
            methods.Add(new PasswordAuthenticationMethod(_options.Username, _options.Password));

        return [.. methods];
    }

    private void OnHostKeyReceived(object? sender, HostKeyEventArgs e)
    {
        if (_options.HostKeyPolicy == SftpHostKeyPolicy.AcceptAny) {
            _logger.LogWarning("Accepting SFTP host key without fingerprint check (AcceptAny policy)");
            e.CanTrust = true;
            return;
        }

        var sha256 = e.FingerPrintSHA256 ?? string.Empty;
        var md5 = e.FingerPrintMD5 ?? string.Empty;
        var allowed = _options.AllowedHostKeyFingerprints.Any(f => {
            var n = f.Trim();
            if (n.StartsWith("SHA256:", StringComparison.OrdinalIgnoreCase))
                n = n["SHA256:".Length..];

            return string.Equals(n, sha256, StringComparison.OrdinalIgnoreCase) || string.Equals(f.Trim(), sha256, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(f.Trim(), "SHA256:" + sha256, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(md5) && string.Equals(f.Trim(), md5, StringComparison.OrdinalIgnoreCase));
        });

        e.CanTrust = allowed;
        if (!allowed)
            _logger.LogError("Rejected SFTP host key fingerprint SHA256:{Fingerprint}", sha256);
    }

    private static async Task EnsureDirectoryAsync(SshNetSftpClient client, string path, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(path) || path == "/")
            return;

        if (await DirectoryUsableAsync(client, path, ct).ConfigureAwait(false))
            return;

        var parent = PathHelpers.GetDirectoryName(PathStyle.Posix, path);
        if (!string.IsNullOrEmpty(parent) && parent != path)
            await EnsureDirectoryAsync(client, parent!, ct).ConfigureAwait(false);

        try {
            await client.CreateDirectoryAsync(path, ct).ConfigureAwait(false);
        }
        catch (SshException) {
            if (!await DirectoryUsableAsync(client, path, ct).ConfigureAwait(false))
                throw;
        }
    }

    private static async Task<bool> DirectoryUsableAsync(SshNetSftpClient client, string path, CancellationToken ct)
    {
        try {
            if (await client.ExistsAsync(path, ct).ConfigureAwait(false)) {
                var attrs = await client.GetAttributesAsync(path, ct).ConfigureAwait(false);
                if (attrs.IsDirectory)
                    return true;
            }
        }
        catch (SshException) {
            /* fall through */
        }

        try {
            await foreach (var _ in client.ListDirectoryAsync(path, ct).ConfigureAwait(false))
                return true;

            return true;
        }
        catch (SshException) {
            return false;
        }
    }

    private static async Task DeleteRecursiveAsync(SshNetSftpClient client, string path, CancellationToken ct)
    {
        await foreach (var entry in client.ListDirectoryAsync(path, ct).ConfigureAwait(false)) {
            if (entry.Name is "." or "..")
                continue;

            var child = PathHelpers.Combine(PathStyle.Posix, path, entry.Name);
            if (entry.IsDirectory)
                await DeleteRecursiveAsync(client, child, ct).ConfigureAwait(false);
            else
                await client.DeleteFileAsync(child, ct).ConfigureAwait(false);
        }

        await client.DeleteDirectoryAsync(path, ct).ConfigureAwait(false);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SftpClient));
    }

    private static T SyncWait<T>(Task<T> task) => task.ConfigureAwait(false).GetAwaiter().GetResult();

    private static void SyncWait(Task task) => task.ConfigureAwait(false).GetAwaiter().GetResult();

    private sealed class PooledConnection(SshNetSftpClient client, SemaphoreSlim gate)
    {
        public SshNetSftpClient Client { get; } = client;

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
                    logger.LogDebug(ex, "Error disposing SFTP read stream");
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