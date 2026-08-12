namespace Lyo.Ftp.Client;

/// <summary>FTP control-channel encryption mode (maps to FluentFTP encryption).</summary>
public enum FtpEncryptionMode
{
    /// <summary>Plain FTP (no TLS).</summary>
    None = 0,

    /// <summary>Explicit FTPS (AUTH TLS on port 21).</summary>
    Explicit,

    /// <summary>Implicit FTPS (TLS from connect, typically port 990).</summary>
    Implicit
}
