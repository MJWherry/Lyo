namespace Lyo.Story.Web.Components;

/// <summary>Plays a saved story: start, choices, advance, variable bag, event log, and causes.</summary>
public partial class StoryPlayView
{
    private bool _busy;
    private string? _causeKey;
    private IReadOnlyList<StoryPlaythroughEventRes>? _causes;
    private List<StoryPlaythroughEventRes> _events = [];
    private string? _playerRef;
    private StoryPlayStateRes? _state;

    /// <summary>Client that starts and advances play.</summary>
    [Parameter]
    [EditorRequired]
    public IStoryClient StoryClient { get; set; } = null!;

    /// <summary>Saved story id. Empty until the graph has been saved.</summary>
    [Parameter]
    public Guid StoryId { get; set; }

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    private async Task StartAsync()
    {
        _busy = true;
        try {
            _state = await StoryClient.StartAsync(new() { StoryId = StoryId, PlayerRef = _playerRef });
            await LoadEventsAsync();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task RestartAsync()
    {
        _state = null;
        _events = [];
        _causes = null;
        await StartAsync();
    }

    private async Task ChooseAsync(string? choiceKey)
    {
        if (_state is null || string.IsNullOrWhiteSpace(choiceKey))
            return;

        _busy = true;
        try {
            _state = await StoryClient.ChooseAsync(_state.Playthrough.Id, new() { ChoiceKey = choiceKey });
            await LoadEventsAsync();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task AdvanceAsync()
    {
        if (_state is null)
            return;

        _busy = true;
        try {
            _state = await StoryClient.AdvanceAsync(_state.Playthrough.Id);
            await LoadEventsAsync();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task CausesAsync(string variableKey)
    {
        if (_state is null)
            return;

        _causeKey = variableKey;
        try {
            _causes = await StoryClient.GetCausesAsync(_state.Playthrough.Id, variableKey);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private async Task LoadEventsAsync()
    {
        if (_state is null)
            return;

        _events = [.. await StoryClient.GetEventsAsync(_state.Playthrough.Id)];
        _causes = null;
    }
}