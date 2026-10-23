using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.FileStorage.Models;
using Lyo.Ftp.Client;

namespace Lyo.FileStorage.Ftp;

/// <summary>Settings for <see cref="FtpFileStorageService" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
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
        ValidateUploadFilters();
    }

    /// <summary>Nested FTP settings and the shared storage options. The FTP password stays on <see cref="FtpClientOptions" />.</summary>
    public override string ToString() => $"Ftp={Ftp} {base.ToString()}";
}