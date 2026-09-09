namespace Lyo.Sftp.Client;

/// <summary>
/// Pooled SFTP calls over SSH.NET. Paths use POSIX form and stay under <see cref="RootRemoteDirectory" /> through
/// <see cref="Lyo.Common.Core.Pathing.PathHelpers" />.
/// </summary>
/// <remarks>
/// <para>Hosts and adapters should call the <c>*Async</c> methods. Sync methods park the calling thread while they wait on the async implementation.</para>
/// <para>
/// Safe for concurrent callers: up to <see cref="SftpClientOptions.MaxPooledClients" /> operations can run at once. Do not share one leased <see cref="Stream" /> across
/// threads.
/// </para>
/// </remarks>
public interface ISftpClient : IDisposable
{
    /// <summary>Remote root jail this client is locked to.</summary>
    string RootRemoteDirectory { get; }

    /// <summary>Turns <paramref name="path" /> into an absolute remote path under the root jail. CPU only; no I/O.</summary>
    string ResolvePath(string path);

    /// <summary>True if a file or directory exists at <paramref name="path" />. Blocks. Prefer <see cref="ExistsAsync" />.</summary>
    bool Exists(string path);

    /// <summary>True if a file or directory exists at <paramref name="path" />.</summary>
    Task<bool> ExistsAsync(string path, CancellationToken ct = default);

    /// <summary>True if a directory exists at <paramref name="path" />. Blocks. Prefer <see cref="DirectoryExistsAsync" />.</summary>
    bool DirectoryExists(string path);

    /// <summary>True if a directory exists at <paramref name="path" />.</summary>
    Task<bool> DirectoryExistsAsync(string path, CancellationToken ct = default);

    /// <summary>True if a file exists at <paramref name="path" />. Blocks. Prefer <see cref="FileExistsAsync" />.</summary>
    bool FileExists(string path);

    /// <summary>True if a file exists at <paramref name="path" />.</summary>
    Task<bool> FileExistsAsync(string path, CancellationToken ct = default);

    /// <summary>Creates <paramref name="path" /> and any missing parents. Blocks. Prefer <see cref="CreateDirectoryAsync" />.</summary>
    void CreateDirectory(string path);

    /// <summary>Creates <paramref name="path" /> and any missing parents.</summary>
    Task CreateDirectoryAsync(string path, CancellationToken ct = default);

    /// <summary>Removes a directory. Blocks. Prefer <see cref="DeleteDirectoryAsync" />.</summary>
    void DeleteDirectory(string path, bool recursive = true);

    /// <summary>Removes a directory.</summary>
    Task DeleteDirectoryAsync(string path, bool recursive = true, CancellationToken ct = default);

    /// <summary>Removes a file. Blocks. Prefer <see cref="DeleteFileAsync" />.</summary>
    void DeleteFile(string path);

    /// <summary>Removes a file when it exists.</summary>
    Task DeleteFileAsync(string path, CancellationToken ct = default);

    /// <summary>Lists immediate children of <paramref name="path" />. Blocks. Prefer <see cref="ListDirectoryAsync" />.</summary>
    IReadOnlyList<SftpEntryInfo> ListDirectory(string path);

    /// <summary>Lists immediate children of <paramref name="path" />.</summary>
    Task<IReadOnlyList<SftpEntryInfo>> ListDirectoryAsync(string path, CancellationToken ct = default);

    /// <summary>Uploads bytes to <paramref name="path" />. Blocks. Prefer <see cref="UploadAsync(string,byte[],CancellationToken)" />.</summary>
    void Upload(string path, byte[] data);

    /// <summary>Uploads bytes to <paramref name="path" />, creating parents as needed.</summary>
    Task UploadAsync(string path, byte[] data, CancellationToken ct = default);

    /// <summary>Uploads a stream to <paramref name="path" />. Blocks. Prefer <see cref="UploadAsync(string,Stream,CancellationToken)" />.</summary>
    void Upload(string path, Stream data);

    /// <summary>Uploads a stream to <paramref name="path" />, creating parents as needed.</summary>
    Task UploadAsync(string path, Stream data, CancellationToken ct = default);

    /// <summary>Downloads a remote file as bytes. Blocks. Prefer <see cref="DownloadBytesAsync" />.</summary>
    byte[] DownloadBytes(string path);

    /// <summary>Downloads a remote file as bytes.</summary>
    Task<byte[]> DownloadBytesAsync(string path, CancellationToken ct = default);

    /// <summary>Downloads a remote file into <paramref name="destination" />. Blocks. Prefer <see cref="DownloadAsync" />.</summary>
    void Download(string path, Stream destination);

    /// <summary>Downloads a remote file into <paramref name="destination" />.</summary>
    Task DownloadAsync(string path, Stream destination, CancellationToken ct = default);

    /// <summary>Renames or moves a remote path. Blocks. Prefer <see cref="RenameAsync" />.</summary>
    void Rename(string source, string dest);

    /// <summary>Renames or moves a remote path.</summary>
    Task RenameAsync(string source, string dest, CancellationToken ct = default);

    /// <summary>Copies a remote file. Blocks. Prefer <see cref="CopyFileAsync" />.</summary>
    void CopyFile(string source, string dest);

    /// <summary>Copies a remote file by downloading it and uploading it again.</summary>
    Task CopyFileAsync(string source, string dest, CancellationToken ct = default);

    /// <summary>File size in bytes. Blocks. Prefer <see cref="GetLengthAsync" />.</summary>
    long GetLength(string path);

    /// <summary>File size in bytes.</summary>
    Task<long> GetLengthAsync(string path, CancellationToken ct = default);

    /// <summary>Last write time in UTC. Blocks. Prefer <see cref="GetLastWriteTimeUtcAsync" />.</summary>
    DateTimeOffset GetLastWriteTimeUtc(string path);

    /// <summary>Last write time in UTC.</summary>
    Task<DateTimeOffset> GetLastWriteTimeUtcAsync(string path, CancellationToken ct = default);

    /// <summary>Opens a sequential read stream. Blocks. Prefer <see cref="OpenReadAsync" />. Dispose releases the lease.</summary>
    Stream OpenRead(string path);

    /// <summary>Opens a sequential read stream. Dispose releases the connection lease.</summary>
    Task<Stream> OpenReadAsync(string path, CancellationToken ct = default);

    /// <summary>Opens a create-or-truncate write stream that commits on close. Blocks. Prefer <see cref="OpenCreateAsync" />.</summary>
    Stream OpenCreate(string path);

    /// <summary>Opens a create-or-truncate write stream that commits on close.</summary>
    Task<Stream> OpenCreateAsync(string path, CancellationToken ct = default);

    /// <summary>Opens an append stream that commits on close. Blocks. Prefer <see cref="OpenAppendAsync" />.</summary>
    Stream OpenAppend(string path);

    /// <summary>Opens an append stream that commits on close.</summary>
    Task<Stream> OpenAppendAsync(string path, CancellationToken ct = default);

    /// <summary>Connects if needed and lists the root directory. Used as a health probe.</summary>
    Task HealthPingAsync(CancellationToken ct = default);
}