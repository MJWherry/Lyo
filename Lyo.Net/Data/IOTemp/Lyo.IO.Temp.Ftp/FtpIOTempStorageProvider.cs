using System.Text;
using Lyo.Common.Pathing;
using Lyo.Exceptions;
using Lyo.Ftp.Client;
using Lyo.IO.Temp.Storage;

namespace Lyo.IO.Temp.Ftp;

/// <summary>FTP-backed <see cref="IIOTempStorageProvider" /> with <see cref="PathStyle.Posix" />.</summary>
// ReSharper disable once InconsistentNaming
public sealed class FtpIOTempStorageProvider : IIOTempStorageProvider
{
    private readonly IFtpClient _client;

    /// <summary>Creates a provider that stores temp data under the client's remote root jail.</summary>
    public FtpIOTempStorageProvider(IFtpClient client)
    {
        ArgumentHelpers.ThrowIfNull(client);
        _client = client;
        RootPath = client.RootRemoteDirectory;
    }

    /// <inheritdoc />
    public string RootPath { get; }

    /// <inheritdoc />
    public PathStyle PathStyle => PathStyle.Posix;

    /// <inheritdoc />
    public bool DirectoryExists(string path) => _client.DirectoryExists(path);

    /// <inheritdoc />
    public void CreateDirectory(string path) => _client.CreateDirectory(path);

    /// <inheritdoc />
    public void DeleteDirectory(string path) => _client.DeleteDirectory(path);

    /// <inheritdoc />
    public IEnumerable<ProviderEntryInfo> EnumerateEntries(string path)
        => _client.ListDirectory(path).Select(e => new ProviderEntryInfo(e.FullPath, e.IsDirectory, e.Length, e.LastWriteTimeUtc));

    /// <inheritdoc />
    public void EnsureDirectoryAccessible(string path)
    {
        _client.CreateDirectory(path);
        var probe = PathHelpers.Combine(PathStyle.Posix, path, $".rw-check-{Guid.NewGuid():N}.tmp");
        try {
            _client.Upload(probe, Encoding.UTF8.GetBytes("rw"));
            _ = _client.DownloadBytes(probe);
        }
        finally {
            if (_client.FileExists(probe))
                _client.DeleteFile(probe);
        }
    }

    /// <inheritdoc />
    public bool FileExists(string path) => _client.FileExists(path);

    /// <inheritdoc />
    public void TouchFile(string path) => _client.Upload(path, []);

    /// <inheritdoc />
    public void WriteAllBytes(string path, byte[] data) => _client.Upload(path, data);

    /// <inheritdoc />
    public void WriteAllText(string path, string text, Encoding encoding)
        => WriteAllBytes(path, encoding.GetBytes(text));

    /// <inheritdoc />
    public void AppendAllText(string path, string text, Encoding encoding)
    {
        using var stream = _client.OpenAppend(path);
        var bytes = encoding.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <inheritdoc />
    public void DeleteFile(string path) => _client.DeleteFile(path);

    /// <inheritdoc />
    public void MoveFile(string source, string dest)
    {
        try {
            _client.Rename(source, dest);
        }
        catch {
            _client.CopyFile(source, dest);
            _client.DeleteFile(source);
        }
    }

    /// <inheritdoc />
    public void CopyFile(string source, string dest) => _client.CopyFile(source, dest);

    /// <inheritdoc />
    public Stream OpenRead(string path) => _client.OpenRead(path);

    /// <inheritdoc />
    public Stream OpenCreate(string path) => _client.OpenCreate(path);

    /// <inheritdoc />
    public Stream OpenAppend(string path) => _client.OpenAppend(path);

    /// <inheritdoc />
    public long GetFileLength(string path) => _client.GetLength(path);

    /// <inheritdoc />
    public DateTimeOffset GetFileCreationTimeUtc(string path) => _client.GetLastWriteTimeUtc(path);

    /// <inheritdoc />
    public Task WriteAllBytesAsync(string path, byte[] data, CancellationToken ct)
        => _client.UploadAsync(path, data, ct);

    /// <inheritdoc />
    public Task WriteAllTextAsync(string path, string text, Encoding encoding, CancellationToken ct)
        => WriteAllBytesAsync(path, encoding.GetBytes(text), ct);

    /// <inheritdoc />
    public async Task AppendAllTextAsync(string path, string text, Encoding encoding, CancellationToken ct)
    {
        using var stream = await _client.OpenAppendAsync(path, ct).ConfigureAwait(false);
        var bytes = encoding.GetBytes(text);
#if NETSTANDARD2_0
        await stream.WriteAsync(bytes, 0, bytes.Length, ct).ConfigureAwait(false);
#else
        await stream.WriteAsync(bytes.AsMemory(), ct).ConfigureAwait(false);
#endif
    }

    /// <inheritdoc />
    public async Task CopyStreamToFileAsync(Stream source, string destPath, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(source);
#if NETSTANDARD2_0
        using var ms = new MemoryStream();
        await source.CopyToAsync(ms).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();
        ms.Position = 0;
        await _client.UploadAsync(destPath, ms, ct).ConfigureAwait(false);
#else
        using var ms = new MemoryStream();
        await source.CopyToAsync(ms, ct).ConfigureAwait(false);
        ms.Position = 0;
        await _client.UploadAsync(destPath, ms, ct).ConfigureAwait(false);
#endif
    }

    /// <inheritdoc />
    public Task CopyFileAsync(string source, string dest, CancellationToken ct)
        => _client.CopyFileAsync(source, dest, ct);
}
