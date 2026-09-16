namespace Lyo.Leaderboard.Enums;

/// <summary>How people become members of a board.</summary>
public enum JoinPolicy
{
    /// <summary>Only an invite token can add a member.</summary>
    InviteOnly = 0,

    /// <summary>Anyone with the board can request to join; owner/admin approve.</summary>
    Request = 1,

    /// <summary>Anyone with <c>leaderboard.write</c> can join without an invite.</summary>
    Open = 2
}
