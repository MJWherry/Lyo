using System.Text.Json;
using Lyo.Json;

namespace Lyo.Leaderboard;

/// <summary>JSON defaults for scoring config and breakdown payloads.</summary>
public static class LeaderboardJson
{
    /// <summary>CamelCase, string enums, ignore nulls.</summary>
    public static JsonSerializerOptions Options { get; } = LyoJsonSerializerOptions.Create();

    /// <summary>Serializes <paramref name="value" /> or returns null when the value is null.</summary>
    public static string? Serialize<T>(T? value) => value == null ? null : JsonSerializer.Serialize(value, Options);

    /// <summary>Deserializes JSON or returns <paramref name="fallback" /> when empty.</summary>
    public static T Deserialize<T>(string? json, T fallback) => string.IsNullOrWhiteSpace(json) ? fallback : JsonSerializer.Deserialize<T>(json!, Options) ?? fallback;
}