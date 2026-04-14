using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record TransactionCounterRes(int? Acquisitions, int? AcquisitionBudgetSpent, int? Drops, int? Trades, int? MoveToIr)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"Acquisitions={Acquisitions} BudgetSpent={AcquisitionBudgetSpent} Drops={Drops} Trades={Trades} MoveToIr={MoveToIr}";
}