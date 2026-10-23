using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.OpenSubtitles.Client;

/// <summary>JSON options for the OpenSubtitles consumer API (snake_case properties, no enum name rewriting).</summary>
public static class OpenSubtitlesJson
{
    /// <summary>Builds serializer options for OpenSubtitles request and response bodies.</summary>
    public static JsonSerializerOptions Create()
        => new() {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };
}