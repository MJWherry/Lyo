using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record PlayerInfoItemRes(int Id, PlayerPoolEntryRes? PlayerPoolEntry, PlayerRes? Player)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"Id={Id} Player={Player?.FullName ?? PlayerPoolEntry?.Player?.FullName}";
}