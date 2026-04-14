using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record PlayerPoolEntryRes(
    int Id,
    double? AppliedStatTotal,
    int? KeeperValue,
    int? KeeperValueFuture,
    bool? LineupLocked,
    int? OnTeamId,
    PlayerRes? Player,
    IReadOnlyDictionary<string, PlayerRatingSummaryRes>? Ratings,
    bool? RosterLocked,
    string? Status,
    bool? TradeLocked)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"Id={Id} Player={Player?.FullName} OnTeamId={OnTeamId} AppliedStatTotal={AppliedStatTotal} Status={Status}";
}