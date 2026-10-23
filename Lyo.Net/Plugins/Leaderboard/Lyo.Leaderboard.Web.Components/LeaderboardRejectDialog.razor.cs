namespace Lyo.Leaderboard.Web.Components;

/// <summary>Collects a rejection reason. Chips stay in <c>TitleChips</c>.</summary>
public partial class LeaderboardRejectDialog
{
    private string _reason = "";

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    /// <summary>Entry being rejected.</summary>
    [Parameter]
    public EntryRecord? Entry { get; set; }

    private void Confirm() => MudDialog.Close(DialogResult.Ok(_reason.Trim()));
}