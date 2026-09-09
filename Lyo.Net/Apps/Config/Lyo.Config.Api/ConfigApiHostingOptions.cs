namespace Lyo.Config.Api;

/// <summary>Host-level settings that annotate HTTP responses with observability hints.</summary>
public sealed class ConfigApiHostingOptions
{
    public const string SectionName = "ConfigApiHosting";

    /// <summary>Optional millisecond hint written to <c>X-Config-Poll-Interval-Ms</c> when operators want a shared poll default.</summary>
    public int? PollIntervalAdvisoryMilliseconds { get; set; }
}