using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;

namespace Lyo.Ftp.Client;

/// <summary>Settings that <see cref="FtpClient" /> binds and validates.</summary>
public sealed class FtpClientOptions
{
    /// <summary>Section key used when binding from configuration.</summary>
    public const string SectionName = "FtpClient";

    /// <summary>Remote host name or IP address.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>FTP port. Starts at <see cref="PortInfo.Ftp" />.</summary>
    public int Port { get; set; } = PortInfo.Ftp;

    /// <summary>Username used to log in.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Password used to log in.</summary>
    public string? Password { get; set; }

    /// <summary>POSIX root jail. Every operation is kept under this path.</summary>
    public string RootRemoteDirectory { get; set; } = "/";

    /// <summary>How the control channel is encrypted.</summary>
    public FtpEncryptionMode EncryptionMode { get; set; } = FtpEncryptionMode.None;

    /// <summary>How the TLS certificate is checked when encryption is on.</summary>
    public FtpTlsPolicy TlsPolicy { get; set; } = FtpTlsPolicy.ValidateCertificate;

    /// <summary>How long to wait for a connection.</summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Read and data timeout passed to FluentFTP.</summary>
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>How many connected clients the pool may hold. At least 1.</summary>
    public int MaxPooledClients { get; set; } = 4;

    /// <summary>
    /// Optional inclusive PASV data-port range advertised by the server. When set, FluentFTP uses PASV and callers should publish the same host ports one-to-one for
    /// NAT or Testcontainers.
    /// </summary>
    public (int Min, int Max)? PassivePortRange { get; set; }

    /// <summary>If true, <c>ftp.*</c> metrics are written through <see cref="Lyo.Metrics.IMetrics" />.</summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>Throws if required fields or policy constraints are invalid.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(Host);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(Username);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(RootRemoteDirectory);
        FormatHelpers.ThrowIfInvalidPort(Port);
        FormatHelpers.ThrowIfNotInRange(MaxPooledClients, 1);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(Password);
        if (PassivePortRange is { } range) {
            FormatHelpers.ThrowIfInvalidPort(range.Min);
            FormatHelpers.ThrowIfInvalidPort(range.Max);
            FormatHelpers.ThrowIf(range.Min > range.Max, "PassivePortRange.Min must be <= Max.", nameof(PassivePortRange), $"{range.Min}-{range.Max}", "min <= max");
        }

        if (EncryptionMode != FtpEncryptionMode.None && TlsPolicy == FtpTlsPolicy.ValidateCertificate) {
            // Fine as-is. No extra fields needed. FluentFTP uses the system trust store.
        }
    }
}