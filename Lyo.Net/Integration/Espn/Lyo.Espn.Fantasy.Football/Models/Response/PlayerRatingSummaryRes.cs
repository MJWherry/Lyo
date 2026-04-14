using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record PlayerRatingSummaryRes(int? PositionalRanking, int? TotalRanking, double? TotalRating)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"PositionalRanking={PositionalRanking} TotalRanking={TotalRanking} TotalRating={TotalRating}";
}