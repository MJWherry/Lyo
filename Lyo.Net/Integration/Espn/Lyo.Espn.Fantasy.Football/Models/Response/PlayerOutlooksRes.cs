using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record PlayerOutlooksRes(IReadOnlyDictionary<string, string>? OutlooksByWeek)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"OutlooksByWeek={OutlooksByWeek?.Count ?? 0}";
}