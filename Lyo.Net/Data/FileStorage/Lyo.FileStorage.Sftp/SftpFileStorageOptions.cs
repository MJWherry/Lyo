using Lyo.Exceptions;
using Lyo.FileStorage.Models;
using Lyo.Sftp.Client;

namespace Lyo.FileStorage.Sftp;

/// <summary>Settings for <see cref="SftpFileStorageService" />.</summary>
public sealed class SftpFileStorageOptions : FileStorageServiceBaseOptions
{
    /// <summary>Section key used when binding from configuration.</summary>
    public const string SectionName = "SftpFileStorage";

    /// <summary>SFTP connection settings: host, auth, remote root jail, and metrics.</summary>
    public SftpClientOptions Sftp { get; set; } = new();

    /// <summary>Throws when nested SFTP options are invalid.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNull(Sftp);
        Sftp.Validate();
    }
}