namespace Lyo.Job.Web.Components;

/// <summary>
/// Basic-info activity panel: currently running / queued counts, last run snapshots, and upcoming scheduled slots. Loads
/// <c>GET Job/Definition/{id}/Stats</c>, <c>POST Job/Definition/LatestRuns</c>, and <c>GET Job/Definition/{id}/NextRuns</c>.
/// </summary>
public partial class JobDefinitionActivity
{
    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    /// <summary>API client used to load stats, latest runs, and next-run timestamps.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Definition CRUD route (e.g. <c>Job/Definition</c>).</summary>
    [Parameter]
    [EditorRequired]
    public string DefinitionRoute { get; set; } = "";

    /// <summary>Definition whose activity is displayed.</summary>
    [Parameter]
    public Guid JobDefinitionId { get; set; }

    private Guid _loadedId;
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

        _loadedId = JobDefinitionId;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try {
            var route = DefinitionRoute.TrimEnd('/');
            var statsTask = ApiClient.GetAsAsync<JobDefinitionStatsRes>($"{route}/{JobDefinitionId}/Stats?days=30");
            var latestTask = ApiClient.PostAsAsync<List<Guid>, List<JobDefinitionLatestRunsRes>>($"{route}/LatestRuns", [JobDefinitionId]);
            var nextTask = ApiClient.GetAsAsync<List<DateTime>>($"{route}/{JobDefinitionId}/NextRuns?count=10");
            await Task.WhenAll(statsTask, latestTask, nextTask);

            _stats = statsTask.Result;
            _latest = latestTask.Result?.FirstOrDefault(r => r.JobDefinitionId == JobDefinitionId) ?? latestTask.Result?.FirstOrDefault();
            _nextRuns = nextTask.Result ?? [];
        }
        catch (Exception ex) {
            _error = ex.Message;
            Snackbar.Add($"Activity load failed: {ex.Message}", Severity.Warning);
        }
        finally {
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
