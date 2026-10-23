using Lyo.Schedule.Models;
using Lyo.Scheduler;

namespace Lyo.Job.Web.Components;

public partial class JobScheduleGrid
{
    private const int MaxUpcomingRuns = 20;
    private const int RunsPerScheduleLookAhead = 5;
    private const int DefinitionQueryAmount = 200;

    private static readonly Dictionary<string, int> CalendarHues = new(StringComparer.Ordinal) {
        ["SetTimes"] = 228,
        ["Interval"] = 174,
        ["IntervalWindow"] = 174,
        ["OneShot"] = 252,
        ["Cron"] = 292,
        ["blackout"] = 52
    };

    private TimeZoneInfo _calendarZone = TimeZoneInfo.Utc;

    private bool _loading;

    private Calendar? _monthCalendar;
    private List<ScheduleRow> _rows = [];
    private List<UpcomingRunRow> _upcomingRuns = [];
    private bool _use24Hour = true;
    private DateOnlyModel _visibleMonth = new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "Job";

    private string _definitionRoute => $"{BaseRoute.TrimEnd('/')}/Definition";

    private string _scheduleRoute => $"{BaseRoute.TrimEnd('/')}/Schedule";

    private string _runRoute => $"{BaseRoute.TrimEnd('/')}/Run";

    protected override async Task OnInitializedAsync() => await RefreshAsync();

    private async Task RefreshAsync()
    {
        _loading = true;
        try {
            var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobDefinitionRes>>(
                $"{_definitionRoute}/QueryConcrete",
                new() { Amount = DefinitionQueryAmount, Include = ["JobSchedules.JobScheduleParameters", "JobSchedules.JobBlackoutCalendar.JobBlackoutWindows"] });

            var definitions = res?.Items ?? [];
            _rows = BuildScheduleRows(definitions);
            _upcomingRuns = BuildUpcomingRuns(_rows);
            RebuildCalendar();
        }
        catch (ApiException ex) {
            Snackbar.Add(ex.Message, Severity.Error);
            _rows = [];
            _upcomingRuns = [];
            _monthCalendar = null;
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

    private static List<UpcomingRunRow> BuildUpcomingRuns(IEnumerable<ScheduleRow> rows)
        => rows.Where(r => r.Schedule.Enabled)
            .SelectMany(r => ScheduleCalculator.GetNextRuns(r.Schedule.ToScheduleDefinition(), maxCount: RunsPerScheduleLookAhead)
                .Select(runAt => new UpcomingRunRow(runAt, r.DefinitionName, r.Schedule.Id, r.Schedule.Description, r.Schedule.Type)))
            .OrderBy(r => r.RunAtUtc)
            .Take(MaxUpcomingRuns)
            .ToList();

    private async Task ToggleScheduleAsync(ScheduleRow row)
    {
        var enabled = !row.Schedule.Enabled;
        ApplyScheduleEnabled(row, enabled);
        try {
            var patch = new PatchRequestBuilder().WithKey(row.Schedule.Id).SetProperty("Enabled", enabled).Build();
            await ApiClient.PatchAsAsync<PatchRequest, PatchResult<object>>(_scheduleRoute, patch);
            Snackbar.Add($"Schedule {(enabled ? "enabled" : "disabled")}", Severity.Success);
        }
        catch (Exception ex) {
            ApplyScheduleEnabled(row, !enabled);
            Snackbar.Add($"Toggle failed: {ex.Message}", Severity.Error);
        }
    }

    private void ApplyScheduleEnabled(ScheduleRow row, bool enabled)
    {
        var updated = row.Schedule with { Enabled = enabled };
        var nextRun = enabled ? ScheduleCalculator.GetNextRun(updated.ToScheduleDefinition()) : null;
        _rows = _rows.Select(candidate => candidate.Schedule.Id == row.Schedule.Id ? new(candidate.DefinitionName, candidate.DefinitionId, updated, nextRun) : candidate)
            .OrderBy(candidate => candidate.NextRunUtc ?? DateTime.MaxValue)
            .ThenBy(candidate => candidate.DefinitionName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _upcomingRuns = BuildUpcomingRuns(_rows);
        RebuildCalendar();
    }

    private async Task RunScheduleAsync(ScheduleRow row)
    {
        if (!await DialogService.ConfirmAsync("Run schedule", $"Create a job run now for '{row.DefinitionName}' using this schedule's stored parameters?", "Run now"))
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
        var req = new QueryConcreteReq { Keys = [[definitionId]], Amount = 1, Include = [.. JobDefinitionEditorQuery.Includes] };
        var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobDefinitionRes>>($"{_definitionRoute}/QueryConcrete", req);
        var def = res?.Items?.FirstOrDefault();
        if (def == null)
            return;

        var parameters = new DialogParameters<JobDefinitionView> {
            { d => d.JobDefinition, def }, { d => d.DefinitionRoute, _definitionRoute }, { d => d.ScheduleRoute, _scheduleRoute }
        };

        await DialogService.ShowAsync<JobDefinitionView>("Job Definition", parameters, LyoDialogPresets.Large);
    }

    private void RebuildCalendar()
    {
        Calendar? merged = null;
        foreach (var row in _rows) {
            var definition = row.Schedule.ToScheduleDefinition();
            var piece = ScheduleCalendar.From(definition, _visibleMonth.Year, _visibleMonth.Month, _calendarZone, keyScope: row.Schedule.Id.ToString());
            if (row.Schedule.JobBlackoutCalendar is { Enabled: true } blackout) {
                piece = JobBlackoutCalendar.Suppress(piece, blackout, definition.TimeZone ?? TimeZoneInfo.Utc);
                piece = piece.Merge(JobBlackoutCalendar.ToCalendar(blackout, _visibleMonth.Year, _visibleMonth.Month, _calendarZone));
            }

            merged = merged is null ? piece : merged.Merge(piece);
        }

        _monthCalendar = merged;
    }

    private Task OnVisibleMonthChanged(DateOnlyModel month)
    {
        _visibleMonth = new(month.Year, month.Month, 1);
        RebuildCalendar();
        return Task.CompletedTask;
    }

    private Task OnCalendarZoneChanged(TimeZoneInfo zone)
    {
        _calendarZone = zone;
        RebuildCalendar();
        return Task.CompletedTask;
    }

    private Task OnHourCycleChanged(bool use24Hour)
    {
        _use24Hour = use24Hour;
        return Task.CompletedTask;
    }

    private ScheduleRow? RowFor(CalendarEntry entry)
    {
        foreach (var row in _rows) {
            if (entry.Key.Contains(row.Schedule.Id.ToString(), StringComparison.Ordinal))
                return row;
        }

        return null;
    }

    private sealed record ScheduleRow(string DefinitionName, Guid DefinitionId, JobScheduleRes Schedule, DateTime? NextRunUtc);

    private sealed record UpcomingRunRow(DateTime RunAtUtc, string DefinitionName, Guid ScheduleId, string? ScheduleDescription, ScheduleType ScheduleType);
}