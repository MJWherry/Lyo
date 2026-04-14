using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record LeagueSettingsRes(
    string? Name,
    int? Size,
    DraftSettingsRes? DraftSettings,
    ScoringSettingsRes? ScoringSettings,
    ScheduleSettingsRes? ScheduleSettings,
    AcquisitionSettingsRes? AcquisitionSettings)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"Name={Name ?? "(none)"} Size={Size} DraftSettings={DraftSettings != null} ScoringSettings={ScoringSettings != null}";
}