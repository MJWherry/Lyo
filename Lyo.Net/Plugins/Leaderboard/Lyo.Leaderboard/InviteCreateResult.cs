namespace Lyo.Leaderboard;

/// <summary>Returned once when an invite is created. The plaintext token is not stored.</summary>
public sealed class InviteCreateResult
{
    /// <summary>Persisted invite row.</summary>
    public InviteRecord Invite { get; set; } = new();

    /// <summary>Plaintext token. Show or send it now; it cannot be recovered later.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Relative join path, e.g. <c>/join/{slug}?token=...</c>.</summary>
    public string JoinPath { get; set; } = string.Empty;

    /// <summary>Pending or existing member row tied to this invite, when one was created.</summary>
    public MemberRecord? Member { get; set; }
}
