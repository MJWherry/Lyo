using System.Diagnostics;
using Lyo.Sms.Models;

namespace Lyo.Sms.Twilio;

/// <summary>Configuration options for Twilio SMS service.</summary>
/// <remarks>
/// <para>This class is not thread-safe. Options should be configured during application startup and not modified after service registration.</para>
/// <para>All properties from the base <see cref="SmsServiceOptions" /> class are also available, including:</para>
/// <list type="bullet">
/// <item><see cref="SmsServiceOptions.DefaultFromPhoneNumber" /> - Default sender phone number</item>
/// <item><see cref="SmsServiceOptions.BulkSmsConcurrencyLimit" /> - Maximum concurrent bulk SMS requests (default: 10)</item>
/// <item><see cref="SmsServiceOptions.MaxMessageBodyLength" /> - Maximum message body length in characters (default: 1600)</item>
/// <item><see cref="SmsServiceOptions.MaxBulkSmsLimit" /> - Maximum messages per bulk operation (default: 1000)</item>
/// <item><see cref="SmsServiceOptions.EnableMetrics" /> - Enable metrics collection (default: false)</item>
/// </list>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class TwilioOptions : SmsServiceOptions
{
    /// <summary>The default configuration section name for TwilioOptions.</summary>
    public const string SectionName = "TwilioOptions";

    /// <summary>Gets or sets the Twilio Account SID (required).</summary>
    /// <remarks>This is your Twilio account identifier, found in your Twilio Console.</remarks>
    public string AccountSid { get; set; } = null!;

    /// <summary>Gets or sets the Twilio Auth Token (required).</summary>
    /// <remarks>This is your Twilio authentication token, found in your Twilio Console. Keep this secure and never commit it to source control.</remarks>
    public string AuthToken { get; set; } = null!;

    /// <summary>Returns a string representation of the options (privacy-safe, does not include AuthToken).</summary>
    /// <returns>A string containing the AccountSid.</returns>
    public override string ToString() => $"AccountSid={AccountSid}";
}