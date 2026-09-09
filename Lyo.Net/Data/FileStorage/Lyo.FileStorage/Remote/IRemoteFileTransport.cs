namespace Lyo.FileStorage.Remote;

/// <summary>
/// POSIX-path file operations that <see cref="RemoteFileStorageServiceBase" /> needs from a remote server. Implementations wrap a protocol client (FTP, SFTP) so
/// <c>Lyo.FileStorage</c> does not take a dependency on that protocol's library.
/// </summary>
/// <remarks>
/// <para>Every path passed in is already absolute and jailed under <see cref="RootRemoteDirectory" /> by the caller, so implementations do not re-check that.</para>
/// </remarks>
public interface IRemoteFileTransport
{
    /// <summary>Short protocol name used in logs and exception text, for example <c>FTP</c> or <c>SFTP</c>.</summary>
    string ProtocolName { get; }

    /// <summary>Absolute remote directory that every stored path lives under.</summary>
    string RootRemoteDirectory { get; }

    /// <summary>Sends a cheap command to prove the connection is usable.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task HealthPingAsync(CancellationToken ct = default);

    /// <summary>Whether a file exists at <paramref name="path" />.</summary>
    /// <param name="path">Absolute remote path.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> FileExistsAsync(string path, CancellationToken ct = default);

    /// <summary>Creates <paramref name="path" /> and any missing parents.</summary>
    /// <param name="path">Absolute remote directory path.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CreateDirectoryAsync(string path, CancellationToken ct = default);

    /// <summary>Removes the file at <paramref name="path" />.</summary>
    /// <param name="path">Absolute remote path.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteFileAsync(string path, CancellationToken ct = default);

    /// <summary>File length in bytes.</summary>
    /// <param name="path">Absolute remote path.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<long> GetLengthAsync(string path, CancellationToken ct = default);

    /// <summary>Opens a sequential read stream. Disposing it releases any underlying connection lease.</summary>
    /// <param name="path">Absolute remote path.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Stream> OpenReadAsync(string path, CancellationToken ct = default);

    /// <summary>Opens a create-or-truncate write stream that commits on close.</summary>
    /// <param name="path">Absolute remote path.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Stream> OpenCreateAsync(string path, CancellationToken ct = default);

    /// <summary>Downloads a remote file into memory. Used only for header rewrites, which are size-bounded.</summary>
    /// <param name="path">Absolute remote path.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<byte[]> DownloadBytesAsync(string path, CancellationToken ct = default);

    /// <summary>Replaces the contents of <paramref name="path" /> with <paramref name="data" />.</summary>
    /// <param name="path">Absolute remote path.</param>
    /// <param name="data">Readable stream positioned at the start of the payload.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UploadAsync(string path, Stream data, CancellationToken ct = default);

    /// <summary>Renames or moves a remote path.</summary>
    /// <param name="source">Existing absolute remote path.</param>
    /// <param name="dest">Target absolute remote path.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RenameAsync(string source, string dest, CancellationToken ct = default);

    /// <summary>Copies a remote file server-side when the protocol supports it.</summary>
    /// <param name="source">Existing absolute remote path.</param>
    /// <param name="dest">Target absolute remote path.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CopyFileAsync(string source, string dest, CancellationToken ct = default);
}
