namespace Lyo.Ftp.Client;

/// <summary>How FTPS checks the server certificate.</summary>
public enum FtpTlsPolicy
{
    /// <summary>Check the server certificate. Use this in production.</summary>
    ValidateCertificate = 0,

    /// <summary>Accept any server certificate. Tests and trusted labs only.</summary>
    AcceptAny
}