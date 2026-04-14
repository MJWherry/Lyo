using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record TeamRecordRes(RecordOutcomeRes? Overall, RecordOutcomeRes? Division, RecordOutcomeRes? Home, RecordOutcomeRes? Away)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"Overall={Overall} Division={Division} Home={Home} Away={Away}";
}