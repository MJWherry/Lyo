using Lyo.Common.Records;
using Lyo.People.Models.Contact;

namespace Lyo.People.Models.Preferences;

/// <summary>General preferences for a person including contact and communication settings</summary>
public class PersonPreferences
{
    /// <summary>Preferred contact method (e.g., "Email", "Phone", "SMS")</summary>
    public string? PreferredContactMethod { get; set; }

    /// <summary>Preferred language for communication</summary>
    public LanguageCodeInfo? PreferredLanguage { get; set; }

    /// <summary>Preferred time zone (e.g., "America/New_York", "UTC")</summary>
    public string? TimeZone { get; set; }

    /// <summary>Communication channel preferences</summary>
    public CommunicationPreferences Communication { get; set; } = new();

    /// <summary>Privacy and data sharing preferences</summary>
    public PrivacyPreferences Privacy { get; set; } = new();
}