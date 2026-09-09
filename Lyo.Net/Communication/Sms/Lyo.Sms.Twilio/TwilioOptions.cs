using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Sms.Models;

namespace Lyo.Sms.Twilio;

/// <summary>Settings for the Twilio SMS provider.</summary>
/// <remarks>
/// <para>Not thread-safe. Configure during startup and leave the instance alone after it is registered.</para>
/// <para>Inherited members from <see cref="SmsServiceOptions" /> remain available, including:</para>
/// <list type="bullet">
/// <item><see cref="SmsServiceOptions.DefaultFromPhoneNumber" /> — default sender number</item>
/// <item><see cref="SmsServiceOptions.BulkSmsConcurrencyLimit" /> — how many bulk SMS items may run at once (default 10)</item>
/// <item><see cref="SmsServiceOptions.MaxMessageBodyLength" /> — longest body, in characters (default 1600)</item>
/// <item><see cref="SmsServiceOptions.MaxBulkSmsLimit" /> — upper bound on messages in one bulk send (default 1000)</item>
/// <item><see cref="SmsServiceOptions.EnableMetrics" /> — whether metrics are recorded (default false)</item>
/// </list>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class TwilioOptions : SmsServiceOptions
{
    /// <summary>Default config section used for TwilioOptions.</summary>
    public const string SectionName = "TwilioOptions";

    /// <summary>Twilio Account SID (required).</summary>
    /// <remarks>Account identifier from the Twilio Console.</remarks>
    public string AccountSid { get; set; } = null!;

    /// <summary>Twilio Auth Token (required).</summary>
    /// <remarks>Auth token from the Twilio Console. Treat as secret; do not commit it.</remarks>
    public string AuthToken { get; set; } = null!;

    /// <summary>Privacy-safe string form of the options (AuthToken omitted).</summary>
    /// <returns>A string that includes AccountSid.</returns>
    public override string ToString() => $"AccountSid={AccountSid}";

    /// <summary>Throws when AccountSid or AuthToken is missing.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(AccountSid);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(AuthToken);
    }
}