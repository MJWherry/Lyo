using System.Diagnostics;
using Lyo.Common.Metadata.Records;
using Lyo.Email.Models;
using Lyo.Exceptions;

namespace Lyo.Email.Smtp;

/// <summary>Settings for the MailKit SMTP provider.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class SmtpEmailOptions : EmailServiceOptions
{
    /// <summary>Default config section used for <see cref="SmtpEmailOptions" />.</summary>
    public const string SectionName = "SmtpEmailOptions";

    /// <summary>SMTP host name. Required.</summary>
    public string Host { get; set; } = null!;

    /// <summary>SMTP port. Defaults to <see cref="PortInfo.SmtpSubmission" /> (587).</summary>
    public int Port { get; set; } = PortInfo.SmtpSubmission;

    /// <summary>
    /// Whether the connection starts with implicit SSL. Defaults to false, which is MailKit <c>SecureSocketOptions.StartTlsWhenAvailable</c>. True is <c>SslOnConnect</c>.
    /// </summary>
    public bool UseSsl { get; set; }

    /// <summary>SMTP user name. Authentication runs only when this and <see cref="Password" /> are both set.</summary>
    public string? Username { get; set; }

    /// <summary>SMTP password. Authentication runs only when this and <see cref="Username" /> are both set.</summary>
    public string? Password { get; set; }

    /// <summary>Throws when the host or port is missing, then checks the shared sender and attachment limit.</summary>
    public override void Validate()
    {
        base.Validate();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(Host);
        FormatHelpers.ThrowIfInvalidPort(Port);
    }

    /// <summary>Includes the host and username. The password is omitted.</summary>
    /// <returns>Text covering host, port, username, and the shared options.</returns>
    public override string ToString() => $"{Host}:{Port} Username={Username} {base.ToString()}";
}
