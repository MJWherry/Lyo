using Lyo.Schedule.Models;
using Lyo.Scheduler;

namespace Lyo.Job.Web.Components;

public partial class JobScheduleGrid
{
    private const int MaxUpcomingRuns = 20;
    private const int RunsPerScheduleLookAhead = 5;
    private const int DefinitionQueryAmount = 200;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "Job";

    private string _definitionRoute => $"{BaseRoute.TrimEnd('/')}/Definition";

    private string _scheduleRoute => $"{BaseRoute.TrimEnd('/')}/Schedule";

    private string _triggerRoute => $"{BaseRoute.TrimEnd('/')}/Trigger";

    private string _runRoute => $"{BaseRoute.TrimEnd('/')}/Run";

    private bool _loading;
    private List<ScheduleRow> _rows = [];
    private List<UpcomingRunRow> _upcomingRuns = [];

    protected override async Task OnInitializedAsync() => await RefreshAsync();

    private async Task RefreshAsync()
    {
        _loading = true;
        try {
            var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobDefinitionRes>>(
                $"{_definitionRoute}/QueryConcrete", new() { Amount = DefinitionQueryAmount, Include = ["JobSchedules.JobScheduleParameters"] });
            var definitions = res?.Items ?? [];
            _rows = BuildScheduleRows(definitions);
            _upcomingRuns = BuildUpcomingRuns(_rows);
        }
        catch (ApiException ex) {
            Snackbar.Add(ex.Message, Severity.Error);
            _rows = [];
            _upcomingRuns = [];
        }
        finally {
            _loading = false;
        }
    }

    private static List<ScheduleRow> BuildScheduleRows(IReadOnlyList<JobDefinitionRes> definitions)
    {
        var rows = new List<ScheduleRow>();
        foreach (var def in definitions) {
            if (def.JobSchedules is not { Count: > 0 })
                continue;

            foreach (var schedule in def.JobSchedules) {
                var nextRun = schedule.Enabled ? ScheduleCalculator.GetNextRun(schedule.ToScheduleDefinition()) : null;
                rows.Add(new(def.Name, def.Id, schedule, nextRun));
            }
        }

        return rows.OrderBy(r => r.NextRunUtc ?? DateTime.MaxValue).ThenBy(r => r.DefinitionName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<UpcomingRunRow> BuildUpcomingRuns(IEnumerable<ScheduleRow> rows) => rows.Where(r => r.Schedule.Enabled).SelectMany(r => ScheduleCalculator.GetNextRuns(r.Schedule.ToScheduleDefinition(), maxCount: RunsPerScheduleLookAhead).Select(runAt => new UpcomingRunRow(runAt, r.DefinitionName, r.Schedule.Id, r.Schedule.Description, r.Schedule.Type))).OrderBy(r => r.RunAtUtc).Take(MaxUpcomingRuns).ToList();

    private async Task ToggleScheduleAsync(ScheduleRow row)
    {
        var patch = new PatchRequestBuilder().WithKey(row.Schedule.Id).SetProperty("Enabled", !row.Schedule.Enabled).Build();
        await ApiClient.PatchAsAsync<PatchRequest, PatchResult<object>>(_scheduleRoute, patch);
        Snackbar.Add($"Schedule {(row.Schedule.Enabled ? "disabled" : "enabled")}", Severity.Success);
        await RefreshAsync();
    }

    private async Task RunScheduleAsync(ScheduleRow row)
    {
        if (!await DialogService.ConfirmAsync(
                "Run schedule",
                $"Create a job run now for '{row.DefinitionName}' using this schedule's stored parameters?",
                "Run now"))
            return;

        try {
            var req = JobScheduleRun.CreateRequest(row.DefinitionId, row.Schedule);
            var result = await ApiClient.PostAsAsync<JobRunReq, CreateResult<JobRunRes>>($"{_runRoute}/Create", req);
            Snackbar.Add($"Job run {result?.Data?.Id.Truncated() ?? "?"} created", Severity.Success);
        }
        catch (Exception ex) {
            Snackbar.Add($"Run failed: {ex.Message}", Severity.Error);
        }
    }

    private async Task ViewDefinitionAsync(Guid definitionId)
    {
        var req = new QueryConcreteReq { Keys = [[definitionId]], Amount = 1, Include = [..JobDefinitionEditorQuery.Includes] };
        var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobDefinitionRes>>($"{_definitionRoute}/QueryConcrete", req);
        var def = res?.Items?.FirstOrDefault();
        if (def == null)
            return;

        var parameters = new DialogParameters<JobDefinitionView> {
            { d => d.JobDefinition, def },
            { d => d.DefinitionRoute, _definitionRoute },
            { d => d.ScheduleRoute, _scheduleRoute },
            { d => d.TriggerRoute, _triggerRoute }
        };

        await DialogService.ShowAsync<JobDefinitionView>("Job Definition", parameters, LyoDialogPresets.Large);
    }

    private sealed record ScheduleRow(string DefinitionName, Guid DefinitionId, JobScheduleRes Schedule, DateTime? NextRunUtc);

    private sealed record UpcomingRunRow(DateTime RunAtUtc, string DefinitionName, Guid ScheduleId, string? ScheduleDescription, ScheduleType ScheduleType);
}
