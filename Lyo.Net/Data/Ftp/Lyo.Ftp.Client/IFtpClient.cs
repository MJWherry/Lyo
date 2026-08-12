namespace Lyo.Ftp.Client;

/// <summary>
/// Pooled FTP/FTPS operations over FluentFTP. Paths are POSIX-style and constrained under <see cref="RootRemoteDirectory" /> via
/// <see cref="Lyo.Common.Pathing.PathHelpers" />.
/// </summary>
/// <remarks>
/// <para>
/// Prefer <c>*Async</c> methods from hosts and adapters. Sync methods block the calling thread by waiting on the async implementation.
/// </para>
/// <para>
/// Thread-safe for concurrent callers: up to <see cref="FtpClientOptions.MaxPooledClients" /> operations run in parallel.
/// Do not use the same leased <see cref="Stream" /> from multiple threads.
/// </para>
/// </remarks>
public interface IFtpClient : IDisposable
{
    /// <summary>Configured remote root jail.</summary>
    string RootRemoteDirectory { get; }

    /// <summary>Resolves <paramref name="path" /> to an absolute remote path under the root jail (CPU-only; no I/O).</summary>
    string ResolvePath(string path);

    /// <summary>Returns whether a file or directory exists at <paramref name="path" />. Blocks; prefer <see cref="ExistsAsync" />.</summary>
    bool Exists(string path);

    /// <summary>Asynchronously returns whether a file or directory exists at <paramref name="path" />.</summary>
    Task<bool> ExistsAsync(string path, CancellationToken ct = default);

    /// <summary>Returns whether a directory exists at <paramref name="path" />. Blocks; prefer <see cref="DirectoryExistsAsync" />.</summary>
    bool DirectoryExists(string path);

    /// <summary>Asynchronously returns whether a directory exists at <paramref name="path" />.</summary>
    Task<bool> DirectoryExistsAsync(string path, CancellationToken ct = default);

    /// <summary>Returns whether a file exists at <paramref name="path" />. Blocks; prefer <see cref="FileExistsAsync" />.</summary>
    bool FileExists(string path);

    /// <summary>Asynchronously returns whether a file exists at <paramref name="path" />.</summary>
    Task<bool> FileExistsAsync(string path, CancellationToken ct = default);

    /// <summary>Creates <paramref name="path" /> and any missing parents. Blocks; prefer <see cref="CreateDirectoryAsync" />.</summary>
    void CreateDirectory(string path);

    /// <summary>Asynchronously creates <paramref name="path" /> and any missing parents.</summary>
    Task CreateDirectoryAsync(string path, CancellationToken ct = default);

    /// <summary>Deletes a directory. Blocks; prefer <see cref="DeleteDirectoryAsync" />.</summary>
    void DeleteDirectory(string path, bool recursive = true);

    /// <summary>Asynchronously deletes a directory.</summary>
    Task DeleteDirectoryAsync(string path, bool recursive = true, CancellationToken ct = default);

    /// <summary>Deletes a file. Blocks; prefer <see cref="DeleteFileAsync" />.</summary>
    void DeleteFile(string path);

    /// <summary>Asynchronously deletes a file when it exists.</summary>
    Task DeleteFileAsync(string path, CancellationToken ct = default);

    /// <summary>Lists immediate children of <paramref name="path" />. Blocks; prefer <see cref="ListDirectoryAsync" />.</summary>
    IReadOnlyList<FtpEntryInfo> ListDirectory(string path);

    /// <summary>Asynchronously lists immediate children of <paramref name="path" />.</summary>
    Task<IReadOnlyList<FtpEntryInfo>> ListDirectoryAsync(string path, CancellationToken ct = default);

    /// <summary>Uploads bytes to <paramref name="path" />. Blocks; prefer <see cref="UploadAsync(string,byte[],CancellationToken)" />.</summary>
    void Upload(string path, byte[] data);

    /// <summary>Asynchronously uploads bytes to <paramref name="path" />, creating parents as needed.</summary>
    Task UploadAsync(string path, byte[] data, CancellationToken ct = default);

    /// <summary>Uploads a stream to <paramref name="path" />. Blocks; prefer <see cref="UploadAsync(string,Stream,CancellationToken)" />.</summary>
    void Upload(string path, Stream data);

    /// <summary>Asynchronously uploads a stream to <paramref name="path" />, creating parents as needed.</summary>
    Task UploadAsync(string path, Stream data, CancellationToken ct = default);

    /// <summary>Downloads a remote file as a byte array. Blocks; prefer <see cref="DownloadBytesAsync" />.</summary>
    byte[] DownloadBytes(string path);

    /// <summary>Asynchronously downloads a remote file as a byte array.</summary>
    Task<byte[]> DownloadBytesAsync(string path, CancellationToken ct = default);

    /// <summary>Downloads a remote file into <paramref name="destination" />. Blocks; prefer <see cref="DownloadAsync" />.</summary>
    void Download(string path, Stream destination);

    /// <summary>Asynchronously downloads a remote file into <paramref name="destination" />.</summary>
    Task DownloadAsync(string path, Stream destination, CancellationToken ct = default);

    /// <summary>Renames or moves a remote path. Blocks; prefer <see cref="RenameAsync" />.</summary>
    void Rename(string source, string dest);

    /// <summary>Asynchronously renames or moves a remote path.</summary>
    Task RenameAsync(string source, string dest, CancellationToken ct = default);

    /// <summary>Copies a remote file. Blocks; prefer <see cref="CopyFileAsync" />.</summary>
    void CopyFile(string source, string dest);

    /// <summary>Asynchronously copies a remote file (download + upload).</summary>
    Task CopyFileAsync(string source, string dest, CancellationToken ct = default);

    /// <summary>Returns file length in bytes. Blocks; prefer <see cref="GetLengthAsync" />.</summary>
    long GetLength(string path);

    /// <summary>Asynchronously returns file length in bytes.</summary>
    Task<long> GetLengthAsync(string path, CancellationToken ct = default);

    /// <summary>Returns last write time UTC. Blocks; prefer <see cref="GetLastWriteTimeUtcAsync" />.</summary>
    DateTimeOffset GetLastWriteTimeUtc(string path);

    /// <summary>Asynchronously returns last write time UTC.</summary>
    Task<DateTimeOffset> GetLastWriteTimeUtcAsync(string path, CancellationToken ct = default);

    /// <summary>Opens a sequential read stream. Blocks; prefer <see cref="OpenReadAsync" />. Dispose releases the lease.</summary>
    Stream OpenRead(string path);

    /// <summary>Asynchronously opens a sequential read stream. Dispose releases the underlying connection lease.</summary>
    Task<Stream> OpenReadAsync(string path, CancellationToken ct = default);

    /// <summary>Opens a create/truncate write stream (commit-on-close). Blocks; prefer <see cref="OpenCreateAsync" />.</summary>
    Stream OpenCreate(string path);

    /// <summary>Asynchronously opens a create/truncate write stream (commit-on-close upload).</summary>
    Task<Stream> OpenCreateAsync(string path, CancellationToken ct = default);

    /// <summary>Opens an append stream (commit-on-close). Blocks; prefer <see cref="OpenAppendAsync" />.</summary>
    Stream OpenAppend(string path);

    /// <summary>Asynchronously opens an append stream (commit-on-close upload).</summary>
    Task<Stream> OpenAppendAsync(string path, CancellationToken ct = default);

    /// <summary>Connects if needed and lists the root directory (health probe).</summary>
    Task HealthPingAsync(CancellationToken ct = default);
}
