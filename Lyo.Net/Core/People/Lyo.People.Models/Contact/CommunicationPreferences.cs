namespace Lyo.People.Models.Contact;

/// <summary>Communication channel preferences for a person</summary>
public class CommunicationPreferences
{
    /// <summary>Whether the person allows contact via email</summary>
    public bool AllowEmail { get; set; }

    /// <summary>Whether the person allows contact via SMS</summary>
    public bool AllowSms { get; set; }

    /// <summary>Whether the person allows contact via phone calls</summary>
    public bool AllowPhoneCalls { get; set; }

    /// <summary>Whether the person allows marketing emails</summary>
    public bool AllowMarketingEmails { get; set; }

    /// <summary>Whether the person allows newsletter subscriptions</summary>
    public bool AllowNewsletters { get; set; }

    /// <summary>Preferred times for contact (e.g., "9am-5pm", "weekdays only")</summary>
    public List<string> PreferredContactTimes { get; set; } = new();
}