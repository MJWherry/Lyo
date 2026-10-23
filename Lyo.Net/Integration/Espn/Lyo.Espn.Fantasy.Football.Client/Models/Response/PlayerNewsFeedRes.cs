using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Client.Models.Response;

/// <summary>Fantasy player news feed from <c>site.api.espn.com</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record PlayerNewsFeedRes(int ResultsCount, IReadOnlyList<PlayerNewsItemRes>? Feed)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"ResultsCount={ResultsCount} Feed={Feed?.Count ?? 0}";
}

/// <summary>One item in the fantasy player news feed.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record PlayerNewsItemRes(int? PlayerId, string? Headline, string? Published, string? Description)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"PlayerId={PlayerId} Headline={Headline}";
}