namespace Lyo.Leaderboard.Enums;

/// <summary>Lifecycle of a board membership row.</summary>
public enum MemberStatus
{
    /// <summary>Invite issued; user has not redeemed.</summary>
    Invited = 0,

    /// <summary>Participating member.</summary>
    Active = 1,

    /// <summary>Invite declined.</summary>
    Declined = 2,

    /// <summary>Removed by an owner/admin.</summary>
    Removed = 3,

    /// <summary>Blocked from rejoining.</summary>
    Banned = 4
}
