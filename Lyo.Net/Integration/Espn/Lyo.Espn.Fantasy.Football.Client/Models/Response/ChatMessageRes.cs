using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Client.Models.Response;

/// <summary>One message inside a Fantasy Chat conversation. Text lives in <see cref="Content" />; adds, drops, and trades live in <see cref="Attributes" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ChatMessageRes(
    string Id,
    string? Author,
    long Date,
    bool IsDeleted,
    int MessageTypeId,
    string? TopicId,
    string? Content = null,
    IReadOnlyDictionary<string, string>? Attributes = null)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"Message Id={Id} Type={MessageTypeId} Author={Author} Date={Date}";
}
