namespace Lyo.Leaderboard.Web.Components;

/// <summary>Dynamic entry form built from <see cref="ILeaderboardApiClient.ListPropertiesAsync" />.</summary>
public partial class LeaderboardEntryForm
{
    private bool _asGuest;
    private bool _busy;
    private List<FieldState> _fields = [];

    private Guid _loadedBoardId;
    private int _loadedVersion = -1;
    private Guid _memberId;
    private IReadOnlyList<MemberRecord> _members = [];
    private string? _notes;
    private DateTime? _occurredDate = DateTime.UtcNow.Date;
    private string? _title;

    /// <summary>Typed Leaderboard API client.</summary>
    [Parameter]
    [EditorRequired]
    public ILeaderboardApiClient Client { get; set; } = null!;

    /// <summary>Board whose properties drive the form.</summary>
    [Parameter]
    public BoardRecord? Board { get; set; }

    /// <summary>Bump to reload properties and members.</summary>
    [Parameter]
    public int DataVersion { get; set; }

    /// <summary>Raised after a successful submit.</summary>
    [Parameter]
    public EventCallback OnChanged { get; set; }

    private static string ChoiceLabel(ScoringPropertyRecord property, ScoringChoice choice) => $"{choice.Label} ({choice.Value})";

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (Board == null || (Board.Id == _loadedBoardId && DataVersion == _loadedVersion))
            return;

        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        if (Board == null)
            return;

        try {
            var properties = await Client.ListPropertiesAsync(Board.Id);
            _members = await Client.ListMembersAsync(Board.Id);
            _fields = properties.Select(p => new FieldState { Property = p }).ToList();
            if (_memberId == Guid.Empty && _members.Count > 0)
                _memberId = _members[0].Id;

            _loadedBoardId = Board.Id;
            _loadedVersion = DataVersion;
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private void OnMemberChanged(Guid id) => _memberId = id;

    private void OnOccurredChanged(DateTime? date) => _occurredDate = date;

    private async Task SubmitAsync()
    {
        if (Board == null)
            return;

        if (_memberId == Guid.Empty) {
            Snackbar.Add("Pick a member.", Severity.Warning);
            return;
        }

        var values = new List<EntryValueRecord>();
        foreach (var field in _fields) {
            if (field.Property.Required && !field.HasValue()) {
                Snackbar.Add($"{field.Property.Label} is required.", Severity.Warning);
                return;
            }

            if (!field.HasValue())
                continue;

            values.Add(field.ToValue());
        }

        _busy = true;
        try {
            var occurred = _occurredDate.HasValue ? DateTime.SpecifyKind(_occurredDate.Value.Date, DateTimeKind.Utc) : DateTime.UtcNow;
            var saved = await Client.CreateEntryAsync(
                Board.Id, new() {
                    MemberId = _memberId,
                    Title = _title,
                    Notes = _notes,
                    OccurredAt = occurred,
                    Values = values
                }, _asGuest);

            Snackbar.Add($"Entry {saved.Status.ToString().ToLowerInvariant()} · {saved.ComputedScore} pts.", Severity.Success);
            await OnChanged.InvokeAsync();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private sealed class FieldState
    {
        public ScoringPropertyRecord Property { get; set; } = new();

        public string? ChoiceKey { get; set; }

        public decimal? Number { get; set; }

        public string? Text { get; set; }

        public bool Bool { get; set; }

        public DateTime? Date { get; set; }

        public bool HasValue()
            => Property.ValueKind switch {
                ScoringValueKind.Choice => !string.IsNullOrWhiteSpace(ChoiceKey),
                ScoringValueKind.Number or ScoringValueKind.Integer or ScoringValueKind.Duration => Number != null,
                ScoringValueKind.Boolean => true,
                ScoringValueKind.DateTime => Date != null,
                var _ => !string.IsNullOrWhiteSpace(Text)
            };

        public EntryValueRecord ToValue()
            => new() {
                PropertyId = Property.Id,
                PropertyKey = Property.Key,
                ChoiceKey = Property.ValueKind == ScoringValueKind.Choice ? ChoiceKey : null,
                NumberValue = Property.ValueKind is ScoringValueKind.Number or ScoringValueKind.Integer or ScoringValueKind.Duration ? Number : null,
                TextValue = Property.ValueKind == ScoringValueKind.Text ? Text : null,
                BoolValue = Property.ValueKind == ScoringValueKind.Boolean ? Bool : null,
                DateTimeValue = Property.ValueKind == ScoringValueKind.DateTime && Date is { } d ? DateTime.SpecifyKind(d, DateTimeKind.Utc) : null
            };
    }
}