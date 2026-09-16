namespace Lyo.Leaderboard.Web.Components;

/// <summary>Creates or updates a board and its scoring properties.</summary>
public partial class LeaderboardBoardEditor
{
    /// <summary>Typed Leaderboard API client.</summary>
    [Parameter]
    [EditorRequired]
    public ILeaderboardApiClient Client { get; set; } = null!;

    /// <summary>Existing board to edit. Null shows the create form.</summary>
    [Parameter]
    public BoardRecord? Board { get; set; }

    /// <summary>Bump to reload properties.</summary>
    [Parameter]
    public int DataVersion { get; set; }

    /// <summary>Raised after create/update so the parent can adopt the board.</summary>
    [Parameter]
    public EventCallback<BoardRecord> OnChanged { get; set; }

    private LyoForm<CreateBoardRequest>? _form;
    private CreateBoardRequest _edit = new();
    private bool _formHasChanges;
    private bool _saving;
    private BoardRecord? _board;
    private int _loadedVersion = -1;
    private IReadOnlyList<ScoringPropertyRecord> _properties = [];

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (Board == _board && DataVersion == _loadedVersion)
            return;

        _board = Board;
        _edit = ToRequest(_board);
        _formHasChanges = _board == null;
        await LoadPropertiesAsync();
        _loadedVersion = DataVersion;
    }

    private async Task LoadPropertiesAsync()
    {
        if (_board == null) {
            _properties = [];
            return;
        }

        try {
            _properties = await Client.ListPropertiesAsync(_board.Id);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_edit.Name)) {
            Snackbar.Add("Name is required.", Severity.Warning);
            return;
        }

        _saving = true;
        try {
            var saved = _board == null
                ? await Client.CreateBoardAsync(_edit)
                : await Client.UpdateBoardAsync(_board.Id, _edit);
            Snackbar.Add(_board == null ? "Board created." : "Board saved.", Severity.Success);
            await OnChanged.InvokeAsync(saved);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _saving = false;
        }
    }

    private async Task ArchiveAsync()
    {
        if (_board == null)
            return;

        _saving = true;
        try {
            await Client.ArchiveBoardAsync(_board.Id);
            Snackbar.Add("Board archived.", Severity.Info);
            var refreshed = await Client.GetBoardAsync(_board.Id);
            if (refreshed != null)
                await OnChanged.InvokeAsync(refreshed);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _saving = false;
        }
    }

    private async Task OpenCreatePropertyAsync()
    {
        if (_board == null)
            return;

        var parameters = new DialogParameters<LeaderboardPropertyEditor> {
            { x => x.Client, Client },
            { x => x.BoardId, _board.Id },
            { x => x.Property, null }
        };
        var dialog = await DialogService.ShowAsync<LeaderboardPropertyEditor>("New property", parameters, LyoDialogPresets.Large);
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await LoadPropertiesAsync();
    }

    private async Task OpenEditPropertyAsync(ScoringPropertyRecord property)
    {
        if (_board == null)
            return;

        var parameters = new DialogParameters<LeaderboardPropertyEditor> {
            { x => x.Client, Client },
            { x => x.BoardId, _board.Id },
            { x => x.Property, property }
        };
        var dialog = await DialogService.ShowAsync<LeaderboardPropertyEditor>("Edit property", parameters, LyoDialogPresets.Large);
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await LoadPropertiesAsync();
    }

    private async Task DeletePropertyAsync(ScoringPropertyRecord property)
    {
        if (_board == null)
            return;

        try {
            await Client.DeletePropertyAsync(_board.Id, property.Id);
            Snackbar.Add($"Deleted {property.Key}.", Severity.Info);
            await LoadPropertiesAsync();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private static CreateBoardRequest ToRequest(BoardRecord? board)
        => board == null
            ? new()
            : new() {
                Name = board.Name,
                Description = board.Description,
                Slug = board.Slug,
                Visibility = board.Visibility,
                JoinPolicy = board.JoinPolicy,
                MembersCanSubmit = board.MembersCanSubmit,
                RequireVerification = board.RequireVerification,
                MembersCanEditOwn = board.MembersCanEditOwn,
                MembersCanDeleteOwn = board.MembersCanDeleteOwn,
                AllowMultipleEntries = board.AllowMultipleEntries,
                AggregationKind = board.AggregationKind,
                AggregationN = board.AggregationN,
                SortDirection = board.SortDirection,
                TieBreak = board.TieBreak,
                TieBreakPropertyKey = board.TieBreakPropertyKey,
                ScoreFormula = board.ScoreFormula,
                StartsAt = board.StartsAt,
                EndsAt = board.EndsAt
            };
}
