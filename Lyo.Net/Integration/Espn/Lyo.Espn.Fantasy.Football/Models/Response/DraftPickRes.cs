using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record DraftPickRes(
    int? OverallPickNumber,
    int? RoundId,
    int? RoundPickNumber,
    int? TeamId,
    string? MemberId,
    int? PlayerId,
    int? BidAmount,
    bool? Keeper,
    int? NominationOrder)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"OverallPick={OverallPickNumber} Round={RoundId} RoundPick={RoundPickNumber} TeamId={TeamId} PlayerId={PlayerId}";
}