using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Common.JsonConverters;

namespace Lyo.Endato.Client.Models.Enrichment.Response;

/// <summary>Phone number returned by Contact Enrichment.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record Phone(
    string FirstReportedDate,
    string LastReportedDate,
    string Type,
    bool IsConnected,
    string Number,
    [property: JsonConverter(typeof(StringDecimalNullableConverter))] decimal? Latitude = null,
    [property: JsonConverter(typeof(StringDecimalNullableConverter))] decimal? Longitude = null,
    string? SourceSummary = null)
{
    public override string ToString()
        => $"Phone: '{Number}', Type='{Type}', Connected={IsConnected}, Reported {FirstReportedDate}–{LastReportedDate}, Lat={Latitude}, Long={Longitude}";
}
