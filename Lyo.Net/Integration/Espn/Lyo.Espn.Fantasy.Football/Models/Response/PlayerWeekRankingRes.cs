using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record PlayerWeekRankingRes(int? AuctionValue, double? AverageRank, bool? Published, int? Rank, int? RankSourceId, string? RankType, int? SlotId)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"Rank={Rank} AvgRank={AverageRank} Type={RankType} Source={RankSourceId} SlotId={SlotId}";
}