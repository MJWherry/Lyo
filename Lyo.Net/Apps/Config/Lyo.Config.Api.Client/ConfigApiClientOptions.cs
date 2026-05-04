using Lyo.Api.Client;

namespace Lyo.Config.Api.Client;

/// <summary>Configuration for the Config API HTTP client. Bind from the "ConfigApi" configuration section.</summary>
public sealed class ConfigApiClientOptions : ApiClientOptions
{
    public new const string SectionName = "ConfigApi";

    /// <summary>Optional default polling interval suggestions for callers (not enforced by this client).</summary>
    public TimeSpan? PollInterval { get; set; }

    /// <summary>Optional API key forwarded as <c>X-Api-Key</c> when the server requires API key authentication.</summary>
    public string? ApiKey { get; set; }
}
