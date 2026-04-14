using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record RosterRes(double? AppliedStatTotal, IReadOnlyList<RosterEntryRes> Entries)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"AppliedStatTotal={AppliedStatTotal} Entries={Entries.Count}";
}