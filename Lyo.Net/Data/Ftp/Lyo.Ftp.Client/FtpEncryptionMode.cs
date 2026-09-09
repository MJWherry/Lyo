namespace Lyo.Ftp.Client;

/// <summary>How the FTP control channel is encrypted. Maps onto FluentFTP encryption.</summary>
public enum FtpEncryptionMode
{
    /// <summary>Plain FTP with no TLS.</summary>
    None = 0,

    /// <summary>Explicit FTPS. AUTH TLS on port 21.</summary>
    Explicit,

    /// <summary>Implicit FTPS. TLS starts on connect, usually port 990.</summary>
    Implicit
}