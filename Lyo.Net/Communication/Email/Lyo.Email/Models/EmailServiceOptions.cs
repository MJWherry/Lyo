using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Email.Models;

/// <summary>Shared settings for email providers. Host, credentials, and API keys live on the provider options.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public abstract class EmailServiceOptions
{
    /// <summary>Default sender address. Required.</summary>
    public string DefaultFromAddress { get; set; } = null!;

    /// <summary>Default sender display name. Required.</summary>
    public string DefaultFromName { get; set; } = null!;

    /// <summary>Whether email operations record metrics. Defaults to false.</summary>
    public bool EnableMetrics { get; set; }

    /// <summary>How many bulk email items may run at once on providers that fan out (default 10). SMTP bulk ignores this and uses one connection.</summary>
    public int BulkEmailConcurrencyLimit { get; set; } = 10;

    /// <summary>Upper bound on messages in one bulk send (default 1000).</summary>
    public int MaxBulkEmailLimit { get; set; } = 1000;

    /// <summary>Upper bound on attachments per message (default 20).</summary>
    public int MaxAttachmentCountPerEmail { get; set; } = 20;

    /// <summary>Throws when the shared required fields are missing.</summary>
    public virtual void Validate()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(DefaultFromAddress);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(DefaultFromName);
        ArgumentHelpers.ThrowIfNotInRange(MaxAttachmentCountPerEmail, 1, int.MaxValue);
    }

    /// <summary>String form of the shared options. Secrets stay on the provider <c>ToString</c>.</summary>
    /// <returns>Text covering the default sender and limits.</returns>
    public override string ToString()
        => $"FromAddress={DefaultFromAddress} FromName={DefaultFromName} BulkEmailConcurrencyLimit={BulkEmailConcurrencyLimit} MaxBulkEmailLimit={MaxBulkEmailLimit} MaxAttachmentCountPerEmail={MaxAttachmentCountPerEmail} EnableMetrics={EnableMetrics}";
}
