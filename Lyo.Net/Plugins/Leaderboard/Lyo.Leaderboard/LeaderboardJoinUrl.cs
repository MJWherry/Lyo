using Lyo.Exceptions;

namespace Lyo.Leaderboard;

/// <summary>Builds the relative join path stored on an invite create result.</summary>
public static class LeaderboardJoinUrl
{
    /// <summary>Returns <c>/join/{slug}?token={token}</c>.</summary>
    public static string BuildPath(string slug, string token)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(slug);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(token);
        return $"/join/{Uri.EscapeDataString(slug.Trim())}?token={Uri.EscapeDataString(token.Trim())}";
    }

    /// <summary>Combines a public origin with <see cref="BuildPath" />.</summary>
    public static string BuildAbsolute(string publicBaseUrl, string slug, string token)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(publicBaseUrl);
        return $"{publicBaseUrl.TrimEnd('/')}{BuildPath(slug, token)}";
    }
}
