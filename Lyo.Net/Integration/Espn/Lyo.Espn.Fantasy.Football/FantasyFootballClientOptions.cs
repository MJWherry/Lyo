using Lyo.Api.Client;

namespace Lyo.Espn.Fantasy.Football;

/// <summary>Configuration options for the ESPN fantasy football client. Inherits <see cref="ApiClientOptions" />; use <see cref="ApiClientOptions.BaseUrl" /> for the ESPN fantasy API root.</summary>
public class FantasyFootballClientOptions : ApiClientOptions
{
    /// <summary>The default configuration section name for <see cref="FantasyFootballClientOptions" />.</summary>
    public new const string SectionName = "FantasyFootballClient";

    /// <summary>Initializes defaults for the public ESPN fantasy API.</summary>
    public FantasyFootballClientOptions()
        => BaseUrl = "https://lm-api-reads.fantasy.espn.com/apis/v3/games/ffl/";

    /// <summary>Gets or sets the value of the <c>espn_s2</c> cookie for private leagues.</summary>
    public string? EspnS2 { get; set; }

    /// <summary>Gets or sets the value of the <c>SWID</c> cookie for private leagues.</summary>
    public string? Swid { get; set; }
}
