using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Trakt.Client;

/// <summary>JSON options for Trakt (snake_case properties). Status strings such as <c>returning series</c> stay strings.</summary>
public static class TraktJson
{
    /// <summary>Builds serializer options for Trakt request and response bodies.</summary>
    public static JsonSerializerOptions Create()
        => new() {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };
}