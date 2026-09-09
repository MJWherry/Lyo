using Lyo.Exceptions;
using Lyo.FileStorage.Remote;
using Lyo.Ftp.Client;

namespace Lyo.FileStorage.Ftp;

/// <summary>Adapts <see cref="IFtpClient" /> to the transport surface that <see cref="RemoteFileStorageServiceBase" /> uses.</summary>
/// <param name="client">Pooled FTP client. Its root directory becomes the storage root.</param>
public sealed class FtpRemoteFileTransport(IFtpClient client) : IRemoteFileTransport
{
    private readonly IFtpClient _client = ArgumentHelpers.ThrowIfNullReturn(client);

    /// <inheritdoc />
    public string ProtocolName => "FTP";

    /// <inheritdoc />
    public string RootRemoteDirectory => _client.RootRemoteDirectory;

    /// <inheritdoc />
    public Task HealthPingAsync(CancellationToken ct = default) => _client.HealthPingAsync(ct);

    /// <inheritdoc />
    public Task<bool> FileExistsAsync(string path, CancellationToken ct = default) => _client.FileExistsAsync(path, ct);

    /// <inheritdoc />
    public Task CreateDirectoryAsync(string path, CancellationToken ct = default) => _client.CreateDirectoryAsync(path, ct);

    /// <inheritdoc />
    public Task DeleteFileAsync(string path, CancellationToken ct = default) => _client.DeleteFileAsync(path, ct);

    /// <inheritdoc />
    public Task<long> GetLengthAsync(string path, CancellationToken ct = default) => _client.GetLengthAsync(path, ct);

    /// <inheritdoc />
    public Task<Stream> OpenReadAsync(string path, CancellationToken ct = default) => _client.OpenReadAsync(path, ct);

    /// <inheritdoc />
    public Task<Stream> OpenCreateAsync(string path, CancellationToken ct = default) => _client.OpenCreateAsync(path, ct);

    /// <inheritdoc />
    public Task<byte[]> DownloadBytesAsync(string path, CancellationToken ct = default) => _client.DownloadBytesAsync(path, ct);

    /// <inheritdoc />
    public Task UploadAsync(string path, Stream data, CancellationToken ct = default) => _client.UploadAsync(path, data, ct);

    /// <inheritdoc />
    public Task RenameAsync(string source, string dest, CancellationToken ct = default) => _client.RenameAsync(source, dest, ct);

    /// <inheritdoc />
    public Task CopyFileAsync(string source, string dest, CancellationToken ct = default) => _client.CopyFileAsync(source, dest, ct);
}
