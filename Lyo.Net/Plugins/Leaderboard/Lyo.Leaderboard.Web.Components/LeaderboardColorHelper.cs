namespace Lyo.Leaderboard.Web.Components;

/// <summary>MudBlazor colors for leaderboard role and status chips. Rendering goes through <see cref="LyoChip" />.</summary>
public static class LeaderboardColorHelper
{
    /// <summary>Identity hue for a board membership role. Unknown roles stay uncolored.</summary>
    public static LyoChipHue? ForRole(MemberRole role)
        => role switch {
            MemberRole.Owner => LyoChipHue.Indigo,
            MemberRole.Admin => LyoChipHue.Blue,
            MemberRole.Contributor => LyoChipHue.Teal,
            MemberRole.Viewer => LyoChipHue.Cyan,
            var _ => null
        };

    /// <summary>Color for a membership lifecycle chip.</summary>
    public static Color ForMemberStatus(MemberStatus status)
        => status switch {
            MemberStatus.Active => Color.Success,
            MemberStatus.Invited => Color.Info,
            MemberStatus.Declined => Color.Warning,
            MemberStatus.Removed => Color.Default,
            MemberStatus.Banned => Color.Error,
            var _ => Color.Default
        };

    /// <summary>Color for an entry verification chip.</summary>
    public static Color ForEntryStatus(EntryStatus status)
        => status switch {
            EntryStatus.Approved => Color.Success,
            EntryStatus.Pending => Color.Warning,
            EntryStatus.Rejected => Color.Error,
            EntryStatus.Withdrawn => Color.Default,
            EntryStatus.Draft => Color.Info,
            var _ => Color.Default
        };

    /// <summary>Color for a 1-based standing rank chip.</summary>
    public static Color ForRank(int rank)
        => rank switch {
            1 => Color.Warning,
            2 => Color.Info,
            3 => Color.Success,
            var _ => Color.Default
        };
}