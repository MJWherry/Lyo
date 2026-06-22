using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Common.JsonConverters;

namespace Lyo.Endato.Client.Models.Enrichment.Response;

/// <summary>Postal address returned by Contact Enrichment.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record Address(
    string FirstReportedDate,
    string LastReportedDate,
    string Street,
    string? Unit,
    string City,
    string State,
    string Zip,
    [property: JsonConverter(typeof(StringDecimalNullableConverter))] decimal? Latitude = null,
    [property: JsonConverter(typeof(StringDecimalNullableConverter))] decimal? Longitude = null,
    string? SourceSummary = null)
{
    public override string ToString()
        => $"Address: '{Street}' Unit='{Unit}', '{City}', {State} {Zip}, Reported {FirstReportedDate}–{LastReportedDate}, Lat={Latitude}, Long={Longitude}";
}
