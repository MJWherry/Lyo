using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record ScheduleSettingsRes(int? MatchupPeriodCount, int? PlayoffTeamCount, IReadOnlyDictionary<string, IReadOnlyList<int>>? MatchupPeriods)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"MatchupPeriodCount={MatchupPeriodCount} PlayoffTeamCount={PlayoffTeamCount} MatchupPeriods={MatchupPeriods?.Count ?? 0}";
}