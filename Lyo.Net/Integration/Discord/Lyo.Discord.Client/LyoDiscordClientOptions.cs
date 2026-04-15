using Lyo.Api.Client;

namespace Lyo.Discord.Client;

/// <summary>Options for <see cref="LyoDiscordClient" />. Inherits <see cref="ApiClientOptions" /> for HTTP behavior; use <see cref="ApiClientOptions.BaseUrl" /> for the Lyo API root.</summary>
public class LyoDiscordClientOptions : ApiClientOptions
{
    /// <summary>Configuration section name for binding (e.g. <c>appsettings.json</c>).</summary>
    public new const string SectionName = "LyoDiscordClient";

    /// <summary>Initializes defaults when the section is absent or does not set <see cref="ApiClientOptions.BaseUrl" />.</summary>
    public LyoDiscordClientOptions() => BaseUrl = "http://localhost:5092/";
}
