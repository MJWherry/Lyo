using Lyo.Api.Client;

namespace Lyo.Config.Api.Client;

/// <summary>Settings for the Config API HTTP client. Bind from the "ConfigApi" section.</summary>
public sealed class ConfigApiClientOptions : ApiClientOptions
{
    public new const string SectionName = "ConfigApi";

    /// <summary>Suggested poll interval for callers. This client does not enforce it.</summary>
    public TimeSpan? PollInterval { get; set; }

    /// <summary>Optional key sent as <c>X-Api-Key</c> when the server demands API-key auth.</summary>
    public string? ApiKey { get; set; }
}