using System.Diagnostics;

namespace Lyo.People.Models.Contact;

/// <summary>Per-channel opt-ins for contacting a person.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class CommunicationPreferences
{
    /// <summary>True when email contact is allowed.</summary>
    public bool AllowEmail { get; set; }

    /// <summary>True when SMS contact is allowed.</summary>
    public bool AllowSms { get; set; }

    /// <summary>True when voice calls are allowed.</summary>
    public bool AllowPhoneCalls { get; set; }

    /// <summary>True when marketing email is allowed.</summary>
    public bool AllowMarketingEmails { get; set; }

    /// <summary>True when newsletter mail is allowed.</summary>
    public bool AllowNewsletters { get; set; }

    /// <summary>Preferred windows (for example "9am-5pm", "weekdays only").</summary>
    public List<string> PreferredContactTimes { get; set; } = new();

    /// <inheritdoc />
    public override string ToString() => $"CommunicationPreferences: email={AllowEmail}, sms={AllowSms}, phone={AllowPhoneCalls}, marketing={AllowMarketingEmails}";
}
