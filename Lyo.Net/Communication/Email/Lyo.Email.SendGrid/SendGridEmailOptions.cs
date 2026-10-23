using System.Diagnostics;
using Lyo.Email.Models;
using Lyo.Exceptions;

namespace Lyo.Email.SendGrid;

/// <summary>Settings for the Twilio SendGrid email provider.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class SendGridEmailOptions : EmailServiceOptions
{
    /// <summary>Default config section used for <see cref="SendGridEmailOptions" />.</summary>
    public const string SectionName = "SendGridEmailOptions";

    /// <summary>SendGrid API key. Required. Treat as secret.</summary>
    public string ApiKey { get; set; } = null!;

    /// <summary>Throws when the API key is missing, then checks the shared sender and attachment limit.</summary>
    public override void Validate()
    {
        base.Validate();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(ApiKey);
    }

    /// <summary>Whether an API key is set, plus the shared options. The key value is omitted.</summary>
    public override string ToString() => $"ApiKeySet={!string.IsNullOrWhiteSpace(ApiKey)} {base.ToString()}";
}
