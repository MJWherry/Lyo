using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record PlayerOwnershipRes(double? AuctionValueAverage, double? AverageDraftPosition, double? PercentChange, double? PercentOwned, double? PercentStarted)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"Owned={PercentOwned} Started={PercentStarted} Change={PercentChange} AvgDraftPos={AverageDraftPosition}";
}