using System.Diagnostics;

namespace Lyo.Sms.Models;

/// <summary>Shared settings for SMS service implementations.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public abstract class SmsServiceOptions
{
    /// <summary>Default sender number; a message may override it.</summary>
    public string? DefaultFromPhoneNumber { get; set; }

    /// <summary>How many bulk SMS items may run at once (default 10).</summary>
    public int BulkSmsConcurrencyLimit { get; set; } = 10;

    /// <summary>Longest allowed body, in characters (default 1600).</summary>
    public int MaxMessageBodyLength { get; set; } = 1600;

    /// <summary>Upper bound on messages in one bulk send (default 1000).</summary>
    public int MaxBulkSmsLimit { get; set; } = 1000;

    /// <summary>Whether SMS operations record metrics. Defaults to false.</summary>
    public bool EnableMetrics { get; set; } = false;

    /// <summary>String form of the configured options.</summary>
    /// <returns>Text covering sender defaults, limits, and the metrics flag.</returns>
    public override string ToString()
        => $"DefaultFromPhoneNumber: {DefaultFromPhoneNumber}, BulkSmsConcurrencyLimit: {BulkSmsConcurrencyLimit}, MaxMessageBodyLength: {MaxMessageBodyLength}, MaxBulkSmsLimit: {MaxBulkSmsLimit}, EnableMetrics: {EnableMetrics}";
}