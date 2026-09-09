using Lyo.Http.Client;

namespace Lyo.Discord.Client;

/// <summary>
/// Options for <see cref="LyoDiscordClient" />. Inherits <see cref="LyoHttpClientOptions" /> for HTTP behavior; use <see cref="LyoHttpClientOptions.BaseUrl" /> of the Lyo API root.
/// </summary>
public class LyoDiscordClientOptions : LyoHttpClientOptions
{
    /// <summary>Options section name for binding (e.g. <c>appsettings.json</c>).</summary>
    public new const string SectionName = "LyoDiscordClient";

    /// <summary>Fills defaults when the section is absent or does not set <see cref="LyoHttpClientOptions.BaseUrl" />.</summary>
    public LyoDiscordClientOptions() => BaseUrl = "http://localhost:5251/";
}
