
namespace Lyo.Job.Web.Components;

public partial class JobDefinitionGrid
{
    static JobDefinitionGrid() => FormatterLyoType.EnsureRegistered();

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "Job";

    /// <summary>Fired when the user asks to see the runs of a definition (id + name). Hosts typically switch to the Runs tab filtered to that definition.</summary>
    [Parameter]
    public EventCallback<(Guid Id, string? Name)> OnViewRuns { get; set; }

    private string _definitionRoute => $"{BaseRoute.TrimEnd('/')}/Definition";

    private string _runRoute => $"{BaseRoute.TrimEnd('/')}/Run";

    private string _scheduleRoute => $"{BaseRoute.TrimEnd('/')}/Schedule";

    private string _triggerRoute => $"{BaseRoute.TrimEnd('/')}/Trigger";

    private LyoDataGridProjected? _dataGrid;

    private readonly ProjectedBoolOverrides _enabled = new("Enabled");

    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("Name"), new("Type"), new("WorkerType", "Worker Type"), new("Enabled", type: LyoTypeInfo.Bool)];

    private bool GetEnabled(object? item) => _enabled.Get(item);

    private async Task<JobDefinitionRes?> FetchDefinition(object? id, string[]? includes = null)
    {
        if (!ProjectedValueHelper.TryGetGuid(id, out var guid))
            return null;

        var req = new QueryConcreteReq { Keys = [[guid]], Amount = 1 };
        if (includes != null)
            req.Include.AddRange(includes);

        var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobDefinitionRes>>($"{_definitionRoute}/QueryConcrete", req);
        return res?.Items?.FirstOrDefault();
    }

    private async Task OpenCreateRunDialog()
    {
        var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobDefinitionRes>>($"{_definitionRoute}/QueryConcrete", new() { Amount = 200, Include = ["JobParameters"] });
        var parameters = new DialogParameters<RunJobDialog> { { d => d.JobDefinitions, res?.Items ?? [] }, { d => d.RunRoute, _runRoute } };
        var dialog = await DialogService.ShowAsync<RunJobDialog>("Create Job Run", parameters, LyoDialogPresets.Medium);
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await RefreshAsync();
    }

    private async Task ViewDefinition(object? item)
    {
        if (item == null)
            return;

        var id = ProjectedValueHelper.GetValue(item, "Id");
        if (id == null)
            return;

        var def = await FetchDefinition(id, JobDefinitionEditorQuery.Includes);
        if (def == null)
            return;

        var parameters = new DialogParameters<JobDefinitionView> {
            { d => d.JobDefinition, def },
            { d => d.DefinitionRoute, _definitionRoute },
            { d => d.ScheduleRoute, _scheduleRoute },
            { d => d.TriggerRoute, _triggerRoute }
        };

        var dialog = await DialogService.ShowAsync<JobDefinitionView>("Job Definition", parameters, LyoDialogPresets.Large);
        var r = await dialog.Result;
        if (r is { Canceled: false })
            await RefreshAsync();
    }

    private async Task RunJob(object? item)
    {
        if (item == null)
            return;

        var id = ProjectedValueHelper.GetValue(item, "Id");
        if (id == null)
            return;

        var def = await FetchDefinition(id, ["JobParameters"]);
        if (def == null)
            return;

        var parameters = new DialogParameters<RunJobDialog> { { d => d.JobDefinitions, new List<JobDefinitionRes> { def } }, { d => d.SelectedJobDefinition, def }, { d => d.RunRoute, _runRoute } };
        var dialog = await DialogService.ShowAsync<RunJobDialog>("Create Job Run", parameters, LyoDialogPresets.Medium);
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await RefreshAsync();
    }

    private async Task ViewRuns(object? item)
    {
        if (item == null || !OnViewRuns.HasDelegate)
            return;

        var id = ProjectedValueHelper.GetValue(item, "Id");
        if (!ProjectedValueHelper.TryGetGuid(id, out var guid))
            return;

        var name = ProjectedValueHelper.GetDisplayValue(item, "Name");
        await OnViewRuns.InvokeAsync((guid, string.IsNullOrWhiteSpace(name) ? null : name));
    }

    private Task ToggleDefinition(object? item) => _enabled.ToggleAsync(ApiClient, _definitionRoute, item, Snackbar, "Definition");

    private async Task BulkToggle(bool value)
    {
        var selected = SelectedRows();
        if (selected.Count == 0)
            return;

        if (await _enabled.PatchManyAsync(ApiClient, _definitionRoute, selected, value, Snackbar, "definition") > 0)
            await RefreshAsync();
    }

    private IReadOnlyList<object?> SelectedRows()
        => ProjectedGridKeys.RowsFromKeys(_dataGrid?.SelectedKeys);

    private async Task RefreshAsync()
    {
        _enabled.Clear();
        await _dataGrid!.RefreshData();
    }
}
