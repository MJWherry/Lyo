using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record PlayerStatRes(
    string Id,
    string? ExternalId,
    int? ProTeamId,
    int? ScoringPeriodId,
    int? SeasonId,
    int? StatSourceId,
    int? StatSplitTypeId,
    double? AppliedAverage,
    double? AppliedTotal,
    IReadOnlyDictionary<string, double>? AppliedStats,
    IReadOnlyDictionary<string, double>? Stats)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"SeasonId={SeasonId} ScoringPeriodId={ScoringPeriodId} StatSourceId={StatSourceId} AppliedTotal={AppliedTotal}";
}