using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record TeamRes(
    int Id,
    string? Abbrev,
    string? Location,
    string? Nickname,
    string? Name,
    string? Logo,
    string? PrimaryOwner,
    int? DivisionId,
    int? PlayoffSeed,
    int? CurrentProjectedRank,
    int? RankCalculatedFinal,
    double? Points,
    double? PointsAdjusted,
    int? WaiverRank,
    TeamRecordRes? Record,
    RosterRes? Roster,
    TransactionCounterRes? TransactionCounter)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"Team Id={Id} Name={Name ?? $"{Location} {Nickname}".Trim()} Points={Points} RosterEntries={Roster?.Entries.Count ?? 0}";
}