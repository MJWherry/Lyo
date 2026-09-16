namespace Lyo.Leaderboard.Enums;

/// <summary>Permission role on a single board. Not a JWT claim.</summary>
public enum MemberRole
{
    /// <summary>Read standings and approved entries.</summary>
    Viewer = 0,

    /// <summary>Submit entries when the board allows it.</summary>
    Contributor = 1,

    /// <summary>Invite, verify, adjust, and enter on behalf of others.</summary>
    Admin = 2,

    /// <summary>Full control including archive and settings.</summary>
    Owner = 3
}
