using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record DraftDetailRes(bool? Drafted, bool? InProgress, bool? Complete, IReadOnlyList<DraftPickRes> Picks)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"Drafted={Drafted} InProgress={InProgress} Complete={Complete} Picks={Picks.Count}";
}