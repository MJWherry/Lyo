using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record TransactionRes(
    string Id,
    string? Type,
    string? Status,
    string? ExecutionType,
    int? ScoringPeriodId,
    string? MemberId,
    int? ProposedBy,
    int? BidAmount,
    int? Rating,
    long? ProposedDate,
    long? ProcessDate,
    bool? IsPending,
    IReadOnlyList<TransactionItemRes> Items)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"Transaction Id={Id} Type={Type} Status={Status} ScoringPeriodId={ScoringPeriodId} Items={Items.Count}";
}