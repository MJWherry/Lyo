namespace Lyo.Leaderboard.Web.Components;

/// <summary>Redeems an invite token through <see cref="ILeaderboardApiClient.RedeemInviteAsync" />.</summary>
public partial class LeaderboardJoinRedeem
{
    /// <summary>Typed Leaderboard API client.</summary>
    [Parameter]
    [EditorRequired]
    public ILeaderboardApiClient Client { get; set; } = null!;

    /// <summary>Raised after a successful redeem.</summary>
    [Parameter]
    public EventCallback OnRedeemed { get; set; }

    private string _token = "";
    private bool _asGuest = true;
    private bool _busy;
    private MemberRecord? _member;

    private async Task RedeemAsync()
    {
        if (string.IsNullOrWhiteSpace(_token))
            return;

        _busy = true;
        try {
            _member = await Client.RedeemInviteAsync(_token.Trim(), _asGuest);
            Snackbar.Add($"Joined as {_member.DisplayName}.", Severity.Success);
            await OnRedeemed.InvokeAsync();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }
}
