using System.Diagnostics;

namespace Lyo.Sms.Models;

/// <summary>Base configuration options for SMS service implementations.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public abstract class SmsServiceOptions
{
    /// <summary>Gets or sets the default sender phone number (optional, can be overridden per message).</summary>
    public string? DefaultFromPhoneNumber { get; set; }

    /// <summary>Gets or sets the maximum number of concurrent bulk SMS requests (default: 10).</summary>
    public int BulkSmsConcurrencyLimit { get; set; } = 10;

    /// <summary>Gets or sets the maximum message body length in characters (default: 1600).</summary>
    public int MaxMessageBodyLength { get; set; } = 1600;

    /// <summary>Gets or sets the maximum number of messages allowed in a single bulk SMS operation (default: 1000).</summary>
    public int MaxBulkSmsLimit { get; set; } = 1000;

    /// <summary>Enable metrics collection for SMS operations. Default: false</summary>
    public bool EnableMetrics { get; set; } = false;

    public override string ToString()
        => $"DefaultFromPhoneNumber: {DefaultFromPhoneNumber}, BulkSmsConcurrencyLimit: {BulkSmsConcurrencyLimit}, MaxMessageBodyLength: {MaxMessageBodyLength}, MaxBulkSmsLimit: {MaxBulkSmsLimit}, EnableMetrics: {EnableMetrics}";
}