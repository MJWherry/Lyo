namespace Lyo.Leaderboard.Enums;

/// <summary>Who can discover a board.</summary>
public enum BoardVisibility
{
    /// <summary>Only members see the board.</summary>
    Private = 0,

    /// <summary>Anyone with the slug or join link can see standings; not listed publicly.</summary>
    Unlisted = 1,

    /// <summary>Listed and readable by any authenticated user with <c>leaderboard.read</c>.</summary>
    Public = 2
}