using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record LeagueStatusRes(int? CurrentMatchupPeriod, int? LatestScoringPeriod, int? FinalScoringPeriod, bool? IsActive)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString()
        => $"CurrentMatchupPeriod={CurrentMatchupPeriod} LatestScoringPeriod={LatestScoringPeriod} FinalScoringPeriod={FinalScoringPeriod} IsActive={IsActive}";
}