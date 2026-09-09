namespace Lyo.Job.Web.Components;

/// <summary>
/// Inline editor for a schedule's blackout calendar and windows. Creates or unlinks a calendar through <c>Job/BlackoutCalendar</c> and persists windows through
/// <c>Job/BlackoutCalendar/Window</c>.
/// </summary>
public partial class JobBlackoutCalendarEditor
{
    private static readonly IReadOnlyList<HolidayInfo> KnownHolidays = [.. HolidayInfo.All.Where(h => !ReferenceEquals(h, HolidayInfo.Unknown))];

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    /// <summary>Calendar currently linked to the selected schedule, or null when none is attached.</summary>
    [Parameter]
    public JobBlackoutCalendarRes? Calendar { get; set; }

    /// <summary>Schedule whose <c>JobBlackoutCalendarId</c> is patched when a calendar is added or removed.</summary>
    [Parameter]
    public Guid ScheduleId { get; set; }

    /// <summary>API client used for calendar, window, and schedule PATCH calls.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>CRUD route for schedules (used when PATCHing the calendar FK).</summary>
    [Parameter]
    [EditorRequired]
    public string ScheduleRoute { get; set; } = "";

    /// <summary>CRUD route for blackout calendars (for example <c>Job/BlackoutCalendar</c>).</summary>
    [Parameter]
    [EditorRequired]
    public string CalendarRoute { get; set; } = "";

    /// <summary>CRUD route for blackout windows (for example <c>Job/BlackoutCalendar/Window</c>).</summary>
    [Parameter]
    [EditorRequired]
    public string WindowRoute { get; set; } = "";

    /// <summary>Fired after a calendar is created, unlinked, or saved so the parent can reload the schedule.</summary>
    [Parameter]
    public EventCallback OnChanged { get; set; }

    private enum WindowKind
    {
        Weekdays,
        CalendarDays,
        DateRange,
        Holiday
    }

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

        public WindowKind Kind { get; set; } = WindowKind.Weekdays;

        public IReadOnlyCollection<DayFlags> Days { get; set; } = FlagEnumUi.SelectedAtomic(DayFlags.EveryDay);

        public IReadOnlyCollection<MonthFlags> Months { get; set; } = FlagEnumUi.SelectedAtomic(MonthFlags.EveryMonth);

        public IReadOnlyCollection<int> DaysOfMonth { get; set; } = [];

        public bool Repeat { get; set; } = true;

        public DateTime? StartDateUtc { get; set; }

        public DateTime? EndDateUtc { get; set; }

        public TimeSpan? StartTime { get; set; } = TimeSpan.Zero;

        public TimeSpan? EndTime { get; set; } = new(23, 59, 0);

        public JobBlackoutPolicy Policy { get; set; } = JobBlackoutPolicy.Skip;

        public bool Enabled { get; set; } = true;

        public string? HolidaySlug { get; set; }

        public bool IncludeObservedDate { get; set; }

        public bool MonthsMatch(MonthFlags preset) => FlagEnumUi.MatchesPreset(Months, preset);

        public void ApplyMonths(MonthFlags preset) => Months = FlagEnumUi.SelectedAtomic(preset);

