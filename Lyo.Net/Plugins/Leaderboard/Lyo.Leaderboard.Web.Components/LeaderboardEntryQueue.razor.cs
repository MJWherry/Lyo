namespace Lyo.Leaderboard.Web.Components;

/// <summary>Pending entries plus approve/reject and a manual adjustment form.</summary>
public partial class LeaderboardEntryQueue
{
    /// <summary>Typed Leaderboard API client.</summary>
    [Parameter]
    [EditorRequired]
    public ILeaderboardApiClient Client { get; set; } = null!;

    /// <summary>Board whose entries are listed.</summary>
    [Parameter]
    public Guid? BoardId { get; set; }

    /// <summary>Bump to reload.</summary>
    [Parameter]
    public int DataVersion { get; set; }

    /// <summary>Raised after verify or adjustment.</summary>
    [Parameter]
    public EventCallback OnChanged { get; set; }

    private Guid? _boardId;
    private int _loadedVersion = -1;
    private bool _loading;
    private bool _adjusting;
    private IReadOnlyList<EntryRecord> _rows = [];
    private IReadOnlyList<MemberRecord> _members = [];
    private Guid _adjustMemberId;
    private decimal _adjustDelta = 40;
    private string _adjustReason = "Judge bonus";

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (BoardId == _boardId && DataVersion == _loadedVersion)
            return;

        _boardId = BoardId;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_boardId is not { } id || id == Guid.Empty) {
            _rows = [];
            _members = [];
            _loadedVersion = DataVersion;
            return;
        }

        _loading = true;
        try {
            _members = await Client.ListMembersAsync(id);
            _rows = await Client.ListEntriesAsync(id);
            if (_adjustMemberId == Guid.Empty && _members.Count > 0)
                _adjustMemberId = _members[0].Id;

            _loadedVersion = DataVersion;
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _loading = false;
        }
    }

    private string MemberName(Guid memberId)
        => _members.FirstOrDefault(m => m.Id == memberId)?.DisplayName ?? memberId.ToString("N")[..8];

    private async Task SetStatusAsync(EntryRecord entry, EntryStatus status, string? reason = null)
    {
        try {
            await Client.SetEntryStatusAsync(entry.Id, new() { Status = status, RejectionReason = reason });
            Snackbar.Add($"{status} · {entry.ComputedScore} pts.", Severity.Success);
            await OnChanged.InvokeAsync();
            await LoadAsync();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private async Task RejectAsync(EntryRecord entry)
    {
        var parameters = new DialogParameters<LeaderboardRejectDialog> { { x => x.Entry, entry } };
        var dialog = await DialogService.ShowAsync<LeaderboardRejectDialog>("Reject entry", parameters, LyoDialogPresets.Small);
        var result = await dialog.Result;
        if (result == null || result.Canceled || result.Data is not string reason)
            return;

        await SetStatusAsync(entry, EntryStatus.Rejected, reason);
    }

    private async Task AdjustAsync()
    {
        if (_boardId is not { } id || id == Guid.Empty || _adjustMemberId == Guid.Empty)
            return;

        if (string.IsNullOrWhiteSpace(_adjustReason)) {
            Snackbar.Add("Reason is required.", Severity.Warning);
            return;
        }

        _adjusting = true;
        try {
            await Client.AddAdjustmentAsync(id, new() { MemberId = _adjustMemberId, Delta = _adjustDelta, Reason = _adjustReason.Trim() });
            Snackbar.Add("Adjustment applied.", Severity.Success);
            await OnChanged.InvokeAsync();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _adjusting = false;
        }
    }
}
