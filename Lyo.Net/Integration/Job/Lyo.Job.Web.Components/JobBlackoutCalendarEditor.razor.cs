namespace Lyo.Job.Web.Components;

/// <summary>
/// Inline editor for a schedule's blackout calendar and windows. Creates or unlinks a calendar via <c>Job/BlackoutCalendar</c> and persists windows via
/// <c>Job/BlackoutCalendar/Window</c>.
/// </summary>
public partial class JobBlackoutCalendarEditor
{
    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    /// <summary>Current calendar on the selected schedule, or null when none is linked.</summary>
    [Parameter]
    public JobBlackoutCalendarRes? Calendar { get; set; }

    /// <summary>Schedule whose <c>JobBlackoutCalendarId</c> is patched when adding or removing a calendar.</summary>
    [Parameter]
    public Guid ScheduleId { get; set; }

    /// <summary>API client for calendar, window, and schedule PATCH calls.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>CRUD route for schedules (used to PATCH the calendar FK).</summary>
    [Parameter]
    [EditorRequired]
    public string ScheduleRoute { get; set; } = "";

    /// <summary>CRUD route for blackout calendars (e.g. <c>Job/BlackoutCalendar</c>).</summary>
    [Parameter]
    [EditorRequired]
    public string CalendarRoute { get; set; } = "";

    /// <summary>CRUD route for blackout windows (e.g. <c>Job/BlackoutCalendar/Window</c>).</summary>
    [Parameter]
    [EditorRequired]
    public string WindowRoute { get; set; } = "";

    /// <summary>Raised after a calendar is created, unlinked, or saved so the parent can reload the schedule.</summary>
    [Parameter]
    public EventCallback OnChanged { get; set; }

    private sealed class CalendarEditModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = "Blackout";

        public string? Description { get; set; }

        public bool Enabled { get; set; } = true;