        public void SetHolidaySlug(string? slug)
        {
            HolidaySlug = slug;
            if (string.IsNullOrWhiteSpace(Name) || Name.StartsWith("Window", StringComparison.Ordinal)) {
                var holiday = HolidayInfo.FromSlug(slug);
                if (!ReferenceEquals(holiday, HolidayInfo.Unknown))
                    Name = holiday.Name;
            }
        }
    }

    private CalendarEditModel? _edit;
    private Guid? _hydratedId;
    private Guid _hydratedScheduleId;
    private HashSet<Guid> _loadedWindowIds = [];
    private bool _expanded = true;

    private IReadOnlyCollection<string> SelectedHolidaySlugs =>
        _edit == null
            ? []
            : [.. _edit.Windows.Where(w => w.Kind == WindowKind.Holiday && !string.IsNullOrWhiteSpace(w.HolidaySlug)).Select(w => w.HolidaySlug!)];

    private static string KindLabel(WindowKind kind) => kind switch {
        WindowKind.CalendarDays => "Calendar days",
        WindowKind.DateRange => "Date range",
        _ => kind.ToString()
    };

    private static string HolidaySelectSummary(IReadOnlyList<string?>? selected)
    {
        var slugs = (selected ?? []).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).ToList();
        if (slugs.Count == 0)
            return "None";
        if (slugs.Count == 1) {
            var holiday = HolidayInfo.FromSlug(slugs[0]);
            return ReferenceEquals(holiday, HolidayInfo.Unknown) ? slugs[0] : holiday.Name;
        }

        return $"{slugs.Count} holidays";
    }

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

    private static WindowKind InferKind(JobBlackoutWindowRes w)
    {
        if (!string.IsNullOrWhiteSpace(w.HolidaySlug))
            return WindowKind.Holiday;

        if (w.DaysOfMonth is { Count: > 0 })
            return WindowKind.CalendarDays;

        return w.StartDateUtc.HasValue ? WindowKind.DateRange : WindowKind.Weekdays;
    }

    private static WindowEditModel ToWindowEdit(JobBlackoutWindowRes w)
    {
        var kind = InferKind(w);
        return new() {
            Id = w.Id,
            IsNew = false,
            Name = w.Name,
            Kind = kind,
            Days = FlagEnumUi.SelectedAtomic(w.DayFlags),
            Months = FlagEnumUi.SelectedAtomic(w.MonthFlags ?? MonthFlags.EveryMonth),
            DaysOfMonth = w.DaysOfMonth ?? [],
            Repeat = kind != WindowKind.CalendarDays || !w.StartDateUtc.HasValue,
            StartDateUtc = w.StartDateUtc,
            EndDateUtc = w.EndDateUtc,
            StartTime = w.StartTime.ToTimeSpan(),
            EndTime = w.EndTime.ToTimeSpan(),
            Policy = w.Policy,
            Enabled = w.Enabled,
            HolidaySlug = w.HolidaySlug,
            IncludeObservedDate = w.IncludeObservedDate
        };
    }

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

        if (!await DialogService.ConfirmAsync(
                "Remove blackout calendar", "Unlink this calendar from the schedule? If no other schedule uses it, the calendar is deleted.", "Remove"))
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

    private void OnSelectedHolidaysChanged(IReadOnlyCollection<string>? slugs)
    {
        if (_edit == null)
            return;

        var wanted = (slugs ?? []).Where(s => !string.IsNullOrWhiteSpace(s)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _edit.Windows.RemoveAll(w => w.Kind == WindowKind.Holiday && !string.IsNullOrWhiteSpace(w.HolidaySlug) && !wanted.Contains(w.HolidaySlug));

        var existing = _edit.Windows
            .Where(w => w.Kind == WindowKind.Holiday && !string.IsNullOrWhiteSpace(w.HolidaySlug))
            .Select(w => w.HolidaySlug!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var holiday in KnownHolidays) {
            if (!wanted.Contains(holiday.Slug) || existing.Contains(holiday.Slug))
                continue;

            _edit.Windows.Add(
                new() {
                    IsNew = true,
                    Name = holiday.Name,
                    Kind = WindowKind.Holiday,
                    HolidaySlug = holiday.Slug
                });
        }
    }

    private void RemoveWindow(WindowEditModel window) => _edit?.Windows.Remove(window);

    private JobBlackoutWindowReq ToWindowReq(WindowEditModel window)
    {
        var req = new JobBlackoutWindowReq {
            JobBlackoutCalendarId = _edit!.Id,
            Name = string.IsNullOrWhiteSpace(window.Name) ? "Window" : window.Name,
            StartTime = TimeOnly.FromTimeSpan(window.StartTime ?? TimeSpan.Zero),
            EndTime = TimeOnly.FromTimeSpan(window.EndTime ?? new TimeSpan(23, 59, 0)),
            Policy = window.Policy,
            Enabled = window.Enabled
        };

        switch (window.Kind) {
            case WindowKind.Holiday:
                req.HolidaySlug = window.HolidaySlug;
                req.IncludeObservedDate = window.IncludeObservedDate;
                req.DayFlags = DayFlags.None;
                break;
            case WindowKind.CalendarDays:
                req.DayFlags = DayFlags.None;
                req.MonthFlags = (window.Months ?? []).Aggregate(MonthFlags.None, (acc, m) => acc | m);
                req.DaysOfMonth = [.. window.DaysOfMonth ?? []];
                if (!window.Repeat) {
                    req.StartDateUtc = window.StartDateUtc is { } sd ? DateTime.SpecifyKind(sd, DateTimeKind.Utc) : null;
                    req.EndDateUtc = window.EndDateUtc is { } ed ? DateTime.SpecifyKind(ed, DateTimeKind.Utc) : null;
                }

                break;
            case WindowKind.DateRange:
                req.DayFlags = DayFlags.None;
                req.StartDateUtc = window.StartDateUtc is { } start ? DateTime.SpecifyKind(start, DateTimeKind.Utc) : null;
                req.EndDateUtc = window.EndDateUtc is { } end ? DateTime.SpecifyKind(end, DateTimeKind.Utc) : null;
                break;
            default:
                req.DayFlags = (window.Days ?? []).Aggregate(DayFlags.None, (acc, d) => acc | d);
                break;
        }

        return req;
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
