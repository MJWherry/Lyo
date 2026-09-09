using Lyo.Schedule.Models;
using Lyo.Web.Components.ParamTable;

namespace Lyo.Job.Web.Components;

/// <summary>
/// Master/detail editor for a job definition's schedules: add or remove, enable toggle, type-specific fields, timezone, blackout calendar, and schedule-parameter overrides
/// (value pickers inherited from matching definition parameters).
/// </summary>
public partial class JobScheduleView
{
    static JobScheduleView() => FormatterLyoType.EnsureRegistered();

    private static readonly TimeZoneInfo[] TimeZones = [..TimeZoneInfo.GetSystemTimeZones()];

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Inject]
    private ILyoTimeZone BrowserTimeZone { get; set; } = null!;

    /// <summary>Schedules from the parent definition payload; used as the starting list until the view reloads after a mutation.</summary>
    [Parameter]
    public IReadOnlyList<JobScheduleRes>? JobSchedules { get; set; }

    /// <summary>Definition parameters whose Options / AllowedValues are reused when a schedule-parameter key matches.</summary>
    [Parameter]
    public IReadOnlyList<JobParameterRes>? JobParameters { get; set; }

    /// <summary>Owning definition, used to build the formatter context offered to formatter-typed schedule parameters. Editing still works if it is omitted.</summary>
    [Parameter]
    public JobDefinitionRes? JobDefinition { get; set; }

    /// <summary>Owning job definition id, used when creating schedules and when reloading.</summary>
    [Parameter]
    public Guid JobDefinitionId { get; set; }

    /// <summary>API client used for schedule, parameter, and definition QueryConcrete calls.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>CRUD route for schedules (for example <c>Job/Schedule</c>).</summary>
    [Parameter]
    [EditorRequired]
    public string ScheduleRoute { get; set; } = "";

    /// <summary>CRUD route for the parent definition (for example <c>Job/Definition</c>). Used to reload schedules after mutations.</summary>
    [Parameter]
    public string DefinitionRoute { get; set; } = "";

    /// <summary>Standalone schedule-parameter CRUD route. Defaults to <c>{ScheduleRoute}Parameters</c> (for example <c>Job/ScheduleParameters</c>).</summary>
    [Parameter]
    public string? ScheduleParameterRoute { get; set; }

    private string ParameterRoute => ScheduleParameterRoute ?? $"{ScheduleRoute.TrimEnd('/')}Parameters";

    private string JobBaseRoute {
        get {
            var def = DefinitionRoute.TrimEnd('/');
            const string suffix = "/Definition";
            return def.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) ? def[..^suffix.Length] : def;
        }
    }

    private string RunRoute => string.IsNullOrEmpty(JobBaseRoute) ? Constants.Rest.Job.Runs : $"{JobBaseRoute}/Run";

    private string CalendarRoute => string.IsNullOrEmpty(JobBaseRoute) ? Constants.Rest.Job.BlackoutCalendars : $"{JobBaseRoute}/BlackoutCalendar";

    private string WindowRoute => $"{CalendarRoute}/Window";

    private sealed class ScheduleEditModel
    {
        public ScheduleType Type { get; set; }

        public string? Description { get; set; }

        public bool Enabled { get; set; }

        public IReadOnlyCollection<DayFlags> Days { get; set; } = [];

        public IReadOnlyCollection<MonthFlags> Months { get; set; } = [];

        public string? CronExpression { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        public int? IntervalMinutes { get; set; }

        public List<TimeSpan> Times { get; set; } = [];

        public JobMisfirePolicy MisfirePolicy { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string? TimeZoneId { get; set; }

        public Guid? JobBlackoutCalendarId { get; set; }
    }

    private List<JobScheduleRes> _schedules = [];

    /// <summary>Definition the current <see cref="_schedules" /> list was seeded from, so a definition switch reseeds instead of showing the previous one's schedules.</summary>
    private Guid? _seededDefinitionId;

    /// <summary>The exact <see cref="JobSchedules" /> instance already seeded. Compared by reference: the parent hands over a fresh list on each reload.</summary>
    private IReadOnlyList<JobScheduleRes>? _seededSchedules;

    private JobScheduleRes? _selected;
    private ScheduleEditModel? _edit;
    private TimeSpan? _newTime;
    private List<LyoParameterEditRow> _paramRows = [];
    private bool _paramsDirty;
    private bool _running;
    private TimeZoneInfo? _browserZone;

    /// <summary>
    /// Rebuilt only when the selection changes, so the selected schedule's own parameters appear as <c>Schedule_Parameter_*</c> keys. Must not be a computed property:
    /// the formatter editor treats a new context instance as a change and rebuilds its token catalog, which a per-render instance would do on each render.
    /// </summary>
    private object? _formatterContext;

    private IReadOnlyList<LyoParameterEditRow> InheritRows =>
        (JobParameters ?? []).Select(p => new LyoParameterEditRow {
            Key = p.Key,
            Type = p.Type,
            Options = p.Options,
            AllowedValues = p.AllowedValues,
            Description = p.Description
        }).ToList();

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        _browserZone ??= await BrowserTimeZone.GetAsync();

        // Reseed when the parent switches definitions or hands over a reloaded list. A one-shot guard left the editor showing the first definition's schedules for the life of
        // the component, and hid schedules created elsewhere.
        if (_seededDefinitionId == JobDefinitionId && ReferenceEquals(_seededSchedules, JobSchedules))
            return;

        _seededDefinitionId = JobDefinitionId;
        _seededSchedules = JobSchedules;
        _schedules = JobSchedules?.ToList() ?? [];
        var keepSelected = _schedules.FirstOrDefault(s => s.Id == _selected?.Id);
        SelectSchedule(keepSelected ?? _schedules.FirstOrDefault());
    }

    private void SelectSchedule(JobScheduleRes? schedule)
    {
        _selected = schedule;
        _newTime = null;
        _formatterContext = JobFormatterContext.Build(JobDefinition, schedule: schedule);
        if (schedule == null) {
            _edit = null;
            _paramRows = [];
            _paramsDirty = false;
            return;
        }

        _edit = new() {
            Type = schedule.Type,
            Description = schedule.Description,
            Enabled = schedule.Enabled,
            Days = FlagEnumUi.SelectedAtomic(schedule.DayFlags),
            Months = FlagEnumUi.SelectedAtomic(schedule.MonthFlags),
            CronExpression = schedule.CronExpression,
            StartTime = schedule.StartTime?.ToTimeSpan(),
            EndTime = schedule.EndTime?.ToTimeSpan(),
            IntervalMinutes = schedule.IntervalMinutes,
            Times = (schedule.Times ?? []).Select(t => t.ToTimeSpan()).ToList(),
            MisfirePolicy = schedule.MisfirePolicy,
            StartDate = ToPickerDate(schedule.StartDateUtc, ResolveZone(schedule.TimeZoneId)),
            EndDate = ToPickerDate(schedule.EndDateUtc, ResolveZone(schedule.TimeZoneId)),
            TimeZoneId = string.IsNullOrWhiteSpace(schedule.TimeZoneId) ? _browserZone?.Id : schedule.TimeZoneId,
            JobBlackoutCalendarId = schedule.JobBlackoutCalendarId
        };

        _paramRows = (schedule.Parameters ?? []).Select(ToParamRow).ToList();
        _paramsDirty = false;
    }

    private LyoParameterEditRow ToParamRow(JobScheduleParameterRes p)
    {
        var def = FindDefinitionParam(p.Key);
        return LyoParameterEditRow.FromMasked(
            p.Id, p.Key, p.Type, p.Value, p.Description,
            encryptedValue: p.EncryptedValue,
            allowedValues: def?.AllowedValues,
            options: def?.Options,
            enabled: p.Enabled);
    }

    private JobParameterRes? FindDefinitionParam(string key)
        => JobParameters?.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));

    private bool DaysMatch(DayFlags preset) => _edit != null && FlagEnumUi.MatchesPreset(_edit.Days, preset);

    private bool MonthsMatch(MonthFlags preset) => _edit != null && FlagEnumUi.MatchesPreset(_edit.Months, preset);

    private void ApplyDays(DayFlags preset)
    {
        if (_edit == null)
            return;

        _edit.Days = FlagEnumUi.SelectedAtomic(preset);
    }

    private void ApplyMonths(MonthFlags preset)
    {
        if (_edit == null)
            return;

        _edit.Months = FlagEnumUi.SelectedAtomic(preset);
    }

    private void AddTime()
    {
        if (_edit == null || _newTime == null)
            return;

        if (!_edit.Times.Contains(_newTime.Value))
            _edit.Times.Add(_newTime.Value);

        _edit.Times.Sort();
        _newTime = null;
    }

    private void RemoveTime(TimeSpan t) => _edit?.Times.Remove(t);

    private static Task<IEnumerable<string>> SearchTimeZones(string? value, CancellationToken ct)
    {
        IEnumerable<TimeZoneInfo> query = TimeZones;
        if (!string.IsNullOrWhiteSpace(value)) {
            query = query.Where(z =>
                z.Id.Contains(value, StringComparison.OrdinalIgnoreCase) || z.DisplayName.Contains(value, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult(query.Take(40).Select(z => z.Id));
    }

    private TimeZoneInfo ResolveZone(string? timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId)) {
            try {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException) {
            }
            catch (InvalidTimeZoneException) {
            }
        }

        return _browserZone ?? TimeZoneInfo.Utc;
    }

    private static DateTime? ToPickerDate(DateTime? utc, TimeZoneInfo zone)
    {
        if (utc is null)
            return null;

        var instant = utc.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc)
            : utc.Value.ToUniversalTime();
        var local = TimeZoneInfo.ConvertTimeFromUtc(instant, zone);
        return DateTime.SpecifyKind(local.Date, DateTimeKind.Unspecified);
    }

    private static DateTime? FromPickerDate(DateTime? localDate, TimeZoneInfo zone)
    {
        if (localDate is null)
            return null;

        var localMidnight = DateTime.SpecifyKind(localDate.Value.Date, DateTimeKind.Unspecified);
        try {
            return TimeZoneInfo.ConvertTimeToUtc(localMidnight, zone);
        }
        catch (ArgumentException) {
            return DateTime.SpecifyKind(localMidnight, DateTimeKind.Utc);
        }
    }

    private static string FormatTimeZone(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return "";

        var tz = TimeZones.FirstOrDefault(z => z.Id == id);
        return tz is null ? id : $"{tz.Id} — {tz.DisplayName}";
    }

    private async Task ReloadSchedulesAsync(Guid? selectId = null)
    {
        var id = selectId ?? _selected?.Id;
        if (JobDefinitionId == default || string.IsNullOrWhiteSpace(DefinitionRoute)) {
            SelectSchedule(_schedules.FirstOrDefault(s => s.Id == id) ?? _schedules.FirstOrDefault());
            return;
        }

        try {
            var req = new QueryConcreteReq {
                Keys = [[JobDefinitionId]],
                Amount = 1,
                Include = ["JobSchedules.JobScheduleParameters", "JobSchedules.JobBlackoutCalendar.JobBlackoutWindows"]
            };
            var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobDefinitionRes>>($"{DefinitionRoute.TrimEnd('/')}/QueryConcrete", req);
            _schedules = res?.Items?.FirstOrDefault()?.JobSchedules?.ToList() ?? [];
            SelectSchedule(_schedules.FirstOrDefault(s => s.Id == id) ?? _schedules.FirstOrDefault());
        }
        catch (Exception ex) {
            Snackbar.Add($"Reload failed: {ex.Message}", Severity.Error);
        }
    }

    private async Task ToggleSchedule(JobScheduleRes schedule)
    {
        try {
            var patch = new PatchRequestBuilder().WithKey(schedule.Id).SetProperty("Enabled", !schedule.Enabled).Build();
            await ApiClient.PatchAsAsync<PatchRequest, PatchResult<object>>(ScheduleRoute, patch);
            Snackbar.Add($"Schedule {(!schedule.Enabled ? "enabled" : "disabled")}", Severity.Success);
            await ReloadSchedulesAsync(schedule.Id);
        }
        catch (Exception ex) {
            Snackbar.Add($"Toggle failed: {ex.Message}", Severity.Error);
        }
    }

    private async Task AddSchedule()
    {
        if (JobDefinitionId == default)
            return;

        try {
            var created = await ApiClient.PostAsAsync<JobScheduleReq, CreateResult<JobScheduleRes>>(
                ScheduleRoute, new() {
                    JobDefinitionId = JobDefinitionId,
                    Type = ScheduleType.SetTimes,
                    DayFlags = DayFlags.EveryDay,
                    MonthFlags = MonthFlags.EveryMonth,
                    Enabled = true,
                    Description = "New schedule",
                    TimeZoneId = _browserZone?.Id,
                    Times = []
                });
            var newId = created?.Data?.Id;
            Snackbar.Add("Schedule added", Severity.Success);
            await ReloadSchedulesAsync(newId);
        }
        catch (Exception ex) {
            Snackbar.Add($"Add failed: {ex.Message}", Severity.Error);
        }
    }

    private async Task DeleteSchedule()
    {
        if (_selected == null)
            return;

        if (!await DialogService.ConfirmDeleteAsync("this schedule", "This cannot be undone.", "Delete schedule"))
            return;

        try {
            await ApiClient.DeleteAsAsync<object>($"{ScheduleRoute.TrimEnd('/')}/{_selected.Id}");
            Snackbar.Add("Schedule deleted", Severity.Success);
            await ReloadSchedulesAsync();
        }
        catch (Exception ex) {
            Snackbar.Add($"Delete failed: {ex.Message}", Severity.Error);
        }
    }

    private async Task OnBlackoutChanged() => await ReloadSchedulesAsync(_selected?.Id);

    private JobScheduleReq BuildRequest()
    {
        var days = _edit!.Days ?? [];
        var months = _edit.Months ?? [];
        return new() {
            JobDefinitionId = _selected!.JobDefinitionId != default ? _selected.JobDefinitionId : JobDefinitionId,
            Type = _edit.Type,
            Description = string.IsNullOrWhiteSpace(_edit.Description) ? null : _edit.Description,
            Enabled = _edit.Enabled,
            DayFlags = days.Aggregate(DayFlags.None, (acc, d) => acc | d),
            MonthFlags = months.Aggregate(MonthFlags.None, (acc, m) => acc | m),
            CronExpression = string.IsNullOrWhiteSpace(_edit.CronExpression) ? null : _edit.CronExpression,
            StartTime = _edit.StartTime is { } st ? TimeOnly.FromTimeSpan(st) : null,
            EndTime = _edit.EndTime is { } et ? TimeOnly.FromTimeSpan(et) : null,
            IntervalMinutes = _edit.IntervalMinutes,
            Times = [.. _edit.Times.Select(TimeOnly.FromTimeSpan)],
            MisfirePolicy = _edit.MisfirePolicy,
            StartDateUtc = FromPickerDate(_edit.StartDate, ResolveZone(_edit.TimeZoneId)),
            EndDateUtc = FromPickerDate(_edit.EndDate, ResolveZone(_edit.TimeZoneId)),
            TimeZoneId = string.IsNullOrWhiteSpace(_edit.TimeZoneId) ? _browserZone?.Id : _edit.TimeZoneId,
            JobBlackoutCalendarId = _edit.JobBlackoutCalendarId
        };
    }

    private async Task RunSchedule()
    {
        if (_selected == null)
            return;

        if (_paramsDirty) {
            Snackbar.Add("Save schedule parameters before running", Severity.Warning);
            return;
        }

        if (!await DialogService.ConfirmAsync(
                "Run schedule",
                "Create a job run now using this schedule's stored parameters?",
                "Run now"))
            return;

        _running = true;
        try {
            var req = JobScheduleRun.CreateRequest(
                _selected.JobDefinitionId != default ? _selected.JobDefinitionId : JobDefinitionId, _selected);
            var result = await ApiClient.PostAsAsync<JobRunReq, CreateResult<JobRunRes>>($"{RunRoute.TrimEnd('/')}/Create", req);
            Snackbar.Add($"Job run {result?.Data?.Id.Truncated() ?? "?"} created", Severity.Success);
        }
        catch (Exception ex) {
            Snackbar.Add($"Run failed: {ex.Message}", Severity.Error);
        }
        finally {
            _running = false;
        }
    }

    private async Task SaveSchedule()
    {
        if (_selected == null || _edit == null)
            return;

        try {
            await ApiClient.PostAsAsync<UpdateRequest<JobScheduleReq>, UpdateResult<JobScheduleRes>>($"{ScheduleRoute.TrimEnd('/')}/Update", new(BuildRequest(), _selected.Id));
            Snackbar.Add("Schedule saved", Severity.Success);
            await ReloadSchedulesAsync(_selected.Id);
        }
        catch (Exception ex) {
            Snackbar.Add($"Save failed: {ex.Message}", Severity.Error);
        }
    }

    private async Task SaveParameters()
    {
        if (_selected == null)
            return;

        try {
            var existing = (_selected.Parameters ?? []).ToDictionary(p => p.Id);
            foreach (var removed in existing.Values.Where(e => _paramRows.All(d => d.Id != e.Id)))
                await ApiClient.DeleteAsAsync<object>($"{ParameterRoute}/{removed.Id}");

            foreach (var newRow in _paramRows.Where(d => d.IsNew)) {
                await ApiClient.PostAsAsync<JobScheduleParameterReq, JobScheduleParameterRes>(
                    ParameterRoute, new() {
                        JobScheduleId = _selected.Id,
                        Key = newRow.Key,
                        Type = newRow.Type,
                        Value = newRow.Value,
                        Description = newRow.Description,
                        Enabled = newRow.Enabled
                    });
            }

            foreach (var existingRow in _paramRows.Where(d => !d.IsNew && d.Id.HasValue)) {
                JobScheduleParameterReq data = new() {
                    JobScheduleId = _selected.Id,
                    Key = existingRow.Key,
                    Type = existingRow.Type,
                    Value = existingRow.Value,
                    Description = existingRow.Description,
                    Enabled = existingRow.Enabled
                };

                await ApiClient.PostAsAsync<UpdateRequest<JobScheduleParameterReq>, UpdateResult<JobScheduleParameterRes>>(
                    $"{ParameterRoute}/Update", new(data, existingRow.Id!.Value));
            }

            Snackbar.Add("Schedule parameters saved", Severity.Success);
            _paramsDirty = false;
            await ReloadSchedulesAsync(_selected.Id);
        }
        catch (Exception ex) {
            Snackbar.Add($"Save failed: {ex.Message}", Severity.Error);
        }
    }
}
