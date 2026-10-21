using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Identifiers;
using Lyo.DateAndTime;
using Lyo.Schedule.Models;
using Lyo.Scheduler;
using Lyo.Scheduler.Models;
using Lyo.Web.Primitives.CheckSelect;
using MudBlazor;

namespace Lyo.Schedule.Web.Components;

public partial class ScheduleWorkbench
{
    private static readonly IReadOnlyList<LyoSelectOption<DayFlags>> DayItems = [
        new(DayFlags.Sun, "Sunday"), new(DayFlags.Mon, "Monday"), new(DayFlags.Tue, "Tuesday"), new(DayFlags.Wed, "Wednesday"), new(DayFlags.Thu, "Thursday"),
        new(DayFlags.Fri, "Friday"), new(DayFlags.Sat, "Saturday")
    ];

    private static readonly IReadOnlyList<LyoSelectOption<MonthFlags>> MonthItems = [
        new(MonthFlags.Jan, "January"), new(MonthFlags.Feb, "February"), new(MonthFlags.Mar, "March"), new(MonthFlags.Apr, "April"), new(MonthFlags.May, "May"),
        new(MonthFlags.Jun, "June"), new(MonthFlags.Jul, "July"), new(MonthFlags.Aug, "August"), new(MonthFlags.Sep, "September"), new(MonthFlags.Oct, "October"),
        new(MonthFlags.Nov, "November"), new(MonthFlags.Dec, "December")
    ];

    private static readonly Dictionary<string, int> CalendarHues = new(StringComparer.Ordinal) {
        ["SetTimes"] = 228,
        ["Interval"] = 174,
        ["IntervalWindow"] = 174,
        ["OneShot"] = 252,
        ["Cron"] = 292
    };

    private readonly List<string> _addedIds = [];

    private readonly string _timePattern = @"^([01]?\d|2[0-3]):[0-5]\d$";

    private bool _busy;
    private TimeZoneInfo _calendarZone = TimeZoneInfo.Utc;

    // Cron expression editor
    private string _cronExpression = "0 9 * * MON-FRI";
    private string _description = string.Empty;
    private bool _enabled = true;
    private TimeSpan? _intervalEndTime = new TimeSpan(17, 0, 0);
    private int _intervalMinutes = 60;

    // Interval window
    private TimeSpan? _intervalStartTime = new TimeSpan(9, 0, 0);

    private List<ScheduleWithNextRun> _liveSchedules = [];

    // OneShot date/time
    private DateTime? _oneShotDate = DateTime.UtcNow.Date.AddDays(1);
    private TimeSpan? _oneShotTime = new TimeSpan(9, 0, 0);
    private bool _previewRequested;
    private List<ScheduleRun> _previewRuns = [];

    // Day and month flags (SetTimes + Interval) stored as individual selections
    private IEnumerable<DayFlags> _selectedDays = [DayFlags.Mon, DayFlags.Tue, DayFlags.Wed, DayFlags.Thu, DayFlags.Fri];

    private IEnumerable<MonthFlags> _selectedMonths = [
        MonthFlags.Jan, MonthFlags.Feb, MonthFlags.Mar, MonthFlags.Apr, MonthFlags.May, MonthFlags.Jun, MonthFlags.Jul, MonthFlags.Aug, MonthFlags.Sep, MonthFlags.Oct,
        MonthFlags.Nov, MonthFlags.Dec
    ];

    // SetTimes clock list
    private IEnumerable<string> _timesList = ["09:00", "17:00"];
    private ScheduleType _type = ScheduleType.Cron;
    private List<ScheduleRun> _upcomingRuns = [];
    private bool _use24Hour = true;

    private string? _validationError;
    private bool _validationPassed;
    private DateOnlyModel _visibleMonth = new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

    protected override TimeSpan StatusAutoClearDelay => TimeSpan.FromMilliseconds(3000);

    private Calendar? BuildPreviewCalendar()
    {
        var definition = TryBuildDefinition(out var _);
        if (definition is null)
            return null;

        try {
            definition.Validate();
            return ScheduleCalendar.From(definition, _visibleMonth.Year, _visibleMonth.Month, _calendarZone);
        }
        catch (Exception) {
            return null;
        }
    }

    private Task OnVisibleMonthChanged(DateOnlyModel month)
    {
        _visibleMonth = new(month.Year, month.Month, 1);
        return Task.CompletedTask;
    }

    private Task OnCalendarZoneChanged(TimeZoneInfo zone)
    {
        _calendarZone = zone;
        return Task.CompletedTask;
    }

    private Task OnHourCycleChanged(bool use24Hour)
    {
        _use24Hour = use24Hour;
        return Task.CompletedTask;
    }

    protected override void OnInitialized() => RefreshLive();

    private Task OnTimesChanged(IEnumerable<string> values)
    {
        _timesList = values;
        return Task.CompletedTask;
    }

