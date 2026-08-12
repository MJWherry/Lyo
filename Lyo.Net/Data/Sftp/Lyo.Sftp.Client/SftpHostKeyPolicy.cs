namespace Lyo.Sftp.Client;

/// <summary>How the SFTP client validates the remote host key.</summary>
public enum SftpHostKeyPolicy
{
    /// <summary>Require the host key fingerprint to appear in <see cref="SftpClientOptions.AllowedHostKeyFingerprints" />.</summary>
    FingerprintAllowList = 0,

    /// <summary>Accept any host key. Intended for local/integration tests only — never use in production.</summary>
    AcceptAny = 1
}