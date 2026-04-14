using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record MatchupRes(int Id, int? MatchupPeriodId, int? PlayoffTierType, string? Winner, MatchupSideRes? Home, MatchupSideRes? Away)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"Matchup Id={Id} MatchupPeriodId={MatchupPeriodId} Winner={Winner} Home={Home?.TeamId} Away={Away?.TeamId}";
}