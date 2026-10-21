using Lyo.Http.Client;

namespace Lyo.Espn.Fantasy.Football.Client;

/// <summary>
/// Settings that control the ESPN fantasy football client. Inherits <see cref="LyoHttpClientOptions" />; use <see cref="LyoHttpClientOptions.BaseUrl" /> for the ESPN fantasy
/// API host root.
/// </summary>
public class FantasyFootballClientOptions : LyoHttpClientOptions
{
    /// <summary>Default options-section name for <see cref="FantasyFootballClientOptions" />.</summary>
    public new const string SectionName = "FantasyFootballClient";

    /// <summary>Holds the value of the <c>espn_s2</c> cookie for private leagues.</summary>
    public string? EspnS2 { get; set; }

    /// <summary>Value of the <c>SWID</c> cookie for private leagues.</summary>
    public string? Swid { get; set; }

    /// <summary>Host root for Fantasy Chat. Defaults to <c>https://lm-api-communication.fantasy.espn.com/apis/v3/games/ffl/</c>.</summary>
    public string CommunicationBaseUrl { get; set; } = "https://lm-api-communication.fantasy.espn.com/apis/v3/games/ffl/";

    /// <summary>Host root for lineup, waiver, and trade writes. Defaults to <c>https://lm-api-writes.fantasy.espn.com/apis/v3/games/ffl/</c>.</summary>
    public string WritesBaseUrl { get; set; } = "https://lm-api-writes.fantasy.espn.com/apis/v3/games/ffl/";

    /// <summary>Absolute URL of the fantasy player news feed. Defaults to <c>https://site.api.espn.com/apis/fantasy/v3/games/ffl/news/players</c>.</summary>
    public string NewsBaseUrl { get; set; } = "https://site.api.espn.com/apis/fantasy/v3/games/ffl/news/players";

    /// <summary>Fills the reads host and turns on the browser User-Agent. The chat host returns 403 when that header is missing.</summary>
    public FantasyFootballClientOptions()
    {
        BaseUrl = "https://lm-api-reads.fantasy.espn.com/apis/v3/games/ffl/";
        UserAgent.Enabled = true;
    }
}