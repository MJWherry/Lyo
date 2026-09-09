using System.Diagnostics;
using Lyo.Common.Metadata.Records;
using Lyo.People.Models.Contact;

namespace Lyo.People.Models.Preferences;

/// <summary>Contact, language, timezone, and privacy settings for a person.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class PersonPreferences
{
    /// <summary>Preferred channel (for example "Email", "Phone", "SMS").</summary>
    public string? PreferredContactMethod { get; set; }

    /// <summary>Language preferred for communication.</summary>
    public LanguageCodeInfo? PreferredLanguage { get; set; }

    /// <summary>IANA timezone (for example "America/New_York", "UTC").</summary>
    public string? TimeZone { get; set; }

    /// <summary>Per-channel communication opt-ins.</summary>
    public CommunicationPreferences Communication { get; set; } = new();

    /// <summary>Data-sharing and directory visibility choices.</summary>
    public PrivacyPreferences Privacy { get; set; } = new();

    /// <inheritdoc />
    public override string ToString() => $"PersonPreferences: contact={PreferredContactMethod ?? "?"}, timezone={TimeZone ?? "?"}";
}
