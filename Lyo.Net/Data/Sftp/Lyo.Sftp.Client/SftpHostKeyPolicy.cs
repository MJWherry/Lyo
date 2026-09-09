namespace Lyo.Sftp.Client;

/// <summary>How the SFTP client checks the remote host key.</summary>
public enum SftpHostKeyPolicy
{
    /// <summary>The host-key fingerprint must appear in <see cref="SftpClientOptions.AllowedHostKeyFingerprints" />.</summary>
    FingerprintAllowList = 0,

    /// <summary>Accept any host key. Local and integration tests only. Do not use in production.</summary>
    AcceptAny = 1
}