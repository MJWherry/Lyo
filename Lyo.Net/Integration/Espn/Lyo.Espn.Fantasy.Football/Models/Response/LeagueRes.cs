using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record LeagueRes(
    int Id,
    int SeasonId,
    int ScoringPeriodId,
    LeagueStatusRes? Status = null,
    LeagueSettingsRes? Settings = null,
    IReadOnlyList<MemberRes>? Members = null,
    IReadOnlyList<TeamRes>? Teams = null)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"Id={Id} SeasonId={SeasonId} ScoringPeriodId={ScoringPeriodId}";
}