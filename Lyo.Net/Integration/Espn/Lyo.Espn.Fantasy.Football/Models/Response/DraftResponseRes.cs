using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record DraftResponseRes(int Id, int SeasonId, int ScoringPeriodId, DraftDetailRes? DraftDetail)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"Draft Id={Id} SeasonId={SeasonId} ScoringPeriodId={ScoringPeriodId} Picks={DraftDetail?.Picks.Count ?? 0}";
}