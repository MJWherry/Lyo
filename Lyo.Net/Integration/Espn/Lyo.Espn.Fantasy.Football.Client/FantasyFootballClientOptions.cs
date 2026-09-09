using Lyo.Http.Client;

namespace Lyo.Espn.Fantasy.Football.Client;

/// <summary>
/// Settings that control the ESPN fantasy football client. Inherits <see cref="LyoHttpClientOptions" />; use <see cref="LyoHttpClientOptions.BaseUrl" /> for the ESPN fantasy API
/// host root.
/// </summary>
public class FantasyFootballClientOptions : LyoHttpClientOptions
{
    /// <summary>Default options-section name for <see cref="FantasyFootballClientOptions" />.</summary>
    public new const string SectionName = "FantasyFootballClient";

    /// <summary>Holds the value of the <c>espn_s2</c> cookie for private leagues.</summary>
    public string? EspnS2 { get; set; }

    /// <summary>Value of the <c>SWID</c> cookie for private leagues.</summary>
    public string? Swid { get; set; }

    /// <summary>Fills defaults for the public ESPN fantasy API.</summary>
    public FantasyFootballClientOptions() => BaseUrl = "https://lm-api-reads.fantasy.espn.com/apis/v3/games/ffl/";
}
