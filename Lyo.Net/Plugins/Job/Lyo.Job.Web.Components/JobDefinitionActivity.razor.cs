namespace Lyo.Job.Web.Components;

/// <summary>
/// Basic-info activity panel: running and queued counts, last-run snapshots, and upcoming scheduled slots. Loads
/// <c>GET Job/Definition/{id}/Stats</c>, <c>POST Job/Definition/LatestRuns</c>, and <c>GET Job/Definition/{id}/NextRuns</c>.
/// </summary>
public partial class JobDefinitionActivity
{
    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    /// <summary>API client used to load stats, latest runs, and next-run times.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Definition CRUD route (for example <c>Job/Definition</c>).</summary>
    [Parameter]
    [EditorRequired]
    public string DefinitionRoute { get; set; } = "";

    /// <summary>Definition whose activity is shown.</summary>
    [Parameter]
    public Guid JobDefinitionId { get; set; }

    /// <summary>Definition whose load is in flight or already rendered. Set only after its data is applied, so a fast switch cannot mark the new definition loaded with old
    /// data.</summary>
    private Guid _loadedId;

    /// <summary>Definition of the most recent load. A response for any other definition is stale and discarded.</summary>
    private Guid _requestedId;

    private bool _loading;
    private string? _error;
    private JobDefinitionStatsRes? _stats;
    private JobDefinitionLatestRunsRes? _latest;
    private List<DateTime> _nextRuns = [];

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        if (JobDefinitionId == default || JobDefinitionId == _loadedId || string.IsNullOrWhiteSpace(DefinitionRoute))
            return;

        await LoadAsync(JobDefinitionId);
    }

    private async Task LoadAsync(Guid definitionId)
    {
        _requestedId = definitionId;
        _loading = true;
        _error = null;
        try {
            var route = DefinitionRoute.TrimEnd('/');
            var statsTask = ApiClient.GetAsAsync<JobDefinitionStatsRes>($"{route}/{definitionId}/Stats?days=30");
            var latestTask = ApiClient.PostAsAsync<List<Guid>, List<JobDefinitionLatestRunsRes>>($"{route}/LatestRuns", [definitionId]);
            var nextTask = ApiClient.GetAsAsync<List<DateTime>>($"{route}/{definitionId}/NextRuns?count=10");
            var stats = await statsTask;
            var latest = await latestTask;
            var next = await nextTask;

            // A newer switch started while these were still in flight; that load now owns the panel.
            if (_requestedId != definitionId)
                return;

            _stats = stats;
            _latest = latest?.FirstOrDefault(r => r.JobDefinitionId == definitionId) ?? latest?.FirstOrDefault();
            _nextRuns = next ?? [];
            _loadedId = definitionId;
        }
        catch (Exception ex) {
            if (_requestedId != definitionId)
                return;

            _error = ex.Message;
            Snackbar.Add($"Activity load failed: {ex.Message}", Severity.Warning);
        }
        finally {
            if (_requestedId == definitionId)
                _loading = false;
        }
    }

    private static RenderFragment RunStamp(JobRunRes? run)
    {
        if (run is null)
            return static builder => builder.AddContent(0, "—");

        var when = run.FinishedTimestamp ?? run.StartedTimestamp ?? run.CreatedTimestamp;
        var outcome = run.Result?.ToString() ?? run.State.ToString();
        var id = run.Id.Truncated();
        return builder => {
            builder.OpenComponent<LyoTimestamp>(0);
            builder.AddAttribute(1, nameof(LyoTimestamp.Value), when);
            builder.CloseComponent();
            builder.AddContent(2, $" · {outcome} · {id}");
        };
    }
}
