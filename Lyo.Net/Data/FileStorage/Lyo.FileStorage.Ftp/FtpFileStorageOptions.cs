using Lyo.Exceptions;
using Lyo.FileStorage.Models;
using Lyo.Ftp.Client;

namespace Lyo.FileStorage.Ftp;

/// <summary>Settings for <see cref="FtpFileStorageService" />.</summary>
public sealed class FtpFileStorageOptions : FileStorageServiceBaseOptions
{
    /// <summary>Section key used when binding from configuration.</summary>
    public const string SectionName = "FtpFileStorage";

    /// <summary>FTP connection settings: host, auth, remote root jail, and metrics.</summary>
    public FtpClientOptions Ftp { get; set; } = new();

    /// <summary>Throws when nested FTP options are invalid.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNull(Ftp);
        Ftp.Validate();
    }
}