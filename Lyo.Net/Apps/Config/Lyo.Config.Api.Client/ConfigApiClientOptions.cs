using System.Diagnostics;
using Lyo.Api.Client;

namespace Lyo.Config.Api.Client;

/// <summary>Settings for the Config API HTTP client. Bind from the "ConfigApi" section.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ConfigApiClientOptions : ApiClientOptions
{
    public new const string SectionName = "ConfigApi";

    /// <summary>Suggested poll interval for callers. This client does not enforce it.</summary>
    public TimeSpan? PollInterval { get; set; }

    /// <summary>Optional key sent as <c>X-Api-Key</c> when the server demands API-key auth.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Poll interval and whether an API key is set. The key is omitted.</summary>
    public override string ToString() => $"{base.ToString()} PollInterval={PollInterval} ApiKeySet={!string.IsNullOrWhiteSpace(ApiKey)}";
}