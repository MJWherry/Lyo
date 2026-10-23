using System.Diagnostics;

namespace Lyo.Config.Api;

/// <summary>Host-level settings that annotate HTTP responses with observability hints.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ConfigApiHostingOptions
{
    public const string SectionName = "ConfigApiHosting";

    /// <summary>Optional millisecond hint written to <c>X-Config-Poll-Interval-Ms</c> when operators want a shared poll default.</summary>
    public int? PollIntervalAdvisoryMilliseconds { get; set; }

    /// <summary>Advisory poll interval written on config responses.</summary>
    public override string ToString() => $"PollIntervalAdvisoryMs={PollIntervalAdvisoryMilliseconds}";
}