using Lyo.Api.Client;

namespace Lyo.Google.Geolocation.Client;

/// <summary>Configuration options for <see cref="GoogleMapsClient" />.</summary>
public sealed class GoogleMapsClientOptions : ApiClientOptions
{
    public new const string SectionName = "GoogleMapsClient";

    /// <summary>Google Maps API key (required).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Default language for geocoding and directions (e.g. en).</summary>
    public string? DefaultLanguage { get; set; }

    /// <summary>Default region bias for geocoding (e.g. us).</summary>
    public string? DefaultRegion { get; set; }
}
