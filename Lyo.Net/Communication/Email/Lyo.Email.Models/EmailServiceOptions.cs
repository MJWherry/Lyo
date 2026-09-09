using System.Diagnostics;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Microsoft.Extensions.Options;

namespace Lyo.Email.Models;

/// <summary>Settings that configure EmailService.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class EmailServiceOptions
{
    /// <summary>Section name used when binding these options from configuration.</summary>
    public const string SectionName = "EmailServiceOptions";

    /// <summary>SMTP host name. Required.</summary>
    public string Host { get; set; } = null!;

    /// <summary>SMTP port. Defaults to <see cref="PortInfo.SmtpSubmission" /> (587).</summary>
    public int Port { get; set; } = PortInfo.SmtpSubmission;

    /// <summary>Whether the SMTP connection uses SSL/TLS. Defaults to false.</summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>Default sender address. Required.</summary>
    public string DefaultFromAddress { get; set; } = null!;

    /// <summary>Default sender display name. Required.</summary>
    public string DefaultFromName { get; set; } = null!;

    /// <summary>SMTP user name. Optional when the server does not require auth.</summary>
    public string? Username { get; set; }

    /// <summary>SMTP password. Optional when the server does not require auth.</summary>
    public string? Password { get; set; }

    /// <summary>Whether email operations record metrics. Defaults to false.</summary>
    public bool EnableMetrics { get; set; } = false;

    /// <summary>How many bulk email items may run at once (default 10).</summary>
    public int BulkEmailConcurrencyLimit { get; set; } = 10;

    /// <summary>Upper bound on messages in one bulk send (default 1000).</summary>
    public int MaxBulkEmailLimit { get; set; } = 1000;

    /// <summary>Upper bound on attachments per message (default 20).</summary>
    public int MaxAttachmentCountPerEmail { get; set; } = 20;

    /// <summary>String form of the configured options.</summary>
    /// <returns>Text covering host, port, sender, and bulk/attachment limits.</returns>
    public override string ToString()
        => $"{Host}:{Port} Username={Username} FromAddress={DefaultFromAddress} FromName={DefaultFromName} BulkEmailConcurrencyLimit={BulkEmailConcurrencyLimit} MaxBulkEmailLimit={MaxBulkEmailLimit} MaxAttachmentCountPerEmail={MaxAttachmentCountPerEmail}";
}

/// <summary>Checks that required EmailServiceOptions members are populated.</summary>
public sealed class EmailServiceOptionsValidator : IValidateOptions<EmailServiceOptions>
{
    /// <summary>Validates one <see cref="EmailServiceOptions" /> instance.</summary>
    /// <param name="name">Named-options id, if any.</param>
    /// <param name="options">Instance to check.</param>
    /// <returns><see cref="ValidateOptionsResult.Success" /> when valid; otherwise a failure with details.</returns>
    public ValidateOptionsResult Validate(string? name, EmailServiceOptions? options)
    {
        if (options == null)
            return ValidateOptionsResult.Fail("EmailServiceOptions cannot be null.");

        if (string.IsNullOrWhiteSpace(options.Host))
            return ValidateOptionsResult.Fail("EmailServiceOptions.Host is required.");

        if (!FormatHelpers.IsValidPort(options.Port))
            return ValidateOptionsResult.Fail($"EmailServiceOptions.Port must be between {FormatHelpers.MinPort} and {FormatHelpers.MaxPort}.");

        if (string.IsNullOrWhiteSpace(options.DefaultFromAddress))
            return ValidateOptionsResult.Fail("EmailServiceOptions.FromAddress is required.");

        if (string.IsNullOrWhiteSpace(options.DefaultFromName))
            return ValidateOptionsResult.Fail("EmailServiceOptions.FromName is required.");

        if (options.MaxAttachmentCountPerEmail <= 0)
            return ValidateOptionsResult.Fail("EmailServiceOptions.MaxAttachmentCountPerEmail must be greater than 0.");

        return ValidateOptionsResult.Success;
    }
}