using Lyo.Leaderboard;

namespace Lyo.Leaderboard.Api.Models;

/// <summary>Invite create response. <see cref="Token" /> is returned once.</summary>
public sealed class InviteCreatedResponse
{
    /// <summary>Persisted invite.</summary>
    public InviteRecord Invite { get; set; } = new();

    /// <summary>Plaintext token.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Relative join path.</summary>
    public string JoinPath { get; set; } = string.Empty;

    /// <summary>Absolute join URL when the host has a public base URL.</summary>
    public string? JoinUrl { get; set; }

    /// <summary>Short URL when shortening succeeded.</summary>
    public string? ShortUrl { get; set; }

    /// <summary>Pending member, when one was created.</summary>
    public MemberRecord? Member { get; set; }
}
