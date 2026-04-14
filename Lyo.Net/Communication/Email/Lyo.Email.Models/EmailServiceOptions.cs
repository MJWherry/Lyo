using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace Lyo.Email.Models;

/// <summary>Configuration options for EmailService.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class EmailServiceOptions
{
    /// <summary>The configuration section name used when binding from configuration.</summary>
    public const string SectionName = "EmailServiceOptions";

    /// <summary>Gets or sets the SMTP server hostname. Required.</summary>
    public string Host { get; set; } = null!;

    /// <summary>Gets or sets the SMTP server port. Default: 587.</summary>
    public int Port { get; set; } = 587;

    /// <summary>Gets or sets whether to use SSL/TLS for the SMTP connection. Default: false.</summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>Gets or sets the default from email address. Required.</summary>
    public string DefaultFromAddress { get; set; } = null!;

    /// <summary>Gets or sets the default from display name. Required.</summary>
    public string DefaultFromName { get; set; } = null!;

    /// <summary>Gets or sets the SMTP username for authentication. Optional if server doesn't require authentication.</summary>
    public string? Username { get; set; }

    /// <summary>Gets or sets the SMTP password for authentication. Optional if server doesn't require authentication.</summary>
    public string? Password { get; set; }

    /// <summary>Gets or sets whether to enable metrics collection for email operations. Default: false.</summary>
    public bool EnableMetrics { get; set; } = false;

    /// <summary>Gets or sets the maximum number of concurrent bulk email requests (default: 10).</summary>
    public int BulkEmailConcurrencyLimit { get; set; } = 10;

    /// <summary>Gets or sets the maximum number of messages allowed in a single bulk email operation (default: 1000).</summary>
    public int MaxBulkEmailLimit { get; set; } = 1000;

    /// <summary>Gets or sets the maximum number of attachments allowed per email (default: 20).</summary>
    public int MaxAttachmentCountPerEmail { get; set; } = 20;

    public override string ToString()
        => $"{Host}:{Port} Username={Username} FromAddress={DefaultFromAddress} FromName={DefaultFromName} BulkEmailConcurrencyLimit={BulkEmailConcurrencyLimit} MaxBulkEmailLimit={MaxBulkEmailLimit} MaxAttachmentCountPerEmail={MaxAttachmentCountPerEmail}";
}

/// <summary>Validator for EmailServiceOptions to ensure required properties are set.</summary>
public sealed class EmailServiceOptionsValidator : IValidateOptions<EmailServiceOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailServiceOptions options)
    {
        if (options == null)
            return ValidateOptionsResult.Fail("EmailServiceOptions cannot be null.");

        if (string.IsNullOrWhiteSpace(options.Host))
            return ValidateOptionsResult.Fail("EmailServiceOptions.Host is required.");

        if (options.Port <= 0 || options.Port > 65535)
            return ValidateOptionsResult.Fail("EmailServiceOptions.Port must be between 1 and 65535.");

        if (string.IsNullOrWhiteSpace(options.DefaultFromAddress))
            return ValidateOptionsResult.Fail("EmailServiceOptions.FromAddress is required.");

        if (string.IsNullOrWhiteSpace(options.DefaultFromName))
            return ValidateOptionsResult.Fail("EmailServiceOptions.FromName is required.");

        if (options.MaxAttachmentCountPerEmail <= 0)
            return ValidateOptionsResult.Fail("EmailServiceOptions.MaxAttachmentCountPerEmail must be greater than 0.");

        return ValidateOptionsResult.Success;
    }
}