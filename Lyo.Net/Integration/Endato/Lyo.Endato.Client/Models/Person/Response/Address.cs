using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Common.JsonConverters;

namespace Lyo.Endato.Client.Models.Person.Response;

/// <summary>Postal address linked to a Person Search result.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record Address(
    bool IsDeliverable,
    bool IsMergedAddress,
    bool IsPublic,
    string AddressHash,
    string HouseNumber,
    string StreetPreDirection,
    string StreetName,
    string StreetPostDirection,
    string StreetType,
    string Unit,
    string City,
    string State,
    string County,
    string Zip,
    string Zip4,
    string FullAddress,
    [property: JsonConverter(typeof(StringDecimalNullableConverter))]
    decimal? Latitude,
    [property: JsonConverter(typeof(StringDecimalNullableConverter))]
    decimal? Longitude,
    int AddressOrder,
    string PropertyIndicator,
    string BldgCode,
    string UtilityCode,
    int UnitCount,
    string FirstReportedDate,
    string LastReportedDate,
    string PublicFirstSeenDate,
    HighRiskMarker? HighRiskMarker = null,
    string? TotalFirstSeenDate = null,
    string? PublicLastSeenDate = null,
    IReadOnlyList<string>? PhoneNumbers = null,
    IReadOnlyList<string>? Neighbors = null,
    string? SourceSummary = null)
{
    public override string ToString()
        => $"Address: '{FullAddress}', Order={AddressOrder}, Deliverable={IsDeliverable}, Public={IsPublic}, City='{City}', State='{State}', Zip='{Zip}', Lat={Latitude}, Long={Longitude}, PhoneNumbers={PhoneNumbers?.Count ?? 0}";
}