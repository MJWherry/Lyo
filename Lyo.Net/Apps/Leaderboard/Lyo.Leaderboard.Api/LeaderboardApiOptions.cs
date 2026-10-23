using System.Diagnostics;

namespace Lyo.Leaderboard.Api;

/// <summary>Hosting options for join URLs and optional invite email.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class LeaderboardApiOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "LeaderboardApi";

    /// <summary>Public origin used to build absolute join URLs (e.g. <c>https://boards.example.com</c>).</summary>
    public string PublicBaseUrl { get; set; } = "http://localhost:5090";

    /// <summary>When true and Email is registered, email/user invites send a join message.</summary>
    public bool SendInviteEmail { get; set; } = true;

    /// <summary>Public origin used for join URLs.</summary>
    public override string ToString() => $"PublicBaseUrl={PublicBaseUrl} SendInviteEmail={SendInviteEmail}";
}