using Lyo.Common.Enums;
using Lyo.Geolocation.Models.Coordinates;

namespace Lyo.Geolocation.Models;

public class IpGeolocationResult
{
    public string IpAddress { get; set; }

    public GeoCoordinate Coordinate { get; set; }

    public string City { get; set; }

    public string Region { get; set; }

    public string RegionCode { get; set; }

    public string Country { get; set; }

    public string CountryCodeString { get; set; } // Raw country code string from API (e.g., "US", "GB")

    public CountryCode? CountryCode { get; set; } // Typed country code enum

    public string Continent { get; set; }

    public string ContinentCode { get; set; }

    public string PostalCode { get; set; }

    public string TimeZone { get; set; }

    public string IspName { get; set; }

    public string OrganizationName { get; set; }

    public bool IsProxy { get; set; }

    public bool IsVpn { get; set; }

    public double? AccuracyRadius { get; set; } // km
}