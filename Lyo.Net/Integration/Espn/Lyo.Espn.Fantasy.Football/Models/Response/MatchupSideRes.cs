using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record MatchupSideRes(int? TeamId, double? TotalPoints, double? TotalProjectedPoints, double? TotalPointsLive)
{
    public JsonElement? RosterForCurrentScoringPeriod { get; init; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"TeamId={TeamId} TotalPoints={TotalPoints} Projected={TotalProjectedPoints} Live={TotalPointsLive}";
}