        public List<WindowEditModel> Windows { get; set; } = [];
    }

    private sealed class WindowEditModel
    {
        public Guid? Id { get; set; }

        public Guid ClientKey { get; } = Guid.NewGuid();

        public bool IsNew { get; set; }

        public string Name { get; set; } = "Window";

        public bool UseDateRange { get; set; }

        public IReadOnlyCollection<DayFlags> Days { get; set; } = FlagEnumUi.SelectedAtomic(DayFlags.EveryDay);

        public DateTime? StartDateUtc { get; set; }

        public DateTime? EndDateUtc { get; set; }

        public TimeSpan? StartTime { get; set; } = TimeSpan.Zero;

        public TimeSpan? EndTime { get; set; } = new(23, 59, 0);

        public JobBlackoutPolicy Policy { get; set; } = JobBlackoutPolicy.Skip;

        public bool Enabled { get; set; } = true;
    }

    private CalendarEditModel? _edit;
    private Guid? _hydratedId;
    private Guid _hydratedScheduleId;
    private HashSet<Guid> _loadedWindowIds = [];

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        if (Calendar == null) {
            _edit = null;
            _hydratedId = null;
            _hydratedScheduleId = default;
            _loadedWindowIds = [];
            return;
        }

        if (_hydratedId == Calendar.Id && _hydratedScheduleId == ScheduleId)
            return;

        _hydratedId = Calendar.Id;
        _hydratedScheduleId = ScheduleId;
        _loadedWindowIds = (Calendar.BlackoutWindows ?? []).Select(w => w.Id).ToHashSet();
        _edit = new() {
            Id = Calendar.Id,
            Name = Calendar.Name,
            Description = Calendar.Description,
            Enabled = Calendar.Enabled,
            Windows = (Calendar.BlackoutWindows ?? []).Select(ToWindowEdit).ToList()
        };
    }

    private static WindowEditModel ToWindowEdit(JobBlackoutWindowRes w)
        => new() {
            Id = w.Id,
            IsNew = false,
            Name = w.Name,
            UseDateRange = w.StartDateUtc.HasValue,
            Days = FlagEnumUi.SelectedAtomic(w.DayFlags),
            StartDateUtc = w.StartDateUtc,
            EndDateUtc = w.EndDateUtc,
            StartTime = w.StartTime.ToTimeSpan(),
            EndTime = w.EndTime.ToTimeSpan(),
            Policy = w.Policy,
            Enabled = w.Enabled
        };

    private async Task AddCalendar()
    {
        try {
            var created = await ApiClient.PostAsAsync<JobBlackoutCalendarReq, CreateResult<JobBlackoutCalendarRes>>(
                CalendarRoute, new() { Name = "Blackout", Enabled = true });
            var id = created?.Data?.Id;
            if (id is null || id == Guid.Empty)
                throw new InvalidOperationException("Calendar create did not return an id.");

            var patch = new PatchRequestBuilder().WithKey(ScheduleId).SetProperty("JobBlackoutCalendarId", id.Value).Build();
            await ApiClient.PatchAsAsync<PatchRequest, PatchResult<object>>(ScheduleRoute, patch);
            Snackbar.Add("Blackout calendar added", Severity.Success);
            await OnChanged.InvokeAsync();
        }
        catch (Exception ex) {
            Snackbar.Add($"Add calendar failed: {ex.Message}", Severity.Error);
        }
    }

    private async Task RemoveCalendar()
    {
        if (_edit == null)
            return;

        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Remove blackout calendar", "Unlink this calendar from the schedule? If no other schedule uses it, the calendar is deleted.", "Remove",
            cancelText: "Cancel");
        if (confirmed != true)
            return;

        var calendarId = _edit.Id;
        try {
            var patch = new PatchRequestBuilder().WithKey(ScheduleId).SetProperty("JobBlackoutCalendarId", (Guid?)null).Build();
            await ApiClient.PatchAsAsync<PatchRequest, PatchResult<object>>(ScheduleRoute, patch);
            try {
                await ApiClient.DeleteAsAsync<object>($"{CalendarRoute.TrimEnd('/')}/{calendarId}");
            }
            catch (Exception ex) {
                Snackbar.Add($"Calendar unlinked (still referenced elsewhere): {ex.Message}", Severity.Warning);
                await OnChanged.InvokeAsync();
                return;
            }

            Snackbar.Add("Blackout calendar removed", Severity.Success);
            await OnChanged.InvokeAsync();
        }
        catch (Exception ex) {
            Snackbar.Add($"Remove calendar failed: {ex.Message}", Severity.Error);
        }
    }

    private void AddWindow()
    {
        _edit?.Windows.Add(new() { IsNew = true, Name = $"Window{(_edit.Windows.Count + 1)}" });
    }

    private void RemoveWindow(WindowEditModel window) => _edit?.Windows.Remove(window);

    private JobBlackoutWindowReq ToWindowReq(WindowEditModel window)
    {
        var days = window.UseDateRange ? DayFlags.None : (window.Days ?? []).Aggregate(DayFlags.None, (acc, d) => acc | d);
        return new() {
            JobBlackoutCalendarId = _edit!.Id,
            Name = string.IsNullOrWhiteSpace(window.Name) ? "Window" : window.Name,
            DayFlags = days,
            StartDateUtc = window.UseDateRange && window.StartDateUtc is { } sd ? DateTime.SpecifyKind(sd, DateTimeKind.Utc) : null,
            EndDateUtc = window.UseDateRange && window.EndDateUtc is { } ed ? DateTime.SpecifyKind(ed, DateTimeKind.Utc) : null,
            StartTime = TimeOnly.FromTimeSpan(window.StartTime ?? TimeSpan.Zero),
            EndTime = TimeOnly.FromTimeSpan(window.EndTime ?? new TimeSpan(23, 59, 0)),
            Policy = window.Policy,
            Enabled = window.Enabled
        };
    }

    private async Task SaveBlackout()
    {
        if (_edit == null)
            return;

        try {
            var calendarReq = new JobBlackoutCalendarReq { Name = string.IsNullOrWhiteSpace(_edit.Name) ? "Blackout" : _edit.Name, Description = _edit.Description, Enabled = _edit.Enabled };
            await ApiClient.PostAsAsync<UpdateRequest<JobBlackoutCalendarReq>, UpdateResult<JobBlackoutCalendarRes>>(
                $"{CalendarRoute.TrimEnd('/')}/Update", new(calendarReq, _edit.Id));

            var currentIds = _edit.Windows.Where(w => w.Id.HasValue).Select(w => w.Id!.Value).ToHashSet();
            foreach (var removedId in _loadedWindowIds.Where(id => !currentIds.Contains(id)))
                await ApiClient.DeleteAsAsync<object>($"{WindowRoute.TrimEnd('/')}/{removedId}");

            foreach (var window in _edit.Windows.Where(w => w.IsNew)) {
                var created = await ApiClient.PostAsAsync<JobBlackoutWindowReq, CreateResult<JobBlackoutWindowRes>>(WindowRoute, ToWindowReq(window));
                if (created?.Data is { } data) {
                    window.Id = data.Id;
                    window.IsNew = false;
                }
            }

            foreach (var window in _edit.Windows.Where(w => !w.IsNew && w.Id.HasValue)) {
                await ApiClient.PostAsAsync<UpdateRequest<JobBlackoutWindowReq>, UpdateResult<JobBlackoutWindowRes>>(
                    $"{WindowRoute.TrimEnd('/')}/Update", new(ToWindowReq(window), window.Id!.Value));
            }

            Snackbar.Add("Blackout saved", Severity.Success);
            await OnChanged.InvokeAsync();
        }
        catch (Exception ex) {
            Snackbar.Add($"Save blackout failed: {ex.Message}", Severity.Error);
        }
    }
}
