namespace Lyo.Espn.Fantasy.Football;

/// <summary>Configuration options for the ESPN fantasy football client.</summary>
public class FantasyFootballClientOptions
{
    /// <summary>The default configuration section name for <see cref="FantasyFootballClientOptions" />.</summary>
    public const string SectionName = "FantasyFootballClient";

    /// <summary>Gets or sets the ESPN fantasy API base URL.</summary>
    public string ApiBaseUrl { get; set; } = "https://lm-api-reads.fantasy.espn.com/apis/v3/games/ffl/";

    /// <summary>Gets or sets the value of the <c>espn_s2</c> cookie for private leagues.</summary>
    public string? EspnS2 { get; set; }

    /// <summary>Gets or sets the value of the <c>SWID</c> cookie for private leagues.</summary>
    public string? Swid { get; set; }

    /// <summary>Gets or sets whether HTTP non-success status codes should throw (default true).</summary>
    public bool EnsureStatusCode { get; set; } = true;
}