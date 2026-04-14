using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record MessageRes(string Id, string? AuthorId, string? From, string? To, string? Message, bool? Deleted, long? Date)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"Message Id={Id} AuthorId={AuthorId} Date={Date} Deleted={Deleted}";
}