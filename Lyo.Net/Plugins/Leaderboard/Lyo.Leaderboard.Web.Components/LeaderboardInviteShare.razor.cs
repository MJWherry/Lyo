namespace Lyo.Leaderboard.Web.Components;

/// <summary>Creates Link / QR / Email / User invites and shows the join URL plus QR PNG from the API.</summary>
public partial class LeaderboardInviteShare
{
    /// <summary>Typed Leaderboard API client.</summary>
    [Parameter]
    [EditorRequired]
    public ILeaderboardApiClient Client { get; set; } = null!;

    /// <summary>Board to invite people onto.</summary>
    [Parameter]
    public BoardRecord? Board { get; set; }

    /// <summary>Unused except to reset local state when the parent reloads the board.</summary>
    [Parameter]
    public int DataVersion { get; set; }

    private InviteShareKind _kind = InviteShareKind.Qr;
    private MemberRole _role = MemberRole.Contributor;
    private string? _email;
    private string _userIdText = "";
    private bool _busy;
    private InviteCreatedResponse? _created;
    private string? _qrSrc;
    private int _seenVersion = int.MinValue;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (_seenVersion == DataVersion)
            return;

        _seenVersion = DataVersion;
        _created = null;
        _qrSrc = null;
    }

    private async Task CreateAsync()
    {
        if (Board == null)
            return;

        Guid? invitedUserId = null;
        if (_kind == InviteShareKind.User) {
            if (!Guid.TryParse(_userIdText.Trim(), out var parsed)) {
                Snackbar.Add("User id must be a GUID.", Severity.Warning);
                return;
            }

            invitedUserId = parsed;
        }

        if (_kind == InviteShareKind.Email && string.IsNullOrWhiteSpace(_email)) {
            Snackbar.Add("Email is required.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            _created = await Client.CreateInviteAsync(
                    Board.Id,
                    new() {
                        ShareKind = _kind,
                        Role = _role,
                        Email = _email,
                        InvitedUserId = invitedUserId
                    });
            _qrSrc = null;
            if (_kind is InviteShareKind.Qr or InviteShareKind.Link) {
                try {
                    var png = await Client.GetInviteQrAsync(_created.Invite.Id);
                    _qrSrc = "data:image/png;base64," + Convert.ToBase64String(png);
                }
                catch (Exception qrEx) {
                    Snackbar.Add(qrEx.Message, Severity.Warning);
                }
            }

            Snackbar.Add("Invite created.", Severity.Success);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task RevokeAsync()
    {
        if (_created == null)
            return;

        try {
            await Client.RevokeInviteAsync(_created.Invite.Id);
            Snackbar.Add("Invite revoked.", Severity.Info);
            _created = null;
            _qrSrc = null;
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }
}
