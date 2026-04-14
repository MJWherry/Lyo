using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record RosterEntryRes(
    long? AcquisitionDate,
    string? AcquisitionType,
    string? InjuryStatus,
    int? LineupSlotId,
    IReadOnlyList<int> PendingTransactionIds,
    int? PlayerId,
    PlayerPoolEntryRes? PlayerPoolEntry,
    string? Status)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"PlayerId={PlayerId} LineupSlotId={LineupSlotId} Status={Status} InjuryStatus={InjuryStatus}";
}