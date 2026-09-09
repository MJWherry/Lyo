
namespace Lyo.Job.Web.Components;

public partial class JobRunGrid
{
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "Job";

    /// <summary>When set, the grid only shows runs of this definition and displays a removable filter chip. Supports two-way binding so the chip's close button clears host
    /// state.</summary>
    [Parameter]
    public Guid? DefinitionId { get; set; }

    [Parameter]
    public EventCallback<Guid?> DefinitionIdChanged { get; set; }

    /// <summary>Display name for the definition filter chip; falls back to a truncated id.</summary>
    [Parameter]
    public string? DefinitionName { get; set; }

    private string _runRoute => $"{BaseRoute.TrimEnd('/')}/Run";

    private string _definitionRoute => $"{BaseRoute.TrimEnd('/')}/Definition";

    private LyoDataGridProjected? _dataGrid;

    private Guid? _appliedDefinitionId;

    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [
        new("JobDefinition.Name", "Definition"), new("State"), new("Result"), new("WorkerMachineName", "Worker"), new("WorkerInstanceId", "Worker ID")
    ];

    private static readonly string[] _keySelectFields = ["Id", "StartedTimestamp", "FinishedTimestamp", "WorkerInstanceId", "WorkerProcessId", "WorkerMachineName"];

    protected override async Task OnParametersSetAsync()
    {
        if (_appliedDefinitionId == DefinitionId)
            return;

        _appliedDefinitionId = DefinitionId;
        if (_dataGrid != null)
            await _dataGrid.RefreshData();
    }

    private void ApplyQuery(ProjectionQueryReqBuilder q)
    {
        // QueryProject derives its EF includes from the projected Select paths; sending include paths only widens entity load on the fallback path.
        if (DefinitionId is { } defId)
            q.AddWhere(new ConditionClause("JobDefinitionId", ComparisonOperatorEnum.Equals, defId));
    }

    private async Task ClearDefinitionFilter()
    {
        DefinitionId = null;
        _appliedDefinitionId = null;
        if (DefinitionIdChanged.HasDelegate)
            await DefinitionIdChanged.InvokeAsync(null);

        if (_dataGrid != null)
            await _dataGrid.RefreshData();
    }

    private static string DisplayOrEmpty(object? item, string field)
    {
        var value = ProjectedValueHelper.GetDisplayValue(item, field);
        return string.IsNullOrWhiteSpace(value) || string.Equals(value, "null", StringComparison.OrdinalIgnoreCase) ? "" : value;
    }


    /// <summary>Cancel is accepted only for <c>Running</c> and <c>Queued</c> runs; offering it on a finished row produced a guaranteed HTTP 400.</summary>
    private static bool CanCancel(object? item)
        => Enum.TryParse<JobState>(ProjectedValueHelper.GetDisplayValue(item, "State"), out var state) && state is JobState.Running or JobState.Queued;

    private static (DateTime? Started, DateTime? Finished) GetTimestamps(object? item)
        => (LyoDateTimeDisplay.ToDateTime(ProjectedValueHelper.GetValue(item, "StartedTimestamp")),
            LyoDateTimeDisplay.ToDateTime(ProjectedValueHelper.GetValue(item, "FinishedTimestamp")));

    private async Task ResyncQueued()
    {
        if (!await DialogService.ConfirmAsync("Resync RabbitMQ", "Republish queued runs that are missing from RabbitMQ? Runs already in the queue are skipped.", "Resync"))
            return;

        try {
            var route = DefinitionId is { } defId ? $"{_runRoute}/Resync?definitionId={defId}" : $"{_runRoute}/Resync";
            var result = await ApiClient.PostAsAsync<JobRunResyncRes>(route);
            var severity = result.Failed > 0 ? Severity.Warning : Severity.Success;
            var failed = result.Failed > 0 ? $", {result.Failed} failed" : "";
            Snackbar.Add($"Resynced: {result.Republished} published, {result.AlreadyInQueue} already in queue{failed}", severity);
            await _dataGrid!.RefreshData();
        }
        catch (Exception ex) {
            Snackbar.Add($"Resync failed: {ex.Message}", Severity.Error);
        }
    }

    private async Task OpenCreateRunDialog()
    {
        var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobDefinitionRes>>($"{_definitionRoute}/QueryConcrete", new() { Amount = 200, Include = ["JobParameters"] });
        var parameters = new DialogParameters<RunJobDialog> { { d => d.JobDefinitions, res?.Items ?? [] }, { d => d.RunRoute, _runRoute } };
        var dialog = await DialogService.ShowAsync<RunJobDialog>("Create Job Run", parameters, LyoDialogPresets.Medium);
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await _dataGrid!.RefreshData();
    }

    private async Task ViewRun(object? item)
    {
        if (item == null)
            return;

        var id = ProjectedValueHelper.GetValue(item, "Id");
        if (id == null)
            return;

        var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobRunRes>>($"{_runRoute}/QueryConcrete", new() { Keys = [[id]], Amount = 1, Include = ["JobDefinition", "JobRunLogs", "JobRunResults", "JobRunParameters", "JobSchedule"] });
        var run = res?.Items?.FirstOrDefault();
        if (run == null)
            return;

        var parameters = new DialogParameters<JobRunDetailView> { { d => d.JobRun, run }, { d => d.RunRoute, _runRoute } };
        var dialog = await DialogService.ShowAsync<JobRunDetailView>("Job run", parameters, LyoDialogPresets.Large);
        var r = await dialog.Result;
        if (r is { Canceled: false })
            await _dataGrid!.RefreshData();
    }

    private async Task RerunJob(object? item)
    {
        if (item == null)
            return;

        var id = ProjectedValueHelper.GetValue(item, "Id");
        if (id == null)
            return;

        try {
            await ApiClient.PostAsAsync<object>($"{_runRoute}/{id}/Rerun");
            Snackbar.Add("Re-run queued", Severity.Success);
            await _dataGrid!.RefreshData();
        }
        catch (Exception ex) {
            Snackbar.Add($"Re-run failed: {ex.Message}", Severity.Error);
        }
    }

    private async Task CancelRun(object? item)
    {
        if (item == null)
            return;

        var id = ProjectedValueHelper.GetValue(item, "Id");
        if (id == null)
            return;

        try {
            await ApiClient.PostAsAsync<object>($"{_runRoute}/{id}/Cancel");
            Snackbar.Add("Cancel requested", Severity.Info);
            await _dataGrid!.RefreshData();
        }
        catch (Exception ex) {
            Snackbar.Add($"Cancel failed: {ex.Message}", Severity.Error);
        }
    }

    private async Task DeleteRun(object? item)
    {
        if (item == null)
            return;

        var id = ProjectedValueHelper.GetValue(item, "Id");
        if (id == null)
            return;

        if (!await DialogService.ConfirmDeleteAsync($"job run {id?.ToString()?[..8]}...", title: "Confirm Delete"))
            return;

        try {
            await ApiClient.DeleteAsAsync<object>($"{_runRoute}/{id}");
            Snackbar.Add("Job run deleted", Severity.Success);
            await _dataGrid!.RefreshData();
        }
        catch (Exception ex) {
            Snackbar.Add($"Delete failed: {ex.Message}", Severity.Error);
        }
    }
}
