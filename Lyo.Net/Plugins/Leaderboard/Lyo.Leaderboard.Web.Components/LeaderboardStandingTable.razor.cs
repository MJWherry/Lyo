namespace Lyo.Leaderboard.Web.Components;

/// <summary>Ranked standings table for one board.</summary>
public partial class LeaderboardStandingTable
{
    private Guid? _boardId;
    private int _loadedVersion = -1;
    private bool _loading;
    private IReadOnlyList<StandingRecord> _rows = [];

    /// <summary>Typed Leaderboard API client.</summary>
    [Parameter]
    [EditorRequired]
    public ILeaderboardApiClient Client { get; set; } = null!;

    /// <summary>Board to load standings for.</summary>
    [Parameter]
    public Guid? BoardId { get; set; }

    /// <summary>Bump to reload.</summary>
    [Parameter]
    public int DataVersion { get; set; }

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
            _loadedVersion = DataVersion;
            return;
        }

        _loading = true;
        try {
            _rows = await Client.GetStandingsAsync(id);
            _loadedVersion = DataVersion;
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _loading = false;
        }
    }

    private async Task RecalculateAsync()
    {
        if (_boardId is not { } id || id == Guid.Empty)
            return;

        _loading = true;
        try {
            await Client.RecalculateStandingsAsync(id);
            await LoadAsync();
            Snackbar.Add("Standings recalculated.", Severity.Success);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
            _loading = false;
        }
    }
}