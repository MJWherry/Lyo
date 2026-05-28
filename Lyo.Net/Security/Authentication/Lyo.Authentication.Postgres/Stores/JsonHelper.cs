using System.Text.Json;

namespace Lyo.Authentication.Postgres.Stores;

internal static class JsonHelper
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    public static string SerializeStringList(IReadOnlyList<string>? values) => values is null || values.Count == 0 ? "[]" : JsonSerializer.Serialize(values, Options);

    public static IReadOnlyList<string> DeserializeStringList(string? json)
        => string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<List<string>>(json!, Options) ?? [];

    public static string? SerializeMetadata(IReadOnlyDictionary<string, object?>? values) => values is null || values.Count == 0 ? null : JsonSerializer.Serialize(values, Options);

    public static IReadOnlyDictionary<string, object?>? DeserializeMetadata(string? json)
        => string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<Dictionary<string, object?>>(json!, Options);
}