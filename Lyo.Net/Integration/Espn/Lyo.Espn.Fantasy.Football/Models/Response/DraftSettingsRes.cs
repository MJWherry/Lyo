using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record DraftSettingsRes(long? Date, int? AuctionBudget, IReadOnlyList<int> PickOrder, string? Type)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"Date={Date} AuctionBudget={AuctionBudget} PickOrderCount={PickOrder.Count} Type={Type}";
}