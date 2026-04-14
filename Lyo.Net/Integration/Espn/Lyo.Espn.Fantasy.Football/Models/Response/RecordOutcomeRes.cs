using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record RecordOutcomeRes(int Wins, int Losses, int Ties, double Percentage, double PointsFor, double PointsAgainst, int StreakLength, string StreakType)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"W={Wins} L={Losses} T={Ties} Pct={Percentage} PF={PointsFor} PA={PointsAgainst}";
}