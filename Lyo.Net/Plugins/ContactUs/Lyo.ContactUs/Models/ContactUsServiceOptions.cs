using System.Diagnostics;

namespace Lyo.ContactUs.Models;

/// <summary>Settings that control the contact form service.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class ContactUsServiceOptions
{
    /// <summary>Default options-section name for binding options.</summary>
    public const string SectionName = "ContactUsOptions";

    /// <summary>Holds the maximum message length in characters (default: 10000).</summary>
    public int MaxMessageLength { get; set; } = 10000;

    /// <summary>Minimum message length in characters (default: 10).</summary>
    public int MinMessageLength { get; set; } = 10;

    /// <summary>Contact form operations. Default: false enable metrics collection.</summary>
    public bool EnableMetrics { get; set; } = false;

    public override string ToString() => $"MaxMessageLength: {MaxMessageLength}, MinMessageLength: {MinMessageLength}, EnableMetrics: {EnableMetrics}";
}