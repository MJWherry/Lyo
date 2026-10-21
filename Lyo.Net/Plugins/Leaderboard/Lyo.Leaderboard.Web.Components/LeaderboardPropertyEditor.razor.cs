namespace Lyo.Leaderboard.Web.Components;

/// <summary>Add or edit a form field. Choice options store a key and a number the board formula can read.</summary>
public partial class LeaderboardPropertyEditor
{
    private List<ChoiceEdit> _choices = [];

    private ScoringPropertyRecord _edit = new();
    private bool _formHasChanges;
    private bool _initialized;
    private bool _saving;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    /// <summary>Typed Leaderboard API client.</summary>
    [Parameter]
    [EditorRequired]
    public ILeaderboardApiClient Client { get; set; } = null!;

    /// <summary>Owning board.</summary>
    [Parameter]
    [EditorRequired]
    public Guid BoardId { get; set; }

    /// <summary>Existing property. Null creates a new one.</summary>
    [Parameter]
    public ScoringPropertyRecord? Property { get; set; }

    private bool IsNew => Property == null || Property.Id == Guid.Empty;

    private bool _canSave => !_saving && !string.IsNullOrWhiteSpace(_edit.Key) && !string.IsNullOrWhiteSpace(_edit.Label) && (_formHasChanges || IsNew);

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (_initialized)
            return;

        _edit = Property == null ? NewProperty() : Clone(Property);
        _choices = Property?.Choices.Select(c => new ChoiceEdit { Key = c.Key, Label = c.Label, Value = c.Value }).ToList() ?? [];
        _initialized = true;
    }

    private ScoringPropertyRecord NewProperty() => new() { BoardId = BoardId, ValueKind = ScoringValueKind.Number };

    private static ScoringPropertyRecord Clone(ScoringPropertyRecord src)
        => new() {
            Id = src.Id,
            BoardId = src.BoardId,
            Key = src.Key,
            Label = src.Label,
            Description = src.Description,
            ValueKind = src.ValueKind,
            Unit = src.Unit,
            Required = src.Required,
            SortOrder = src.SortOrder,
            MinValue = src.MinValue,
            MaxValue = src.MaxValue,
            ValidationRegex = src.ValidationRegex,
            CreatedAt = src.CreatedAt,
            UpdatedAt = src.UpdatedAt
        };

    private void OnValueKindChanged(ScoringValueKind value)
    {
        _edit.ValueKind = value;
        _formHasChanges = true;
    }

    private void AddChoice() => _choices.Add(new());

    private async Task SaveAsync()
    {
        _saving = true;
        try {
            _edit.BoardId = BoardId;
            _edit.Choices = _edit.ValueKind == ScoringValueKind.Choice ? _choices.Select(c => new ScoringChoice { Key = c.Key, Label = c.Label, Value = c.Value }).ToList() : [];
            await Client.SavePropertyAsync(BoardId, _edit);
            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _saving = false;
        }
    }

    private sealed class ChoiceEdit
    {
        public string Key { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public double Value { get; set; }
    }
}