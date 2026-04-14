using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record PlayerRes(
    int Id,
    int? ProTeamId,
    int? UniverseId,
    int? DefaultPositionId,
    bool? Active,
    bool? Droppable,
    bool? Injured,
    string? InjuryStatus,
    IReadOnlyList<int> EligibleSlots,
    string? FirstName,
    string? LastName,
    string? FullName,
    long? LastNewsDate,
    string? SeasonOutlook,
    IReadOnlyDictionary<string, PlayerDraftRankRes>? DraftRanksByRankType,
    PlayerOutlooksRes? Outlooks,
    PlayerOwnershipRes? Ownership,
    IReadOnlyDictionary<string, IReadOnlyList<PlayerWeekRankingRes>>? Rankings,
    IReadOnlyList<PlayerStatRes> Stats)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"Player Id={Id} Name={FullName} ProTeamId={ProTeamId} DefaultPositionId={DefaultPositionId} EligibleSlots={EligibleSlots.Count}";
}