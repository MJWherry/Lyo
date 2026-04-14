using System.Diagnostics;

namespace Lyo.ContactUs.Models;

/// <summary>Configuration options for the contact form service.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class ContactUsServiceOptions
{
    /// <summary>Default configuration section name for binding options.</summary>
    public const string SectionName = "ContactUsOptions";

    /// <summary>Gets or sets the maximum message length in characters (default: 10000).</summary>
    public int MaxMessageLength { get; set; } = 10000;

    /// <summary>Gets or sets the minimum message length in characters (default: 10).</summary>
    public int MinMessageLength { get; set; } = 10;

    /// <summary>Enable metrics collection for contact form operations. Default: false.</summary>
    public bool EnableMetrics { get; set; } = false;

    public override string ToString() => $"MaxMessageLength: {MaxMessageLength}, MinMessageLength: {MinMessageLength}, EnableMetrics: {EnableMetrics}";
}