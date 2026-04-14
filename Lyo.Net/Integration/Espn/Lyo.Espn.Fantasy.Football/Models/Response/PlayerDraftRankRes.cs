using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record PlayerDraftRankRes(int? AuctionValue, bool? Published, int? Rank, int? RankSourceId, string? RankType, int? SlotId)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"Rank={Rank} Type={RankType} Source={RankSourceId} SlotId={SlotId} AuctionValue={AuctionValue}";
}