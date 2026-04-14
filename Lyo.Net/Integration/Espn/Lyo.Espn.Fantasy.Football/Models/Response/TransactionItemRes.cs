using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record TransactionItemRes(string? Type, int? FromTeamId, int? ToTeamId, int? PlayerId, int? Amount, int? KeeperValue, PlayerRes? Player)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"Type={Type} FromTeamId={FromTeamId} ToTeamId={ToTeamId} PlayerId={PlayerId} Amount={Amount}";
}