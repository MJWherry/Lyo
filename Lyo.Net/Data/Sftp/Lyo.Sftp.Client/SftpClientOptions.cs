using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Sftp.Client;

/// <summary>Settings that <see cref="SftpClient" /> binds and validates.</summary>
public sealed class SftpClientOptions
{
    /// <summary>Section key used when binding from configuration.</summary>
    public const string SectionName = "SftpClient";

    /// <summary>Remote host name or IP address.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>SSH port. Starts at <see cref="PortInfo.Ssh" />.</summary>
    public int Port { get; set; } = PortInfo.Ssh;

    /// <summary>Username used to log in.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Password login. Optional when a private key is set.</summary>
    public string? Password { get; set; }

    /// <summary>PEM private key text. Optional.</summary>
    public string? PrivateKeyPem { get; set; }

    /// <summary>Path to a private key file. Optional.</summary>
    public string? PrivateKeyPath { get; set; }

    /// <summary>Passphrase for an encrypted private key.</summary>
    public string? PrivateKeyPassphrase { get; set; }

    /// <summary>
    /// Allowed host-key fingerprints (SHA256 base64 or hex). Required when <see cref="HostKeyPolicy" /> is <see cref="SftpHostKeyPolicy.FingerprintAllowList" />.
    /// </summary>
    public List<string> AllowedHostKeyFingerprints { get; set; } = [];

    /// <summary>How the remote host key is checked.</summary>
    public SftpHostKeyPolicy HostKeyPolicy { get; set; } = SftpHostKeyPolicy.FingerprintAllowList;

    /// <summary>POSIX root jail. Every operation is kept under this path.</summary>
    public string RootRemoteDirectory { get; set; } = "/";

    /// <summary>How long to wait for a connection.</summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Per-operation timeout passed to the SSH.NET client when the client supports it.</summary>
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>How many connected clients the pool may hold. At least 1.</summary>
    public int MaxPooledClients { get; set; } = 4;

    /// <summary>If true, <c>sftp.*</c> metrics are written through <see cref="Lyo.Metrics.IMetrics" />.</summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>Throws if required fields or policy constraints are invalid.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(Host);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(Username);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(RootRemoteDirectory);
        FormatHelpers.ThrowIfInvalidPort(Port);
        FormatHelpers.ThrowIfNotInRange(MaxPooledClients, 1);
        var hasPassword = !string.IsNullOrEmpty(Password);
        var hasPem = !string.IsNullOrWhiteSpace(PrivateKeyPem);
        var hasKeyPath = !string.IsNullOrWhiteSpace(PrivateKeyPath);
        if (!hasPassword && !hasPem && !hasKeyPath) {
            throw new InvalidFormatException("SFTP authentication requires Password, PrivateKeyPem, or PrivateKeyPath.", nameof(Password), null, "password or private key");
        }

        if (HostKeyPolicy == SftpHostKeyPolicy.FingerprintAllowList && (AllowedHostKeyFingerprints is null || AllowedHostKeyFingerprints.Count == 0)) {
            throw new InvalidFormatException(
                "AllowedHostKeyFingerprints is required when HostKeyPolicy is FingerprintAllowList.", nameof(AllowedHostKeyFingerprints), null, "one or more fingerprints");
        }
    }
}