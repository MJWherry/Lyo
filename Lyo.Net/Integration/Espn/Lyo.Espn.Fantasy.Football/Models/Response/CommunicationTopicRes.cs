using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record CommunicationTopicRes(
    string Id,
    string? TopicType,
    int? MessageTypeId,
    string? MessageType,
    string? CreatedBy,
    long? Date,
    IReadOnlyList<MessageRes> Messages,
    IReadOnlyList<TransactionRes> Transactions)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"Topic Id={Id} Type={TopicType} MessageTypeId={MessageTypeId} Messages={Messages.Count} Transactions={Transactions.Count}";
}