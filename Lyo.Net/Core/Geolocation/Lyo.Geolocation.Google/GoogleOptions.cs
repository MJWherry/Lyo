using System.Diagnostics;

namespace Lyo.Geolocation.Google;

/// <summary>Configuration options for Google Geolocation service.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class GoogleOptions
{
    /// <summary>The default configuration section name for GoogleOptions.</summary>
    public const string SectionName = "GoogleOptions";

    /// <summary>Gets or sets the Google Maps API key (required).</summary>
    public string ApiKey { get; set; } = null!;

    /// <summary>Gets or sets the base URL for Google Maps API. Defaults to "https://maps.googleapis.com/maps/api".</summary>
    public string BaseUrl { get; set; } = "https://maps.googleapis.com/maps/api";

    /// <summary>Gets or sets the default language code for API responses (e.g., "en", "es", "fr").</summary>
    public string? DefaultLanguage { get; set; }

    /// <summary>Gets or sets the default region code (e.g., "us", "gb") to bias results.</summary>
    public string? DefaultRegion { get; set; }

    /// <summary>Gets or sets the timeout for API requests in seconds. Defaults to 30 seconds.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    public override string ToString() => $"ApiKey=***, BaseUrl={BaseUrl}";
}