namespace Lyo.Leaderboard;

/// <summary>Authenticated caller for store authorization. Board roles are looked up in the database.</summary>
public sealed class LeaderboardActor
{
    /// <summary>Creates an actor.</summary>
    public LeaderboardActor(Guid userId, string? email = null, string? displayName = null)
    {
        UserId = userId;
        Email = email;
        DisplayName = displayName;
    }

    /// <summary><c>LyoUser</c> id from <c>lyo:user</c>.</summary>
    public Guid UserId { get; }

    /// <summary>Email used to lock personal invites.</summary>
    public string? Email { get; }

    /// <summary>Display name snapshot when creating membership.</summary>
    public string? DisplayName { get; }
}