    private ScheduleDefinition? TryBuildDefinition(out string? error)
    {
        error = null;
        try {
            var builder = ScheduleDefinition.Create().WithDescription(string.IsNullOrWhiteSpace(_description) ? null! : _description).Enabled(_enabled);
            switch (_type) {
                case ScheduleType.Cron:
                    builder.SetCron(_cronExpression.Trim());
                    break;
                case ScheduleType.SetTimes:
                    var timeValues = _timesList.ToList();
                    if (timeValues.Count == 0) {
                        error = "Add at least one time.";
                        return null;
                    }

                    builder.SetTimes(timeValues.Select(TimeOnly.Parse).ToArray())
                        .SetDays(_selectedDays.Aggregate(DayFlags.None, (acc, f) => acc | f))
                        .SetMonths(_selectedMonths.Aggregate(MonthFlags.None, (acc, f) => acc | f));

                    break;
                case ScheduleType.Interval:
                    if (!_intervalStartTime.HasValue || !_intervalEndTime.HasValue) {
                        error = "Select start and end times.";
                        return null;
                    }

                    builder.SetInterval(TimeOnly.FromTimeSpan(_intervalStartTime.Value), TimeOnly.FromTimeSpan(_intervalEndTime.Value), _intervalMinutes)
                        .SetDays(_selectedDays.Aggregate(DayFlags.None, (acc, f) => acc | f))
                        .SetMonths(_selectedMonths.Aggregate(MonthFlags.None, (acc, f) => acc | f));

                    break;
                case ScheduleType.OneShot:
                    if (!_oneShotDate.HasValue || !_oneShotTime.HasValue) {
                        error = "Select a date and time.";
                        return null;
                    }

                    builder.SetExecuteAt(DateTime.SpecifyKind(_oneShotDate.Value.Date + _oneShotTime.Value, DateTimeKind.Utc));
                    break;
                default:
                    error = $"Unknown schedule type: {_type}.";
                    return null;
            }

            return builder.Build();
        }
        catch (Exception ex) {
            error = ex.Message;
            return null;
        }
    }

    private void ValidateSchedule()
    {
        _validationError = null;
        _validationPassed = false;
        _previewRuns = [];
        _previewRequested = false;
        var def = TryBuildDefinition(out var buildError);
        if (def == null) {
            _validationError = buildError ?? "Failed to build definition.";
            return;
        }

        try {
            def.Validate();
            _validationPassed = true;
            SetStatus("Schedule is valid.", Severity.Success);
        }
        catch (Exception ex) {
            _validationError = ex.Message;
        }
    }

    private async Task PreviewNextRunsAsync()
    {
        _busy = true;
        _previewRequested = true;
        _previewRuns = [];
        _validationError = null;
        _validationPassed = false;
        try {
            var def = TryBuildDefinition(out var buildError);
            if (def == null) {
                _validationError = buildError ?? "Failed to build definition.";
                return;
            }

            try {
                def.Validate();
            }
            catch (Exception ex) {
                _validationError = ex.Message;
                return;
            }

            var previewId = $"workbench-preview-{LyoGuid.CreateV7():N}";
            SchedulerService.AddSchedule(previewId, "Preview", def, _ => Task.CompletedTask);
            try {
                _previewRuns = SchedulerService.GetUpcomingRuns(maxRuns: 200).Where(r => r.Schedule.Id == previewId).Take(15).ToList();
            }
            finally {
                SchedulerService.RemoveSchedule(previewId);
            }

            _validationPassed = true;
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }

        await Task.CompletedTask;
    }

    private async Task AddTestScheduleAsync()
    {
        _busy = true;
        try {
            var def = TryBuildDefinition(out var buildError);
            if (def == null) {
                SetStatus(buildError ?? "Failed to build definition.", Severity.Warning);
                return;
            }

            try {
                def.Validate();
            }
            catch (Exception ex) {
                SetStatus($"Validation failed: {ex.Message}", Severity.Warning);
                return;
            }

            var rawId = LyoGuid.CreateV7().ToString("N");
            var id = $"workbench-test-{rawId[..8]}";
            var name = string.IsNullOrWhiteSpace(_description) ? $"Test – {_type}" : _description;
            SchedulerService.AddSchedule(id, name, def, _ => Task.CompletedTask);
            _addedIds.Add(id);
            RefreshLive();
            SetStatus($"Added test schedule '{name}'.", Severity.Success);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }

        await Task.CompletedTask;
    }

    private void RemoveSchedule(string id)
    {
        SchedulerService.RemoveSchedule(id);
        _addedIds.Remove(id);
        RefreshLive();
        SetStatus($"Removed schedule {id}.", Severity.Info);
    }

    private void RemoveAllTestSchedules()
    {
        foreach (var id in _addedIds.ToList())
            SchedulerService.RemoveSchedule(id);

        _addedIds.Clear();
        RefreshLive();
        SetStatus("All test schedules removed.", Severity.Info);
    }

    private void RefreshLive()
    {
        _liveSchedules = SchedulerService.GetSchedulesOrderedByNextRun().ToList();
        _upcomingRuns = SchedulerService.GetUpcomingRuns(maxRuns: 20).ToList();
    }

    private async Task StartSchedulerAsync()
    {
        _busy = true;
        try {
            await SchedulerService.StartAsync();
            RefreshLive();
            SetStatus("Scheduler started.", Severity.Success);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task StopSchedulerAsync()
    {
        _busy = true;
        try {
            await SchedulerService.StopAsync();
            RefreshLive();
            SetStatus("Scheduler stopped.", Severity.Info);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }
}