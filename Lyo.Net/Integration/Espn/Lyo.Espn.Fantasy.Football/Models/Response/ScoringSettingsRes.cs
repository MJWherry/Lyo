using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record ScoringSettingsRes(string? MatchupTieRule, string? PlayerRankType)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"MatchupTieRule={MatchupTieRule} PlayerRankType={PlayerRankType}";
}