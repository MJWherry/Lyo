using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.HomeInventory.Models;

/// <summary>
/// One identifier on an item: kind (SKU, Wi-Fi MAC, …), value, and an optional note for that pair (which radio, which kit part). The same kind may appear many
/// times. Stored as a jsonb array on the item row.
/// </summary>
public sealed class HomeProductKeyEntry
{
    /// <summary>Kind string; prefer a <see cref="HomeProductKeys"/> constant, or a free-text custom kind.</summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    /// <summary>Identifier value.</summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    /// <summary>Optional note for this pair (which part or radio this belongs to).</summary>
    [JsonPropertyName("note")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Note { get; set; }
}

/// <summary>Serialize <see cref="HomeProductKeyEntry"/> lists to the item <c>product_keys</c> jsonb column.</summary>
public static class HomeProductKeyJson
{
    /// <summary>CamelCase kind/value/note; omit null notes.</summary>
    public static readonly JsonSerializerOptions Options = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>Empty array when <paramref name="entries"/> is null.</summary>
    public static string Serialize(IReadOnlyList<HomeProductKeyEntry>? entries)
        => JsonSerializer.Serialize(entries ?? [], Options);

    /// <summary>Empty list when <paramref name="json"/> is missing or invalid.</summary>
    public static List<HomeProductKeyEntry> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try {
            return JsonSerializer.Deserialize<List<HomeProductKeyEntry>>(json, Options) ?? [];
        }
        catch (JsonException) {
            return [];
        }
    }
}
