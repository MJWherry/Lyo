namespace Lyo.Ftp.Client;

/// <summary>TLS certificate validation policy for FTPS.</summary>
public enum FtpTlsPolicy
{
    /// <summary>Validate the server certificate (default for production).</summary>
    ValidateCertificate = 0,

    /// <summary>Accept any server certificate (tests / trusted labs only).</summary>
    AcceptAny
}
