using Microsoft.AspNetCore.Components.Web;

namespace Lyo.Leaderboard.Web.Components;

/// <summary>Tabbed leaderboard dashboard. Hosts inject <see cref="ILeaderboardApiClient" /> and pass it in.</summary>
public partial class LeaderboardManagement
{
    private BoardRecord? _board;
    private int _dataVersion;
    private string? _error;

    private string _idDraft = "";
    private bool _initialized;
    private string _slugDraft = "";
    private int _tabIndex;

    /// <summary>Typed Leaderboard API client. Required. Hosts inject the client and pass it in — this package has no <c>AddXxx</c> DI.</summary>
    [Parameter]
    [EditorRequired]
    public ILeaderboardApiClient Client { get; set; } = null!;

    /// <summary>Optional board id to load on first render.</summary>
    [Parameter]
    public Guid? InitialBoardId { get; set; }

    /// <summary>Optional board slug when the id is not known yet.</summary>
    [Parameter]
    public string? InitialSlug { get; set; }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (_initialized)
            return;

        _idDraft = InitialBoardId is { } id && id != Guid.Empty ? id.ToString("D") : "";
        _slugDraft = InitialSlug?.Trim() ?? "";
        _initialized = true;
        if ((InitialBoardId is { } loadId && loadId != Guid.Empty) || !string.IsNullOrWhiteSpace(InitialSlug))
            await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _error = null;
        try {
            BoardRecord? board = null;
            if (Guid.TryParse(_idDraft.Trim(), out var id) && id != Guid.Empty)
                board = await Client.GetBoardAsync(id);
            else if (!string.IsNullOrWhiteSpace(_slugDraft))
                board = await Client.GetBoardBySlugAsync(_slugDraft.Trim());

            if (board == null) {
                Snackbar.Add("Board not found.", Severity.Warning);
                return;
            }

            ApplyBoard(board);
        }
        catch (Exception ex) {
            _error = ex.Message;
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private async Task RefreshAsync()
    {
        if (_board == null)
            return;

        try {
            var board = await Client.GetBoardAsync(_board.Id);
            if (board != null)
                ApplyBoard(board);
            else
                _dataVersion++;
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private Task HandleBoardChangedAsync(BoardRecord board)
    {
        ApplyBoard(board);
        return Task.CompletedTask;
    }

    private void ApplyBoard(BoardRecord board)
    {
        _board = board;
        _idDraft = board.Id.ToString("D");
        _slugDraft = board.Slug;
        _dataVersion++;
    }

    private async Task OnToolbarKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            await LoadAsync();
    }
}