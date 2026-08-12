using Lyo.Common.Records;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Ftp.Client;

/// <summary>Configuration for <see cref="FtpClient" />.</summary>
public sealed class FtpClientOptions
{
    /// <summary>Default configuration section name.</summary>
    public const string SectionName = "FtpClient";

    /// <summary>Remote host name or IP.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>FTP port (default <see cref="PortInfo.Ftp" />).</summary>
    public int Port { get; set; } = PortInfo.Ftp;

    /// <summary>Login username.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Login password.</summary>
    public string? Password { get; set; }

    /// <summary>Remote root directory jail (POSIX). All operations are constrained under this path.</summary>
    public string RootRemoteDirectory { get; set; } = "/";

    /// <summary>Control-channel encryption mode.</summary>
    public FtpEncryptionMode EncryptionMode { get; set; } = FtpEncryptionMode.None;

    /// <summary>TLS certificate validation when encryption is enabled.</summary>
    public FtpTlsPolicy TlsPolicy { get; set; } = FtpTlsPolicy.ValidateCertificate;

    /// <summary>Connection timeout.</summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Read/data timeout applied to FluentFTP config.</summary>
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Maximum pooled connected clients (minimum 1).</summary>
    public int MaxPooledClients { get; set; } = 4;

    /// <summary>
    /// Optional passive data-port range advertised by the server (inclusive). When set, FluentFTP is configured for PASV and
    /// callers should publish the same host ports (1:1) for NAT/Testcontainers.
    /// </summary>
    public (int Min, int Max)? PassivePortRange { get; set; }

    /// <summary>When true, emit <c>ftp.*</c> metrics via <see cref="Lyo.Metrics.IMetrics" />.</summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>Validates required fields and policy constraints.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(Host);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(Username);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(RootRemoteDirectory);
        FormatHelpers.ThrowIfInvalidPort(Port);
        FormatHelpers.ThrowIfNotInRange(MaxPooledClients, min: 1);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(Password);

        if (PassivePortRange is { } range) {
            FormatHelpers.ThrowIfInvalidPort(range.Min);
            FormatHelpers.ThrowIfInvalidPort(range.Max);
            FormatHelpers.ThrowIf(range.Min > range.Max, "PassivePortRange.Min must be <= Max.", nameof(PassivePortRange), $"{range.Min}-{range.Max}", "min <= max");
        }

        if (EncryptionMode != FtpEncryptionMode.None && TlsPolicy == FtpTlsPolicy.ValidateCertificate) {
            // Valid — no extra fields required; FluentFTP uses system trust.
        }
    }
}
