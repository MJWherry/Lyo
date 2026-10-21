using Lyo.Api.Client;

namespace Lyo.Leaderboard.Api.Client;

/// <summary>Configuration for the Leaderboard API HTTP client. Bind from the "LeaderboardApi" section.</summary>
public sealed class LeaderboardApiClientOptions : ApiClientOptions
{
    /// <summary>Configuration section name.</summary>
    public new const string SectionName = "LeaderboardApi";
}