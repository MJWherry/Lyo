using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Leaderboard;

/// <summary>JSON settings shared with <c>Lyo.Algorithms</c> catalog payloads.</summary>
public static class LeaderboardAlgorithmJson
{
    /// <summary>Camel case, case-insensitive, string enums.</summary>
    public static JsonSerializerOptions Options { get; } = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
