using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Client.Models.Response;

/// <summary>Topic list returned by Fantasy Chat <c>view=chat_conversations</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ChatConversationsRes(IReadOnlyList<ChatConversationRes>? Topics)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"Topics={Topics?.Count ?? 0}";
}