using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Common.JsonConverters;

namespace Lyo.Endato.Client.Models.Person.Response;

/// <summary>Phone number linked to a Person Search result.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record Phone(
    string PhoneNumber,
    string Company,
    string Location,
    string PhoneType,
    bool IsConnected,
    bool IsPublic,
    [property: JsonConverter(typeof(StringDecimalNullableConverter))] decimal? Latitude,
    [property: JsonConverter(typeof(StringDecimalNullableConverter))] decimal? Longitude,
    int PhoneOrder,
    string FirstReportedDate,
    string LastReportedDate,
    string PublicFirstSeenDate,
    string? SourceSummary = null)
{
    public override string ToString()
        => $"Phone: '{PhoneNumber}', Type='{PhoneType}', Order={PhoneOrder}, Connected={IsConnected}, Public={IsPublic}, Location='{Location}', Lat={Latitude}, Long={Longitude}";
}
