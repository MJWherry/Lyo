using System.Diagnostics;
using Lyo.Http.Client;

namespace Lyo.Google.Geolocation.Client;

/// <summary>Settings that control <see cref="GoogleMapsClient" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class GoogleMapsClientOptions : LyoHttpClientOptions
{
    public new const string SectionName = "GoogleMapsClient";

    /// <summary>Required Google Maps API key.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Default language for geocode and directions (e.g. en).</summary>
    public string? DefaultLanguage { get; set; }

    /// <summary>Default geocode region bias (e.g. us).</summary>
    public string? DefaultRegion { get; set; }

    /// <summary>Language and region, plus the shared HTTP options. The API key value is omitted.</summary>
    public override string ToString() => $"ApiKeySet={!string.IsNullOrWhiteSpace(ApiKey)} DefaultLanguage={DefaultLanguage} DefaultRegion={DefaultRegion} {base.ToString()}";
}