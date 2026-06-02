using System.Diagnostics;
using Lyo.Common.Enums;
using Lyo.Geolocation.Models.Coordinates;

namespace Lyo.Geolocation.Models;

[DebuggerDisplay("{ToString(),nq}")]
public class IpGeolocationResult
{
    public string IpAddress { get; set; } = string.Empty;

    public GeoCoordinate Coordinate { get; set; } = null!;

    public string? City { get; set; }

    public string? Region { get; set; }

    public string? RegionCode { get; set; }

    public string? Country { get; set; }

    public string? CountryCodeString { get; set; }

    public CountryCode? CountryCode { get; set; }

    public string? Continent { get; set; }

    public string? ContinentCode { get; set; }

    public string? PostalCode { get; set; }

    public string? TimeZone { get; set; }

    public string? IspName { get; set; }

    public string? OrganizationName { get; set; }

    public bool IsProxy { get; set; }

    public bool IsVpn { get; set; }

    public double? AccuracyRadius { get; set; }

    public override string ToString()
        => $"IpGeolocationResult: {IpAddress}, {City}, {Country ?? CountryCodeString}, {Coordinate}";
}
