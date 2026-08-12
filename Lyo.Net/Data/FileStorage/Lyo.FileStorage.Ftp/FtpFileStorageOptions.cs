using Lyo.FileStorage.Models;
using Lyo.Ftp.Client;

namespace Lyo.FileStorage.Ftp;

/// <summary>Configuration for <see cref="FtpFileStorageService" />.</summary>
public sealed class FtpFileStorageOptions : FileStorageServiceBaseOptions
{
    /// <summary>Default configuration section name.</summary>
    public const string SectionName = "FtpFileStorage";

    /// <summary>FTP connection settings (host, auth, remote root jail, metrics).</summary>
    public FtpClientOptions Ftp { get; set; } = new();
}