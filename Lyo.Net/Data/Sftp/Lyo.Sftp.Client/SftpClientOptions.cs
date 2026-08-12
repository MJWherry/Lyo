using Lyo.Common.Records;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Sftp.Client;

/// <summary>Configuration for <see cref="SftpClient" />.</summary>
public sealed class SftpClientOptions
{
    /// <summary>Default configuration section name.</summary>
    public const string SectionName = "SftpClient";

    /// <summary>Remote host name or IP.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>SSH port (default <see cref="PortInfo.Ssh" />).</summary>
    public int Port { get; set; } = PortInfo.Ssh;

    /// <summary>Login username.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Password authentication (optional when a private key is configured).</summary>
    public string? Password { get; set; }

    /// <summary>PEM private key contents (optional).</summary>
    public string? PrivateKeyPem { get; set; }

    /// <summary>Path to a private key file (optional).</summary>
    public string? PrivateKeyPath { get; set; }

    /// <summary>Passphrase for the private key when encrypted.</summary>
    public string? PrivateKeyPassphrase { get; set; }

    /// <summary>Allowed host key fingerprints (SHA256 base64 or hex). Required when <see cref="HostKeyPolicy" /> is <see cref="SftpHostKeyPolicy.FingerprintAllowList" />.</summary>
    public List<string> AllowedHostKeyFingerprints { get; set; } = [];

    /// <summary>Host key validation policy.</summary>
    public SftpHostKeyPolicy HostKeyPolicy { get; set; } = SftpHostKeyPolicy.FingerprintAllowList;

    /// <summary>Remote root directory jail (POSIX). All operations are constrained under this path.</summary>
    public string RootRemoteDirectory { get; set; } = "/";

    /// <summary>Connection timeout.</summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Per-operation timeout applied to the underlying SSH.NET client when supported.</summary>
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Maximum pooled connected clients (minimum 1).</summary>
    public int MaxPooledClients { get; set; } = 4;

    /// <summary>When true, emit <c>sftp.*</c> metrics via <see cref="Lyo.Metrics.IMetrics" />.</summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>Validates required fields and policy constraints.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(Host);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(Username);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(RootRemoteDirectory);
        FormatHelpers.ThrowIfInvalidPort(Port);
        FormatHelpers.ThrowIfNotInRange(MaxPooledClients, min: 1);

        var hasPassword = !string.IsNullOrEmpty(Password);
        var hasPem = !string.IsNullOrWhiteSpace(PrivateKeyPem);
        var hasKeyPath = !string.IsNullOrWhiteSpace(PrivateKeyPath);
        if (!hasPassword && !hasPem && !hasKeyPath)
            throw new InvalidFormatException(
                "SFTP authentication requires Password, PrivateKeyPem, or PrivateKeyPath.", nameof(Password), null,
                "password or private key");

        if (HostKeyPolicy == SftpHostKeyPolicy.FingerprintAllowList && (AllowedHostKeyFingerprints is null || AllowedHostKeyFingerprints.Count == 0))
            throw new InvalidFormatException(
                "AllowedHostKeyFingerprints is required when HostKeyPolicy is FingerprintAllowList.", nameof(AllowedHostKeyFingerprints), null,
                "one or more fingerprints");
    }
}
