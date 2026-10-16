using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Client.Models.Response;

/// <summary>One Fantasy Chat topic. The conversation view returns the message page; the list view may omit messages.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ChatConversationRes(
    string Id,
    string? Author,
    long? Date,
    long? DateEdited,
    bool IsDeleted,
    bool IsEdited,
    IReadOnlyList<ChatMessageRes>? Messages,
    int TotalMessageCount,
    string? Type,
    int UnreadMessageCount,
    string? Subject = null,
    IReadOnlyList<string>? ViewableBy = null)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"Conversation Id={Id} Type={Type} Messages={Messages?.Count ?? 0} Total={TotalMessageCount}";
}
