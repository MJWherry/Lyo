using Lyo.FileStorage.Models;
using Lyo.Sftp.Client;

namespace Lyo.FileStorage.Sftp;

/// <summary>Configuration for <see cref="SftpFileStorageService" />.</summary>
public sealed class SftpFileStorageOptions : FileStorageServiceBaseOptions
{
    /// <summary>Default configuration section name.</summary>
    public const string SectionName = "SftpFileStorage";

    /// <summary>SFTP connection settings (host, auth, remote root jail, metrics).</summary>
    public SftpClientOptions Sftp { get; set; } = new();
}