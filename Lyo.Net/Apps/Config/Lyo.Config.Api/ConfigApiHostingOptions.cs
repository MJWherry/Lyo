namespace Lyo.Config.Api;

/// <summary>Top-level knobs for observability helpers on HTTP responses.</summary>
public sealed class ConfigApiHostingOptions
{
    public const string SectionName = "ConfigApiHosting";

    /// <summary>Optional advisory milliseconds emitted as <c>X-Config-Poll-Interval-Ms</c> for operators that want centralized defaults.</summary>
    public int? PollIntervalAdvisoryMilliseconds { get; set; }
}