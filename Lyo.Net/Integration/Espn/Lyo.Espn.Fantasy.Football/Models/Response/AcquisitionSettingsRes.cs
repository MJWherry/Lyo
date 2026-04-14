using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record AcquisitionSettingsRes(int? AcquisitionBudget, int? AcquisitionLimit, int? WaiverHours)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"AcquisitionBudget={AcquisitionBudget} AcquisitionLimit={AcquisitionLimit} WaiverHours={WaiverHours}";
}